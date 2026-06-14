using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>
/// 增强版动态数据容器 - 统一数据管理系统
/// 支持：强类型访问、元数据约束、修改器系统、计算数据、事件监听
/// 
/// 【核心理念】
/// Data 是唯一数据源，所有数据（普通数据、可修改数据、计算数据）统一从 Data 容器访问。
/// 
/// 【公式】
/// 最终值 = (基础值 + Σ加法修改器) × Π乘法修改器
/// </summary>
public class Data
{
    private static readonly Log _log = new(nameof(Data), LogLevel.Warning);

    private IEntity? _owner;
    private DataRuntimeStorage? _runtimeStorage;

    public Data(IEntity? owner = null)
    {
        BindRuntimeCatalog(owner, DataRuntimeBootstrap.Default.Catalog);
    }

    /// <summary>
    /// 使用 descriptor catalog 创建无 owner 的 Data 容器。
    /// </summary>
    /// <param name="catalog">字段定义 catalog。</param>
    public Data(DataDefinitionCatalog catalog)
        : this(null, catalog)
    {
    }

    /// <summary>
    /// 使用 descriptor catalog 创建 Data 容器。
    /// </summary>
    /// <param name="owner">归属实体，用于 Entity.Events 数据变更通知。</param>
    /// <param name="catalog">字段定义 catalog。</param>
    public Data(IEntity? owner, DataDefinitionCatalog catalog)
    {
        BindRuntimeCatalog(owner, catalog);
    }

    /// <summary>
    /// 将现有 Data 容器切换为 descriptor-first runtime storage。
    /// </summary>
    /// <param name="owner">归属实体，用于 Entity.Events 数据变更通知。</param>
    /// <param name="catalog">字段定义 catalog。</param>
    public void BindRuntimeCatalog(IEntity? owner, DataDefinitionCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (_runtimeStorage != null)
        {
            _runtimeStorage.TypedChanged -= OnRuntimeTypedDataChanged;
            _runtimeStorage.Changed -= OnRuntimeDataChanged;
        }

        _owner = owner;
        _runtimeStorage = new DataRuntimeStorage(catalog, this); // descriptor-first 运行时存储
        _runtimeStorage.TypedChanged += OnRuntimeTypedDataChanged;
        _runtimeStorage.Changed += OnRuntimeDataChanged;
    }

    /// <summary>
    /// 将现有 Data 容器切换为 descriptor-first runtime storage，并保留当前 owner。
    /// </summary>
    /// <param name="catalog">字段定义 catalog。</param>
    public void BindRuntimeCatalog(DataDefinitionCatalog catalog)
    {
        BindRuntimeCatalog(_owner, catalog);
    }

    // ================= 基础数据操作 =================

