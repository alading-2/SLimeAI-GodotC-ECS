---
name: ability-system
description: 实现或修改技能功能时使用。适用于：新建技能、配置冷却/充能/目标选择、触发技能流水线、读取触发结果、实现技能效果处理器。触发关键词：技能、AbilitySystem、TryTrigger、CastContext、CooldownComponent、ChargeComponent、TriggerComponent、AbilityEntity、IFeatureHandler、FeatureHandlerId。
---

# AbilitySystem 技能系统规范

## 核心架构

技能流水线：`TryTrigger → CanUse检查 → ConsumeCharge → StartCooldown → ConsumeCost → FeatureSystem.OnFeatureActivated → IFeatureHandler.OnActivated → IFeatureHandler.OnExecute → Ability.Executed → FeatureSystem.OnFeatureEnded`

内置组件（无需手写）：

- `CooldownComponent` - 冷却管理
- `ChargeComponent` - 充能计数
- `TriggerComponent` - 触发模式（Periodic/OnEvent/Manual）
- `CostComponent` - 资源消耗

当前职责边界：

- `AbilitySystem` 只编排流水线，不做通用自动索敌或点选决策
- 实体目标查询写在具体 Handler 的 `ExecuteAbility`
- Point 点选由输入层在正式 `TryTrigger` 前发起，确认后再提交正式 `TryTrigger`
- `TargetingManager` / `TargetingIndicatorControlComponent` 只负责异步点选会话

## 触发技能（统一入口）

```csharp
// ✅ 标准触发方式（统一走 TryTrigger 事件）
var context = new CastContext
{
    Ability = abilityEntity,
    Caster = ownerEntity,
    ResponseContext = new EventContext()
};
abilityEntity.Events.Emit(
    GameEventType.Ability.TryTrigger,
    new GameEventType.Ability.TryTriggerEventData(context)
);

// ✅ 读取触发结果
var result = context.ResponseContext?.HasResult == true
    ? context.ResponseContext.GetResult<TriggerResult>()
    : TriggerResult.Failed;
// TriggerResult: Success / Failed
```

## 配置技能数据

```csharp
// 通过 Data 配置，内置组件自动响应
ability.Data.Set(DataKey.AbilityEnabled, true);
ability.Data.Set(DataKey.AbilityCooldown, 5.0f);          // CooldownComponent 自动管理
ability.Data.Set(DataKey.IsAbilityUsesCharges, true);     // 启用充能
ability.Data.Set(DataKey.AbilityChargeMax, 3);            // 最大充能数
ability.Data.Set(DataKey.AbilityTriggerMode, (int)AbilityTriggerMode.Manual);

// 目标选择配置
ability.Data.Set(DataKey.AbilityTargetSelection, (int)AbilityTargetSelection.Entity);
ability.Data.Set(DataKey.AbilityCastRange, 200f);      // 施法距离（由具体 Handler 解释为索敌/点选射程）
ability.Data.Set(DataKey.AbilityEffectRadius, 150f);   // 效果半径（AOE范围/命中半径/技能自定义语义）
ability.Data.Set(DataKey.AbilityDamageInterval, 0.5f);      // 持续伤害间隔；0 表示单次
ability.Data.Set(DataKey.AbilityDamageDuration, 3f);        // 持续伤害总时长；0 表示单次
ability.Data.Set(DataKey.AbilityRepeatHitSameTarget, true); // 是否允许同一施法内重复命中同一目标
ability.Data.Set(DataKey.AbilityApplyImmediateDamage, true);// DoT 开始时是否先立即造成一次伤害
```

## 两条时间轴

`Ability` 里有两套完全不同的“时间”语义，不能混为一谈：

- `TriggerComponent + AbilityTriggerMode.Periodic + AbilityCooldown`
- 决定的是“多久重新执行一次整条技能流水线”
- 每次都会重新进入 `TryTrigger → ExecuteAbility`
- 适合“每 N 秒放一次圈伤 / 回血 / 爆炸”

- `DamageApplyOptions.TickInterval + TotalDuration`
- 决定的是“单次 ExecuteAbility 启动后，这次伤害内部要不要继续跳 DoT”
- 不会重新执行技能流水线，只会在当前命中上下文里继续结算伤害
- 适合“这次命中后挂一个持续灼烧 / 毒伤 / 持续范围伤害”

`CircleDamage` 当前就是第一种，不是第二种：

- `TriggerComponent` 会按 `AbilityCooldown` 周期触发技能
- [`CircleDamage.cs`](/mnt/e/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Data/Data/Ability/Ability/CircleDamage/CircleDamage.cs) 里的 `DamageApplyOptions` 没有设置 `TickInterval / TotalDuration`
- 所以每次触发都是“一次即时范围伤害”，不会在单次技能内部继续跳 DoT

