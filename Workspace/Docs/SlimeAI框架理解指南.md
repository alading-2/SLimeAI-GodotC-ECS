# SlimeAI 框架理解指南：从 ECS 到 Capability Composition Runtime

> 生成日期：2026-05-20
> 目标读者：希望理解 SlimeAI 架构、解耦机制和功能开关的开发者

---

## 一、一句话定位

SlimeAI 不是传统 ECS 框架，而是 **AI-first GameOS**，运行时架构叫 **Capability Composition Runtime**（能力组合运行时）。

核心公式：

```text
Small Runtime Kernel（小内核）
  + Capability Composition Runtime（能力组合层）
  + Optional Capabilities（可选能力包）
  + DataOS（数据驱动层）
  + GodotBridge（引擎桥接层）
  + Validation / Observation（验证/观察层）
```

---

## 二、必须理解的 6 个核心概念

### 1. Runtime Entity（运行时实体容器）

旧框架：Entity 是继承 `Node` 的具体类，如 `PlayerEntity`、`EnemyEntity`、`ProjectileEntity`，承载业务逻辑和 Godot 生命周期。

新框架：`RuntimeEntity` 是 **纯 C# 容器**，只含三样东西：

```csharp
// RuntimeEntity 的核心结构
EntityId     // 稳定的运行时身份（typed value，不是 string）
Data         // 运行时数据容器（绑定 DataCatalog）
Events       // 实体级事件总线（EntityEventBus）
```

它不承载任何业务逻辑，不是 `Enemy`/`Player` 的继承根。玩法逻辑由 Capability Service 操作 Entity 的 Data 和 Events 来实现。

**为什么这样设计**：
- AI 不需要猜 "PlayerEntity 继承了什么、覆盖了什么方法"
- 所有状态通过 DataKey 读写，可搜索、可验证
- 测试时可用 `RuntimeWorld.CreateScoped()` 创建完全隔离的沙箱

### 2. Runtime Data / DataKey<T>（类型化的状态契约）

旧框架：状态散落在各种 Component 的 public 字段中，如 `HealthComponent.CurrentHp`、`MovementComponent.Speed`。字段名靠约定，无编译时保护。

新框架：状态通过 **typed DataKey<T>** 访问：

```csharp
// 定义（Capability 内集中声明）
public static readonly DataKey<float> CurrentHp = DataKey.Create<float>("Damage.CurrentHp", defaultValue: 0f);
public static readonly DataKey<float> MaxHp = DataKey.Create<float>("Damage.MaxHp", defaultValue: 0f, supportsModifiers: true);

// 读写
entity.Data.Set(DamageDataKeys.CurrentHp, 100f);
var hp = entity.Data.Get<float>(DamageDataKeys.CurrentHp);
```

每个 DataKey 携带：stable key、默认值、分类、数值边界、是否支持 modifier、是否 computed。

**为什么这样设计**：
- `DamageDataKeys.CurrentHp` 这个符号就是路由入口，AI 看到就知道归 Damage Capability
- 类型错误在 `dotnet build` 阶段暴露，不等到运行时
- DataOS validator 可以检查 descriptor 是否缺失、默认值是否漂移

### 3. Capability（能力包 = 解耦单元）

旧框架：解耦靠 Component（数据）+ System（逻辑）。System 通过全局 query 扫描所有 Entity，检查是否包含目标 Component。

新框架：解耦靠 **Capability**。每个 Capability 是一个自包含的玩法模块，至少包含：

| 成员 | 职责 | 示例 |
|------|------|------|
| Service | 业务逻辑入口 | `DamageService.Process(info)` |
| DataKeys | 状态字段定义 | `DamageDataKeys.CurrentHp / MaxHp / Armor` |
| Events | 事件定义 | `Damaged / Dodged / Killed` |
| Tool / Handler | 辅助工具 | `DamageTool`（多目标伤害）、`FeatureHandlerRegistry` |
| GodotBridge | 引擎表现层桥接 | `GodotContactDamageComponent` |
| System | Schedule 调度单元 | `MovementSystem.Tick(delta)` |
| capability.json | 元数据 | id、依赖、DataKey 列表、验证命令 |

**关键区别**：Capability 是 **所有权边界**，不是 "数据+逻辑分离" 那么简单。Damage 的 HP 逻辑由 `DamageService` 持有，AI 的寻敌逻辑由 `AIService` 持有，Movement 的位置更新由 `MovementSystem` 持有。AI 修改行为时，先路由到对应 Capability，不新增泛型 Component 或绕过 owner 的全局 System。

