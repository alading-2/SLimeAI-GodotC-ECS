# FeatureSystem 模块契约

本文是 AI 修改通用能力生命周期、Feature Handler、Modifier 和 Ability 接入时必须阅读的执行契约。人类背景见 `Docs/框架/ECS/System/FeatureSystem/FeatureSystem.md`。

## 职责边界

FeatureSystem 是通用能力生命周期层，只依赖 `IEntity`，不依赖 Ability、CastContext、UI 或测试模块专有类型。

FeatureSystem 负责：

- `Granted / Removed` 一次性授予和移除。
- `Enabled / Disabled` 启停状态切换。
- `Activated / Execute / Ended` 单次运行生命周期。
- 授予时应用 `FeatureModifierEntry`，移除时按 `source=feature` 回滚 Modifier。
- 通过 `FeatureContext.ActivationData` 接受子系统上下文，通过 `ExecuteResult` 返回子系统结果。

FeatureSystem 不负责：

- 技能冷却、充能、资源消耗和点选流程。
- UI 测试模块生命周期。
- 目标选择、投射物生成、伤害计算等具体玩法效果。
- 读取子系统专有类型或直接引用 `AbilitySystem`。

## 核心入口

- `Src/ECS/Base/System/FeatureSystem/FeatureSystem.cs`
- `Src/ECS/Base/System/FeatureSystem/FeatureContext.cs`
- `Src/ECS/Base/System/FeatureSystem/FeatureInstance.cs`
- `Src/ECS/Base/System/FeatureSystem/IFeatureHandler.cs`
- `Src/ECS/Base/System/FeatureSystem/FeatureHandlerRegistry.cs`
- `Src/ECS/Base/System/FeatureSystem/FeatureEndReason.cs`
- `Src/ECS/Base/System/FeatureSystem/Action/`
- `Data/Feature/Definition/FeatureDefinition.cs`
- `Data/Feature/Definition/FeatureModifierEntry.cs`
- `Data/DataOS/Snapshots/runtime_snapshot.json`
- `Data/EventType/Feature/GameEventType_Feature.cs`

## 数据 / 事件 / 生命周期

- 生命周期顺序是 `Granted -> Enabled/Disabled -> Activated -> Execute -> Ended -> Removed`。
- 简单属性 Feature 优先只配置 `Modifiers`，不写 `IFeatureHandler`。
- 复杂逻辑实现 `IFeatureHandler`，用完整唯一 `FeatureId` 注册到 `FeatureHandlerRegistry`。
- 子系统专有输入放 `FeatureContext.ActivationData`，Handler 自行转型。
- Handler 的 `OnExecute` 返回值会写入 `FeatureContext.ExecuteResult`，调用方自行转型。
- Ability 子域通过 `AbilityFeatureHandler` 把 `CastContext` 转给具体技能逻辑。
- `ability` snapshot record 的 `FeatureHandlerId` 是运行时 Handler 主键；`FeatureGroupId` 只做 UI / 测试分组。
- TestSystem 调试 Feature 时必须复用 `FeatureDebugService` 和正式生命周期。

## 禁止事项

- 禁止在 FeatureSystem 核心引用 `CastContext`、`AbilityEntity` 或具体技能类型。
- 禁止把 TestSystem 模块做成 `IFeatureHandler`。
- 禁止用 FeatureSystem 替代 DamageSystem 手写伤害结算。
- 禁止在 Handler 中绕过 `EntityManager`、`SystemManager`、`TimerManager` 等门禁。
- 禁止让 `FeatureGroupId` 参与运行时 Handler 查找。
- 禁止移除 Feature 时忘记解除全局事件订阅或取消定时器。

## 修改流程

1. 判断需求是纯数据 Modifier、复杂 Handler、Ability 接入、测试调试还是 DataKey / EventType。
2. 纯属性加成优先配置 `FeatureModifierEntry`。
3. 复杂逻辑新增 `IFeatureHandler` 并注册完整唯一 `FeatureId`。
4. Ability 技能逻辑优先继承 `AbilityFeatureHandler`，在 `ExecuteAbility(CastContext)` 中做具体效果。
5. 需要目标选择读 `DocsAI/Modules/Tools.md`，需要伤害读 `DocsAI/Modules/DamageSystem.md`。
6. 更新 `DocsAI/Modules/FeatureSystem.md`、相关 Skill 映射和项目索引。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/AbilitySystemTest/AbilitySystemPipelineTest.tscn --build`
- 涉及 TestSystem 调试入口时运行 `res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn`。
- 涉及核心生命周期或系统门禁时追加 `DocsAI/Tests/测试矩阵.md` 中对应场景。

## 人工审查重点

- FeatureSystem 是否保持对子系统零依赖。
- `FeatureHandlerId` 是否完整唯一并能注册。
- `ActivationData` / `ExecuteResult` 是否只在子系统边界转型。
- Modifier 是否能授予时应用、移除时整体回滚。
- Handler 的事件订阅、Timer 和对象池资源是否有清理点。
