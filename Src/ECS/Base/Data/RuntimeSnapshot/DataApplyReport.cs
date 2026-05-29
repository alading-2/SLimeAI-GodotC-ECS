using System.Collections.Generic;
using System.Text;

/// <summary>
/// Runtime snapshot record 应用报告。
/// </summary>
public sealed class DataApplyReport
{
    /// <summary>
    /// 创建 record 应用报告。
    /// </summary>
    /// <param name="table">record table。</param>
    /// <param name="recordId">record id。</param>
    public DataApplyReport(string table, string recordId)
    {
        Table = table; // record table
        RecordId = recordId; // record id
    }

    /// <summary>
    /// record table。
    /// </summary>
    public string Table { get; }

    /// <summary>
    /// record id。
    /// </summary>
    public string RecordId { get; }

    /// <summary>
    /// 成功写入字段数量。
    /// </summary>
    public int AppliedFieldCount { get; set; }

    /// <summary>
    /// 结构化错误列表。
    /// </summary>
    public List<DataApplyError> Errors { get; } = new();

    /// <summary>
    /// 是否存在错误。
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// 追加结构化错误。
    /// </summary>
    /// <param name="code">错误码。</param>
    /// <param name="stableKey">字段 stable key。</param>
    /// <param name="message">错误信息。</param>
    public void AddError(string code, string stableKey, string message)
    {
        Errors.Add(new DataApplyError(code, stableKey, message));
    }

    /// <summary>
    /// 输出人类可读摘要。
    /// </summary>
    public string ToSummary()
    {
        if (!HasErrors)
        {
            return $"DataApplyReport ok: {Table}/{RecordId}, applied={AppliedFieldCount}";
        }

        var builder = new StringBuilder();
        builder.Append($"DataApplyReport failed: {Table}/{RecordId}, applied={AppliedFieldCount}, errors={Errors.Count}");
        for (var i = 0; i < Errors.Count; i++)
        {
            var error = Errors[i];
            builder.Append($"\n- {error.Code} {error.StableKey}: {error.Message}");
        }

        return builder.ToString();
    }
}

/// <summary>
/// Runtime snapshot record 应用错误。
/// </summary>
/// <param name="Code">错误码。</param>
/// <param name="StableKey">字段 stable key。</param>
/// <param name="Message">错误信息。</param>
public sealed record DataApplyError(string Code, string StableKey, string Message);
