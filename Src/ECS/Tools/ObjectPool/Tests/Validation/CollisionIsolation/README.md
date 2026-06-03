# ObjectPool Validation

expectedInputs:
- Headless Godot scenes `res://SlimeAI/Src/ECS/Tools/ObjectPool/Tests/Contracts/ObjectPoolContractRuntimeTest.tscn` and `res://SlimeAI/Src/ECS/Tools/ObjectPool/Tests/Validation/CollisionIsolation/ObjectPoolCollisionIsolationValidation.tscn`, or framework-local `res://Src/ECS/Tools/ObjectPool/Tests/...`.
- Test-only pool names under `Test/ObjectPool/...`; no gameplay pool such as `ProjectilePool`, `EnemyPool` or `EffectPool` is reused.
- `Area2D` and `CharacterBody2D` roots are acquired, released, reactivated and observed through `ObjectPoolRuntimeStateStore`, `CollisionLogicGuard` and a raw-callback-to-business-event oracle.

expectedObservations:
- Released collision roots stay inside the scene tree, become hidden, stop processing, move to a parking-grid position and record `CollisionLogicActive=false`.
- `Activate()` records `CollisionReadyPhysicsFrame=currentPhysicsFrame+1`; the acquire frame is rejected and the ready frame is accepted by `CollisionLogicGuard`.
- Parking-grid positions are distributed instead of placing every pooled collision object at the same coordinate.
- Detach fallback remains an explicit control path and is not the default release path.
- Runtime contract checks cover warmup stats, static return for plain objects, active snapshot behavior, capacity discard and duplicate release guard.
- Raw collision callbacks are recorded separately from accepted business collision events.

passCriteria:
- Scene exits with code 0.
- Stdout contains `PASS ObjectPoolCollisionIsolationValidation` for the collision scene.
- Artifact `status` is `PASS`.
- Artifact `checks[]` contains `collision_area_release_parked_in_tree`, `collision_character_release_parked_in_tree`, `collision_activate_first_frame_embargo`, `collision_activate_after_ready_frame`, `collision_immediate_reuse_same_frame`, `collision_guard_event_oracle`, `collision_parking_grid_pressure`, `collision_detach_fallback_control` and `collision_artifact_oracle_complete`.
- Artifact standard answer fields are non-empty.

failCriteria:
- Scene exits non-zero or prints `FAIL ObjectPoolCollisionIsolationValidation`.
- Any check records `passed=false`.
- Released collision roots are detached from the tree by the default path, collision logic remains active while in pool, the first activation frame is allowed, or raw pooled/first-frame callbacks become business events.
- Artifact is missing, has empty standard answer fields, or lacks any expected check.
- Only stdout PASS exists without runner `index.json`, per-scene `result.json` and PASS artifact evidence.

artifactPath:
- `.ai-temp/scene-tests/artifacts/objectpool-collision-isolation-validation.json`

## Run

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/ObjectPool/Tests/Validation/CollisionIsolation/ObjectPoolCollisionIsolationValidation.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

当前承载游戏没有可用 runner 或本机没有 Godot CLI 时，不能声明 scene-gate 通过；只能使用框架 `dotnet build`、SDD validate 和文件级检查作为临时证据。