### 4. GodotBridge Adapter（引擎表现层桥接）

旧框架：Component 直接继承 Godot Node，既处理逻辑又处理表现，数据和 Godot 节点混在一起。

新框架：明确分层：

```text
Runtime 层（纯 C#，无 Godot 依赖）
  -> Entity.Data / Events 持有玩法真相
  -> Capability Service 处理逻辑
  
GodotBridge 层（只在引擎边界）
  -> GodotEntity2D 把 Node2D 接入 Runtime Entity
  -> GodotMovementDriver 把 Movement Data 同步回 Node2D.Position
  -> GodotUnitAnimationComponent 把 Runtime 动画请求转为 AnimatedSprite2D 播放
```

`IGodotComponent` 是 legacy compatibility name，本质上是 "GodotBridge 生命周期协议"，不是传统 ECS 的 data component。

### 5. RuntimeSchedule / Runtime Process（调度执行单元）

旧框架：System 是全局 query processor，通过 `EntityManager.GetAll()` 或 `GetComponentsByType<T>()` 扫描世界。

新框架：`RuntimeSchedule` 是纯 C# 调度器，管理 System 的注册、依赖、启停和运行条件。System 的核心职责变成：

```text
1. 生命周期：OnRegistered / OnStarted / OnStopped / OnUnregistered
2. 依赖：通过 SystemConfig.Dependencies 声明
3. 运行条件：SystemRunCondition（如只在 Gameplay 状态运行）
4. Tick 调用：由外部调度（如 Godot _Process 驱动 MovementSystem.Tick）
```

**System 不扫描世界**。MovementSystem 只遍历自己持有的 `activeMovements` 字典，DamageService 只处理传入的 `DamageInfo`。全局扫描被 Capability-owned selector（注入的 query）替代。

### 6. DataOS（Authoring -> Snapshot -> Runtime）

旧框架：数据配置直接写在 Resource (.tres) 或 Component 默认值中，运行时热修改。

新框架：三层分离：

```text
SQLite Authoring DB（seed + migration）
  -> Generator 生成 typed snapshot JSON
  -> Validator 检查 descriptor 一致性
  -> RuntimeDataSnapshot Loader 按 DataKey<T> apply 到 Entity.Data
```

游戏数值通过 DataOS 修改，经 validator 检查后才进入 runtime。AI 修改数据后，验证命令是固定的（`Tools/run-dataos-validate.sh`）。

---

## 三、新旧框架解耦方式深度对比

### 旧框架（brotato-my）：伪 ECS

```text
Entity（Node 子类，如 PlayerEntity）
  + Component A（Node 子类，如 HealthComponent）  <- 挂载到 Entity 下
  + Component B（Node 子类，如 AttackComponent）
  + Component C（Node 子类，如 AIComponent）

System X（全局扫描：遍历所有 Entity，检查有没有 A 和 B）
System Y（全局扫描：遍历所有 Entity，检查有没有 C）
```

- **解耦方式**：Component 纯数据分离 + System 全局 query 处理
- **Component 挂载**：任意时刻可以 `EntityManager.AddComponent(entity, comp)`
- **System 发现**：通过 `EntityManager.GetComponentsByType<HealthComponent>()` 全局扫描
- **功能开关**：通过 SystemManager.EnableSystem/DisableSystem 动态启停 System；Component 仍存在但 System 不处理

### 新框架：Capability Composition Runtime

```text
Runtime Entity（EntityId + Data + Events）
  + Data: Damage.CurrentHp = 100       <- DataKey 写入
  + Data: Movement.Position = (0,0)     <- DataKey 写入
  + Data: AI.IsEnabled = true           <- DataKey 写入

DamageService（Capability 逻辑 owner）
  -> 读取 DamageDataKeys，处理 DamageInfo，发布 Damage Events

MovementSystem（Schedule 调度单元）
  -> 遍历 activeMovements（自己持有的字典，不扫描全局）
  -> 更新 MovementDataKeys.Position / Velocity

AIService（Capability 逻辑 owner）
  -> 通过注入的 IAITargetQuery 获取候选目标（不是 EntityManager.GetAll()）
  -> 写入 AI DataKeys，发布 Attack Requested 事件
```