两种模式都可以同时存在，但语义要明确：

- 如果同时配置 `Periodic` 和 `DamageApplyOptions.TickInterval / TotalDuration`
- 那么效果就是“每次周期触发都会再启动一条新的内部 DoT”
- 这是合法设计，但要确认你要的就是叠多层持续伤害，而不是误配

## 推荐：优先复用 AbilityImpactTool

当技能需要"目标查询 + 特效 + 伤害结算"时，优先调用 `AbilityImpactTool`，避免在技能处理器中重复手写 `TargetSelector + EffectTool + foreach + DamageService`：

```csharp
// 固定落点命中（Slam / Dash 落地 / 投射物爆炸）
var result = AbilityImpactTool.Execute(caster, new AbilityImpactOptions
{
    Query = new TargetSelectorQuery
    {
        Geometry = GeometryType.Circle,
        Origin = impactPosition,
        Range = ability.Data.Get<float>(DataKey.AbilityEffectRadius),
        CenterEntity = caster,
        TeamFilter = AbilityTargetTeamFilter.Enemy,
        Sorting = TargetSorting.Nearest,
        MaxTargets = -1
    },
    Effect = effectScene != null
        ? new EffectSpawnOptions(effectScene, Name: "技能特效")
        : null,
    Damage = new DamageApplyOptions
    {
        Damage = ability.Data.Get<float>(DataKey.AbilityDamage) * caster.Data.Get<float>(DataKey.AbilityDamageBonus) / 100f,
        Type = DamageType.Magical,
        Tags = DamageTags.Ability | DamageTags.Area,
        Attacker = casterNode
    }
});

// 以施法者当前位置命中（光环 / CircleDamage 等跟随施法者的技能）
var result = AbilityImpactTool.Execute(caster, new AbilityImpactOptions
{
    Query = new TargetSelectorQuery
    {
        Geometry = GeometryType.Circle,
        Origin = casterNode.GlobalPosition,
        OriginProvider = () => casterNode.GlobalPosition,
        Range = ability.Data.Get<float>(DataKey.AbilityEffectRadius),
        CenterEntity = caster,
        TeamFilter = AbilityTargetTeamFilter.Enemy,
        MaxTargets = -1
    },
    Effect = effectScene != null
        ? new EffectSpawnOptions(
            effectScene,
            Name: "光环特效",
            Scale: new Vector2(2f, 2f))
        : null,
    Damage = new DamageApplyOptions
    {
        Damage = ability.Data.Get<float>(DataKey.AbilityDamage) * caster.Data.Get<float>(DataKey.AbilityDamageBonus) / 100f,
        Type = DamageType.Magical,
        Tags = DamageTags.Ability | DamageTags.Area,
        Attacker = casterNode,
        // DoT 参数（可选）：
        TickInterval = ability.Data.Get<float>(nameof(DataKey.AbilityDamageInterval)),
        TotalDuration = ability.Data.Get<float>(nameof(DataKey.AbilityDamageDuration)),
        AllowRepeatHitSameTarget = ability.Data.Get<bool>(nameof(DataKey.AbilityRepeatHitSameTarget)),
        ApplyImmediateTick = ability.Data.Get<bool>(DataKey.AbilityApplyImmediateDamage)
    }
});
```

`AbilityImpactTool` 入口说明：

- `Execute(caster, options)` - 统一入口；位置统一放在 `Query` / `Effect` 参数对象里
- 四个 options 字段均为可选（`null` 时跳过该步骤）：`Query?` / `Targets?` / `Effect?` / `Damage?`
- `Query.Origin / OriginProvider` 是唯一命中锚点来源
- `Targets` 适合碰撞命中、链路里已经明确拿到目标的场景，不必再伪造范围查询
- `Effect.EffectPosition` 仅在特效需要显式偏离命中点时再填写；不填则默认复用 Query 原点
- `Damage.ApplyImmediateTick` 用于控制 DoT 在开始时是否先同步结算一次，默认 `true`
- `Damage != null` 但 `Query == null` 且 `Targets == null` 时不会隐式兜底选目标
- DoT 调度与重复命中控制由 `DamageTool` 统一管理（见 damage-system Skill）

## 什么时候写在 ExecuteAbility，什么时候写在 OnGranted

- 写在 `AbilityFeatureHandler.ExecuteAbility(CastContext)`：表示“每次技能被触发时执行一次”
- 配合 `TriggerComponent.Periodic`：表示“每隔一段时间重新执行一次技能”
- 配合 `DamageApplyOptions.TickInterval / TotalDuration`：表示“这次技能执行内部再挂一段 DoT”

