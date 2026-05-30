---
name: ecs-component
description: 修改 SlimeAI.GameOS GodotBridge Adapter 协议、legacy IGodotComponent 或可挂节点桥接脚本时使用；skill ID 暂保留 ecs-component 只为兼容搜索，不表示传统 ECS component。
---

# GodotBridge Adapter 入口

## 必读入口

- `DocsAI/GameOS/Contracts.md`
- `DocsAI/GameOS/ApiIndex.md`
- `DocsAI/GameOS/DebugGuide.md`
- `DocsAI/ProjectState.md`

## 源码位置

- `GameOS/GodotBridge/`
- `GameOS/Runtime/World/`
- `GameOS/Runtime/CommandBuffer/`
- `GameOS/Capabilities/*/`
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/Src/Game/`
- `/home/slime/Code/SlimeAI/SlimeAI/Src/Validation/GameOS/GodotBridge/`
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/Src/Validation/Game/`

## 规则

- 新文档和新 API 优先称为 GodotBridge Adapter；`IGodotComponent` / `Godot*Component` 是 legacy compatibility name。
- 框架 bridge adapter 当前仍实现 `IGodotComponent`，注册时通过 `GodotBridgeContext.RegisterComponents` 或默认 `GameOSGodotBridge` facade 接入 Runtime Entity。
- scoped bridge 修改必须使用 `GodotBridgeContext` 和 context-owned `GodotBridgeNodeRegistry`；static `GodotNodeRegistry` 只代表默认 context。
- adapter callback guard 必须来自目标 context 的 `RuntimeWorld.Commands.EnterGuard("godot-bridge-callback")`。
- Adapter 业务状态写入 `Entity.Data` / DataKey，不要用私有字段作为长期状态真相。
- Adapter 间通信走 `Entity.Events` 或 Capability / Runtime 服务，不直接互调具体节点方法。
- `_Process` 中避免分配对象和 LINQ。
- 单位组合入口归本 skill 路由：`GodotUnitComposer`、`GodotUnitCompositionProfile`、`GodotUnitCompositionResult` 只能组合框架通用 adapter，不引用 `BrotatoLike.*` namespace，不挂游戏输入、主动技能、HUD、波次或游戏数值逻辑。
- `GodotUnitComposer.Compose(GodotEntity2D, GodotUnitCompositionProfile)` 若在 entity 已进入 SceneTree 后追加 adapter，必须使用当前 `BridgeContext.RegisterComponents(entity, entity)` 重新注册；若未进树，调用方应在 `AddChild(entity)` 前完成 composition。
- `VisualRoot` 从 `UnitDataKeys.VisualScenePath` 加载；hurtbox circle 半径优先读 `CollisionDataKeys.CollisionRadius`，缺失或无效时才用 profile fallback。
- 修改 `GodotUnitAnimationComponent` 的 locomotion、damage/death 或 Unit animation event 行为时，要验证旧 `PlayAnimationRequested / StopAnimationRequested / AnimationFinished` 兼容性。

## 验证

```bash
Tools/run-build.sh
Tools/run-tests.sh
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/GodotBridge/UnitComposition/UnitCompositionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
