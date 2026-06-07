using Godot;
using System;

/// <summary>
/// Runtime mount 默认进程入口。
/// </summary>
public static class RuntimeMountService
{
    private static RuntimeMountRegistry? _current;

    public static bool IsInitialized => _current != null;

    public static RuntimeMountRegistry Current
    {
        get
        {
            if (_current == null)
                throw new InvalidOperationException("RuntimeMountService 尚未初始化。请先调用 Initialize(root)。");

            return _current;
        }
    }

    public static void Initialize(Node root)
    {
        _current = new RuntimeMountRegistry(root);
    }

    public static Node GetOrCreate(RuntimeMountId id)
    {
        return Current.GetOrCreate(id);
    }

    public static Node GetOrCreate(RuntimeMountId id, string relativePath, string owner, string usage)
    {
        return Current.GetOrCreate(id, relativePath, owner, usage);
    }

    public static RuntimeMountSnapshot GetSnapshot()
    {
        return Current.GetSnapshot();
    }
}
