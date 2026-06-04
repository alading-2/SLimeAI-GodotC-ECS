# Component Manifest

> 状态：current
> 更新：2026-06-04
> 对应 SDD：`SDD-0030 Component Code Composition And Contract Hardening`
> 源码范围：`Src/ECS/Runtime/Component/`、`Src/ECS/Runtime/Entity/Components/`、`Src/ECS/Capabilities/*/Component/`、`Src/ECS/Capabilities/*/Entity/*ComponentCompositionProfiles.cs`

## 1. Runtime Contract

| Contract | 位置 | 规则 |
| --- | --- | --- |
| `IComponent` | `Src/ECS/Runtime/Component/IComponent.cs` | 只定义 `OnComponentRegistered(Node)` / `OnComponentUnregistered()`，不扩弱类型参数。 |
| `ComponentRegistrar` | `Src/ECS/Runtime/Entity/Components/ComponentRegistrar.cs` | 维护 Entity -> Component owner index；不恢复旧 Entity-Component relationship 常量。 |
| `IComponentCompositionProvider` | `Src/ECS/Runtime/Component/ComponentComposition.cs` | Entity 默认组件组合入口；Runtime 只认识通用 profile，不引用 Capability 具体组件。 |
| `ComponentComposer` | `Src/ECS/Runtime/Component/ComponentComposition.cs` | 注册前创建默认组件并注入 typed options；已有同名子节点时跳过。 |

注册顺序：

```text
EntitySpawnPipeline / EntityManager.RegisterComponents
  -> ComponentComposer.Compose(entity)
  -> ComponentRegistrar.RegisterComponents(entity)
  -> IComponent.OnComponentRegistered(entity)
```

`EntityManager.RegisterComponents(entity)` 也会先执行 `ComponentComposer.Compose(entity)`。如果 entity 实现了 `IComponentCompositionProvider`，本次注册会绕过预热路径缓存并实时扫描，避免代码化组合的组件被旧 cache 漏掉。

## 2. Composition Profiles

| Entity | Profile | 组件集合 |
| --- | --- | --- |
| `PlayerEntity` | `UnitComponentCompositionProfiles.Player()` | `UnitCore` + `PickupComponent` + `ActiveSkillInputComponent` |
| `EnemyEntity` | `UnitComponentCompositionProfiles.Enemy()` | `AIComponent` + `AttackComponent` + `UnitCore` |
| `TargetingIndicatorEntity` | `UnitComponentCompositionProfiles.UnitCore()` | `UnitCore`；`TargetingIndicatorControlComponent` 仍由 root scene 显式挂载 |
| `AbilityEntity` | `AbilityComponentCompositionProfiles.Default()` | `TriggerComponent` + `CooldownComponent` + `ChargeComponent` + `CostComponent` |

`UnitCore` 当前组件集合：

```text
HealthComponent
LifecycleComponent
UnitStateComponent
RecoveryComponent
DataInitComponent
UnitAnimationComponent
EntityMovementComponent
EntityOrientationComponent
CollisionComponent
```

`EntityOrientationComponent` 由 `EntityOrientationComponentOptions(OrientationSink.VisualFlipX)` 注入输出策略。该值是 Godot bridge 结构参数，不进入 DataOS，也不新增 DataKey。

旧 `Src/ECS/Capabilities/*/Presets/*Preset.tscn` 只作为 legacy 对照输入保留；新默认组合不新增 Component Preset，也不通过 Inspector 导出参数承载默认参数。

## 3. Component Catalog

| Owner | Component | 主要风险点 |
| --- | --- | --- |
| AI | `AIComponent` | `_Process` 驱动行为树。 |
| Ability | `TriggerComponent` | `TimerManager` 周期触发。 |
| Ability | `CooldownComponent` | `Entity.Events` + `TimerManager.Delay`。 |
| Ability | `ChargeComponent` | `Entity.Events` + `TimerManager.Loop`。 |
| Ability | `CostComponent` | `Entity.Events`，读写 Data。 |
| Ability | `ActiveSkillInputComponent` | `_Process` 读输入；只属于 Player profile。 |
| Collision | `CollisionComponent` | Godot 物理信号桥接。 |
| Collision | `ContactDamageComponent` | `Entity.Events` + per-target `TimerManager.Loop`。 |
| Collision | `HurtboxComponent` | `Area2D` hurtbox bridge。 |
| Collision | `PickupComponent` | `Area2D` pickup bridge。 |
| Effect | `EffectComponent` | 生命周期 timer 与 effect runtime state。 |
| Movement | `EntityMovementComponent` | `_Process` / `_PhysicsProcess` 移动调度，监听移动与碰撞事件。 |
| Movement | `EntityOrientationComponent` | `_Process` 朝向输出，typed options 注入 sink。 |
| Unit | `AttackComponent` | 攻击前后摇与验证 timer。 |
| Unit | `DataInitComponent` | 生成后数据初始化桥。 |
| Unit | `HealthComponent` | 监听 heal request，写 HP Data。 |
| Unit | `LifecycleComponent` | 生命、死亡、复活 timer 与事件。 |
| Unit | `RecoveryComponent` | 监听 Data 变化，处理恢复逻辑。 |
| Unit | `StatusControllerComponent` | 状态 timer 管理。 |
| Unit | `UnitAnimationComponent` | `_Process` 动画状态与 Unit animation events。 |
| Unit | `UnitStateComponent` | Unit runtime state bridge。 |
| Unit | `TargetingIndicatorControlComponent` | `_Process` 跟随目标选择输入；root scene 显式组件。 |

## 4. Authoring Rules

- 新组件优先实现 `IComponent`，初始化只放 `OnComponentRegistered`，清理只放 `OnComponentUnregistered`。
- 具体业务组件放 `Src/ECS/Capabilities/<owner>/Component/`；Runtime/Component 只保留通用 contract。
- 默认组件组合写 C# profile：`ComponentCompositionProfile` / `ComponentCompositionEntry` / `ComponentComposer`。
- 固定结构参数用 `Configure(TOptions)` 或构造期 options；共享业务状态、runtime snapshot 配置和跨系统可观察结果进入 `Entity.Data`。
- 禁止新增 Inspector 导出参数默认配置作为 Component contract。
- Component 间通信优先 `Entity.Events`；少数兼容或调试场景才使用 `EntityManager.GetComponent<T>()`。

当前 documented `GetComponent<T>()` 例外：

- 调试、测试和迁移兼容代码。
- 必须直连 Godot bridge 节点且已有 owner 文档说明原因的局部路径。
- 旧调用点清理前的过渡代码；新增业务流程应优先事件或 service。

## 5. Verification Gates

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate SDD-0030
python3 Workspace/SDD/sdd.py validate --all
rg -n "\\[Export\\]" Src/ECS/Capabilities/*/Component Src/ECS/Runtime/Component -g '*.cs'
rg -n "Capabilities/.*/Presets/.*Preset\\.tscn" Src/ECS/Capabilities/*/Entity -g '*.tscn'
# 旧 Entity-Component relationship 常量 gate：按 ecs-component skill 中的 grep 门禁执行，预期无命中。
```

Godot CLI / 承载游戏 runner 可用时，补跑：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Src/ECS/Runtime/Entity/Tests/EntitySpawnPipelineRuntimeTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/ECS/Runtime/Entity/Tests/ComponentRegistrarRuntimeTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
