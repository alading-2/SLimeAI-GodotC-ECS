using System;
using System.Collections.Generic;

/// <summary>
/// Runtime 挂载点 manifest。
/// </summary>
public static class RuntimeMountManifest
{
    private static readonly Dictionary<RuntimeMountId, RuntimeMountManifestEntry> Entries = new()
    {
        [RuntimeMountIds.EntityRuntimeRoot] = new RuntimeMountManifestEntry(
            RuntimeMountIds.EntityRuntimeRoot,
            "ECS/Entity",
            RuntimeMountCreationMode.DeferredRoot,
            "Runtime.Entity",
            "非对象池 Entity 默认挂载点"),
        [RuntimeMountIds.PoolRuntimeRoot] = new RuntimeMountManifestEntry(
            RuntimeMountIds.PoolRuntimeRoot,
            "ECS/Pool",
            RuntimeMountCreationMode.Immediate,
            "Tools.ObjectPool",
            "对象池节点与 parking root"),
        [RuntimeMountIds.UiRuntimeRoot] = new RuntimeMountManifestEntry(
            RuntimeMountIds.UiRuntimeRoot,
            "UI",
            RuntimeMountCreationMode.Immediate,
            "UI",
            "框架 UI 默认挂载点"),
        [RuntimeMountIds.ToolRuntimeRoot] = new RuntimeMountManifestEntry(
            RuntimeMountIds.ToolRuntimeRoot,
            "Tool",
            RuntimeMountCreationMode.Immediate,
            "Runtime.Tool",
            "工具运行时节点默认挂载点"),
        [RuntimeMountIds.SystemRuntimeRoot] = new RuntimeMountManifestEntry(
            RuntimeMountIds.SystemRuntimeRoot,
            "System",
            RuntimeMountCreationMode.Immediate,
            "Runtime.System",
            "系统宿主节点默认挂载点")
    };

    public static RuntimeMountManifestEntry Get(RuntimeMountId id)
    {
        if (Entries.TryGetValue(id, out var entry))
            return entry;

        throw new InvalidOperationException($"未声明 Runtime mount id: {id.Value}");
    }
}

/// <summary>
/// Runtime 挂载点稳定 id 集合。
/// </summary>
public static class RuntimeMountIds
{
    public static readonly RuntimeMountId EntityRuntimeRoot = RuntimeMountId.From("runtime.entity.root");
    public static readonly RuntimeMountId PoolRuntimeRoot = RuntimeMountId.From("runtime.pool.root");
    public static readonly RuntimeMountId UiRuntimeRoot = RuntimeMountId.From("runtime.ui.root");
    public static readonly RuntimeMountId ToolRuntimeRoot = RuntimeMountId.From("runtime.tool.root");
    public static readonly RuntimeMountId SystemRuntimeRoot = RuntimeMountId.From("runtime.system.root");

    public static RuntimeMountId EntityType(string typeName) => RuntimeMountId.From($"runtime.entity.{typeName}");
    public static RuntimeMountId Pool(string poolName) => RuntimeMountId.From($"runtime.pool.{poolName}");
}
