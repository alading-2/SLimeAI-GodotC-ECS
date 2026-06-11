using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Slime.Test
{
    public partial class LogTest : Node
    {
        public override void _Ready()
        {
            CallDeferred(nameof(RunTests));
        }

        private void RunTests()
        {
            TestStructuredEntrySplitsSeverityOutcomeAndValidation();
            TestDefaultSinksWriteSummaryAndJsonlWithoutGodotEditorSink();
            TestValidationSessionWritesArtifactAndStructuredChecks();
            TestProfileLoadsConfigAndWritesMetadata();
            TestBudgetSuppressesRepeatedEntriesAndWritesSummary();
        }

        private static void TestStructuredEntrySplitsSeverityOutcomeAndValidation()
        {
            Log.ResetForTests();
            Log.Configure(new LogOptions
            {
                ProfileName = "test",
                MinimumSeverity = LogSeverity.Trace,
                EnableStdoutSummary = false,
                EnableJsonlFile = false,
                EnableMemorySink = true,
                EnableGodotEditorSink = false,
            });

            var log = new Log("LoggerContract", owner: "Tools.Logger", operation: "Contract");
            log.Info(
                "structured entry",
                outcome: LogOutcome.Succeeded,
                validationStatus: LogValidationStatus.Pass,
                fields: new LogFields
                {
                    ["entityId"] = "logger-test",
                    ["reasonCode"] = "contract-check"
                });

            var entries = Log.GetMemoryEntries();
            AssertTrue(entries.Count == 1, "memory sink captures one structured entry");

            var entry = entries[0];
            AssertTrue(entry.Severity == LogSeverity.Info, "severity is independent from success/pass");
            AssertTrue(entry.Outcome == LogOutcome.Succeeded, "outcome records operation result");
            AssertTrue(entry.ValidationStatus == LogValidationStatus.Pass, "validation status records PASS/FAIL semantics");
            AssertTrue(entry.Owner == "Tools.Logger", "owner is preserved");
            AssertTrue(entry.Operation == "Contract", "operation is preserved");
            AssertTrue(entry.RunElapsedMs >= 0, "run elapsed is monotonic field");
            AssertTrue(entry.Frame >= 0, "process frame field exists");
            AssertTrue(entry.PhysicsFrame >= 0, "physics frame field exists");
            AssertTrue(entry.Fields["entityId"]?.ToString() == "logger-test", "structured field is preserved");
        }

        private static void TestDefaultSinksWriteSummaryAndJsonlWithoutGodotEditorSink()
        {
            Log.ResetForTests();

            var runDir = Path.Combine(".ai-temp", "logger-tests", Guid.NewGuid().ToString("N"));
            var stdout = new StringWriter();
            Log.Configure(new LogOptions
            {
                ProfileName = "test-default-sinks",
                RunDirectory = runDir,
                Stdout = stdout,
                MinimumSeverity = LogSeverity.Trace,
                EnableStdoutSummary = true,
                EnableJsonlFile = true,
                EnableMemorySink = true,
                EnableGodotEditorSink = false,
            });

            var log = new Log("LoggerSink", owner: "Tools.Logger", operation: "SinkContract");
            log.Warn("warn summary", outcome: LogOutcome.Completed, fields: new LogFields { ["budgetKey"] = "logger-test" });
            Log.Flush();

            var stdoutText = stdout.ToString();
            AssertTrue(stdoutText.Contains("[WARN][Tools.Logger][LoggerSink]"), "stdout summary contains owner/context/severity");
            AssertTrue(stdoutText.Contains("operation=SinkContract"), "stdout summary contains operation");
            AssertTrue(!stdoutText.Contains("[PASS]"), "stdout summary does not use PASS marker");

            var jsonlPath = Path.Combine(runDir, "raw", "scene-log.jsonl");
            AssertTrue(File.Exists(jsonlPath), "jsonl sink writes raw scene log");

            var firstLine = File.ReadAllLines(jsonlPath)[0];
            using var document = JsonDocument.Parse(firstLine);
            var root = document.RootElement;
            AssertTrue(root.GetProperty("severity").GetString() == "Warn", "jsonl records severity");
            AssertTrue(root.GetProperty("outcome").GetString() == "Completed", "jsonl records outcome");
            AssertTrue(root.GetProperty("owner").GetString() == "Tools.Logger", "jsonl records owner");
            AssertTrue(root.GetProperty("fields").GetProperty("budgetKey").GetString() == "logger-test", "jsonl records fields");
            AssertTrue(!root.TryGetProperty("timestampUtc", out _), "jsonl does not write legacy wall clock timestamp by default");
            AssertTrue(!root.TryGetProperty("wallClockUtc", out _), "jsonl omits wall clock by default");
        }

        private static void TestValidationSessionWritesArtifactAndStructuredChecks()
        {
            Log.ResetForTests();

            var runDir = Path.Combine(".ai-temp", "validation-tests", Guid.NewGuid().ToString("N"));
            Log.Configure(new LogOptions
            {
                ProfileName = "validation-test",
                RunDirectory = runDir,
                MinimumSeverity = LogSeverity.Trace,
                EnableStdoutSummary = false,
                EnableJsonlFile = true,
                EnableMemorySink = true,
                EnableGodotEditorSink = false,
            });

            using var session = ValidationSession.Start(new ValidationSessionOptions
            {
                Name = "LoggerValidationContract",
                Owner = "Tools.Logger",
                ArtifactPath = Path.Combine(runDir, "artifacts", "logger-validation.json"),
                ExpectedInputs = "one passing check and one skipped check",
                ExpectedObservations = "checks are stored in artifact and structured log",
                PassCriteria = "all required checks pass",
                FailCriteria = "required check failure marks session failed",
            });

            session.Check("required-pass", true, expected: "true", actual: "true", reasonCode: "required-pass");
            session.Skip("optional-skip", reasonCode: "not-needed");
            var artifact = session.Complete();
            Log.Flush();

            AssertTrue(artifact.ValidationStatus == LogValidationStatus.Pass, "validation session status is pass");
            AssertTrue(artifact.Checks.Count == 2, "artifact keeps check list");
            AssertTrue(File.Exists(artifact.ArtifactPath), "validation artifact is written");

            var json = File.ReadAllText(artifact.ArtifactPath);
            AssertTrue(json.Contains("\"expectedInputs\""), "artifact includes expected inputs");
            AssertTrue(json.Contains("\"expectedObservations\""), "artifact includes expected observations");
            AssertTrue(json.Contains("\"passCriteria\""), "artifact includes pass criteria");
            AssertTrue(json.Contains("\"failCriteria\""), "artifact includes fail criteria");
            AssertTrue(json.Contains("\"checks\""), "artifact includes checks");
            AssertTrue(json.Contains("\"failures\""), "artifact includes failures");

            var entries = Log.GetMemoryEntries();
            AssertTrue(entries.Any(entry => entry.Channel == LogChannel.Validation && entry.ValidationStatus == LogValidationStatus.Pass), "validation pass is structured log field");
        }

        private static void TestProfileLoadsConfigAndWritesMetadata()
        {
            Log.ResetForTests();

            var root = Path.Combine(".ai-temp", "logger-profile-tests", Guid.NewGuid().ToString("N"));
            var configDir = Path.Combine(root, "Config", "Log");
            var runDir = Path.Combine(root, "run");
            Directory.CreateDirectory(configDir);

            File.WriteAllText(Path.Combine(configDir, "log.profile.json"), """
            {
              "profile": "profile-contract",
              "defaultSeverity": "Warn",
              "includeWallClockUtc": true,
              "sinks": {
                "stdoutSummary": false,
                "jsonlFile": true,
                "memory": true,
                "artifact": true,
                "godotEditor": false
              },
              "budget": {
                "enabled": true,
                "defaultPerSecond": 20
              },
              "rules": [
                {
                  "owner": "Tools.Logger",
                  "context": "LoggerProfile",
                  "operation": "ProfileDebug",
                  "minimumSeverity": "Debug",
                  "budgetPerSecond": 5
                }
              ]
            }
            """);

            File.WriteAllText(Path.Combine(configDir, "log.rules.json"), """
            {
              "rules": [
                {
                  "owner": "Tools.Logger",
                  "context": "LoggerProfile",
                  "operation": "RulesInfo",
                  "minimumSeverity": "Info"
                }
              ]
            }
            """);

            File.WriteAllText(Path.Combine(configDir, "log.overrides.json"), """
            {
              "profile": "profile-contract-override",
              "reason": "logger profile contract test"
            }
            """);

            var options = Log.LoadOptionsFromConfig(configDir, runDir);
            Log.Configure(options);

            var log = new Log("LoggerProfile", owner: "Tools.Logger", operation: "ProfileDebug");
            log.Write(LogSeverity.Debug, "profile debug allowed by rule", operation: "ProfileDebug");
            log.Write(LogSeverity.Debug, "rules debug filtered", operation: "RulesInfo");
            log.Write(LogSeverity.Info, "rules info allowed", operation: "RulesInfo");
            Log.Flush();

            var entries = Log.GetMemoryEntries();
            AssertTrue(entries.Any(entry => entry.Message == "profile debug allowed by rule"), "profile rule opens debug for selected operation");
            AssertTrue(!entries.Any(entry => entry.Message == "rules debug filtered"), "rules file minimum severity filters debug");
            AssertTrue(entries.Any(entry => entry.Message == "rules info allowed"), "rules file allows info");

            var metadataPath = Path.Combine(runDir, "metadata", "log-profile.json");
            AssertTrue(File.Exists(metadataPath), "log profile metadata is written to run dir");

            using var metadata = JsonDocument.Parse(File.ReadAllText(metadataPath));
            var rootElement = metadata.RootElement;
            AssertTrue(rootElement.GetProperty("profile").GetString() == "profile-contract-override", "override profile is reflected in metadata");
            AssertTrue(rootElement.GetProperty("sinks").GetProperty("godotEditor").GetBoolean() == false, "godot editor sink remains disabled by profile");
            AssertTrue(rootElement.GetProperty("budget").GetProperty("enabled").GetBoolean(), "budget config is reflected in metadata");

            var jsonlPath = Path.Combine(runDir, "raw", "scene-log.jsonl");
            using var jsonl = JsonDocument.Parse(File.ReadAllLines(jsonlPath).First());
            AssertTrue(jsonl.RootElement.TryGetProperty("runElapsedMs", out _), "jsonl uses camelCase envelope fields");
            AssertTrue(jsonl.RootElement.TryGetProperty("wallClockUtc", out _), "profile can opt in wall clock for cross artifact alignment");
            AssertTrue(!jsonl.RootElement.TryGetProperty("timestampUtc", out _), "profile opt-in uses wallClockUtc instead of timestampUtc");
        }

        private static void TestBudgetSuppressesRepeatedEntriesAndWritesSummary()
        {
            Log.ResetForTests();

            var runDir = Path.Combine(".ai-temp", "logger-budget-tests", Guid.NewGuid().ToString("N"));
            Log.Configure(new LogOptions
            {
                ProfileName = "budget-test",
                RunDirectory = runDir,
                MinimumSeverity = LogSeverity.Trace,
                EnableStdoutSummary = false,
                EnableJsonlFile = true,
                EnableMemorySink = true,
                EnableArtifactSink = true,
                EnableGodotEditorSink = false,
                Budget = new LogBudgetOptions { Enabled = true, DefaultPerSecond = 1 },
            });

            var log = new Log("LoggerBudget", owner: "Tools.Logger", operation: "BudgetedOperation");
            for (var index = 0; index < 4; index += 1)
            {
                log.Info(
                    "budgeted repeated entry",
                    fields: new LogFields { ["budgetKey"] = "logger-budget-contract" },
                    operation: "BudgetedOperation");
            }

            Log.Flush();

            var entries = Log.GetMemoryEntries();
            var repeatedEntries = entries.Where(entry => entry.Message == "budgeted repeated entry").ToList();
            var summaries = entries.Where(entry => entry.Operation == "SuppressedSummary").ToList();

            AssertTrue(repeatedEntries.Count == 1, "budget keeps only first repeated entry in one-second window");
            AssertTrue(summaries.Count >= 1, "budget writes suppressed summary");
            AssertTrue(summaries.Any(entry => Convert.ToInt32(entry.Fields["suppressedCount"]) >= 1), "suppressed summary includes count");
            AssertTrue(summaries.All(entry => entry.Fields["budgetKey"]?.ToString() == "logger-budget-contract"), "suppressed summary keeps budget key");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"LogTest assertion failed: {message}");
            }
        }
    }
}
