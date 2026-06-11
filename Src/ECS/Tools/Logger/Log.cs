using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 兼容旧调用点的日志等级。新代码应优先使用 <see cref="LogSeverity"/>。
/// </summary>
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Success = 3,
    Warning = 4,
    Error = 5,
    None = 99
}

/// <summary>
/// 只表达运行健康度的严重程度，不承载成功/失败结果。
/// </summary>
public enum LogSeverity
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4,
    Fatal = 5,
    None = 99
}

/// <summary>
/// 表达操作或流程结果，避免把 Success 当 severity。
/// </summary>
public enum LogOutcome
{
    None,
    Started,
    Completed,
    Succeeded,
    Failed,
    Skipped,
    Suppressed
}

/// <summary>
/// 仅用于 Validation/Test channel 的断言状态。
/// </summary>
public enum LogValidationStatus
{
    None,
    Pass,
    Fail,
    Skip,
    ExpectedFailure
}

public enum LogChannel
{
    Runtime,
    Validation,
    Flow,
    Diagnostics
}

public sealed class LogFields : Dictionary<string, object?>
{
    public LogFields()
    {
    }

    public LogFields(IDictionary<string, object?> values) : base(values)
    {
    }
}

public sealed class LogSinkOptions
{
    public bool? StdoutSummary { get; set; }
    public bool? JsonlFile { get; set; }
    public bool? Memory { get; set; }
    public bool? Artifact { get; set; }
    public bool? GodotEditor { get; set; }
}

public sealed class LogBudgetOptions
{
    public bool Enabled { get; set; }
    public int DefaultPerSecond { get; set; }
}

public sealed class LogRule
{
    public string? Owner { get; set; }
    public string? Context { get; set; }
    public string? Operation { get; set; }
    public string? BudgetKey { get; set; }
    public int? BudgetPerSecond { get; set; }
    public LogSeverity? MinimumSeverity { get; set; }

    public bool Matches(LogEntry entry)
    {
        return Matches(Owner, entry.Owner)
            && Matches(Context, entry.Context)
            && Matches(Operation, entry.Operation)
            && MatchesBudget(entry);
    }

    private bool MatchesBudget(LogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(BudgetKey))
        {
            return true;
        }

