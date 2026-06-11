---
name: test-system
description: 修改 SlimeAI ECS 测试、Validation、Observation、日志分析或测试包装脚本时使用。
---

# Validation / Test 入口

## 必读入口

- `DocsAI/README.md` — 方向决策入口
- `Src/ECS/Test/**` 旁 README — 框架级验证场景说明
- `DocsAI/ECS/Runtime/Data/Data系统说明.md` — Data 测试场景说明
- `DocsAI/ECS/Runtime/README.md`
- `DocsAI/ECS/Capabilities/README.md`

## 源码位置

- `Src/ECS/Test/`
- `Src/ECS/Capabilities/*/Tests/`
- `Src/ECS/Runtime/`
- `Src/ECS/Capabilities/`
- `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`
- `Data/DataOS/Tools/validate-dataos.sh`
- `/home/slime/Code/SlimeAI/Games/<GameWithRunner>/Tools/run-godot-scene.sh`（存在时）

## 规则

- 框架纯逻辑优先补 Runtime tests。
- **新 Runtime / Capability 测试优先使用显式隔离状态，不要复用全局单例造成测试污染**。旧 backlog 用例若暂未支持显式注入，必须在当前 SDD 或 DocsAI 对应 owner 文档登记。
- **Capability Service 测试必须用 `new XxxService()` 独立实例，禁用 `Default / Instance`**。例如 `new DamageService()`、`new DamageService(new HealService())`、`new AbilityService(timerManager)`。两个 scoped world 测试共享 `XxxService.Default` 会导致状态污染。
- RuntimeWorld dispose 顺序以当前 SDD design 和 `DocsAI/ECS/` 文档为准。
- Godot 场景行为用当前承载游戏的统一 runner 验证；当前 `Games/BrotatoLike` 只有文档入口，执行前先确认 runner 是否存在。
- 日志和 artifacts 写到 `.ai-temp/scene-tests/runs/<date>/<time>/`，不要污染源码目录。
- 新能力必须有最小 build / tests / smoke 验证路径。
- **Tests 中所有 spawn / lookup 字面量必须 `new EntityId("...")` 或 `EntityId.From("...")` 显式构造，不允许 raw `string` 直接赋值给 `EntityId` / `EntityId?` 字段或参数**：例如 `EntityManager.Spawn(new EntitySpawnConfig { EntityId = new EntityId("player") })` 而不是 `EntityId = "player"`。
- AssertEqual 比较 entity-id 时也用 typed `EntityId` 字面量，不混用 string 期望值与 typed 实际值。
- 涉及 Godot / 游戏侧时，先跑承载游戏显式 build（若提供 build runner），再跑 Godot；不要只依赖 Godot runner 内部 build。
- 触及旧输入仓 `Resources/Else/brotato-my` 时追加 `cd /home/slime/Code/SlimeAI/Resources/Else/brotato-my && dotnet build Brotato_my.sln`。
- 新功能如果涉及 GodotBridge、真实 Godot Node 生命周期、Physics、Input、Resource、UI、动画或游戏侧胶水，必须补独立 Godot 验证场景；`run-main-smoke` 只能作为回归补充，不能替代专项场景。
- 新 Godot 验证场景遵守 scene gate 规则：`Src/Validation/...`、旁置 `README.md`、JSON artifact 和结构化日志是主事实源；旧 PASS/FAIL marker 只允许作为 runner 过渡 fallback。
- 新或改动 Godot 验证场景必须通过 scene gate：README 包含 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath`，最近 PASS run 的 `index.json`、`result.json` 和 scene artifact 存在且 artifact 标准答案五字段非空。
- 新测试不要用裸 `GD.Print("PASS")`、`GD.PushError("FAIL")`、`[PASS]`、`[FAIL]` 作为断言事实；断言结果必须进入 Validation artifact / structured log，Godot editor sink 默认关闭。
- 新 Runtime / Capability / Godot validation 断言优先使用 `ValidationSession` / `CheckResult`；artifact 和 structured log 是 runner 判定主事实源，stdout pattern 只能是 `stdout-pattern-fallback`。
- Log profile 默认事实源是 `Config/Log/log.profile.json`、`Config/Log/log.rules.json`、`Config/Log/log.overrides.json`；排查测试前可先跑 `Workspace/Tools/logctl/logctl profile show --config-dir Config/Log` 确认可用 sink、budget 和 rules。
- 排查场景日志时先运行 `Workspace/Tools/logctl/logctl analyze --run-dir <run> --out <run>/analysis`，默认先读 `summary.md`、`ai-context.md`、`noise/top-contexts.md`、`missing-fields/index.md`、`flows/index.md`，再用 `logctl query` 按 `owner / operation / validationStatus / severity` 缩小范围；不要把全量 stdout 或 raw JSONL 直接交给 AI。
- Gate status 只允许 artifact pass 或 Validation channel pass 报 `passed`；只有 structured log 且没有 artifact/Validation 时必须是 `no-failure-observed`，legacy stdout pattern 只能是 `stdout-pattern-fallback`。
- ObjectPool 专项验证当前分层位于 `Src/ECS/Tools/ObjectPool/Tests/Contracts/` 和 `Src/ECS/Tools/ObjectPool/Tests/Validation/CollisionIsolation/`；`ObjectPoolCollisionIsolationValidation` 必须输出 `.ai-temp/scene-tests/artifacts/objectpool-collision-isolation-validation.json`，artifact 至少包含标准答案五字段、`checks[]`、`poolStats`、`nodeStates`、`collisionEvents`、`businessCollisionEvents` 和 `failureReasons`，并用 `collision_guard_event_oracle` 证明 raw callback 未直接等同于业务事件。
- 框架仓新增 / 修改 Godot validation scene 后，跑 Godot runner 前必须选定提供 runner 的承载游戏，并按该游戏的框架版本策略同步 submodule 指针或工作树镜像；后续多游戏 / 成品阶段不默认同步所有游戏。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
Workspace/Tools/logctl/logctl profile show --config-dir Config/Log
# 如果承载游戏提供 runner，再执行专项场景和 smoke：
# cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
# Tools/run-godot-scene.sh run res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
# Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
# Tools/analyze-godot-scene-logs.sh
# Workspace/Tools/logctl/logctl analyze --run-dir <latest-run-dir> --out <latest-run-dir>/analysis
```

## guard 测试模式 + sandbox world

- Deferred structural change 测试必须使用 `using var world = RuntimeWorld.CreateScoped();`。
- 显式覆盖 guarded 路径：`using var guard = world.Commands.EnterGuard("test");` 后调用 `world.Entities.Spawn / Destroy` 或 `world.Lifecycle.Attach / Detach`。
- 覆盖 playback 路径：对 test-only completion 使用 `world.Schedule.RunPhase(SchedulePhase.Manual)`；对默认 guarded structural mutation 使用命令默认 phase（当前 `EndOfFrame`）并显式 `RunPhase(SchedulePhase.EndOfFrame)`。
- 断言 guarded spawn：同一 guard 内 `world.Entities.Get(capturedId) == null`，返回的 `RuntimeEntity.Data.Set(...)` 保留到 playback 后。
- 断言 dispose discard：保留 `var commands = world.Commands`，`world.Dispose()` 后读取 `commands.LastDiscardReport`，期望 pending commands 为 `Skipped / WorldDisposing`。
