---
name: project-index
description: 查找项目任意模块的文档、源码、模板文件时使用。当需要了解项目整体架构、定位某个系统的实现文件、查找设计文档或 API 手册时自动触发。这是整个 Godot C# ECS 项目的导航地图。
---

# 项目导航地图 - Godot 复刻土豆兄弟

**架构**：Godot 伪 ECS（Scene Tree 处理渲染/物理 + 组件化逻辑 + 数据驱动状态）
**技术栈**：Godot 4.6 / C# / .NET 8.0

---

## 模块速查：我需要什么，去哪找

### Entity（实体生命周期）

- **接口定义** → `Src/ECS/Base/Entity/IEntity.cs`
- **标准模板** → `Src/ECS/Base/Entity/TemplateEntity.cs` ← 新建 Entity 从这里复制
- **生命周期管理 API** → `Src/ECS/Base/Entity/Core/EntityManager.md`
- **核心实现** → `Src/ECS/Base/Entity/Core/EntityManager.cs`
- **对象池激活时序** → 对象池 Entity 必须先完成位置/旋转设置与 `ForceUpdateTransform()`，再恢复处理和碰撞
- **宿主回溯解析** → `Src/ECS/Base/Entity/Core/EntityManager_Component.cs`（从任意碰撞子节点回溯所属 `Entity / IEntity`）
- **生成参数角度语义** → `EntitySpawnConfig.Rotation` 对外使用度，内部写入 `GlobalRotationDegrees`
- **关系管理** → `Src/ECS/Base/Entity/Core/EntityRelationshipManager.cs`
- **开发规范** → `Src/ECS/Base/Entity/Entity规范.md`
- **架构设计** → `Docs/框架/ECS/Entity/Entity架构设计理念.md`

### 碰撞 / Collision

- **碰撞系统总览** → `Docs/框架/ECS/Collision/碰撞系统说明.md`
- **碰撞层级说明** → `Docs/框架/ECS/Collision/碰撞层级说明.md`
- **本次排查总结** → `Docs/思考/碰撞问题路径/2026-04-03_对象池碰撞误触发总结.md`
- **碰撞桥接组件** → `Src/ECS/Base/Component/Collision/CollisionComponent/CollisionComponent.cs`
- **受击区桥接组件** → `Src/ECS/Base/Component/Collision/HurtboxComponent/HurtboxComponent.cs`（组件本身即 `Area2D`，直接挂在 Entity 场景上）
- **接触伤害组件** → `Src/ECS/Base/Component/Collision/ContactDamageComponent/ContactDamageComponent.cs`
- **碰撞事件定义** → `Data/EventType/Base/Collision/GameEventType_Collision.cs`
- **碰撞预设资源** → 碰撞模板场景（用于视觉体与受击区的 Area2D 预配置）
- **碰撞预设说明** → 碰撞预设目录说明文档
- **SpriteFramesGenerator 规则** → `addons/SpriteFramesGenerator/sprite_frames_config.gd`
- **ResourceGenerator** → `Tools/ResourceGenerator/ResourceGenerator.cs`（当前仅生成资源路径索引，不再生成碰撞注册表）
- **关键约定**：视觉体碰撞由 `CollisionComponent` 桥接，受击区碰撞由 `HurtboxComponent` 自身 `Area2D` 事件负责，`ContactDamageComponent` 只消费 `HurtboxEntered / HurtboxExited`

### Component（组件）

- **接口定义** → `Src/ECS/Base/Component/IComponent.cs`
- **标准模板** → `Src/ECS/Base/Component/TemplateComponent.cs` ← 新建 Component 从这里复制
- **开发规范** → `Src/ECS/Base/Component/Component规范.md`
- **设计理念** → `Docs/框架/ECS/Component/Component数据驱动设计理念.md`
- **现有组件目录**：
  - 通用单位组件 → `Src/ECS/Base/Component/Unit/Common/`（HealthComponent、AttackComponent、UnitAnimationComponent 等）
  - 技能组件 → `Src/ECS/Base/Component/Ability/`（CooldownComponent、ChargeComponent、TriggerComponent 等）
  - 玩家组件 → `Src/ECS/Base/Component/Player/`

### Data（数据容器）

