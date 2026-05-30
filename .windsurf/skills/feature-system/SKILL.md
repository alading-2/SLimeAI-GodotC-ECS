---
name: feature-system
description: 修改 SlimeAI.GameOS FeatureService、FeatureDefinition、FeatureModifierEntry、IFeatureHandler、IFeatureAction、FeatureAutoTriggerService 或 Feature 与 Ability 接入时使用。
---

# Feature Capability 入口

## 必读入口

- `DocsNew/README.md` — 方向决策入口
- `Src/ECS/**` 旁文档 — 模块事实源
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/` — 设计文档

## 源码位置

- `GameOS/Capabilities/Feature/`
- `GameOS/Capabilities/Ability/`
- `GameOS/Capabilities/Damage/`
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/Src/Game/BrotatoLikeAbilityHandlers.cs`
- `Tests/SlimeAI.GameOS.Tests/`
- 旧 ECS Feature bridge (historical): `Src/ECS/Base/System/FeatureSystem/`

## 规则

- Feature 核心不引用游戏专有类型。
- 子系统上下文优先通过 `FeatureContext.ActivationPayload` / `TryGetActivation<T>()` 传入，执行结果通过 `ExecutionResult` / `TryGetExecutionResult<T>()` 返回；`ActivationData`、`ExecuteResult`、`SourceEventData`、`ExtraData` 仅是 `[Obsolete]` 兼容入口。
- `FeatureDefinition.Actions` 承载 `IFeatureAction` 原子动作，`FeatureService.ExecuteActions` 统一执行；action 不引用 Godot 或 BrotatoLike 专有类型。
- `FeatureAutoTriggerService` 负责 Feature 自身 Periodic / OnEvent 注册，调用方必须持有并 Dispose 注册句柄。
- `Feature.TriggerChance` 使用 0-100 百分比；新增 DataKey 必须同步 DataOS descriptor / seed / snapshot mirror。
- Modifier 授予 / 回滚由 `FeatureService` 管理，避免 handler 私自写长期状态。
- 旧 ECS `Feature.Modifiers` 必须使用 descriptor stable key `Feature.Modifiers`，Granted 只调用 Data modifier API，Removed 只按 feature source 回滚，不接管 computed 计算。
- 游戏具体 Feature actions 可以先放游戏仓库 handler，抽象稳定后再上迁框架。

## 验证

```bash
Tools/run-build.sh
Tools/run-tests.sh
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Feature/FeatureCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