        return string.Equals(BudgetKey, LogBudgetRuntime.GetBudgetKey(entry), StringComparison.OrdinalIgnoreCase);
    }

    private static bool Matches(string? expected, string actual)
    {
        return string.IsNullOrWhiteSpace(expected)
            || string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class LogProfileDocument
{
    public string Profile { get; set; } = "ai-default";
    public LogSeverity? DefaultSeverity { get; set; }
    public LogSeverity? MinimumSeverity { get; set; }
    public LogSinkOptions Sinks { get; set; } = new();
    public LogBudgetOptions? Budget { get; set; }
    public List<LogRule> Rules { get; set; } = new();
}

public sealed class LogRulesDocument
{
    public List<LogRule> Rules { get; set; } = new();
}

public sealed class LogOverridesDocument
{
    public string? Profile { get; set; }
    public string? Reason { get; set; }
    public string? Expires { get; set; }
    public LogSeverity? DefaultSeverity { get; set; }
    public LogSeverity? MinimumSeverity { get; set; }
    public LogSinkOptions Sinks { get; set; } = new();
    public LogBudgetOptions? Budget { get; set; }
    public List<LogRule> Rules { get; set; } = new();
}

public sealed class LogOptions
{
    public string ProfileName { get; set; } = "default";
    public string ConfigDirectory { get; set; } = Path.Combine("Config", "Log");
    public string? ProfilePath { get; set; }
    public string? RulesPath { get; set; }
    public string? OverridesPath { get; set; }
    public string RunDirectory { get; set; } = Path.Combine(".ai-temp", "log-runs", "current");
    public LogSeverity MinimumSeverity { get; set; } = LogSeverity.Debug;
    public bool EnableStdoutSummary { get; set; } = true;
    public bool EnableJsonlFile { get; set; } = true;
    public bool EnableMemorySink { get; set; } = true;
    public bool EnableArtifactSink { get; set; } = true;
    public bool EnableGodotEditorSink { get; set; }
    public bool EnableGodotFrameCounters { get; set; } = true;
    public LogBudgetOptions Budget { get; set; } = new();
    public List<LogRule> Rules { get; set; } = new();
    public List<string> ConfigWarnings { get; set; } = new();
    public TextWriter? Stdout { get; set; }
}

public sealed class LogEntry
{
    public string TimestampUtc { get; set; } = DateTimeOffset.UtcNow.ToString("O");
    public long RunElapsedMs { get; set; }
    public ulong Frame { get; set; }
    public ulong PhysicsFrame { get; set; }
    public LogSeverity Severity { get; set; } = LogSeverity.Info;
    public LogOutcome Outcome { get; set; } = LogOutcome.None;
    public LogValidationStatus ValidationStatus { get; set; } = LogValidationStatus.None;
    public LogChannel Channel { get; set; } = LogChannel.Runtime;
    public string Owner { get; set; } = "Unknown";
    public string Context { get; set; } = "Unknown";
    public string Operation { get; set; } = "Log";
    public string Phase { get; set; } = "Runtime";
    public string Message { get; set; } = string.Empty;
    public string? EventId { get; set; }
    public string? CorrelationId { get; set; }
    public string? SourceFile { get; set; }
    public int? SourceLine { get; set; }
    public Dictionary<string, object?> Fields { get; set; } = new();
}

public sealed class OperationTrace : IDisposable
{
    private readonly string _owner;
    private readonly string _context;
    private readonly string _operation;
    private readonly string _phase;
    private readonly string _correlationId;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly List<string> _steps = new();
    private bool _completed;
    private readonly string? _sourceFile;
    private readonly int? _sourceLine;

    internal OperationTrace(string owner, string context, string operation, string phase, string? correlationId, string? sourceFile, int? sourceLine)
    {
        _owner = owner;
        _context = context;
        _operation = operation;
        _phase = phase;
        _correlationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId;
        _sourceFile = sourceFile;
        _sourceLine = sourceLine;
        Log.Emit(new LogEntry
        {
            Severity = LogSeverity.Debug,
            Outcome = LogOutcome.Started,
            Channel = LogChannel.Flow,
            Owner = _owner,
            Context = _context,
            Operation = _operation,
            Phase = _phase,
            CorrelationId = _correlationId,
            SourceFile = _sourceFile,
            SourceLine = _sourceLine,
            Message = "flow started",
            Fields = new Dictionary<string, object?> { ["entryType"] = "flow_start" }
        });
    }

    public void Step(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _steps.Add(message);
        Log.Emit(new LogEntry
        {
            Severity = LogSeverity.Trace,
            Outcome = LogOutcome.None,
            Channel = LogChannel.Flow,
            Owner = _owner,
            Context = _context,
            Operation = _operation,
            Phase = _phase,
            CorrelationId = _correlationId,
            SourceFile = _sourceFile,
            SourceLine = _sourceLine,
            Message = message,
            Fields = new Dictionary<string, object?>
            {
                ["entryType"] = "flow_step",
                ["stepIndex"] = _steps.Count,
                ["durationMs"] = _stopwatch.ElapsedMilliseconds
            }
        });
    }

    public void Complete(LogOutcome outcome = LogOutcome.Completed, string? message = null, LogFields? fields = null)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        var values = fields is null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(fields);
        values["entryType"] = "flow_summary";
        values["durationMs"] = _stopwatch.ElapsedMilliseconds;
        values["stepCount"] = _steps.Count;

        Log.Emit(new LogEntry
        {
            Severity = outcome == LogOutcome.Failed ? LogSeverity.Error : LogSeverity.Info,
            Outcome = outcome,
            Channel = LogChannel.Flow,
            Owner = _owner,
            Context = _context,
            Operation = _operation,
            Phase = _phase,
            CorrelationId = _correlationId,
            SourceFile = _sourceFile,
            SourceLine = _sourceLine,
            Message = message ?? $"[FLOW:{_operation}] {outcome}",
            Fields = values
        });
    }

    public void Dispose()
    {
        Complete(LogOutcome.Completed);
    }
}

internal interface ILogSink : IDisposable
{
    void Write(LogEntry entry);
    void Flush();
}

internal sealed class LogBudgetDecision
{
    public bool ShouldWrite { get; set; } = true;
    public LogEntry? Summary { get; set; }
}

internal sealed class LogBudgetWindow
{
    public long WindowStartMs { get; set; }
    public int WrittenCount { get; set; }
    public int SuppressedCount { get; set; }
    public int ReportedSuppressedCount { get; set; }
    public int BudgetPerSecond { get; set; }
    public string Owner { get; set; } = "Unknown";
    public string Context { get; set; } = "Unknown";
    public string Operation { get; set; } = "Log";
    public string BudgetKey { get; set; } = string.Empty;
}

internal static class LogBudgetRuntime
{
    private const long WindowMs = 1000;
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, LogBudgetWindow> Windows = new();

    public static void Reset()
    {
        lock (SyncRoot)
        {
            Windows.Clear();
        }
    }

    public static string GetBudgetKey(LogEntry entry)
    {
        if (TryGetField(entry, "budgetKey", out var budgetKey) && !string.IsNullOrWhiteSpace(budgetKey))
        {
            return budgetKey!;
        }

        var reason = TryGetField(entry, "reasonCode", out var reasonCode) ? reasonCode : null;
        if (string.IsNullOrWhiteSpace(reason) && TryGetField(entry, "reason", out var reasonValue))
        {
            reason = reasonValue;
        }

        var entityId = TryGetField(entry, "entityId", out var entityValue) ? entityValue : null;
        return string.Join(
            "|",
            entry.Channel,
            entry.Owner,
            entry.Context,
            entry.Operation,
            string.IsNullOrWhiteSpace(reason) ? "none" : reason,
            string.IsNullOrWhiteSpace(entityId) ? "none" : entityId);
    }