- **解耦方式**：Capability 所有权边界 + 构造注入 + 事件通信 + DataKey 类型契约
- **数据写入**：通过 Capability Service 的 API（如 `DamageService.Process`）间接修改，不直接 `entity.Data.Set`
- **目标查询**：通过注入的 query 接口（`IAITargetQuery`、`IAbilityTargetQuery`），由 owner Capability 持有查询缓存
- **功能开关**：三层机制（见下节）

### 核心差异总结

| 维度 | 旧框架 | 新框架 |
|------|--------|--------|
| Entity | 继承 Node 的具体类 | 纯 C# 容器，无子类 |
| 状态存储 | Component public 字段 | DataKey<T> + DataSlot |
| 逻辑归属 | System 全局 query | Capability Service / Tool / Handler |
| 解耦单元 | Component + System | Capability（含 Service/DataKey/Event/Tool） |
| 全局扫描 | `GetAll()` / `GetComponentsByType()` | Capability-owned 注入 query |
| 引擎边界 | Component 直接是 Godot Node | GodotBridge Adapter 桥接 |
| 数据配置 | Resource / Component 默认值 | DataOS seed -> snapshot -> validator |
| 测试隔离 | 难（全局 static） | `RuntimeWorld.CreateScoped()` |

---

## 四、功能开关三层机制（怎么打开/关闭某个功能）

用户问 "解耦就是可选，可以设置打开/关闭某个功能，那游戏里面怎么设置打开哪些不打开哪些"。新框架有三层开关，从静态到动态：

### 第一层：DataOS capability_manifest（编译/生成期开关）

```sql
-- DataOS Schema: capability_manifest
CREATE TABLE capability_manifest (
    capability_id TEXT PRIMARY KEY,
    enabled INTEGER NOT NULL DEFAULT 1 CHECK (enabled IN (0, 1)),
    trim_policy TEXT NOT NULL DEFAULT 'fail' CHECK (trim_policy IN ('fail', 'trim'))
);

-- 示例：关闭 Movement Capability
INSERT INTO capability_manifest(capability_id, enabled)
VALUES ('Movement', 0);
```

- **作用**：被禁用的 Capability 的 DataKey 不会进入 DataCatalog，snapshot 中不包含其数据
- **生效时机**：snapshot 生成时（build 前）
- **能否运行时切换**：**不能**。这是编译期/生成期决定
- **验证**：DataOS validator 会检查 `disabled capability 未裁剪` 并阻断

**在游戏中的使用**：修改 seed SQL -> 重新生成 snapshot -> runtime 加载时该 Capability 的 DataKey 不存在。

### 第二层：RuntimeSchedule（运行时动态开关）

```csharp
// 注册系统到调度器
var schedule = RuntimeWorld.Default.Schedule;
schedule.Register(
    new SystemDescriptor("MovementSystem", () => new MovementSystem()),
    new SystemConfig {
        SystemId = "MovementSystem",
        StartEnabled = true,      // 初始是否启用
        Required = false,          // 是否必需（Required=true 时不可禁用）
        Dependencies = [],         // 依赖其他系统
        RunCondition = SystemRunCondition.GameplayOnly
    }
);

// 运行时动态启停
schedule.SetSystemEnabled("MovementSystem", false);  // 关闭
schedule.SetSystemEnabled("MovementSystem", true);   // 打开

// 查询系统状态
var info = schedule.GetRuntimeInfo();
// 每个系统有：IsAdded / IsEnabled / IsRunning / IsStateAllowed / BlockedReason
```

- **作用**：控制某个 System 是否参与 Tick / 是否响应命令
- **生效时机**：即时
- **能否运行时切换**：**能**
- **约束**：`Required=true` 的系统不可禁用；有被依赖的系统不可移除

**在游戏中的使用**：
- 暂停菜单打开时，调用 `ProjectState.OpenPauseMenu()`，所有 `RunCondition = GameplayOnly` 的系统自动被 Blocked
- 某个关卡不需要 AI 时，调用 `SetSystemEnabled("AIService", false)`

### 第三层：游戏状态 / 游戏配置（运行时即时开关）

```csharp
// 通过 ProjectStateService 切换全局状态
var projectState = RuntimeWorld.Default.Schedule.ProjectState;
projectState.OpenPauseMenu();        // 所有 gameplay-only 系统停止
projectState.BeginGameplaySession(); // 恢复

// 通过 Godot [Export] 变量暴露给玩家
public partial class GameConfig : Resource
{
    [Export] public bool EnableAI = true;
    [Export] public bool EnableDamageNumbers = true;
}
```