- **核心容器** → `Src/ECS/Base/Data/Data.cs`
- **使用指南** → `Src/ECS/Base/Data/README.md`
- **Data 顶层分工** → `Data/README.md`
- **Config / Resource 映射** → `Data/Data/README.md`
- **DataKey 定义规范** → `Data/DataKey/README.md`
- **DataKey 定义目录** → `Data/DataKey/`
  - 基础键 → `Data/DataKey/Base/`
  - 单位属性 → `Data/DataKey/Unit/`
  - 状态快照键 → `Data/DataKey/Unit/DataKey_Status.cs`
  - 技能数据 → `Data/DataKey/Ability/`
  - 属性系统 → `Data/DataKey/Attribute/`
  - AI 数据 → `Data/DataKey/AI/`
- **架构设计** → `Docs/框架/ECS/Data/DataSystem_Design.md`
- **Skill 分工**：运行时容器问题看 `@ecs-data`，Data 目录配置与映射问题看 `@data-authoring`

### EventBus（事件系统）

- **核心引擎** → `Src/ECS/Base/Event/EventBus.cs`
- **全局总线** → `Src/ECS/Base/Event/GlobalEventBus.cs`
- **事件上下文** → `Src/ECS/Base/Event/EventContext.cs`
- **最佳实践** → `Src/ECS/Base/Event/README_EventBus.md`
- **事件类型定义目录** → `Data/EventType/`
  - 技能事件 → `Data/EventType/Ability/GameEventType_Ability.cs`
  - 单位事件 → `Data/EventType/Unit/`
  - 攻击事件 → `Data/EventType/Unit/Attack/GameEventType_Attack.cs`
  - 瞄准事件 → `Data/EventType/Unit/Targeting/GameEventType_Targeting.cs`

### System Core（系统治理 / 项目状态 / 实体状态）

- **总览入口** → `docs/框架/ECS/System/Core/系统与状态分层总览.md`
- **正式设计文档** → `docs/框架/ECS/System/Core/系统生命周期与项目状态设计.md`
- **状态效果设计文档** → `docs/框架/ECS/System/实体状态效果系统设计.md`
- **历史三案 ADR** → `docs/框架/ECS/System/Core/系统生命周期三案设计.md`
- **历史状态分析** → `docs/框架/ECS/System/实体状态管理与AI系统协调方案.md`
- **启动入口** → `Src/ECS/Base/System/Core/SystemManager.cs`
- **系统运行时管理器** → `Src/ECS/Base/System/Core/SystemManager.cs`（启动和 `ProjectState` 切换后会输出系统状态报告；单系统异常记录后继续装载其他系统）
- **系统注册表** → `Src/ECS/Base/System/Core/SystemRegistry.cs`（重复 `SystemId` 记录错误日志并保留首个描述符）
- **系统描述符** → `Src/ECS/Base/System/Core/SystemDescriptor.cs`（只保存 `SystemId + Factory`）
- **系统配置 DataNew** → `Data/DataNew/System/SystemData.cs`（唯一运行时数据源）
- **系统预设 DataNew** → `Data/DataNew/System/SystemPresetData.cs`（唯一运行时数据源；默认预设显式加载 TestSystem / MouseSelectionSystem，不整体启用 Debug / Test 标签）
- **旧系统配置资源** → `Data/Config/System/System/SystemConfig.cs` / `Data/Config/System/System/Resource/*.tres`（保留但运行时不导入）
- **旧系统预设资源** → `Data/Config/System/Preset/SystemPreset.cs` / `Data/Config/System/Preset/Resource/*.tres`（保留但运行时不导入）
- **项目状态服务** → `Src/ECS/Base/System/Core/State/ProjectStateService.cs`
- **项目状态切换参数** → `Src/ECS/Base/System/Core/State/ProjectStateChangedEventArgs.cs`
- **项目状态桥接** → `Src/ECS/Base/System/Core/State/ProjectStateBridge.cs`（把 `GameStart / GamePause / GameResume / GameOver` 统一转成项目状态）
- **运行条件** → `Src/ECS/Base/System/Core/Lifecycle/SystemRunCondition.cs`
- **系统接口** → `Src/ECS/Base/System/Core/Lifecycle/ISystem/ISystem.cs`
- **系统命令接口** → `Src/ECS/Base/System/Core/Lifecycle/ISystem/ISystemCommandHandler.cs`
- **关键边界**：代码注册只管 `SystemId + Factory`，`SystemData` 管系统元数据、三域运行条件和外部命令门禁，`SystemPresetData` 管启动装载选择，`Enable/Disable` 管运行时人工开关，`OnStarted/OnStopped` 管实际运行态
- **状态控制组件** → `Src/ECS/Base/Component/Unit/Common/StatusControllerComponent/StatusControllerComponent.cs`
- **状态聚合器** → `Src/ECS/Base/System/Status/StatusCollection.cs`
- **状态快照** → `Src/ECS/Base/System/Status/StatusSnapshot.cs`
- **关键边界**：`SystemManager` 管系统启停，`ProjectStateService` 管项目级运行态，`StatusControllerComponent` 管实体状态效果，`Src/ECS/AI` 行为树只管单体敌人决策
- **暂停菜单系统** → `Src/ECS/Base/System/PauseMenu/PauseMenuSystem.cs` / `.tscn`（`Overlay` 生命周期；负责暂停输入和暂停菜单显隐）

