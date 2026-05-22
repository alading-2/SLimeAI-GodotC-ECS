# AbilitySystem 模块契约

本文是 AI 修改技能系统、技能 Handler、主动输入、目标选择和 Feature 接入前必须阅读的执行契约。Feature 生命周期细节见 `DocsAI/Modules/FeatureSystem.md`。

## 职责边界

AbilitySystem 只编排正式施法提交，不做所有技能的统一自动索敌。

AbilitySystem 负责：

- 接收正式 `TryTrigger`。
- 做启用、激活、冷却、充能、资源检查。
- 消耗充能、启动冷却、消耗成本。
- 构建 `FeatureContext` 并调用 `FeatureSystem.OnFeatureActivated` / `OnFeatureEnded`。

具体技能 Handler 负责：

- 在 `ExecuteAbility` 内读取 `CastContext`。
- 自行查询目标、读取点位、生成投射物、特效或结算伤害。
- 自行定义无目标时打空、降级或失败。

TargetingManager 负责：

- 点选会话。
- 指示器输入。
- 确认后重新提交正式 `TryTrigger`。

## 核心入口

- `Src/ECS/Base/System/AbilitySystem/AbilitySystem.cs`
- `Src/ECS/Base/System/AbilitySystem/EntityManager_Ability.cs`
- `Src/ECS/Base/System/AbilitySystem/AbilityFeatureHandler.cs`
- `Src/ECS/Base/System/AbilitySystem/AbilityImpactTool.cs`
- `Src/ECS/Base/System/AbilitySystem/AbilityTool.cs`
- `Src/ECS/Base/System/TargetingSystem/`
- `Data/DataNew/Ability/AbilityData.cs`

## 数据 / 事件 / 生命周期

- 手动技能必须显式写 `AbilityType.Active + AbilityTriggerMode.Manual`。
- 技能运行时配置从 `AbilityData` 注入，`AbilityData` 的数据来源是 DataOS snapshot，不从旧 `.tres` AbilityConfig 导入。
- `FeatureHandlerId` 必须是完整唯一 Handler Id；`FeatureGroupId` 只做展示分组。
- `FeatureContext.ActivationData` 承载 `CastContext`；`ExecuteResult` 承载 `AbilityExecutedResult`。
- 点选取消不扣资源、不启动冷却、不执行技能。
- 正式 `TryTrigger` 会再次做 `CanUseAbility`，防止点选期间状态变化。
- `DamageApplyOptions.TickInterval / TotalDuration` 是单次技能执行内部 DoT 轴，不等于 `TriggerComponent.Periodic`。

## 禁止事项

- 禁止在 AbilitySystem 里做通用自动索敌。
- 禁止把点选等待当成 AbilitySystem 返回状态。
- 禁止绕过 `FeatureSystem` 自己复制技能生命周期。
- 禁止重新启用旧 `AbilityTargetSelectionComponent`。
- 禁止技能逻辑参数继续堆回 Skill 入口。

## 修改流程

1. 判断修改点属于输入、正式提交、Feature 中转、具体 Handler 还是数据配置。
2. 新技能优先写具体 Handler，不改 AbilitySystem 核心。
3. 需要伤害时优先用 `AbilityImpactTool` / `DamageTool`。
4. 需要目标时在 Handler 中用 `EntityTargetSelector.Query`。
5. 更新 `AbilityData`、相关 DataKey、测试和文档。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/AbilitySystemTest/AbilitySystemPipelineTest.tscn --build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/ActiveSkillInputTest/ActiveSkillInputTest.tscn --build`
- 涉及伤害时追加 DamageSystem 测试。

## 人工审查重点

- 是否把目标选择放错到 AbilitySystem 核心。
- 是否区分点选预检查和正式 TryTrigger。
- 是否正确使用 `FeatureHandlerId`。
- 冷却、充能、成本消耗顺序是否被破坏。
- DoT 时间轴和周期触发时间轴是否混淆。