    public static LogBudgetDecision Check(LogEntry entry, LogOptions options)
    {
        if (!ShouldApplyBudget(entry, options, out var budgetPerSecond))
        {
            return new LogBudgetDecision();
        }

        var budgetKey = GetBudgetKey(entry);
        var windowStart = entry.RunElapsedMs - (entry.RunElapsedMs % WindowMs);

        lock (SyncRoot)
        {
            if (!Windows.TryGetValue(budgetKey, out var window))
            {
                window = CreateWindow(entry, budgetKey, windowStart, budgetPerSecond);
                Windows[budgetKey] = window;
            }

            LogEntry? rolloverSummary = null;
            if (window.WindowStartMs != windowStart)
            {
                rolloverSummary = CreateSummary(window);
                window.WindowStartMs = windowStart;
                window.WrittenCount = 0;
                window.SuppressedCount = 0;
                window.ReportedSuppressedCount = 0;
                window.BudgetPerSecond = budgetPerSecond;
                window.Owner = entry.Owner;
                window.Context = entry.Context;
                window.Operation = entry.Operation;
            }

            if (window.WrittenCount < budgetPerSecond)
            {
                window.WrittenCount += 1;
                return new LogBudgetDecision { ShouldWrite = true, Summary = rolloverSummary };
            }

            window.SuppressedCount += 1;
            if (window.ReportedSuppressedCount == 0)
            {
                var summary = CreateSummary(window, "budget_exceeded_first");
                window.ReportedSuppressedCount = window.SuppressedCount;
                return new LogBudgetDecision
                {
                    ShouldWrite = false,
                    Summary = summary
                };
            }

            return new LogBudgetDecision { ShouldWrite = false, Summary = rolloverSummary };
        }
    }

    public static List<LogEntry> DrainSummaries()
    {
        lock (SyncRoot)
        {
            var summaries = new List<LogEntry>();
            foreach (var window in Windows.Values)
            {
                var summary = CreateSummary(window);
                if (summary is not null)
                {
                    summaries.Add(summary);
                    window.ReportedSuppressedCount = window.SuppressedCount;
                }
            }

            return summaries;
        }
    }