### AbilitySystem（技能系统）

- **核心系统** → `Src/ECS/Base/System/AbilitySystem/AbilitySystem.cs`
- **技能 CRUD** → `Src/ECS/Base/System/AbilitySystem/EntityManager_Ability.cs`
- **模块说明** → `Src/ECS/Base/System/AbilitySystem/README.md`
- **技能实体** → `Src/ECS/Base/Entity/Ability/AbilityEntity.cs`
- **施法上下文** → `Data/EventType/Ability/CastContext.cs`
- **技能配置** → `Data/DataNew/Ability/AbilityData.cs`（`FeatureGroupId` 仅用于 UI / 调试分组；`FeatureHandlerId` 是运行时处理器映射主键）
- **枚举定义** → `Data/DataKey/Ability/AbilityEnums.cs`
- **架构设计（唯一概念文档）** → `Docs/框架/ECS/Ability/技能系统架构设计理念.md`
- **内置技能组件**：
  - 触发 → `Src/ECS/Base/Component/Ability/TriggerComponent/`
  - 冷却 → `Src/ECS/Base/Component/Ability/CooldownComponent/`
  - 充能 → `Src/ECS/Base/Component/Ability/ChargeComponent/`
  - 消耗 → `Src/ECS/Base/Component/Ability/CostComponent/`
  - 目标选择 → `Src/ECS/Base/Component/Ability/AbilityTargetSelectionComponent/`

### EffectSystem（特效系统）

- **核心工具** → `Src/ECS/Base/System/EffectSystem/EffectTool.cs`（统一入口 `EffectTool.Spawn`，独立特效位置使用 `EffectSpawnOptions.EffectPosition`；Host? 可选参数区分独立/附着；不走 `EntityManager.Spawn`，而是内部执行 Effect 专用 Spawn 编排；`EffectSpawnOptions.Rotation` 对外使用度）
- **特效实体** → `Src/ECS/Base/Entity/Effect/EffectEntity.cs`（Node2D + IEntity + IPoolable）
- **特效组件** → `Src/ECS/Base/Component/Effect/EffectComponent/EffectComponent.cs`（完整特效管理：附着跟随、MaxLifeTime 计时器、直接控制 AnimatedSprite2D 播放、宿主销毁监听）
- **参数入口** → `EffectTool.Spawn(new EffectSpawnOptions(visualScenePath, Host: host, EffectPosition: position))`
- **宿主销毁监听** → EffectComponent 监听 `GameEventType.Global.EntityDestroyed`（通用 Entity 销毁事件）
- **特效 DataKey** → `Data/DataKey/Effect/DataKey_Effect.cs`
- **设计提示词** → `Docs/框架/ECS/Entity/特效系统_Entity生成提示词.md`

### DamageSystem（伤害系统）

- **核心服务** → `Src/ECS/Base/System/DamageSystem/DamageService.cs`
- **伤害信息** → `Src/ECS/Base/System/DamageSystem/DamageInfo.cs`
- **处理器接口** → `Src/ECS/Base/System/DamageSystem/IDamageProcessor.cs`
- **扩展指南** → `Src/ECS/Base/System/DamageSystem/README.md`
- **内置处理器目录** → `Src/ECS/Base/System/DamageSystem/Processors/`
- **设计理念** → `Docs/框架/ECS/System/伤害系统设计理念.md`

### UI System（UI 系统）