    /// <summary>
    /// 设置基础值（自动应用元数据约束）
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="key">键名</param>
    /// <param name="value">要设置的新值</param>
    /// <returns>如果值发生了实际变化则返回 true</returns>
    internal bool Set<T>(string key, T value)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.SetUntyped(key, value, DataWriteSource.Runtime);
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 获取最终值（泛型访问，编译期类型安全）
    /// 核心逻辑：统一处理计算数据、修改器和基础值
    /// </summary>
    /// <typeparam name="T">期望获取的类型</typeparam>
    /// <param name="key">键名</param>
    /// <param name="defaultValue">兼容旧签名的占位参数；实际默认值来自 DataDefinitionCatalog。</param>
    /// <returns>最终计算值</returns>
    internal T Get<T>(string key, object? defaultValue = null)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.Get<T>(key);
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 通过类型安全句柄设置字段值。
    /// </summary>
    /// <typeparam name="T">字段值类型。</typeparam>
    /// <param name="key">descriptor stable key 句柄。</param>
    /// <param name="value">要设置的新值。</param>
    public bool Set<T>(DataKey<T> key, T value)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.Set(key, value);
        }

        throw CreateUnboundDataException(key.StableKey);
    }

    /// <summary>
    /// 通过类型安全句柄设置字段值，并输出结构化诊断。
    /// </summary>
    /// <typeparam name="T">字段值类型。</typeparam>
    /// <param name="key">descriptor stable key 句柄。</param>
    /// <param name="value">要设置的新值。</param>
    /// <param name="report">写入诊断报告。</param>
    public bool TrySet<T>(DataKey<T> key, T value, out DataWriteReport report)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.TrySet(key, value, out report);
        }

        throw CreateUnboundDataException(key.StableKey);
    }

    /// <summary>
    /// 通过类型安全句柄以 System 来源写入字段值。
    /// 用于 system_only / runtime_only 字段，避免绕过 DataKey&lt;T&gt;。
    /// </summary>
    public bool SetSystem<T>(DataKey<T> key, T value)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.Set(key, value, DataWriteSource.System);
        }

        throw CreateUnboundDataException(key.StableKey);
    }

    /// <summary>
    /// 通过类型安全句柄以 System 来源写入字段值，并输出结构化诊断。
    /// </summary>
    public bool TrySetSystem<T>(DataKey<T> key, T value, out DataWriteReport report)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.TrySet(key, value, out report, DataWriteSource.System);
        }

        throw CreateUnboundDataException(key.StableKey);
    }

    /// <summary>
    /// 通过类型安全句柄以 Debug 来源写入字段值，并输出结构化诊断。
    /// 仅用于调试工具，不作为普通业务写入入口。
    /// </summary>
    public bool TrySetDebug<T>(DataKey<T> key, T value, out DataWriteReport report)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.TrySet(key, value, out report, DataWriteSource.Debug);
        }

        throw CreateUnboundDataException(key.StableKey);
    }

    /// <summary>
    /// 通过类型安全句柄读取字段值。
    /// </summary>
    /// <typeparam name="T">字段值类型。</typeparam>
    /// <param name="key">descriptor stable key 句柄。</param>
    public T Get<T>(DataKey<T> key)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.Get(key);
        }

        throw CreateUnboundDataException(key.StableKey);
    }

    /// <summary>
    /// 通过类型安全句柄读取字段值；仅旧调用点需要显式覆盖默认值时使用。
    /// </summary>
    /// <typeparam name="T">字段值类型。</typeparam>
    /// <param name="key">descriptor stable key 句柄。</param>
    /// <param name="defaultValue">字段未显式写入且 descriptor default 不适用时的回退值。</param>
    public T Get<T>(DataKey<T> key, T defaultValue)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.HasValue(key.StableKey)
                ? _runtimeStorage.Get(key)
                : defaultValue;
        }

        throw CreateUnboundDataException(key.StableKey);
    }

    /// <summary>
    /// 边界入口：按 descriptor definition 写入未泛型化值。
    /// 仅用于 snapshot loader、debug 和 TestSystem；业务热路径应使用 DataKey&lt;T&gt;。
    /// </summary>
    /// <param name="definition">字段 descriptor 定义。</param>
    /// <param name="value">要写入的值。</param>
    /// <param name="source">写入来源。</param>
    public bool SetUntyped(DataDefinition definition, object? value, DataWriteSource source = DataWriteSource.Loader)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.SetUntyped(definition, value, source);
        }

        throw CreateUnboundDataException(definition.StableKey);
    }

    /// <summary>
    /// 边界入口：按 descriptor definition 写入未泛型化值，并输出结构化诊断。
    /// 值类型进入 object 参数会装箱；业务代码不要绕过 DataKey&lt;T&gt; 调用。
    /// </summary>
    /// <param name="definition">字段 descriptor 定义。</param>
    /// <param name="value">要写入的值。</param>
    /// <param name="source">写入来源。</param>
    /// <param name="report">写入诊断报告。</param>
    public bool TrySetUntyped(DataDefinition definition, object? value, DataWriteSource source, out DataWriteReport report)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.TrySetUntyped(definition, value, source, out report);
        }

        throw CreateUnboundDataException(definition.StableKey);
    }

    /// <summary>
    /// 边界入口：按 stable key 写入未泛型化值。
    /// 仅用于旧测试和调试胶水；业务热路径应使用 generated DataKey&lt;T&gt;。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    /// <param name="value">要写入的值。</param>
    /// <param name="source">写入来源。</param>
    internal bool SetUntyped(string stableKey, object? value, DataWriteSource source = DataWriteSource.Loader)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.SetUntyped(stableKey, value, source);
        }

        throw CreateUnboundDataException(stableKey);
    }

    /// <summary>
    /// 边界入口：按 stable key 写入未泛型化值，并输出结构化诊断。
    /// 值类型进入 object 参数会装箱；新业务代码不要新增此调用。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    /// <param name="value">要写入的值。</param>
    /// <param name="source">写入来源。</param>
    /// <param name="report">写入诊断报告。</param>
    internal bool TrySetUntyped(string stableKey, object? value, DataWriteSource source, out DataWriteReport report)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.TrySetUntyped(stableKey, value, source, out report);
        }

        throw CreateUnboundDataException(stableKey);
    }

    /// <summary>
    /// 获取基础值（不应用修改器，用于计算数据内部调用）
    /// </summary>
    /// <typeparam name="T">期望获取的类型</typeparam>
    /// <param name="key">键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>基础值</returns>
    internal T GetBase<T>(string key, T defaultValue = default!)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.HasValue(key)
                ? _runtimeStorage.Get<T>(key)
                : defaultValue;
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 尝试获取数据值
    /// </summary>
    internal bool TryGetValue<T>(string key, out T value)
    {
        if (_runtimeStorage != null)
        {
            if (_runtimeStorage.HasValue(key))
            {
                value = _runtimeStorage.Get<T>(key);
                return true;
            }

            value = default!;
            return false;
        }

        value = default!;
        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 检查是否存在指定的键名
    /// </summary>
    internal bool Has(string key)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.HasDefinition(key);
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 通过类型安全句柄检查字段是否存在于 catalog。
    /// </summary>
    public bool Has<T>(DataKey<T> key)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.HasDefinition(key.StableKey);
        }

        throw CreateUnboundDataException(key.StableKey);
    }

    /// <summary>
    /// 移除指定的数据项
    /// </summary>
    internal bool Remove(string key)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.Remove(key);
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 通过类型安全句柄移除字段运行时值和修改器。
    /// </summary>
    public bool Remove<T>(DataKey<T> key)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.Remove(key.StableKey);
        }

        throw CreateUnboundDataException(key.StableKey);
    }

    // ================= 算术运算 =================

    /// <summary>
    /// 对现有数值执行加法操作
    /// </summary>
    internal void Add<T>(string key, T delta) where T : INumber<T>
    {
        var current = GetBase<T>(key, T.Zero);
        Set(key, current + delta);
    }

    /// <summary>
    /// 通过类型安全句柄对现有数值执行加法操作。
    /// </summary>
    public void Add<T>(DataKey<T> key, T delta) where T : INumber<T>
    {
        var current = Get<T>(key, T.Zero);
        Set(key, current + delta);
    }

    /// <summary>
    /// 对现有数值执行乘法操作
    /// </summary>
    internal void Multiply<T>(string key, T factor) where T : INumber<T>
    {
        var current = GetBase<T>(key, T.Zero);
        Set(key, current * factor);
    }

    /// <summary>
    /// 批量设置多个数据项
    /// </summary>
    internal void SetMultiple(Dictionary<string, object> properties)
    {
        foreach (var kvp in properties)
        {
            Set(kvp.Key, kvp.Value);
        }
    }

    // ================= 修改器系统 =================

    /// <summary>
    /// 添加修改器
    /// </summary>
    /// <param name="key">目标数据键</param>
    /// <param name="modifier">修改器实例</param>
    internal void AddModifier(string key, DataModifier modifier)
    {
        TryAddModifier(key, modifier);
    }

    /// <summary>
    /// 尝试添加修改器，并返回是否成功应用。
    /// </summary>
    /// <param name="key">目标数据键</param>
    /// <param name="modifier">修改器实例</param>
    internal bool TryAddModifier(string key, DataModifier modifier)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.AddModifier(key, modifier);
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 通过类型安全句柄添加字段修改器。
    /// </summary>
    public void AddModifier<T>(DataKey<T> key, DataModifier modifier)
    {
        TryAddModifier(key, modifier);
    }

    /// <summary>
    /// 通过类型安全句柄尝试添加字段修改器。
    /// </summary>
    public bool TryAddModifier<T>(DataKey<T> key, DataModifier modifier)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.AddModifier(key.StableKey, modifier);
        }

        throw CreateUnboundDataException(key.StableKey);
    }

    /// <summary>
    /// 通过类型安全句柄尝试添加字段修改器，并输出结构化诊断。
    /// </summary>
    public bool TryAddModifier<T>(DataKey<T> key, DataModifier modifier, out DataWriteReport report)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.TryAddModifier(key.StableKey, modifier, DataWriteSource.Runtime, out report);
        }

        throw CreateUnboundDataException(key.StableKey);
    }

    /// <summary>
    /// 移除修改器
    /// </summary>
    /// <param name="key">目标数据键</param>
    /// <param name="modifierId">修改器 ID</param>
    internal void RemoveModifier(string key, string modifierId)
    {
        if (_runtimeStorage != null)
        {
            _runtimeStorage.RemoveModifier(key, modifierId);
            return;
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 根据 ID 移除所有匹配的修改器（跨所有数据键）
    /// </summary>
    /// <param name="modifierId">修改器 ID</param>
    public void RemoveModifierById(string modifierId)
    {
        if (_runtimeStorage != null)
        {
            _runtimeStorage.RemoveModifierById(modifierId);
            return;
        }

        throw CreateUnboundDataException("*");
    }

    /// <summary>
    /// 根据稳定来源移除所有匹配的修改器（跨所有数据键）。
    /// 常用于卸载装备、移除 Buff 或 Feature 回滚。
    /// </summary>
    public void RemoveModifiersBySource(DataModifierSource source)
    {
        if (_runtimeStorage != null)
        {
            _runtimeStorage.RemoveModifiersBySource(source);
            return;
        }

        throw CreateUnboundDataException("*");
    }

    /// <summary>
    /// 将另一个 Data 容器的数据转换为修改器应用到当前容器。
    /// 常用于：装备属性应用到角色。
    /// </summary>
    /// <param name="sourceData">源数据容器。</param>
    /// <param name="sourceEntity">来源实体（作为修改器 Source）。</param>
    public void ApplyDataAsModifiers(Data sourceData, IEntity sourceEntity)
    {
        if (sourceData == null || sourceEntity == null) return;

        var allData = sourceData.GetDiagnosticSnapshot();
        var sourceId = DataModifierSource.FromEntity(sourceEntity);
        foreach (var kvp in allData)
        {
            // 仅处理数值类型
            if (kvp.Value is float || kvp.Value is int)
            {
                float value = Convert.ToSingle(kvp.Value);
                if (Math.Abs(value) < float.Epsilon) continue;

                var modifier = new DataModifier(
                    ModifierType.Additive,
                    value,
                    priority: 0,
                    id: null,
                    sourceId: sourceId
                );
                AddModifier(kvp.Key, modifier);
            }
        }
    }

    /// <summary>
    /// 检查是否拥有特定修改器
    /// </summary>
    internal bool HasModifier(string key, string modifierId)
    {
        if (_runtimeStorage != null)
        {
            var runtimeModifiers = _runtimeStorage.GetModifiers(key);
            for (var i = 0; i < runtimeModifiers.Count; i++)
            {
                if (runtimeModifiers[i].Id == modifierId)
                {
                    return true;
                }
            }

            return false;
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 获取指定数据键的所有修改器副本
    /// 返回副本是为了确保迭代安全性，防止在遍历时修改器列表发生变动导致异常
    /// </summary>
    internal List<DataModifier> GetModifiers(string key)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.GetModifiers(key);
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 清除指定数据键的所有修改器
    /// </summary>
    internal void ClearModifiers(string key)
    {
        if (_runtimeStorage != null)
        {
            _runtimeStorage.ClearModifiers(key);
            return;
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 清除所有修改器
    /// </summary>
    public void ClearAllModifiers()
    {
        if (_runtimeStorage != null)
        {
            _runtimeStorage.ClearAllModifiers();
            return;
        }

        throw CreateUnboundDataException("*");
    }

    // ================= 事件监听 (已移除) =================
    // 业务代码请使用 Entity.Events.On<GameEventType.Data.Changed<T>>(...)。
    // PropertyChanged 只保留给 TestSystem/debug diagnostic 边界。


    // ================= 工具方法 =================

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        if (_runtimeStorage != null)
        {
            _runtimeStorage.Clear();
            return;
        }

        throw CreateUnboundDataException("*");
    }

    /// <summary>
    /// 获取当前所有基础数据的 diagnostic 副本。
    /// 仅供 TestSystem、debug 和 migration 使用；返回 Dictionary 会让值类型装箱。
    /// </summary>
    public Dictionary<string, object> GetDiagnosticSnapshot()
    {
        if (_runtimeStorage != null)
        {
            var runtimeValues = _runtimeStorage.GetAllValuesForDiagnostics();
            var result = new Dictionary<string, object>(runtimeValues.Count);
            foreach (var pair in runtimeValues)
            {
                if (pair.Value != null)
                {
                    result[pair.Key] = pair.Value;
                }
            }

            return result;
        }

        throw CreateUnboundDataException("*");
    }

    /// <summary>
    /// 兼容边界：获取当前所有基础数据的副本。
    /// 新调用请使用 GetDiagnosticSnapshot，明确这是 boxed diagnostic dump。
    /// </summary>
    [Obsolete("GetAll 是 diagnostic 兼容包装；请使用 GetDiagnosticSnapshot。")]
    public Dictionary<string, object> GetAll()
    {
        return GetDiagnosticSnapshot();
    }

    /// <summary>
    /// 按数据分类批量重置为默认值。
    /// 当前 DataDefinitionCatalog 不再提供旧 DataMeta 分类重置路径。
    /// </summary>
    /// <param name="category">数据分类枚举值（如 DataCategory_Movement.Orbit）</param>
    public void ResetByCategory(Enum category)
    {
        throw CreateUnboundDataException(category.ToString() ?? "*");
    }

    /// <summary>
    /// 按多个数据分类批量重置为默认值
    /// </summary>
    /// <param name="categories">要重置的分类列表</param>
    public void ResetByCategories(params Enum[] categories)
    {
        for (int i = 0; i < categories.Length; i++)
        {
            ResetByCategory(categories[i]);
        }
    }

    /// <summary>
    /// 重置数据容器（用于对象池复用）
    /// </summary>
    public void Reset()
    {
        if (_runtimeStorage != null)
        {
            _runtimeStorage.Clear();
            _log.Debug("Data 容器已重置");
            return;
        }

        throw CreateUnboundDataException("*");
    }

    // ================= 私有方法 =================

    /// <summary>
    /// 触发变更通知
    /// </summary>
    private void NotifyChanged(string key, object? oldValue, object? newValue)
    {
        if (_owner != null)
        {
            // 通过 Entity 事件总线广播 diagnostic 数据变更。
            // 业务监听由 OnRuntimeTypedDataChanged 发出 Changed<T>。
            _owner.Events.Emit(new GameEventType.Data.PropertyChanged(key, oldValue, newValue));
        }
    }

    private void OnRuntimeDataChanged(DataChangeRecord change)
    {
        NotifyChanged(change.StableKey, change.OldValue, change.NewValue);
    }

    private void OnRuntimeTypedDataChanged(IDataChangeRecord change)
    {
        if (_owner == null)
        {
            return;
        }

        change.EmitTyped(_owner.Events);
    }

    private static InvalidOperationException CreateUnboundDataException(string key)
    {
        return new InvalidOperationException($"Data 容器未绑定 DataDefinitionCatalog，拒绝访问字段：{key}");
    }

}
