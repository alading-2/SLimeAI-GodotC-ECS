---
name: test-system
description: 修改 SlimeAI.GameOS 测试、Validation、Observation、日志分析或测试包装脚本时使用。
---

# Validation / Test 入口

## 必读入口

- `DocsAI/ProjectState.md`
- `DocsAI/GameOS/DebugGuide.md`
- `Tests/SlimeAI.GameOS.Tests/`
- `/home/slime/Code/SlimeAI/SlimeAI/DocsAI/Tests/GodotSceneTesting.md`

## 源码位置

- `Tests/SlimeAI.GameOS.Tests/`
- `GameOS/Validation/`
- `GameOS/Observation/`
- `Tools/run-build.sh`
- `Tools/run-tests.sh`
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/Tools/run-godot-scene.sh`

## 规则

- 框架纯逻辑优先补 Runtime tests。
- **新 Runtime / Capability 测试 MUST 使用 `using var world = RuntimeWorld.CreateScoped();` 隔离状态，禁止直接 mutate `RuntimeWorld.Default` / `EntityManager.Clear()` / `WorldEvents.World.Clear()` 作为新测试 setup**。仅允许旧 backlog 用例在尚未支持显式 world 注入的 static Capability 工具中保留手工 Clear，并登记到 `DocsAI/GameOS/Migration.md`。
- **Capability Service 测试必须用 `new XxxService()` 独立实例，禁用 `Default / Instance`**。例如 `new DamageService()`、`new DamageService(new HealService())`、`new AbilityService(timerManager)`。两个 scoped world 测试共享 `XxxService.Default` 会导致状态污染。
- RuntimeWorld dispose 顺序事实源：`DocsAI/GameOS/Contracts.md#runtime-world-契约`；P4 后固定为 `Schedule -> Commands -> Pools -> Resources -> Lifecycle -> Entities -> Events`。
- Godot 场景行为用 BrotatoLike 统一 runner 验证。
- 日志和 artifacts 写到 `.ai-temp/scene-tests/runs/<date>/<time>/`，不要污染源码目录。
- 新能力必须有最小 build / tests / smoke 验证路径。
- **Tests 中所有 spawn / lookup 字面量必须 `new EntityId("...")` 或 `EntityId.From("...")` 显式构造，不允许 raw `string` 直接赋值给 `EntityId` / `EntityId?` 字段或参数**：例如 `EntityManager.Spawn(new EntitySpawnConfig { EntityId = new EntityId("player") })` 而不是 `EntityId = "player"`。
- AssertEqual 比较 entity-id 时也用 typed `EntityId` 字面量，不混用 string 期望值与 typed 实际值。
- 涉及 Godot / 游戏侧时，先跑 BrotatoLike `Tools/run-build.sh`，再跑 Godot；不要只依赖 Godot runner 内部 build。
- 触及旧输入仓 `Resources/Else/brotato-my` 时追加 `cd /home/slime/Code/SlimeAI/Resources/Else/brotato-my && dotnet build Brotato_my.sln`。
- 新功能如果涉及 GodotBridge、真实 Godot Node 生命周期、Physics、Input、Resource、UI、动画或游戏侧胶水，必须补独立 Godot 验证场景；`run-main-smoke` 只能作为回归补充，不能替代专项场景。
- 新 Godot 验证场景遵守 `DocsAI/Tests/GodotSceneValidation.md`：`Src/Validation/...`、`Src/Validation/...`、旁置 `README.md`、稳定 PASS/FAIL marker 和 JSON artifact。
- 新或改动 Godot 验证场景必须通过 scene gate：README 包含 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath`，最近 PASS run 的 `index.json`、`result.json` 和 scene artifact 存在且 artifact 标准答案五字段非空，并同步 `DocsAI/Tests/ValidationCatalog.md` 或游戏侧 `DocsAI/ValidationCatalog.md`。
- 框架仓新增 / 修改 Godot validation scene 后，跑 Godot runner 前必须选定承载游戏。当前初始开发阶段默认 BrotatoLike，并直接复制到 `Games/BrotatoLike/SlimeAI/` submodule 工作树；后续多游戏 / 成品阶段不默认同步所有游戏，改按各游戏版本策略更新 submodule 指针。

## 验证

```bash
Tools/run-build.sh
Tools/run-tests.sh
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/AI/AICapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Ability/AbilityCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Attack/AttackCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Collision/CollisionCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Damage/DamageCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Effect/EffectCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Feature/FeatureCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Movement/MovementCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Projectile/ProjectileCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Unit/UnitCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Convergence/CapabilityConvergenceValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/GodotBridge/UnitComposition/UnitCompositionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

## guard 测试模式 + sandbox world

- Deferred structural change 测试必须使用 `using var world = RuntimeWorld.CreateScoped();`。
- 显式覆盖 guarded 路径：`using var guard = world.Commands.EnterGuard("test");` 后调用 `world.Entities.Spawn / Destroy` 或 `world.Lifecycle.Attach / Detach`。
- 覆盖 playback 路径：对 test-only completion 使用 `world.Schedule.RunPhase(SchedulePhase.Manual)`；对默认 guarded structural mutation 使用命令默认 phase（当前 `EndOfFrame`）并显式 `RunPhase(SchedulePhase.EndOfFrame)`。
- 断言 guarded spawn：同一 guard 内 `world.Entities.Get(capturedId) == null`，返回的 `RuntimeEntity.Data.Set(...)` 保留到 playback 后。
- 断言 dispose discard：保留 `var commands = world.Commands`，`world.Dispose()` 后读取 `commands.LastDiscardReport`，期望 pending commands 为 `Skipped / WorldDisposing`。
