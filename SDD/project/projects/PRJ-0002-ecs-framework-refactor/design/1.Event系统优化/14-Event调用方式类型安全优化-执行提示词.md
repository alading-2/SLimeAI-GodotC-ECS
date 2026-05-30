# Event 调用方式类型安全优化 — 执行提示词

> 生成时间：2026-05-26
> 设计文档：`13-旧ECS框架Event系统问题分析与优化方向.md`
> 目标：删掉 `const string` 事件名，只保留 `readonly record struct` payload 作为事件标识

## 任务概述

将 `SlimeAI/` 仓的 Event 系统从字符串主键改为 payload 类型主键。

**改动范围**：
- `SlimeAI/Data/EventType/`：28 个 .cs 文件
- `SlimeAI/Src/ECS/Base/Event/EventBus.cs`：1 个文件
- `SlimeAI/Src/ECS/Base/Event/GlobalEventBus.cs`：1 个文件
- `SlimeAI/Src/**/*.cs`：约 63 个调用点文件（478 处 `GameEventType.` 引用，214 处 `Emit/On/Off/Once` 调用）
- `SlimeAI/Src/ECS/Base/Event/EventContext.cs`：不改

**不改的**：EventContext、Once 语义、EventPriority、GlobalEventBus 结构、Data/EventType 目录结构、同类一文件。

## 执行步骤

### Step 1: 修改 EventBus.cs

将 `Dictionary<string, List<Subscription>>` 改为 `Dictionary<Type, List<Subscription>>`，所有 API 签名去掉 `string eventName`。

**改动清单**：

1. `Subscription` 类：
   - `EventName` 属性改为 `EventType`（类型 `Type`）
   - 构造函数参数从 `string eventName` 改为 `Type eventType`

2. 字段：
   - `_subscriptions` 类型从 `Dictionary<string, List<Subscription>>` 改为 `Dictionary<Type, List<Subscription>>`
   - `_emittingEvents` 类型从 `HashSet<string>` 改为 `HashSet<Type>`

3. 公开 API 签名：
   ```csharp
   // 旧 → 新
   void On<T>(string eventName, Action<T> handler, int priority) → void On<T>(Action<T> handler, int priority) where T : struct
   void On(string eventName, Action handler, int priority) → 删除（无参事件改用空 struct）
   void Once<T>(string eventName, Action<T> handler, int priority) → void Once<T>(Action<T> handler, int priority) where T : struct
   void Once(string eventName, Action handler, int priority) → 删除
   void Off<T>(string eventName, Action<T> handler) → void Off<T>(Action<T> handler) where T : struct
   void Off(string eventName, Action handler) → 删除
   void Emit<T>(string eventName, T data) → void Emit<T>(in T data) where T : struct
   void Emit(string eventName) → 删除
   void ClearEvent(string eventName) → void ClearEvent<T>() where T : struct
   ```

4. 内部方法：
   - `Subscribe(string eventName, ...)` → `Subscribe<T>(...) where T : struct`，内部用 `typeof(T)` 作为 key
   - `Unsubscribe(string eventName, ...)` → `Unsubscribe<T>(...) where T : struct`
   - `Trigger<T>(string eventName, T data)` → `Trigger<T>(in T data) where T : struct`
   - 所有 `_subscriptions[eventName]` → `_subscriptions[typeof(T)]`
   - 所有 `_emittingEvents.Contains(eventName)` → `_emittingEvents.Contains(typeof(T))`
   - 日志中 `{eventName}` → `{typeof(T).Name}`

5. 性能注意：
   - `typeof(T)` 在泛型方法中是 JIT 常量，无反射开销
   - `Dictionary<Type, ...>` 查找性能与 `Dictionary<string, ...>` 相当
   - `in T data` 避免大 struct 拷贝

### Step 2: 修改 Data/EventType 所有文件

对 28 个 .cs 文件执行以下变换：

**规则**：
1. 删掉所有 `public const string Xxx = "xxx:yyy";` 行
2. `XxxEventData` 改名为 `Xxx`（payload 即事件本身）
3. 保留 `GameEventType` partial class 树的命名空间组织——它就是分类机制，`GameEventType.Unit.Damaged` 和 `GameEventType.Ability.Activated` 是不同类型，不会重名
4. 保留文件结构（同类事件放同一个文件）
5. 保留注释中的发布者/订阅者/语义说明
6. 同一个 partial class 内部不能重名（如 `GameEventType.Unit` 里不能有两个 `Damaged`），这与旧 `const string` 约束一致

**变换示例**：

```csharp
// 旧
public const string Damaged = "unit:damaged";
public readonly record struct DamagedEventData(
    IEntity Victim, float Amount, IEntity? Attacker = null,
    DamageType Type = DamageType.True, bool IsCritical = false);

// 新
/// <summary>单位受到伤害。HealthComponent 是 producer。</summary>
public readonly record struct Damaged(
    IEntity Victim, float Amount, IEntity? Attacker = null,
    DamageType Type = DamageType.True, bool IsCritical = false);
```

