using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class ValidationSessionOptions
{
    public string Name { get; set; } = "Validation";
    public string Owner { get; set; } = "Validation";
    public string ArtifactPath { get; set; } = Path.Combine(".ai-temp", "scene-tests", "artifacts", "validation.json");
    public string ExpectedInputs { get; set; } = string.Empty;
    public string ExpectedObservations { get; set; } = string.Empty;
    public string PassCriteria { get; set; } = string.Empty;
    public string FailCriteria { get; set; } = string.Empty;
}

public sealed class CheckResult
{
    public string Name { get; set; } = string.Empty;
    public LogValidationStatus Status { get; set; } = LogValidationStatus.None;
    public string Expected { get; set; } = string.Empty;
    public string Actual { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ValidationArtifact
{
    public string Name { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string ArtifactPath { get; set; } = string.Empty;
    public string ExpectedInputs { get; set; } = string.Empty;
    public string ExpectedObservations { get; set; } = string.Empty;
    public string PassCriteria { get; set; } = string.Empty;
    public string FailCriteria { get; set; } = string.Empty;
    public LogValidationStatus ValidationStatus { get; set; } = LogValidationStatus.None;
    public List<CheckResult> Checks { get; set; } = new();
    public List<CheckResult> Failures { get; set; } = new();
}

/// <summary>
/// 场景和运行时测试的统一断言事实源，负责写 artifact 和 Validation channel 日志。
/// </summary>
public sealed class ValidationSession : IDisposable
{
    private readonly ValidationSessionOptions _options;
    private readonly Log _log;
    private readonly List<CheckResult> _checks = new();
    private bool _completed;
    private ValidationArtifact? _artifact;

    private ValidationSession(ValidationSessionOptions options)
    {
        _options = options;
        _log = new Log(options.Name, owner: options.Owner, operation: "ValidationSession");
    }

    public static ValidationSession Start(ValidationSessionOptions options)
    {
        var session = new ValidationSession(options);
        session._log.Info(
            "validation started",
            outcome: LogOutcome.Started,
            validationStatus: LogValidationStatus.None,
            fields: new LogFields
            {
                ["artifactPath"] = options.ArtifactPath,
                ["expectedInputs"] = options.ExpectedInputs,
                ["expectedObservations"] = options.ExpectedObservations
            },
            channel: LogChannel.Validation,
            operation: options.Name,
            phase: "Validation");
        return session;
    }

    public CheckResult Check(
        string name,
        bool condition,
        string expected = "",
        string actual = "",
        string reasonCode = "",
        string message = "")
    {
        var result = new CheckResult
        {
            Name = name,
            Status = condition ? LogValidationStatus.Pass : LogValidationStatus.Fail,
            Expected = expected,
            Actual = actual,
            ReasonCode = reasonCode,
            Message = message
        };

        Record(result);
        return result;
    }

    public CheckResult Skip(string name, string reasonCode = "", string message = "")
    {
        var result = new CheckResult
        {
            Name = name,
            Status = LogValidationStatus.Skip,
            ReasonCode = reasonCode,
            Message = message
        };

        Record(result);
        return result;
    }

    public ValidationArtifact Complete()
    {
        if (_completed && _artifact is not null)
        {
            return _artifact;
        }

        _completed = true;
        var failures = _checks.Where(check => check.Status == LogValidationStatus.Fail).ToList();
        var status = failures.Count == 0 ? LogValidationStatus.Pass : LogValidationStatus.Fail;

        _artifact = new ValidationArtifact
        {
            Name = _options.Name,
            Owner = _options.Owner,
            ArtifactPath = _options.ArtifactPath,
            ExpectedInputs = _options.ExpectedInputs,
            ExpectedObservations = _options.ExpectedObservations,
            PassCriteria = _options.PassCriteria,
            FailCriteria = _options.FailCriteria,
            ValidationStatus = status,
            Checks = _checks.ToList(),
            Failures = failures
        };

        Directory.CreateDirectory(Path.GetDirectoryName(_options.ArtifactPath)!);
        File.WriteAllText(_options.ArtifactPath, LogJson.Serialize(_artifact, indented: true));

        _log.Info(
            "validation completed",
            outcome: status == LogValidationStatus.Pass ? LogOutcome.Succeeded : LogOutcome.Failed,
            validationStatus: status,
            fields: new LogFields
            {
                ["artifactPath"] = _options.ArtifactPath,
                ["checks"] = _checks.Count,
                ["failures"] = failures.Count
            },
            channel: LogChannel.Validation,
            operation: _options.Name,
            phase: "Validation");

        return _artifact;
    }

    public void Dispose()
    {
        Complete();
    }

    private void Record(CheckResult result)
    {
        _checks.Add(result);
        _log.Info(
            result.Name,
            outcome: result.Status == LogValidationStatus.Fail ? LogOutcome.Failed : LogOutcome.Succeeded,
            validationStatus: result.Status,
            fields: new LogFields
            {
                ["expected"] = result.Expected,
                ["actual"] = result.Actual,
                ["reasonCode"] = result.ReasonCode,
                ["message"] = result.Message
            },
            channel: LogChannel.Validation,
            operation: _options.Name,
            phase: "Validation");
    }
}
