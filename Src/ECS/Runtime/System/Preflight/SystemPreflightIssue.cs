/// <summary>
/// System preflight 单条检查问题。
/// </summary>
public sealed record SystemPreflightIssue(
    string RuleId,
    SystemPreflightSeverity Severity,
    string SystemId,
    string Message);
