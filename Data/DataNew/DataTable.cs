using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace slime.data;

/// <summary>
/// DataNew DTO 兼容读取工具。
/// <para>表由 C# DTO 类型表示，例如 EnemyData / AbilityData；行只来自 DataOS runtime snapshot。</para>
/// <para>首次访问某个表类型时建立缓存，之后按 Name 字典读取。</para>
/// </summary>
public static class DataTable
{
    private static readonly Log _log = new(nameof(DataTable));

    /// <summary>
    /// 获取指定表类型的全部数据行。
    /// </summary>
    /// <typeparam name="T">表类型，例如 EnemyData。</typeparam>
    /// <returns>全部数据行，只读列表。</returns>
    public static IReadOnlyList<T> GetAll<T>() where T : class
    {
        return Cache<T>.All;
    }

    /// <summary>
    /// 按数据 Name 获取数据行。
    /// <para>找不到时记录 Error 并返回 null，由调用方使用 ?? 处理默认值或中断逻辑。</para>
    /// </summary>
    /// <typeparam name="T">表类型，例如 EnemyData。</typeparam>
    /// <param name="name">数据 Name，例如“鱼人”。</param>
    /// <returns>匹配的数据行；找不到返回 null。</returns>
    public static T? GetByName<T>(string name) where T : class
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _log.Error($"{typeof(T).Name} 查询 Name 为空");
            return null;
        }

        if (Cache<T>.ByName.TryGetValue(name, out var value))
        {
            return value;
        }

        _log.Error($"{typeof(T).Name} 未找到 Name='{name}' 的数据");
        return null;
    }

    /// <summary>
    /// 按 Name 获取必需数据行；缺失时抛出明确异常，供命名快捷属性使用。
    /// </summary>
    public static T GetRequiredByName<T>(string name) where T : class
    {
        return GetByName<T>(name)
            ?? throw new InvalidOperationException($"{typeof(T).Name} 缺少必需数据: Name='{name}'");
    }

    /// <summary>
    /// 泛型缓存桶：每个表类型 T 首次访问时自动构建并缓存全部数据行和按 Name 索引。
    /// <para>利用 C# 泛型静态字段的每类型独立性，无需手动管理缓存字典。</para>
    /// </summary>
    private static class Cache<T> where T : class
    {
        /// <summary>当前表类型的全部数据行（按类型全名 + 字段声明顺序排列）。</summary>
        public static readonly IReadOnlyList<T> All = BuildAll();

        /// <summary>以 Name 为键的索引字典（序数字符串比较）。</summary>
        public static readonly Dictionary<string, T> ByName = BuildByName(All);

        /// <summary>
        /// 从 DataOS runtime snapshot 构建数据行。
        /// </summary>
        private static IReadOnlyList<T> BuildAll()
        {
            return RuntimeDataSnapshot.GetAll<T>();
        }

        /// <summary>
        /// 根据全部数据行构建 Name 索引字典。
        /// <para>跳过 Name 为空的行并记录错误；重复 Name 也跳过并记录错误。</para>
        /// </summary>
        private static Dictionary<string, T> BuildByName(IReadOnlyList<T> all)
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal); // 序数字符串比较，保证一致性
            for (int i = 0; i < all.Count; i++)
            {
                var data = all[i];
                var name = ResolveName(data); // 按 Name > SystemId > PresetName 优先级解析
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException($"{typeof(T).Name} 存在 Name/SystemId/PresetName 为空的数据: {data.GetType().Name}");
                }

                if (result.ContainsKey(name))
                {
                    throw new InvalidOperationException($"{typeof(T).Name} 存在重复 Name='{name}'，请保证同一张表内 Name 唯一");
                }

                result.Add(name, data);
            }

            return result;
        }

        /// <summary>
        /// 解析数据行的名称标识。
        /// <para>按优先级依次尝试：DataKey.Name → SystemId → PresetName，返回首个非空属性值。</para>
        /// </summary>
        private static string? ResolveName(T data)
        {
            var type = data.GetType();
            var property = type.GetProperty(DataKey.Name, BindingFlags.Public | BindingFlags.Instance) // 优先使用 DataKey.Name
                ?? type.GetProperty("SystemId", BindingFlags.Public | BindingFlags.Instance) // 兼容旧字段 SystemId
                ?? type.GetProperty("PresetName", BindingFlags.Public | BindingFlags.Instance); // 兼容旧字段 PresetName
            return property?.GetValue(data) as string;
        }
    }
}