    private static bool ShouldApplyBudget(LogEntry entry, LogOptions options, out int budgetPerSecond)
    {
        budgetPerSecond = 0;
        if (!options.Budget.Enabled)
        {
            return false;
        }

        if (entry.Channel == LogChannel.Validation
            || entry.Severity >= LogSeverity.Error
            || entry.Outcome == LogOutcome.Failed
            || string.Equals(entry.Operation, "SuppressedSummary", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        budgetPerSecond = ResolveBudgetPerSecond(entry, options);
        return budgetPerSecond >= 0;
    }

    private static int ResolveBudgetPerSecond(LogEntry entry, LogOptions options)
    {
        for (var index = options.Rules.Count - 1; index >= 0; index -= 1)
        {
            var rule = options.Rules[index];
            if (rule.BudgetPerSecond.HasValue && rule.Matches(entry))
            {
                return Math.Max(0, rule.BudgetPerSecond.Value);
            }
        }

        return Math.Max(0, options.Budget.DefaultPerSecond);
    }

    private static LogBudgetWindow CreateWindow(LogEntry entry, string budgetKey, long windowStart, int budgetPerSecond)
    {
        return new LogBudgetWindow
        {
            WindowStartMs = windowStart,
            BudgetPerSecond = budgetPerSecond,
            Owner = entry.Owner,
            Context = entry.Context,
            Operation = entry.Operation,
            BudgetKey = budgetKey
        };
    }

    private static LogEntry? CreateSummary(LogBudgetWindow window, string reasonCode = "budget_exceeded")
    {
        if (window.SuppressedCount <= window.ReportedSuppressedCount)
        {
            return null;
        }

        return new LogEntry
        {
            Severity = LogSeverity.Warn,
            Outcome = LogOutcome.Suppressed,
            Channel = LogChannel.Diagnostics,
            Owner = window.Owner,
            Context = window.Context,
            Operation = "SuppressedSummary",
            Phase = "Diagnostics",
            Message = "[FLOW:SuppressedSummary] log budget suppressed repeated entries",
            Fields = new Dictionary<string, object?>
            {
                ["budgetKey"] = window.BudgetKey,
                ["budgetPerSecond"] = window.BudgetPerSecond,
                ["suppressedCount"] = window.SuppressedCount,
                ["reportedSuppressedCount"] = window.ReportedSuppressedCount,
                ["windowMs"] = WindowMs,
                ["reasonCode"] = reasonCode
            }
        };
    }

    private static bool TryGetField(LogEntry entry, string key, out string? value)
    {
        value = null;
        if (!entry.Fields.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        value = raw.ToString();
        return true;
    }
}

internal sealed class StdoutSummarySink : ILogSink
{
    private readonly TextWriter _writer;

    public StdoutSummarySink(TextWriter writer)
    {
        _writer = writer;
    }

    public void Write(LogEntry entry)
    {
        if (!ShouldWriteSummary(entry))
        {
            return;
        }

        var elapsed = entry.RunElapsedMs / 1000.0;
        var fields = entry.Fields.Count == 0
            ? string.Empty
            : " " + string.Join(" ", entry.Fields.Select(pair => $"{pair.Key}={pair.Value}"));
        _writer.WriteLine(
            $"t={elapsed:0.000}s f={entry.Frame} pf={entry.PhysicsFrame} [{entry.Severity.ToString().ToUpperInvariant()}][{entry.Owner}][{entry.Context}] operation={entry.Operation} outcome={entry.Outcome} validationStatus={entry.ValidationStatus} {entry.Message}{fields}");
    }

    public void Flush()
    {
        _writer.Flush();
    }

    public void Dispose()
    {
        Flush();
    }

    private static bool ShouldWriteSummary(LogEntry entry)
    {
        return entry.Severity >= LogSeverity.Warn
            || entry.Channel == LogChannel.Validation
            || (entry.Channel == LogChannel.Flow && entry.Outcome is LogOutcome.Completed or LogOutcome.Succeeded or LogOutcome.Failed);
    }
}

internal sealed class JsonlBufferedFileSink : ILogSink
{
    private readonly StreamWriter _writer;

    public JsonlBufferedFileSink(string runDirectory)
    {
        var rawDirectory = Path.Combine(runDirectory, "raw");
        Directory.CreateDirectory(rawDirectory);
        var path = Path.Combine(rawDirectory, "scene-log.jsonl");
        _writer = new StreamWriter(new FileStream(path, FileMode.Append, System.IO.FileAccess.Write, FileShare.Read), System.Text.Encoding.UTF8)
        {
            AutoFlush = false
        };
    }

    public void Write(LogEntry entry)
    {
        _writer.WriteLine(LogJson.Serialize(entry));
    }

    public void Flush()
    {
        _writer.Flush();
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}

internal sealed class MemorySink : ILogSink
{
    public List<LogEntry> Entries { get; } = new();

    public void Write(LogEntry entry)
    {
        Entries.Add(entry);
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}

internal sealed class ArtifactSink : ILogSink
{
    private readonly string _runDirectory;
    private readonly List<LogEntry> _validationEntries = new();

    public ArtifactSink(string runDirectory)
    {
        _runDirectory = runDirectory;
    }

    public void Write(LogEntry entry)
    {
        if (entry.Channel == LogChannel.Validation)
        {
            _validationEntries.Add(entry);
        }
    }

    public void Flush()
    {
        if (_validationEntries.Count == 0)
        {
            return;
        }

        var path = Path.Combine(_runDirectory, "artifacts", "validation-log.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, LogJson.Serialize(new
        {
            generatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            entries = _validationEntries
        }));
    }

    public void Dispose()
    {
        Flush();
    }
}

internal sealed class GodotEditorSink : ILogSink
{
    public void Write(LogEntry entry)
    {
        var message = $"[{entry.Severity}][{entry.Owner}][{entry.Context}] {entry.Message}";
        if (entry.Severity >= LogSeverity.Error)
        {
            GD.PushError(message);
            return;
        }

        if (entry.Severity == LogSeverity.Warn)
        {
            GD.PushWarning(message);
            return;
        }

        GD.PrintRich(message);
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}

internal static class LogJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T value, bool indented = false)
    {
        return JsonSerializer.Serialize(value, indented ? IndentedOptions : Options);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }
}

/// <summary>
/// AI-first 结构化日志入口。默认写 JSONL 和 stdout summary，Godot editor sink 默认关闭。
/// </summary>
public class Log
{
    private static readonly object SyncRoot = new();
    private static readonly Stopwatch RunStopwatch = Stopwatch.StartNew();
    private static readonly Dictionary<string, LogSeverity> ContextFilters = new();
    private static readonly List<ILogSink> Sinks = new();
    private static MemorySink? _memorySink;
    private static bool _isWritingBudgetSummary;
    private static LogOptions _options = CreateDefaultOptions();

    private readonly string _contextName;
    private readonly string _owner;
    private readonly string _operation;
    private readonly LogSeverity _localMinimumSeverity;

    public static LogLevel GlobalLevel
    {
        get => ToLegacyLevel(_options.MinimumSeverity);
        set => _options.MinimumSeverity = ToSeverity(value);
    }

    public static bool ShowTimestamp { get; set; }
    public static bool ShowContext { get; set; } = true;

    static Log()
    {
        Configure(CreateDefaultOptions());
    }

    public Log(string contextName, LogLevel localLevel = LogLevel.None)
        : this(contextName, owner: InferOwner(contextName), operation: contextName, minimumSeverity: ToSeverity(localLevel))
    {
    }

    public Log(string contextName, string owner, string? operation = null, LogSeverity minimumSeverity = LogSeverity.None)
    {
        _contextName = string.IsNullOrWhiteSpace(contextName) ? "Unknown" : contextName;
        _owner = string.IsNullOrWhiteSpace(owner) ? InferOwner(_contextName) : owner;
        _operation = string.IsNullOrWhiteSpace(operation) ? _contextName : operation!;
        _localMinimumSeverity = minimumSeverity;
    }

    public static void Configure(LogOptions options)
    {
        lock (SyncRoot)
        {
            DisposeSinks();
            _options = options;
            Directory.CreateDirectory(_options.RunDirectory);
            LogBudgetRuntime.Reset();
            _memorySink = null;

            if (_options.EnableStdoutSummary)
            {
                Sinks.Add(new StdoutSummarySink(_options.Stdout ?? Console.Out));
            }

            if (_options.EnableJsonlFile)
            {
                Sinks.Add(new JsonlBufferedFileSink(_options.RunDirectory));
            }

            if (_options.EnableMemorySink)
            {
                _memorySink = new MemorySink();
                Sinks.Add(_memorySink);
            }

            if (_options.EnableArtifactSink)
            {
                Sinks.Add(new ArtifactSink(_options.RunDirectory));
            }

            if (_options.EnableGodotEditorSink)
            {
                Sinks.Add(new GodotEditorSink());
            }

            WriteProfileMetadata();
        }
    }

    public static LogOptions LoadOptionsFromConfig(string? configDirectory = null, string? runDirectory = null, TextWriter? stdout = null)
    {
        var options = new LogOptions
        {
            ConfigDirectory = string.IsNullOrWhiteSpace(configDirectory)
                ? Path.Combine("Config", "Log")
                : configDirectory!,
            RunDirectory = ResolveRunDirectory(runDirectory),
            ProfileName = "ai-default",
            MinimumSeverity = LogSeverity.Debug,
            EnableStdoutSummary = true,
            EnableJsonlFile = true,
            EnableMemorySink = true,
            EnableArtifactSink = true,
            EnableGodotEditorSink = false,
            EnableGodotFrameCounters = true,
            Budget = new LogBudgetOptions { Enabled = true, DefaultPerSecond = 200 },
            Stdout = stdout
        };

        var requestedProfile = System.Environment.GetEnvironmentVariable("SLIMEAI_LOG_PROFILE");
        if (!string.IsNullOrWhiteSpace(requestedProfile))
        {
            options.ProfileName = requestedProfile!;
        }

        options.ProfilePath = ResolveProfilePath(options.ConfigDirectory, options.ProfileName);
        options.RulesPath = Path.Combine(options.ConfigDirectory, "log.rules.json");
        options.OverridesPath = ResolveOverridesPath(options.ConfigDirectory);

        var profile = ReadJson<LogProfileDocument>(options.ProfilePath, options.ConfigWarnings, "profile");
        if (profile is not null)
        {
            ApplyProfile(options, profile);
        }

        var rules = ReadJson<LogRulesDocument>(options.RulesPath, options.ConfigWarnings, "rules");
        if (rules?.Rules is not null)
        {
            options.Rules.AddRange(rules.Rules);
        }

        var overrides = ReadJson<LogOverridesDocument>(options.OverridesPath, options.ConfigWarnings, "overrides");
        if (overrides is not null)
        {
            ApplyOverrides(options, overrides);
        }

        return options;
    }

    public static void ResetForTests()
    {
        lock (SyncRoot)
        {
            ContextFilters.Clear();
            ShowTimestamp = false;
            ShowContext = true;
            Configure(new LogOptions
            {
                ProfileName = "test-reset",
                RunDirectory = Path.Combine(".ai-temp", "log-tests", Guid.NewGuid().ToString("N")),
                MinimumSeverity = LogSeverity.Trace,
                EnableStdoutSummary = false,
                EnableJsonlFile = false,
                EnableMemorySink = true,
                EnableArtifactSink = true,
                EnableGodotEditorSink = false,
                EnableGodotFrameCounters = false,
                Budget = new LogBudgetOptions { Enabled = false, DefaultPerSecond = 0 },
            });
        }
    }

    public static IReadOnlyList<LogEntry> GetMemoryEntries()
    {
        lock (SyncRoot)
        {
            return _memorySink?.Entries.ToList() ?? new List<LogEntry>();
        }
    }

    public static void SetLevel(string contextName, LogLevel level)
    {
        ContextFilters[contextName] = ToSeverity(level);
    }

    public static OperationTrace BeginTrace(
        string owner,
        string context,
        string operation,
        string phase = "Runtime",
        string? correlationId = null,
        [CallerFilePath] string? sourceFile = null,
        [CallerLineNumber] int sourceLine = 0)
    {
        return new OperationTrace(owner, context, operation, phase, correlationId, sourceFile, sourceLine);
    }

    public static void Emit(LogEntry entry)
    {
        PrepareEntry(entry);
        var effectiveMinimumSeverity = ResolveMinimumSeverity(entry);
        if (entry.Severity < effectiveMinimumSeverity && entry.Channel != LogChannel.Validation)
        {
            return;
        }

        if (!_isWritingBudgetSummary)
        {
            var decision = LogBudgetRuntime.Check(entry, _options);
            if (decision.Summary is not null)
            {
                WriteBudgetSummary(decision.Summary);
            }

            if (!decision.ShouldWrite)
            {
                return;
            }
        }

        lock (SyncRoot)
        {
            foreach (var sink in Sinks)
            {
                sink.Write(entry);
            }
        }
    }

    public static void Flush()
    {
        foreach (var summary in LogBudgetRuntime.DrainSummaries())
        {
            WriteBudgetSummary(summary);
        }

        lock (SyncRoot)
        {
            foreach (var sink in Sinks)
            {
                sink.Flush();
            }
        }
    }

    [Conditional("DEBUG")]
    public void Trace(
        object message,
        LogOutcome outcome = LogOutcome.None,
        LogValidationStatus validationStatus = LogValidationStatus.None,
        LogFields? fields = null,
        LogChannel channel = LogChannel.Runtime,
        string? operation = null,
        string? phase = null,
        [CallerFilePath] string? sourceFile = null,
        [CallerLineNumber] int sourceLine = 0)
    {
        Write(LogSeverity.Trace, message, outcome, validationStatus, fields, channel, operation, phase, sourceFile, sourceLine);
    }

    [Conditional("DEBUG")]
    public void Debug(
        object message,
        LogOutcome outcome = LogOutcome.None,
        LogValidationStatus validationStatus = LogValidationStatus.None,
        LogFields? fields = null,
        LogChannel channel = LogChannel.Runtime,
        string? operation = null,
        string? phase = null,
        [CallerFilePath] string? sourceFile = null,
        [CallerLineNumber] int sourceLine = 0)
    {
        Write(LogSeverity.Debug, message, outcome, validationStatus, fields, channel, operation, phase, sourceFile, sourceLine);
    }

    public void Info(
        object message,
        LogOutcome outcome = LogOutcome.None,
        LogValidationStatus validationStatus = LogValidationStatus.None,
        LogFields? fields = null,
        LogChannel channel = LogChannel.Runtime,
        string? operation = null,
        string? phase = null,
        [CallerFilePath] string? sourceFile = null,
        [CallerLineNumber] int sourceLine = 0)
    {
        Write(LogSeverity.Info, message, outcome, validationStatus, fields, channel, operation, phase, sourceFile, sourceLine);
    }

    public void Success(
        object message,
        LogOutcome outcome = LogOutcome.Succeeded,
        LogValidationStatus validationStatus = LogValidationStatus.None,
        LogFields? fields = null,
        LogChannel channel = LogChannel.Runtime,
        string? operation = null,
        string? phase = null,
        [CallerFilePath] string? sourceFile = null,
        [CallerLineNumber] int sourceLine = 0)
    {
        Write(LogSeverity.Info, message, outcome, validationStatus, fields, channel, operation, phase, sourceFile, sourceLine);
    }

    public void Warn(
        object message,
        LogOutcome outcome = LogOutcome.None,
        LogValidationStatus validationStatus = LogValidationStatus.None,
        LogFields? fields = null,
        LogChannel channel = LogChannel.Runtime,
        string? operation = null,
        string? phase = null,
        [CallerFilePath] string? sourceFile = null,
        [CallerLineNumber] int sourceLine = 0)
    {
        Write(LogSeverity.Warn, message, outcome, validationStatus, fields, channel, operation, phase, sourceFile, sourceLine);
    }

    public void Error(
        object message,
        LogOutcome outcome = LogOutcome.Failed,
        LogValidationStatus validationStatus = LogValidationStatus.None,
        LogFields? fields = null,
        LogChannel channel = LogChannel.Runtime,
        string? operation = null,
        string? phase = null,
        [CallerFilePath] string? sourceFile = null,
        [CallerLineNumber] int sourceLine = 0)
    {
        Write(LogSeverity.Error, message, outcome, validationStatus, fields, channel, operation, phase, sourceFile, sourceLine);
    }

    public void Write(
        LogSeverity severity,
        object message,
        LogOutcome outcome = LogOutcome.None,
        LogValidationStatus validationStatus = LogValidationStatus.None,
        LogFields? fields = null,
        LogChannel channel = LogChannel.Runtime,
        string? operation = null,
        string? phase = null,
        [CallerFilePath] string? sourceFile = null,
        [CallerLineNumber] int sourceLine = 0)
    {
        if (!ShouldLog(severity, channel, operation ?? _operation) && channel != LogChannel.Validation)
        {
            return;
        }

        Emit(new LogEntry
        {
            Severity = severity,
            Outcome = outcome,
            ValidationStatus = validationStatus,
            Channel = channel,
            Owner = _owner,
            Context = _contextName,
            Operation = operation ?? _operation,
            Phase = phase ?? (channel == LogChannel.Validation ? "Validation" : "Runtime"),
            Message = message?.ToString() ?? string.Empty,
            SourceFile = sourceFile,
            SourceLine = sourceLine,
            Fields = fields is null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(fields)
        });
    }

    private bool ShouldLog(LogSeverity severity, LogChannel channel, string operation)
    {
        if (ContextFilters.TryGetValue(_contextName, out var filterLevel))
        {
            return severity >= filterLevel;
        }

        if (_localMinimumSeverity != LogSeverity.None)
        {
            return severity >= _localMinimumSeverity;
        }

        return severity >= ResolveMinimumSeverity(_owner, _contextName, operation, channel);
    }

    private static void PrepareEntry(LogEntry entry)
    {
        entry.TimestampUtc = DateTimeOffset.UtcNow.ToString("O");
        entry.RunElapsedMs = RunStopwatch.ElapsedMilliseconds;
        entry.Frame = _options.EnableGodotFrameCounters ? TryGetProcessFrames() : 0;
        entry.PhysicsFrame = _options.EnableGodotFrameCounters ? TryGetPhysicsFrames() : 0;
        entry.Owner = string.IsNullOrWhiteSpace(entry.Owner) ? "Unknown" : entry.Owner;
        entry.Context = string.IsNullOrWhiteSpace(entry.Context) ? "Unknown" : entry.Context;
        entry.Operation = string.IsNullOrWhiteSpace(entry.Operation) ? entry.Context : entry.Operation;
        entry.Phase = string.IsNullOrWhiteSpace(entry.Phase) ? "Runtime" : entry.Phase;
        entry.Message ??= string.Empty;
        entry.Fields ??= new Dictionary<string, object?>();
    }

    private static LogOptions CreateDefaultOptions()
    {
        return LoadOptionsFromConfig();
    }

    private static string ResolveRunDirectory(string? requestedRunDirectory)
    {
        var runDirectory = requestedRunDirectory;
        if (string.IsNullOrWhiteSpace(runDirectory))
        {
            runDirectory = System.Environment.GetEnvironmentVariable("SLIMEAI_LOG_RUN_DIR");
        }

        if (string.IsNullOrWhiteSpace(runDirectory))
        {
            runDirectory = Path.Combine(".ai-temp", "log-runs", DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss"));
        }

        return runDirectory!;
    }

    private static string ResolveProfilePath(string configDirectory, string profileName)
    {
        var candidates = new[]
        {
            Path.Combine(configDirectory, $"{profileName}.profile.json"),
            Path.Combine(configDirectory, $"log.{profileName}.profile.json"),
            Path.Combine(configDirectory, "log.profile.json")
        };

        return candidates.FirstOrDefault(File.Exists) ?? candidates[^1];
    }

    private static string ResolveOverridesPath(string configDirectory)
    {
        var envPath = System.Environment.GetEnvironmentVariable("SLIMEAI_LOG_OVERRIDES");
        return string.IsNullOrWhiteSpace(envPath) ? Path.Combine(configDirectory, "log.overrides.json") : envPath!;
    }

    private static T? ReadJson<T>(string? path, List<string> warnings, string label)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return default;
        }

        try
        {
            return LogJson.Deserialize<T>(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            warnings.Add($"{label} parse failed: {path}: {ex.Message}");
            return default;
        }
    }

    private static void ApplyProfile(LogOptions options, LogProfileDocument profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.Profile))
        {
            options.ProfileName = profile.Profile;
        }

        ApplySeverity(options, profile.DefaultSeverity ?? profile.MinimumSeverity);
        ApplySinks(options, profile.Sinks);

        if (profile.Budget is not null)
        {
            options.Budget = profile.Budget;
        }

        options.Rules.AddRange(profile.Rules);
    }

    private static void ApplyOverrides(LogOptions options, LogOverridesDocument overrides)
    {
        if (!string.IsNullOrWhiteSpace(overrides.Profile))
        {
            options.ProfileName = overrides.Profile!;
        }

        ApplySeverity(options, overrides.DefaultSeverity ?? overrides.MinimumSeverity);
        ApplySinks(options, overrides.Sinks);

        if (overrides.Budget is not null)
        {
            options.Budget = overrides.Budget;
        }

        options.Rules.AddRange(overrides.Rules);
    }

    private static void ApplySeverity(LogOptions options, LogSeverity? severity)
    {
        if (severity.HasValue)
        {
            options.MinimumSeverity = severity.Value;
        }
    }

    private static void ApplySinks(LogOptions options, LogSinkOptions? sinks)
    {
        if (sinks is null)
        {
            return;
        }

        if (sinks.StdoutSummary.HasValue)
        {
            options.EnableStdoutSummary = sinks.StdoutSummary.Value;
        }

        if (sinks.JsonlFile.HasValue)
        {
            options.EnableJsonlFile = sinks.JsonlFile.Value;
        }

        if (sinks.Memory.HasValue)
        {
            options.EnableMemorySink = sinks.Memory.Value;
        }

        if (sinks.Artifact.HasValue)
        {
            options.EnableArtifactSink = sinks.Artifact.Value;
        }

        if (sinks.GodotEditor.HasValue)
        {
            options.EnableGodotEditorSink = sinks.GodotEditor.Value;
        }
    }

    private static LogSeverity ResolveMinimumSeverity(LogEntry entry)
    {
        return ResolveMinimumSeverity(entry.Owner, entry.Context, entry.Operation, entry.Channel);
    }

    private static LogSeverity ResolveMinimumSeverity(string owner, string context, string operation, LogChannel channel)
    {
        var probe = new LogEntry
        {
            Owner = owner,
            Context = context,
            Operation = operation,
            Channel = channel
        };

        for (var index = _options.Rules.Count - 1; index >= 0; index -= 1)
        {
            var rule = _options.Rules[index];
            if (rule.MinimumSeverity.HasValue && rule.Matches(probe))
            {
                return rule.MinimumSeverity.Value;
            }
        }

        return _options.MinimumSeverity;
    }

    private static void WriteBudgetSummary(LogEntry summary)
    {
        _isWritingBudgetSummary = true;
        try
        {
            PrepareEntry(summary);
            lock (SyncRoot)
            {
                foreach (var sink in Sinks)
                {
                    sink.Write(summary);
                }
            }
        }
        finally
        {
            _isWritingBudgetSummary = false;
        }
    }

    private static void WriteProfileMetadata()
    {
        try
        {
            var path = Path.Combine(_options.RunDirectory, "metadata", "log-profile.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, LogJson.Serialize(new
            {
                generatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                profile = _options.ProfileName,
                configDirectory = _options.ConfigDirectory,
                profilePath = _options.ProfilePath,
                rulesPath = _options.RulesPath,
                overridesPath = _options.OverridesPath,
                sinks = new
                {
                    stdoutSummary = _options.EnableStdoutSummary,
                    jsonlFile = _options.EnableJsonlFile,
                    memory = _options.EnableMemorySink,
                    artifact = _options.EnableArtifactSink,
                    godotEditor = _options.EnableGodotEditorSink,
                    godotFrameCounters = _options.EnableGodotFrameCounters
                },
                minimumSeverity = _options.MinimumSeverity,
                budget = _options.Budget,
                rules = _options.Rules,
                warnings = _options.ConfigWarnings
            }, indented: true));
        }
        catch
        {
            // 日志元数据写入失败不能反过来阻断游戏启动。
        }
    }

