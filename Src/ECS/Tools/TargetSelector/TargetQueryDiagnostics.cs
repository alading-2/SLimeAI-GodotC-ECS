/// <summary>
/// 目标查询诊断信息。
/// 用于判断查询阶段是否发生过滤、截断，以及返回数量是否符合预期。
/// </summary>
public readonly record struct TargetQueryDiagnostics(
    int CandidateCount,
    int ReturnedCount,
    int MaxTargets,
    bool Truncated);