- 写在 `IFeatureHandler.OnGranted`：表示“能力一被授予就开始常驻运行”
- 适合常驻光环、被动灼烧圈、长期监听器、长期计时器
- 这类逻辑如果自己创建了 `GameTimer` / 订阅 / 持续特效，必须在 `OnRemoved` 里显式取消和清理

推荐判断：

- “每隔 N 秒释放一次技能” → `TriggerComponent.Periodic + ExecuteAbility`
- “能力授予后持续存在，直到被移除” → `OnGranted + OnRemoved`
- “单次技能命中后内部继续跳伤害” → `ExecuteAbility + DamageApplyOptions.TickInterval / TotalDuration`

## 目标选择类型

| 类型            | 说明                       | 建议写法                                                 |
| :-------------- | :------------------------- | :------------------------------------------------------- |
| `Entity`        | 需要实体目标               | 在 `ExecuteAbility` 内索敌；无目标时返回 0 命中或降级    |
| `Point`         | 需要玩家指定位置           | 输入层先点选；Handler 在 `ExecuteAbility` 读取 `TargetPosition` |
| `EntityOrPoint` | 由具体技能自己定义先后策略 | 不写回 AbilitySystem；输入层与 Handler 为该技能明确约定 |
| `None`          | 不依赖前置目标             | `ExecuteAbility` 自行处理                                |

`AbilityTargetSelection` 只保留“输入语义/编辑器表达”作用；`AbilitySystem` 不会按它做统一前置选目标。

## 推荐：在 ExecuteAbility 中做目标决策

```csharp
internal class ArcShot : AbilityFeatureHandler
{
    public override string FeatureId => FeatureId.Ability_Movement_ArcShot;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        float castRange = ability.Data.Get<float>(DataKey.AbilityCastRange);
        if (castRange <= 0f)
        {
            castRange = ability.Data.Get<float>(DataKey.AbilityEffectRadius);
        }

        var targets = castRange > 0f
            ? EntityTargetSelector.Query(new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Origin = casterNode.GlobalPosition,
                Range = castRange,
                CenterEntity = caster,
                TeamFilter = AbilityTargetTeamFilter.Enemy,
                Sorting = TargetSorting.Nearest,
                MaxTargets = 1
            })
            : new List<IEntity>();
        if (targets.Count == 0)
        {
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        context.Targets = new List<IEntity> { targets[0] };
        // 后续生成投射物 / 伤害逻辑
        return new AbilityExecutedResult { TargetsHit = 1 };
    }
}
```

Point 型技能建议：

```csharp
// 输入层：
if ((AbilityTargetSelection)ability.Data.Get<int>(DataKey.AbilityTargetSelection) == AbilityTargetSelection.Point)
{
    if (AbilitySystem.CanUseAbility(ability))
    {
        GlobalEventBus.Global.Emit(
            GameEventType.Targeting.StartTargeting,
            new GameEventType.Targeting.StartTargetingEventData(context)); //施法上下文
    }
}

// Handler：
protected override AbilityExecutedResult ExecuteAbility(CastContext context)
{
    if (!context.TargetPosition.HasValue)
    {
        return new AbilityExecutedResult { TargetsHit = 0 };
    }

    var targetPosition = context.TargetPosition.Value;
    // 使用 targetPosition 执行具体技能逻辑
    return new AbilityExecutedResult { TargetsHit = 0 };
}
```

## 实现技能效果处理器

```csharp
// 推荐：Ability 技能统一继承 AbilityFeatureHandler，由中转层读取 CastContext
internal class MyAbilityHandler : AbilityFeatureHandler
{
    public override string FeatureId => "技能.主动.我的技能";

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new MyAbilityHandler());
    }

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        var result = AbilityImpactTool.Execute(caster, new AbilityImpactOptions
        {
            Query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Origin = casterNode.GlobalPosition,
                Range = ability.Data.Get<float>(DataKey.AbilityEffectRadius),
                CenterEntity = caster,
                TeamFilter = AbilityTargetTeamFilter.Enemy,
                MaxTargets = -1
            },
            Damage = new DamageApplyOptions
            {
                Damage = ability.Data.Get<float>(DataKey.AbilityDamage) * caster.Data.Get<float>(DataKey.AbilityDamageBonus) / 100f,
                Type = DamageType.Physical,
                Tags = DamageTags.Ability,
                Attacker = casterNode
            }
        });

        return new AbilityExecutedResult { TargetsHit = result.TargetsHit };
    }
}
```

