using System;
using System.Linq;
using System.Text;

/// <summary>
/// Timer diagnostics 文本格式化工具。只在显式 debug 调用时分配字符串。
/// </summary>
public static class TimerDiagnosticsFormatter
{
    public static string FormatSummary(TimerDiagnosticsSnapshot snapshot, int topN = 10)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Timer Diagnostics Summary");
        builder.AppendLine($"Active={snapshot.ActiveCount}, Paused={snapshot.PausedCount}, DispatchQueue={snapshot.DispatchQueueCount}, PerFrame={snapshot.PerFrameUpdateCount}");
        builder.AppendLine($"LazyCancelled={snapshot.CancelledLazyHeapItems}, LastTickMs={snapshot.LastTickCostMs:F4}, MaxTickMs={snapshot.MaxTickCostMs:F4}, LastDispatchMs={snapshot.LastDispatchCostMs:F4}, MaxDispatchMs={snapshot.MaxDispatchCostMs:F4}");
        builder.AppendLine("HeapByClock: " + string.Join(", ", snapshot.HeapCountByClock.Select(item => $"{item.Key}={item.Value}")));
        builder.AppendLine("ActiveByPurpose: " + string.Join(", ", snapshot.ActiveCountByPurpose.Select(item => $"{item.Key}={item.Value}")));
        builder.AppendLine("TopOwners: " + string.Join(", ", snapshot.TopOwners.Take(Math.Max(0, topN)).Select(item => $"{item.Owner}={item.Count}")));
        if (snapshot.LeakHints.Count > 0)
        {
            builder.AppendLine("LeakHints: " + string.Join(" | ", snapshot.LeakHints.Take(Math.Max(1, topN))));
        }

        return builder.ToString();
    }

    public static string FormatDump(TimerDiagnosticsSnapshot snapshot)
    {
        var builder = new StringBuilder();
        builder.AppendLine(FormatSummary(snapshot, topN: 10));
        builder.AppendLine("Entries:");
        foreach (var entry in snapshot.Entries)
        {
            builder.AppendLine(
                $"{entry.Handle.Id}:{entry.Handle.Generation} mode={entry.Mode} clock={entry.Clock} owner={entry.Owner} purpose={entry.Purpose} remaining={entry.Remaining:F3} progress={entry.Progress:F3} pause={entry.PauseMask} tag={entry.Tag}");
        }

        return builder.ToString();
    }
}
