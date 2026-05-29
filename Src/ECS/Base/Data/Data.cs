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
        _owner = owner;
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
            _runtimeStorage.Changed -= OnRuntimeDataChanged;
        }

        _owner = owner;
        _runtimeStorage = new DataRuntimeStorage(catalog, this); // descriptor-first 运行时存储
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

    /// <summary>
    /// 当任何数据发生变化时触发的全局事件
    /// 参数依次为：键名 (Key), 旧值 (OldValue), 新值 (NewValue)
    /// </summary>
    // 事件监听移交给 Entity.Events


    // ================= 基础数据操作 =================

    /// <summary>
    /// 设置基础值（自动应用元数据约束）
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="key">键名</param>
    /// <param name="value">要设置的新值</param>
    /// <returns>如果值发生了实际变化则返回 true</returns>
    public bool Set<T>(string key, T value)
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
    public T Get<T>(string key, object? defaultValue = null)
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
        return Set(key.StableKey, value);
    }

    /// <summary>
    /// 通过类型安全句柄读取字段值。
    /// </summary>
    /// <typeparam name="T">字段值类型。</typeparam>
    /// <param name="key">descriptor stable key 句柄。</param>
    public T Get<T>(DataKey<T> key)
    {
        return Get<T>(key.StableKey);
    }

    /// <summary>
    /// 内部入口：按 descriptor definition 写入未泛型化值。
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
    /// 内部入口：按 stable key 写入未泛型化值。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    /// <param name="value">要写入的值。</param>
    /// <param name="source">写入来源。</param>
    public bool SetUntyped(string stableKey, object? value, DataWriteSource source = DataWriteSource.Loader)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.SetUntyped(stableKey, value, source);
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
    public T GetBase<T>(string key, T defaultValue = default!)
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
    public bool TryGetValue<T>(string key, out T value)
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
    public bool Has(string key)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.HasDefinition(key);
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 移除指定的数据项
    /// </summary>
    public bool Remove(string key)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.Remove(key);
        }

        throw CreateUnboundDataException(key);
    }

    // ================= 算术运算 =================

    /// <summary>
    /// 对现有数值执行加法操作
    /// </summary>
    public void Add<T>(string key, T delta) where T : INumber<T>
    {
        var current = GetBase<T>(key, T.Zero);
        Set(key, current + delta);
    }

    /// <summary>
    /// 对现有数值执行乘法操作
    /// </summary>
    public void Multiply<T>(string key, T factor) where T : INumber<T>
    {
        var current = GetBase<T>(key, T.Zero);
        Set(key, current * factor);
    }

    /// <summary>
    /// 批量设置多个数据项
    /// </summary>
    public void SetMultiple(Dictionary<string, object> properties)
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
    public void AddModifier(string key, DataModifier modifier)
    {
        TryAddModifier(key, modifier);
    }

    /// <summary>
    /// 尝试添加修改器，并返回是否成功应用。
    /// </summary>
    /// <param name="key">目标数据键</param>
    /// <param name="modifier">修改器实例</param>
    public bool TryAddModifier(string key, DataModifier modifier)
    {
        if (_runtimeStorage != null)
        {
            return _runtimeStorage.AddModifier(key, modifier);
        }

        throw CreateUnboundDataException(key);
    }

    /// <summary>
    /// 移除修改器
    /// </summary>
    /// <param name="key">目标数据键</param>
    /// <param name="modifierId">修改器 ID</param>
    public void RemoveModifier(string key, string modifierId)
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
    /// 根据来源对象移除所有匹配的修改器（跨所有数据键）
    /// 常用于：卸载装备、移除 Buff
    /// </summary>
    /// <param name="source">来源对象（如 ItemEntity）</param>
    public void RemoveModifiersBySource(object source)
    {
        if (_runtimeStorage != null)
        {
            _runtimeStorage.RemoveModifiersBySource(source);
            return;
        }

        throw CreateUnboundDataException("*");
    }

    /// <summary>
    /// 将另一个 Data 容器的数据转换为修改器应用到当前容器
    /// 常用于：装备属性应用到角色
    /// </summary>
    /// <param name="sourceData">源数据容器</param>
    /// <param name="sourceEntity">来源实体（作为修改器 Source）</param>
    public void ApplyDataAsModifiers(Data sourceData, object sourceEntity)
    {
        if (sourceData == null || sourceEntity == null) return;

        var allData = sourceData.GetAll();
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
                    source: sourceEntity
                );
                AddModifier(kvp.Key, modifier);
            }
        }
    }

    /// <summary>
    /// 检查是否拥有特定修改器
    /// </summary>
    public bool HasModifier(string key, string modifierId)
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
    public List<DataModifier> GetModifiers(string key)
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
    public void ClearModifiers(string key)
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
    // 请使用 Entity.Events.On(GameEventType.Data.PropertyChanged, ...)
    // 数据变更事件负载类型: (string Key, object? OldValue, object? NewValue)


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
    /// 获取当前所有基础数据的副本
    /// </summary>
    public Dictionary<string, object> GetAll()
    {
        if (_runtimeStorage != null)
        {
            var runtimeValues = _runtimeStorage.GetAllValues();
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
            // 通过 Entity 事件总线广播数据变更
            // 下游监听示例: 
            // entity.Events.On<GameEventType.Data.PropertyChangedEvent>(GameEventType.Data.PropertyChanged, evt => ...);
            _owner.Events.Emit(new GameEventType.Data.PropertyChanged(key, oldValue, newValue));
        }
    }

    private void OnRuntimeDataChanged(DataChangeRecord change)
    {
        NotifyChanged(change.StableKey, change.OldValue, change.NewValue);
    }

    private static InvalidOperationException CreateUnboundDataException(string key)
    {
        return new InvalidOperationException($"Data 容器未绑定 DataDefinitionCatalog，拒绝访问字段：{key}");
    }

}
