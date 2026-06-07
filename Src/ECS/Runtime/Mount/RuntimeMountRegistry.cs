using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Runtime SceneTree 挂载注册表。
/// </summary>
public sealed class RuntimeMountRegistry
{
    public const string RuntimeRootName = "SlimeAIRuntime";
    public const string RuntimeRootPath = "/root/SlimeAIRuntime";

    private readonly Node _root;
    private readonly Node _runtimeRoot;
    private readonly Dictionary<RuntimeMountId, RuntimeMountRecord> _records = new();
    private readonly Dictionary<string, Node> _pendingRootChildren = new(StringComparer.Ordinal);

    public RuntimeMountRegistry(Node root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _runtimeRoot = EnsureRuntimeRoot(root);
    }

    public Node GetOrCreate(RuntimeMountId id)
    {
        return GetOrCreate(RuntimeMountManifest.Get(id));
    }

    public Node GetOrCreate(RuntimeMountManifestEntry entry)
    {
        if (_records.TryGetValue(entry.Id, out var existingRecord)
            && IsValid(existingRecord.Node))
        {
            return existingRecord.Node;
        }

        var node = EnsurePath(_runtimeRoot, entry.RelativePath, entry.CreationMode);
        _records[entry.Id] = new RuntimeMountRecord(entry, node);
        return node;
    }

    public Node GetOrCreate(RuntimeMountId id, string relativePath, string owner, string usage)
    {
        var entry = new RuntimeMountManifestEntry(
            id,
            relativePath,
            RuntimeMountCreationMode.Immediate,
            owner,
            usage);

        return GetOrCreate(entry);
    }

    public Node? GetExisting(RuntimeMountId id)
    {
        if (!_records.TryGetValue(id, out var record))
            return null;

        return IsValid(record.Node)
            ? record.Node
            : null;
    }

    public RuntimeMountSnapshot GetSnapshot()
    {
        var entries = _records.Values
            .Select(record =>
            {
                var status = GetStatus(record.Node);
                return new RuntimeMountSnapshotEntry(
                    record.Manifest.Id,
                    record.Manifest.RelativePath,
                    BuildAbsolutePath(record.Manifest.RelativePath),
                    status,
                    record.Manifest.CreationMode,
                    record.Manifest.Owner,
                    record.Manifest.Usage,
                    record.Node);
            })
            .ToArray();

        return new RuntimeMountSnapshot(RuntimeRootPath, entries);
    }

    private Node EnsureRuntimeRoot(Node root)
    {
        var existing = root.GetNodeOrNull(RuntimeRootName);
        if (IsValid(existing))
            return existing!;

        var node = new Node { Name = RuntimeRootName };

        // Godot root 在启动阶段可能锁树，统一 deferred 并让 diagnostics 暴露 Pending。
        root.CallDeferred(Node.MethodName.AddChild, node);
        _pendingRootChildren[RuntimeRootName] = node;
        return node;
    }

    private Node EnsurePath(Node ancestor, string relativePath, RuntimeMountCreationMode creationMode)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return ancestor;

        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = ancestor;

        foreach (var segment in segments)
        {
            var next = current.GetNodeOrNull(segment);
            if (next == null && current == _root)
                next = _pendingRootChildren.GetValueOrDefault(segment);

            if (next == null)
            {
                next = new Node { Name = segment };

                if (current == _root || creationMode == RuntimeMountCreationMode.DeferredRoot)
                    current.CallDeferred(Node.MethodName.AddChild, next);
                else
                    current.AddChild(next);

                if (current == _root)
                    _pendingRootChildren[segment] = next;
            }

            current = next;
        }

        return current;
    }

    private static bool IsValid(Node? node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();
    }

    private static RuntimeMountStatus GetStatus(Node? node)
    {
        if (!IsValid(node))
            return RuntimeMountStatus.Invalid;

        return node!.IsInsideTree()
            ? RuntimeMountStatus.InTree
            : RuntimeMountStatus.Pending;
    }

    private static string BuildAbsolutePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return RuntimeRootPath;

        return $"{RuntimeRootPath}/{relativePath.Trim('/')}";
    }

    private sealed record RuntimeMountRecord(RuntimeMountManifestEntry Manifest, Node Node);
}
