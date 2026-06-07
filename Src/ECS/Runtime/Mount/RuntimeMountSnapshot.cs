using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Runtime 挂载点诊断快照。
/// </summary>
public sealed class RuntimeMountSnapshot
{
    public RuntimeMountSnapshot(string runtimeRootPath, IReadOnlyList<RuntimeMountSnapshotEntry> entries)
    {
        RuntimeRootPath = runtimeRootPath;
        Entries = entries;
    }

    public string RuntimeRootPath { get; }
    public IReadOnlyList<RuntimeMountSnapshotEntry> Entries { get; }
    public int PendingCount => Entries.Count(entry => entry.Status == RuntimeMountStatus.Pending);
    public int InTreeCount => Entries.Count(entry => entry.Status == RuntimeMountStatus.InTree);
    public int InvalidCount => Entries.Count(entry => entry.Status == RuntimeMountStatus.Invalid);

    public RuntimeMountSnapshotEntry? GetEntry(RuntimeMountId id)
    {
        return Entries.FirstOrDefault(entry => entry.Id == id);
    }
}

/// <summary>
/// 单个 Runtime 挂载点诊断条目。
/// </summary>
public sealed class RuntimeMountSnapshotEntry
{
    public RuntimeMountSnapshotEntry(
        RuntimeMountId id,
        string relativePath,
        string absolutePath,
        RuntimeMountStatus status,
        RuntimeMountCreationMode creationMode,
        string owner,
        string usage,
        Node? node)
    {
        Id = id;
        RelativePath = relativePath;
        AbsolutePath = absolutePath;
        Status = status;
        CreationMode = creationMode;
        Owner = owner;
        Usage = usage;
        Node = node;
    }

    public RuntimeMountId Id { get; }
    public string RelativePath { get; }
    public string AbsolutePath { get; }
    public RuntimeMountStatus Status { get; }
    public RuntimeMountCreationMode CreationMode { get; }
    public string Owner { get; }
    public string Usage { get; }
    public Node? Node { get; }
}