- **作用**：全局状态切换或玩家配置
- **生效时机**：即时
- **能否运行时切换**：**能**

### 三层开关对比

| 层级 | 机制 | 生效时机 | 运行时可变？ | 适用场景 |
|------|------|----------|-------------|----------|
| DataOS | `capability_manifest.enabled = 0/1` | snapshot 生成时 | 否 | 永久移除某个 Capability |
| RuntimeSchedule | `SetSystemEnabled(id, false)` | 即时 | 是 | 动态启停 System Tick |
| 游戏状态 | `ProjectState.OpenPauseMenu()` | 即时 | 是 | 全局暂停/恢复/菜单切换 |

**优先级**：DataOS 是 "有没有这个能力"；RuntimeSchedule 是 "这个能力的调度器是否运行"；游戏状态是 "当前场景下是否应该运行"。

---

## 五、阅读路径建议（从哪里开始看）

### 快速理解（30 分钟）

1. **`SlimeAI/DocsAI/Framework/Overview.md`** — 框架总览和定位
2. **`SlimeAI/DocsAI/GameOS/Overview.md`** — GameOS 子域和术语表（必读！术语护栏在这里）
3. **`SlimeAI/DocsAI/GameOS/Overview.md#功能开关总览`** — 本文档第四节的原始来源

### 深入理解（2 小时）

4. **`SlimeAI/DocsAI/ArchitectureDecisionRecords/深度分析：AI-firstGameOS与ECS概念边界.md`** — 新旧框架对比的深度分析（本文档的核心输入）
5. **`SlimeAI/DocsAI/GameOS/Contracts.md`** — 所有子系统的正式契约
6. **`SlimeAI/DocsAI/GameOS/Capabilities/CapabilityIndex.md`** — Capability 索引，看每个 Capability 的 DataKeys、Events、Dependencies

### 源码阅读（按兴趣选一个 Capability）

7. **选一个 Capability 深入**：
   - 想看 "数据+逻辑怎么组织"：`SlimeAI/GameOS/Capabilities/Damage/DamageService.cs`
   - 想看 "System 怎么调度"：`SlimeAI/GameOS/Capabilities/Movement/MovementSystem.cs`
   - 想看 "目标查询怎么注入"：`SlimeAI/GameOS/Capabilities/AI/AIService.cs`
   - 想看 "事件怎么组织"：`SlimeAI/GameOS/Capabilities/<Name>/Events/`
8. **看 Runtime 内核**：
   - `SlimeAI/GameOS/Runtime/World/RuntimeWorld.cs` — 世界容器 facade
   - `SlimeAI/GameOS/Runtime/Schedule/RuntimeSchedule.cs` — 调度器
   - `SlimeAI/GameOS/Runtime/Entity/RuntimeEntity.cs` — 实体容器
   - `SlimeAI/GameOS/Runtime/Data/DataKey.cs` — DataKey 定义

### DataOS 层（如果想理解数据配置）

9. **`SlimeAI/DocsAI/DataOS/Overview.md`** — DataOS 总览
10. **`SlimeAI/DataOS/Schema/core.sql`** — capability_manifest 和 data_key_descriptor 表结构
11. **`SlimeAI/DataOS/Authoring/Framework.seed.sql`** — 框架级 seed 示例

### 游戏侧示例（看怎么接入）

12. **`Games/BrotatoLike/Src/Game/Bridge/`** — 游戏侧 Bridge Adapter 示例
13. **`Games/BrotatoLike/Src/Game/Bootstrap/`** — DataOS snapshot 加载和初始化

---

## 六、关键文件速查表

| 想理解什么 | 读哪个文件 |
|-----------|----------|
| 框架是什么 | `SlimeAI/DocsAI/Framework/Overview.md` |
| 为什么不用 ECS 了 | `SlimeAI/DocsAI/ArchitectureDecisionRecords/深度分析：AI-firstGameOS与ECS概念边界.md` |
| Entity 是什么 | `SlimeAI/GameOS/Runtime/Entity/RuntimeEntity.cs` |
| DataKey 是什么 | `SlimeAI/GameOS/Runtime/Data/DataKey.cs` |
| Capability 有哪些 | `SlimeAI/DocsAI/GameOS/Capabilities/CapabilityIndex.md` |
| 某个 Capability 的契约 | `SlimeAI/DocsAI/GameOS/Capabilities/<Name>/Contract.md` |
| System 怎么启停 | `SlimeAI/GameOS/Runtime/Schedule/RuntimeSchedule.cs` |
| 世界容器 | `SlimeAI/GameOS/Runtime/World/RuntimeWorld.cs` |
| Capability 元数据 | `SlimeAI/GameOS/Capabilities/<Name>/capability.json` |
| 数据层 | `SlimeAI/DocsAI/DataOS/Overview.md` |
| 游戏侧怎么接入 | `Games/BrotatoLike/Src/Game/Bridge/` |