`AbilityConfig` 中必须优先显式填写完整 `FeatureHandlerId`：

- `.tres` 维护 `Name`（技能名称）、`FeatureGroupId`（展示分组）和 `FeatureHandlerId`（执行器 ID）
- 多个技能模板可以填写同一个 `FeatureHandlerId`，通过各自 DataKey 参数复用同一个处理器
- 运行时技能测试面板按完整 `FeatureGroupId` 分组和显示，不应再用 `AbilityType` 作为主分类来源
- 调试/UI 删除技能时，如果已经拿到了运行时 `AbilityEntity` 或 `DataKey.Id`，必须按实例移除，不要再退回按 `Name` 删除
- 运行时技能测试面板依赖 `TestSystem.SelectedEntity` 作为操作目标；全局测试场景生成玩家后，应主动把玩家设为当前选中实体，避免左侧技能库点击后只提示未选中实体
- `EntityManager.AddAbility` 会校验 `AbilityConfig.FeatureHandlerId`；缺失或没有注册对应 `IFeatureHandler` 时添加失败

处理器不再声明 `FeatureGroup`；运行时只通过完整 `FeatureHandlerId` 查找 `IFeatureHandler`。

点选发起逻辑保持在具体输入入口内：通过 `CanUseAbility` 预检查后直接发送 `Targeting.StartTargeting`。Ability 领域通用 helper 统一放进 `AbilityTool`，不要再往 `AbilityFeatureHandler` 塞方法。

## 在处理器中使用特效

技能执行时通过 `EffectTool.Spawn` 生成特效（详见 `Docs/框架/ECS/System/特效系统使用指南.md`）：

```csharp
protected override AbilityExecutedResult ExecuteAbility(CastContext context)
{
    var caster = context.Caster!;
    var casterNode = (Node2D)caster;

    // 在施法者位置生成独立特效（播完自动销毁）
    var effectScene = ResourceManagement.Load<PackedScene>(
        ResourcePaths.Asset_Effect_020, ResourceCategory.Asset);
    if (effectScene != null)
    {
        EffectTool.Spawn(new EffectSpawnOptions(
            VisualScene: effectScene,
            Name: "技能特效",
            Scale: new Vector2(1.5f, 1.5f),
            EffectPosition: casterNode.GlobalPosition
        ));
    }

    // 附着特效（跟随施法者移动，播完自动销毁）
    var dashEffectScene = ResourceManagement.Load<PackedScene>(
        ResourcePaths.Asset_Effect_004龙卷风, ResourceCategory.Asset);
    if (dashEffectScene != null)
    {
        EffectTool.Spawn(new EffectSpawnOptions(
            VisualScene: dashEffectScene,
            Host: casterNode   // 传 Host 则为附着模式
        ));
    }
    return new AbilityExecutedResult { TargetsHit = 0 };
}
```

**可用特效常量**（`ResourceCategory.Asset`）：

- `ResourcePaths.Asset_Effect_003` - 光环/范围爆炸
- `ResourcePaths.Asset_Effect_004龙卷风` - 冲刺/位移
- `ResourcePaths.Asset_Effect_020` - 地面撞击/近战AOE
- `ResourcePaths.Asset_Effect_lrsc3` - 闪电命中

## 在处理器中使用投射物

投射物技能直接从 `AbilityConfig.ProjectileScene` 读取视觉场景，通过 `ProjectileTool.Spawn` 生成，不再维护独立的 `Data/Data/Projectile/*.tres`：

```csharp
protected override AbilityExecutedResult ExecuteAbility(CastContext context)
{
    var caster = context.Caster!;
    var ability = context.Ability!;
    var casterNode = (Node2D)caster;

    var projectileScene = ability.Data.Get<PackedScene>(DataKey.ProjectileScene);
    var projectile = ProjectileTool.Spawn(
        casterNode.GlobalPosition, // 生成位置
        projectileScene, // 投射物视觉
        "AbilityProjectile" // 投射物名称
    );
    if (projectile == null)
    {
        return new AbilityExecutedResult { TargetsHit = 0 };
    }

    projectile.Events.Emit(
        GameEventType.Unit.MovementStarted,
        new GameEventType.Unit.MovementStartedEventData(
            MoveMode.SineWave,
            new MovementParams
            {
                Mode = MoveMode.SineWave,
                ActionSpeed = 360f,
                MaxDistance = 800f,
                DestroyOnComplete = true,
                Collision = new MovementCollisionParams
                {
                    TeamFilter = TeamFilter.Enemy,
                    EntityTypeFilter = EntityType.Unit,
                    StopAfterCollisionCount = 1,
                    DestroyOnStop = true
                }
            }));

    return new AbilityExecutedResult { TargetsHit = 1 };
}
```