- **核心基类** → `Src/ECS/UI/Core/UIBase.cs`
- **管理器** → `Src/ECS/UI/Core/UIManager.cs`
- **开发指南** → `Src/ECS/UI/README.md`
- **架构设计** → `Docs/框架/UI/UI架构设计理念.md`
- **现有 UI 组件**：
  - 血条 → `Src/ECS/UI/UI/HealthBarUI/HealthBarUI.cs`
  - 伤害数字 → `Src/ECS/UI/UI/DamageNumberUI/DamageNumberUI.cs`
  - 技能栏 → `Src/ECS/UI/UI/SkillUI/ActiveSkillBarUI.cs`
  - 技能槽 → `Src/ECS/UI/UI/SkillUI/ActiveSkillSlotUI.cs`

### TestSystem（运行时测试系统）

- **系统宿主** → `Src/ECS/Base/System/TestSystem/TestSystem.cs`
- **模块基类** → `Src/ECS/Base/System/TestSystem/TestModuleBase.cs`
- **属性测试模块** → `Src/ECS/Base/System/TestSystem/Attribute/AttributeTestModule.cs`
- **调试适配层** → `Src/ECS/Base/System/TestSystem/FeatureDebugService.cs`（统一转发技能 / Feature / 临时 Modifier 调试操作）
- **技能测试服务** → `Src/ECS/Base/System/TestSystem/Ability/AbilityTestService.cs`（按完整 `FeatureGroupId` 分组和显示）
- **技能测试视图模型** → `Src/ECS/Base/System/TestSystem/Ability/AbilityTestViewModels.cs`
- **技能测试界面** → `Src/ECS/Base/System/TestSystem/Ability/AbilityTestModule.cs`
- **源码目录说明** → `Src/ECS/Base/System/TestSystem/README.md`
- **正式说明** → `Docs/框架/ECS/System/TestSystem.md`
- **专用 Skill** → `.codex/skills/test-system/SKILL.md`
- **场景运行 Skill** → `.codex/skills/godot-scene-test/SKILL.md`；CLI runner → `scripts/godot-scene-runner.mjs`

### AI System（AI 系统）

- **运动策略调度器** → `Src/ECS/Base/Component/Movement/EntityMovementComponent.cs`（统一 Node2D/CharacterBody2D；含 PlayerInput/AIControlled）
- **通用朝向组件** → `Src/ECS/Base/Component/Movement/EntityOrientationComponent.cs`（唯一朝向输出层；既可跟随 `MovementFacingDirection`，也可纯自转或叠加自转，并通过 `OrientationSink` 分流到 `RootRotation` 或 `VisualFlipX`）
- **角度语义约定** → `MovementParams` 对外角度输入统一使用“度”（`Angle / Orbit* / WavePhase`），策略内部仅在三角函数/旋转计算时转弧度
- **运动通用工具** → `Src/ECS/Base/System/Movement/Utils/MovementHelper.cs`（跨策略通用能力）
- **BezierCurve 驱动约束** → `Src/ECS/Base/System/Movement/Strategies/Curve/BezierCurveStrategy.cs`（现支持 `ActionSpeed` / `MaxDuration` 双驱动；两者都缺失会直接完成，避免实体永久滞留）
- **Boomerang 返程宿主约束** → `Src/ECS/Base/System/Movement/Strategies/Projectile/BoomerangStrategy.cs`（调用方应显式传入 `TargetNode` 作为返程宿主，不要依赖祖先回溯）
- **BoomerangThrow 技能语义** → `Data/Data/Ability/Ability/Movement/BoomerangThrow/BoomerangThrow.cs`（去程目标点改为施法者周围圆环随机点；自转改走通用 `Orientation` 参数，不要再把回旋表现写死进策略）
- **通用参数驱动代码** → `Src/ECS/Base/System/Movement/ScalarDriver/ScalarMotion.cs`（`ScalarDriver` 实现，统一处理 Orbit 半径、Wave 振幅/频率的上下限、PingPong、BounceDecay、触边完成）
- **通用参数驱动说明** → `Src/ECS/Base/System/Movement/ScalarDriver/README.md`（说明职责边界、运行模型、边界模式与接入方式）
- **Orbit 专用工具（partial）** → `Src/ECS/Base/System/Movement/Strategies/Orbit/MovementHelper.Orbit.cs`
- **行为树运行器** → `Src/ECS/AI/Core/BehaviorTreeRunner.cs`
- **节点基类** → `Src/ECS/AI/Core/BehaviorNode.cs`
- **运行时上下文** → `Src/ECS/AI/Core/AIContext.cs`
- **敌人行为树** → `Src/ECS/AI/Nodes/EnemyBehaviorTreeBuilder.cs`
- **AI DataKey** → `Data/DataKey/AI/DataKey_AI.cs`
- **状态协同** → `AIComponent` 已接入 `DataKey.StatusCanThink`；AI 行为树不负责项目级状态和系统启停
- **架构说明** → `Docs/框架/ECS/System/AI/AI系统说明.md`
- **源码说明** → `Src/ECS/AI/README.md`