```csharp
// 旧
public const string WaveStarted = "global:wave_started";
public readonly record struct WaveStartedEventData(int WaveIndex);

// 新
/// <summary>波次开始。GlobalEventBus 是 producer。</summary>
public readonly record struct WaveStarted(int WaveIndex);
```

**特殊文件**：
- `Ability/CastContext.cs`：不是事件定义，不改
- `Ability/Context_AbilityExecutedResult.cs`：不是事件定义，不改
- `Base/Collision/GameEventType_Collision.cs`：照常删 const string + 改名

### Step 3: 修改 GlobalEventBus.cs

所有便捷方法的 `Emit(string, data)` 改为 `Emit(data)`。

```csharp
// 旧
public static void TriggerWaveStarted(int waveIndex)
{
    Global.Emit(GameEventType.Global.WaveStarted, new GameEventType.Global.WaveStartedEventData(waveIndex));
}

// 新
public static void TriggerWaveStarted(int waveIndex)
{
    Global.Emit(new GameEventType.Global.WaveStarted(waveIndex));
}
```

所有便捷方法都按此模式改。`GameEventType.XxxEventData` → `GameEventType.Xxx`，去掉第一个字符串参数。

### Step 4: 迁移所有调用点

约 63 个文件，478 处 `GameEventType.` 引用，214 处 `Emit/On/Off/Once` 调用。

**Emit 变换**：
```csharp
// 旧
entity.Events.Emit(GameEventType.Unit.Damaged, new GameEventType.Unit.DamagedEventData(victim, amount, attacker, type, isCritical));
// 新
entity.Events.Emit(new GameEventType.Unit.Damaged(victim, amount, attacker, type, isCritical));
```

**On 变换**：
```csharp
// 旧
entity.Events.On<GameEventType.Unit.DamagedEventData>(GameEventType.Unit.Damaged, OnDamaged);
// 新
entity.Events.On<GameEventType.Unit.Damaged>(OnDamaged);
```

**Once 变换**：
```csharp
// 旧
entity.Events.Once<GameEventType.Unit.KilledEventData>(GameEventType.Unit.Killed, OnKilled);
// 新
entity.Events.Once<GameEventType.Unit.Killed>(OnKilled);
```

**Off 变换**：
```csharp
// 旧
entity.Events.Off<GameEventType.Unit.DamagedEventData>(GameEventType.Unit.Damaged, OnDamaged);
// 新
entity.Events.Off<GameEventType.Unit.Damaged>(OnDamaged);
```

**无参事件变换**：
```csharp
// 旧
entity.Events.Emit(GameEventType.Global.GameStart);
entity.Events.On(GameEventType.Global.GameStart, OnGameStart);
// 新：用空 struct 替代
entity.Events.Emit(new GameEventType.Global.GameStart());
entity.Events.On<GameEventType.Global.GameStart>(OnGameStart);

// GameStartEventData 改名为 GameStart，变成无字段空 struct：
public readonly record struct GameStart();
```

### Step 5: EventContext 相关调用

EventContext 不改结构，只是调用签名变化：

```csharp
// 旧
var ctx = new EventContext();
entity.Events.Emit(GameEventType.Ability.CastRequested, new GameEventType.Ability.CastRequestedEventData(ctx, ability));
var result = ctx.GetResult<TriggerResult>();

// 新
var ctx = new EventContext();
entity.Events.Emit(new GameEventType.Ability.CastRequested(ctx, ability));
var result = ctx.GetResult<TriggerResult>();
```

### Step 6: 验证

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build
# 确认 0 error

# 搜索残留
rg 'const string.*=.*".*:"' Src/ Data/EventType/
rg 'EventData' Src/ Data/EventType/
rg '\.Emit\(' --type cs -c Src/
# 确认没有旧模式残留
```

## 迁移顺序建议

1. **先改 EventBus.cs** — 否则所有调用点编译不过
2. **再改 Data/EventType** — 删 const string + 改名
3. **再改 GlobalEventBus.cs** — 便捷方法
4. **最后批量改调用点** — 63 个文件
5. **dotnet build 验证**

## 风险点

- **无参事件**：旧 `Emit(string)` 和 `On(string, Action)` 需要改为空 struct。搜索 `Emit(GameEventType.` 后面没有 `new` 的调用。
- **handler 签名**：旧 `Action` 无参 handler 需要改为 `Action<EmptyStruct>`。
- **ClearEvent**：旧 `ClearEvent(string)` 改为 `ClearEvent<T>()`，需要确认所有调用点。
- **_Process 中的 Emit**：确认 `in T` 不会在 `_Process` 热路径引入额外拷贝（`readonly record struct` 通常足够小）。