投射物命中语义约定（2026-04）：

- `MovementCollision` 现在表示“有效碰撞通知”，不再等价于“运动已经结束”
- 命中即停子弹：配置 `Collision.StopAfterCollisionCount = 1`
- 穿透子弹：配置 `Collision.StopAfterCollisionCount = N`
- 只通知不停止：配置 `Collision.StopAfterCollisionCount = -1`
- `ArcShot` 这类“追踪特定目标并自然到达后命中”的技能，不应订阅 `MovementCollision` 作为命中入口，而应使用 `MovementParams.OnStop` 并判断 `stopCtx.Reason == MovementStopReason.Completed`

投射物接入运动策略时，必须保证 `MovementParams` 和策略契约一致：

- `MoveMode.BezierCurve`
  - 至少提供 `ActionSpeed` 或 `MaxDuration` 之一
  - 两者都缺失会被视为无效配置，策略会直接完成以避免实体永久滞留
- `MoveMode.Boomerang`
  - 必须显式传入 `TargetNode = casterNode`
  - 回旋镖返程依赖该宿主节点，不能依赖祖先回溯兜底
- `MoveMode.SineWave`
  - 需保证存在明确的推进参数与结束参数，常见组合为 `ActionSpeed + MaxDistance`

如果一个投射物技能需要“飞行后自动销毁”，除了设置 `DestroyOnComplete = true`，还必须先确认对应策略一定存在可达成的完成条件。

## 技能增删查

```csharp
// 添加技能到 Entity
var ability = EntityManager.AddAbility(ownerEntity, abilityConfig);

// 获取所有技能
var abilities = EntityManager.GetAbilities(ownerEntity);

// 移除技能
EntityManager.RemoveAbility(ownerEntity, ability);
```

## 禁止事项

- ❌ 手写冷却计时逻辑 → 用 `CooldownComponent`
- ❌ 手写充能计数 → 用 `ChargeComponent`
- ❌ 把自动索敌重新写回 `AbilitySystem`
- ✅ 在 `ExecuteAbility` 中直接调用 `EntityTargetSelector.Query`
- ❌ 同类技能重复手写“查目标 + 发伤害 + DoT 定时” → 优先复用 `AbilityImpactTool`
- ❌ 绕过 `TryTrigger` 直接调用执行逻辑
- ❌ 新增 `IAbilityExecutor` / `AbilityExecutorRegistry`
- ❌ 在具体技能中重复读取 `FeatureContext.GetActivationData<CastContext>()` 或把 `Caster / Ability` 读取封装成工具方法
- ❌ 新增第二套 Ability 专属 Handler 基类；统一使用现有 `AbilityFeatureHandler`
- ❌ 绕过 `FeatureSystem` 直接手工分发技能效果
- ❌ 在 `_Process` 中直接触发技能（用 `TriggerComponent` 的 Periodic 模式）

## 关键文件路径

- **架构设计（唯一概念文档）** → `Docs/框架/ECS/Ability/技能系统架构设计理念.md`
- **核心系统** → `Src/ECS/Base/System/AbilitySystem/AbilitySystem.cs`
- **Ability Handler 中转层** → `Src/ECS/Base/System/AbilitySystem/AbilityFeatureHandler.cs`
- **Ability 薄工具（仅阵营过滤）** → `Src/ECS/Base/System/AbilitySystem/AbilityTool.cs`
- **技能命中工具** → `Src/ECS/Base/System/AbilitySystem/AbilityImpactTool.cs`
- **技能 CRUD** → `Src/ECS/Base/System/AbilitySystem/EntityManager_Ability.cs`
- **模块说明** → `Src/ECS/Base/System/AbilitySystem/README.md`
- **Feature 生命周期系统** → `Src/ECS/Base/System/FeatureSystem/FeatureSystem.cs`
- **技能实体** → `Src/ECS/Base/Entity/Ability/AbilityEntity.cs`
- **施法上下文** → `Data/EventType/Ability/CastContext.cs`
- **事件定义** → `Data/EventType/Ability/GameEventType_Ability.cs`
- **技能枚举定义** → `Data/DataKey/Ability/AbilityEnums.cs`
- **目标排序枚举** → `Src/ECS/Tools/TargetSelector/TargetSorting.cs`
- **触发组件** → `Src/ECS/Base/Component/Ability/TriggerComponent/`
- **冷却组件** → `Src/ECS/Base/Component/Ability/CooldownComponent/`
- **充能组件** → `Src/ECS/Base/Component/Ability/ChargeComponent/`