### Tools（工具类）

- **TimerManager** → `Src/ECS/Tools/Timer/TimerManager.cs` | 文档 → `Src/ECS/Tools/Timer/TimerManager.md`（项目级暂停时自动暂停 scaled timer）
- **ObjectPool** → `Src/ECS/Tools/ObjectPool/ObjectPool.cs` | 文档 → `Src/ECS/Tools/ObjectPool/ObjectPool.md`
- **对象池碰撞时序** → `ObjectPool.Get(false)` 先保持禁用，`EntityManager.Spawn()` 完成变换与注册后再显式激活
- **TargetSelector** → `Src/ECS/Tools/TargetSelector/TargetSelector.cs` | 文档 → `Src/ECS/Tools/TargetSelector/README.md`
- **ResourceManagement** → `Data/ResourceManagement/ResourceManagement.cs` | 文档 → `Data/ResourceManagement/ResourceManagement.md`
- **Log** → `Src/ECS/Tools/Logger/Log.cs`
- **日志规范**：业务/系统代码统一使用 `Log`（`Info/Warn/Error` 等）输出；禁止直接调用 `GD.Print` / `GD.PrintRich` / `GD.PushWarning` / `GD.PushError`（`Log` 内部封装除外）
- **InputManager** → `Src/ECS/Tools/Input/InputManager.cs`
- **ObjectPoolInit（初始化配置）** → 搜索 `ObjectPoolInit.cs`

### 数据与配置

- **ResourceManagement（资源加载）** → `Data/ResourceManagement/ResourceManagement.cs`
- **ResourcePaths（自动生成路径索引）** → `Data/ResourceManagement/ResourcePaths.cs`
- **DataConfigEditor（纯 C# 配置表格编辑器）** → `addons/DataConfigEditor/`
- **DataConfigEditor 元数据入口** → `addons/DataConfigEditor/ConfigReflectionCache.cs`（Property -> DataKey -> DataMeta 映射、路径键别名、const/DataMeta 双支持）
- **DataConfigEditor 交互入口** → `addons/DataConfigEditor/ConfigTablePanel.cs`（四层表头、批量修改、普通枚举下拉、Flags 勾选）
- **路径拖拽与校验** → `addons/DataConfigEditor/PathLineEdit.cs`（支持拖拽项目路径、自动转 `res://`、即时校验）
- **表格空白排查点** → `addons/DataConfigEditor/ConfigTablePanel.cs` 的 `SetContentVisibility()`；若状态栏已有“属性/实例/源文件”，但中间表格为空，先查父级 `_scrollV` 是否和 `_grid` 同步显示
- **SpriteFramesGenerator（C#）** → `addons/SpriteFramesGenerator_CSharp/SpriteFramesGeneratorPlugin.cs`
- **SpriteFramesGenerator（GDScript）** → `addons/SpriteFramesGenerator/sprite_frames_generator_plugin.gd`
- **生成器规则表** → `addons/SpriteFramesGenerator_CSharp/SpriteFramesConfig.cs` / `addons/SpriteFramesGenerator/sprite_frames_config.gd`
- **DataForge 插件（可视化数据编辑）** → `addons/DataForge/`
- **系统启动入口** → `Src/ECS/Base/System/Core/SystemManager.cs`（唯一 autoload；负责 ParentManager 初始化和系统树启动）

---

## 如何使用这份地图

1. **新建某类型文件** → 找对应的"标准模板"文件复制
2. **查 API 用法** → 找对应的 `.md` 文档
3. **理解设计决策** → 找 `Docs/框架/` 下的设计理念文档
4. **找事件类型定义** → `Data/EventType/` 目录
5. **找 DataKey / DataMeta 定义** → `Data/DataKey/` 目录
6. **找现有实现参考** → 对应模块的 `Src/` 目录

---

## 完整文档索引

详细架构文档：`Docs/框架/项目索引.md`（人类可读的完整导航）
