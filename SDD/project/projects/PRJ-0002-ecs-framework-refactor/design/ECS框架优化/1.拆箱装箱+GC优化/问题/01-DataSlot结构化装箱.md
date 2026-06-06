---
title: DataSlot结构化装箱问题
status: 初稿
created: 2026-06-06
tags: [ecs, boxing, data, critical]
description: DataSlot用object?存储值类型导致的系统性装箱拆箱问题分析与解决方案
---

# DataSlot结构化装箱问题

> 严重度：🔴 CRITICAL
> 影响范围：所有实体的每次数据读写
> 预估影响：每帧2000+次装箱+2000+次拆箱 = 32KB/帧 = 1.92MB/秒

## 问题描述

DataSlot是Data系统的底层存储单元。它用 `object?` 存储所有值，导致每次读写值类型（float, int, bool, Vector2等）都要装箱和拆箱。

## 涉及代码（10处）

### 1. DataSlot.Value 存储为 object?

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第124行
```csharp
public object? Value { get; private set; }
```
**问题：** 所有float、int、bool、Vector2值写入DataSlot时都被装箱为object引用。

### 2. DataSlot.SetValue 接受 object?

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第139行
```csharp
public bool SetValue(object? value)
```
**问题：** 类型化的Set<T>方法将T值装箱为object?后传入。

### 3. DataSlot.GetEffectiveValue 返回 object?

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第129行
```csharp
public object? GetEffectiveValue()
```
**问题：** 返回已装箱的值类型。

### 4. DataSlot.ConvertNumericToDefinitionType 显式装箱

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第285-294行
```csharp
private object ConvertNumericToDefinitionType(double value)
{
    return Definition.ValueType switch
    {
        DataValueType.Int => (object)(int)value,
        DataValueType.Float => (object)(float)value,
        DataValueType.Double => (object)value,
        _ => (object)value
    };
}
```
**问题：** 每次修改器应用都触发显式装箱。

### 5. DataSlot.ApplyModifiers 拆箱→计算→重新装箱

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第231行
```csharp
private object? ApplyModifiers(object? baseValue)
```
**问题：** 拆箱数值→执行计算→将结果重新装箱。

### 6. DataSlot.TryGetNumeric 拆箱

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第296-313行
```csharp
private static bool TryGetNumeric(object value, out double numericValue)
```
**问题：** 通过模式匹配拆箱int/float/double。

### 7. DataRuntimeStorage.Get<T> 拆箱路径

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第1139-1150行
```csharp
public T Get<T>(string stableKey)
{
    var value = GetOrCreateSlot(definition).GetEffectiveValue(); // object?
    return (T)DataValueConverter.ConvertForRead(value, typeof(T), definition.ValueType)!; // unbox
}
```
**问题：** 每次读取值类型数据都拆箱。

### 8. DataRuntimeStorage.SetUntyped 装箱路径

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第1184行
```csharp
public bool SetUntyped(string stableKey, object? value, DataWriteSource source)
```
**问题：** Set<T>将值传给TrySetUntyped(object?)，装箱。

### 9. DataValueConverter.ConvertXxx 返回 object

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第690-731行
```csharp
private static object ConvertInt(object rawValue) { return rawValue switch { int v => v, ... }; }
private static object ConvertFloat(object rawValue) { return rawValue switch { float v => v, ... }; }
private static object ConvertBool(object rawValue) { ... }
private static object ConvertVector2(object rawValue) { ... }
```
**问题：** 返回object导致值类型装箱。每次数据写入操作都调用。

### 10. TryReadVector2 使用反射

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第1008-1041行
```csharp
private static bool TryReadVector2(object rawValue, out float x, out float y)
{
    var type = rawValue.GetType();
    var xMember = type.GetProperty("X") ?? (object?)type.GetField("X");
    object? rawX = xMember switch
    {
        PropertyInfo property => property.GetValue(rawValue),
        FieldInfo field => field.GetValue(rawValue),
        ...
    };
}
```
**问题：** 使用反射读取Godot.Vector2字段。每次Vector2有修改器应用时触发。

## 调用频率估算

| 调用路径 | 每实体每帧 | 装箱次数 |
|---------|-----------|--------|
| RecoverySystem读HP | 1次Get<float> | 1次拆箱 |
| RecoverySystem写HP | 1次Set<float> | 1次装箱 |
| DamageTool读攻击力 | 1次Get<float> | 1次拆箱 |
| AI决策读各种属性 | 3-5次Get<> | 3-5次拆箱 |
| HealthBarUI读HP | 1次Get<float> | 1次拆箱 |
| 移动系统读速度 | 1次Get<float> | 1次拆箱 |
| PropertyChanged事件 | 每次变更 | 2次装箱(old+new) |
| 修改器应用 | 每次有修改器 | 拆箱+装箱 |

**每实体每帧：** ~10-15次装箱+拆箱
**100个实体：** ~1000-1500次装箱+拆箱/帧
**200个实体（战斗场景）：** ~2000-3000次装箱+拆箱/帧

## 解决方案

### 方案A：类型分区存储（推荐）

```csharp
// 当前（每帧2000+次装箱拆箱）
private object? _value;

// 优化后（零装箱热路径）
internal sealed class DataRuntimeStorage
{
    private readonly Dictionary<string, float> _floatSlots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _intSlots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _boolSlots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _complexSlots = new(StringComparer.Ordinal);

    public T Get<T>(string stableKey)
    {
        var definition = _catalog.GetRequired(stableKey);
        return definition.ValueType switch
        {
            DataValueType.Float => (T)(object)_floatSlots.GetValueOrDefault(stableKey),
            DataValueType.Int => (T)(object)_intSlots.GetValueOrDefault(stableKey),
            DataValueType.Bool => (T)(object)_boolSlots.GetValueOrDefault(stableKey),
            _ => (T)_complexSlots.GetValueOrDefault(stableKey)!
        };
    }
}
```

**注意：** 方案A的Get<T>仍有(T)(object)转换。要完全零装箱需要方案B。

### 方案B：泛型DataSlot<T>（完全零装箱）

```csharp
internal sealed class DataSlot<T> where T : struct
{
    private T _value;
    private readonly List<IModifier<T>> _modifiers = new();

    public T Value => _value;
    public void SetValue(T value) { _value = value; }
    public T GetEffectiveValue() { return ApplyModifiers(_value); }
}
```

**代价：** 需要改DataKey<T>的泛型传递链路，改动范围大。

### 方案C：混合方案（推荐平衡点）

```csharp
// 用union type替代object?
[StructLayout(LayoutKind.Explicit)]
internal struct DataValueUnion
{
    [FieldOffset(0)] public float FloatValue;
    [FieldOffset(0)] public int IntValue;
    [FieldOffset(0)] public bool BoolValue;
    [FieldOffset(4)] public DataValueType ActiveType;
}
```

**优点：** 零装箱、零堆分配。**缺点：** 只适用于基本类型。

## 验证方法

```bash
# 1. 使用dotnet-counters监控GC
dotnet-counters monitor -n SlimeAI --counters System.Runtime

# 2. 使用dotnet-trace捕获分配热点
dotnet-trace collect -n SlimeAI --providers Microsoft-Windows-DotNETRuntime:0x1:4

# 3. 对比优化前后 Gen0 GC次数/秒
```