---

## 七、常见误解澄清

### 误解 1："没有 Component 了，怎么组合行为？"

**正解**：Component 被拆成了三层：
- **数据** -> `DataKey<T>`（不是 Component）
- **逻辑** -> `Capability Service/Tool/Handler`（不是 System query）
- **Godot 桥接** -> `GodotBridge Adapter`（legacy 叫 Component，但语义不同）

组合方式从 "挂载 Component 节点" 变成 "写入 DataKey + 调用 Service API + 订阅 Event"。

### 误解 2："System 不见了，怎么执行逻辑？"

**正解**：System 还在，但语义收窄了：
- `MovementSystem` 只调度 Movement，不扫描全局 Entity
- `DamageService` 处理伤害逻辑，不通过 query 找目标
- 大多数 Capability 用 `Service`（事件/请求驱动）而不是 `System`（每帧 tick）

### 误解 3："没有全局 query，怎么找敌人？"

**正解**：通过 **注入的 Capability-owned selector**：
- AI 有 `IAITargetQuery`，默认实现 `RuntimeAITargetQuery`
- Ability 有 `IAbilityTargetQuery`，默认实现 `RuntimeAbilityTargetQuery`
- Movement 有 `IMovementCollisionTargetQuery`

这些 query 由 owner Capability 持有，可以替换为游戏专属实现（如只查询屏幕内敌人），不开放全局 world scan。

### 误解 4："怎么测试？不能全局扫描了。"

**正解**：测试更简单了：
```csharp
using var world = RuntimeWorld.CreateScoped();
var entity = world.Entities.Spawn(new EntitySpawnConfig { ... });
entity.Data.Set(DamageDataKeys.MaxHp, 100f);

var damageService = new DamageService();  // 独立实例，不共享全局状态
var result = damageService.Process(new DamageInfo { Victim = entity, ... });
Assert.That(entity.Data.Get<float>(DamageDataKeys.CurrentHp), Is.LessThan(100f));
```

`CreateScoped()` 创建完全隔离的世界，两个测试互不干扰。

---

## 八、对 AI 开发者的额外说明

如果你是用 AI（如 Claude、Codex、Windsurf）修改这个框架，额外需要理解：

1. **路由优先**：修改前先查 `DocsAI/INDEX.md` 和 `CapabilityIndex.md`，确定修改归哪个 Capability 管
2. **owner skill**：每个 Capability 有 owner skill（如 `damage-system`、`movement-system`），修改后检查对应 skill 路由
3. **验证命令**：每次改动必须说明验证命令（`Tools/run-build.sh`、`Tools/run-tests.sh`、Godot scene smoke）
4. **DataKey 同步**：新增/修改 DataKey 后，必须同步 DataOS descriptor，否则 `Tools/run-dataos-validate.sh` 会失败
5. **不要新增全局 query**：Capabiilty-owned selector 可注入，不要写 `EntityManager.GetAll()` 扫描

---

## 参考来源

- `SlimeAI/DocsAI/Framework/Overview.md`
- `SlimeAI/DocsAI/GameOS/Overview.md`
- `SlimeAI/DocsAI/GameOS/Contracts.md`
- `SlimeAI/DocsAI/GameOS/Capabilities/CapabilityIndex.md`
- `SlimeAI/DocsAI/ArchitectureDecisionRecords/深度分析：AI-firstGameOS与ECS概念边界.md`
- `SlimeAI/DocsAI/DataOS/Overview.md`
- `SlimeAI/GameOS/Runtime/World/RuntimeWorld.cs`
- `SlimeAI/GameOS/Runtime/Schedule/RuntimeSchedule.cs`
- `SlimeAI/DataOS/Schema/core.sql`
- `Resources/Else/brotato-my/Src/ECS/Base/` (旧框架源码)