    private static void DisposeSinks()
    {
        foreach (var sink in Sinks)
        {
            sink.Dispose();
        }

        Sinks.Clear();
    }

    private static string InferOwner(string context)
    {
        if (context.Contains("Ability", StringComparison.OrdinalIgnoreCase))
        {
            return "Ability";
        }

        if (context.Contains("Damage", StringComparison.OrdinalIgnoreCase))
        {
            return "Damage";
        }

        if (context.Contains("Movement", StringComparison.OrdinalIgnoreCase))
        {
            return "Movement";
        }

        if (context.Contains("Pool", StringComparison.OrdinalIgnoreCase))
        {
            return "ObjectPool";
        }

        if (context.Contains("Timer", StringComparison.OrdinalIgnoreCase))
        {
            return "Timer";
        }

        if (context.Contains("Target", StringComparison.OrdinalIgnoreCase))
        {
            return "TargetSelector";
        }

        if (context.Contains("System", StringComparison.OrdinalIgnoreCase))
        {
            return "System";
        }

        if (context.Contains("Data", StringComparison.OrdinalIgnoreCase))
        {
            return "Data";
        }

        if (context.Contains("Entity", StringComparison.OrdinalIgnoreCase))
        {
            return "Entity";
        }

        if (context.Contains("Log", StringComparison.OrdinalIgnoreCase))
        {
            return "Tools.Logger";
        }

        return "Runtime";
    }

    private static LogSeverity ToSeverity(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => LogSeverity.Trace,
            LogLevel.Debug => LogSeverity.Debug,
            LogLevel.Info => LogSeverity.Info,
            LogLevel.Success => LogSeverity.Info,
            LogLevel.Warning => LogSeverity.Warn,
            LogLevel.Error => LogSeverity.Error,
            _ => LogSeverity.None
        };
    }

    private static LogLevel ToLegacyLevel(LogSeverity severity)
    {
        return severity switch
        {
            LogSeverity.Trace => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            LogSeverity.Info => LogLevel.Info,
            LogSeverity.Warn => LogLevel.Warning,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Fatal => LogLevel.Error,
            _ => LogLevel.None
        };
    }

    private static ulong TryGetProcessFrames()
    {
        try
        {
            return Engine.GetProcessFrames();
        }
        catch
        {
            return 0;
        }
    }

    private static ulong TryGetPhysicsFrames()
    {
        try
        {
            return Engine.GetPhysicsFrames();
        }
        catch
        {
            return 0;
        }
    }
}
