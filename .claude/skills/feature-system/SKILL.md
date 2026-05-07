---
name: feature-system
description: 实现或修改通用能力生命周期时使用。适用于：新增 FeatureDefinition、配置 Modifiers、实现 IFeatureHandler、把子系统上下文装入 FeatureContext、将 FeatureSystem 接入 AbilitySystem。触发关键词：FeatureSystem、IFeatureHandler、FeatureContext、FeatureDefinition、FeatureModifierEntry、FeatureHandlerRegistry、FeatureHandlerId。
---

# FeatureSystem 入口

## 什么时候用

- 新增可授予 / 移除的能力、被动词条、装备效果、光环、状态效果。
- 配置 `FeatureModifierEntry`。
- 实现或注册 `IFeatureHandler`。
- 修改 `FeatureContext`、`FeatureSystem`、`FeatureHandlerRegistry`。
- 判断 Ability 子域如何接入 Feature 生命周期。

## 转向其它 Skill

- 主动技能、冷却、充能、点选 -> `@ability-system`
- 伤害结算 -> `@damage-system`
- DataKey / Feature 数据配置 -> `@data-authoring`
- TestSystem 调试入口 -> `@test-system`

## 必读

- `DocsAI/Modules/FeatureSystem.md`
- Ability 接入读 `DocsAI/Modules/AbilitySystem.md`
- 数据目录读 `DocsAI/Modules/DataAuthoring.md`
- 涉及核心门禁读 `DocsAI/Workflows/ECS核心修改门禁.md`

## 最短流程

- SkilmeAI 迁移目标已存在 Feature Runtime 最小生命周期：`/home/slime/Code/SkilmeAI/SkilmeAI/GameOS/Capabilities/Feature`。
- 当前覆盖 `FeatureService.Grant / Remove / Enable / Disable / Activate / End`、Modifier 授予回滚、`IFeatureHandler` 生命周期、`GameEventType.Feature` 和 AbilityService 可选接入。
- 仍未迁入大部分 Feature actions 和具体 Ability 逻辑；DataOS authoring 已到 M27 第三段，Ability / Projectile / Effect / Movement handler 参数已可从 snapshot 写入 Runtime Data，SineWave / Boomerang / BezierCurve / CircularArc / Orbit 和 ChainLightning 已通过游戏侧 Feature handler 执行闭环接入。

1. 判断需求是纯 Modifier、复杂 Handler、Ability 接入还是测试调试。
2. 纯属性加成优先写 `FeatureModifierEntry`。
3. 复杂逻辑实现 `IFeatureHandler`，注册完整唯一 `FeatureId`。
4. 子系统输入放 `FeatureContext.ActivationData`，执行结果从 `ExecuteResult` 取。
5. Handler 中的事件订阅、Timer、对象池资源必须有清理点。
6. 运行构建和 Ability / TestSystem 相关场景。
7. 更新 `DocsAI/Modules/FeatureSystem.md` 或相关映射。

## 禁止事项

- 不要在 FeatureSystem 核心引用 Ability 专有类型。
- 不要把 TestSystem 模块做成 `IFeatureHandler`。
- 不要让 `FeatureGroupId` 参与运行时 Handler 查找。
- 不要绕过 DamageSystem、EntityManager、SystemManager 等门禁。
- 不要忘记移除全局订阅或取消定时器。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/AbilitySystemTest/AbilitySystemPipelineTest.tscn --build
```
