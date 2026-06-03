# TimerStressValidation

expectedInputs:
- Headless Godot scene `res://SlimeAI/Src/ECS/Tools/Timer/Tests/TimerStressValidation.tscn` or framework-local `res://Src/ECS/Tools/Timer/Tests/TimerStressValidation.tscn`.
- Synthetic TimerScheduler deltas for Game and Real clocks; no gameplay world or external input is required.

expectedObservations:
- `LongDelayNoDue` keeps 10k long-delay timers active, fires no callback and records zero no-due tick allocation.
- `StaggeredDue` dispatches 10k due callbacks and clears active timers.
- `CancelByOwner` cancels 10k owner timers without callback execution.
- `LoopRepeatCountdown` verifies loop catch-up, repeat completion and countdown completion semantics.
- `GamePauseRealClock` proves paused Game clock does not block Real clock timers.
- `PerFrameUpdateIsolation` updates only explicit per-frame timers.
- `OwnerPurposeLeakHints` reports missing owner/purpose leak hints and owner/purpose diagnostics.
- `MainThreadDispatch` proves callbacks are queued during tick and invoked by main-thread dispatch.

passCriteria:
- Scene exits with code 0.
- Stdout contains `PASS TimerStressValidation`.
- Artifact `status` is `PASS`.
- Artifact `checks[]` contains all eight expected check names and every check has `passed=true`.
- Artifact standard answer fields are non-empty.

failCriteria:
- Scene exits non-zero or prints `FAIL TimerStressValidation`.
- Artifact is missing, has `status=FAIL`, has empty standard answer fields or lacks any expected check.
- Any check records `passed=false`.
- Only stdout PASS exists without `index.json`, per-scene `result.json` and PASS artifact evidence.

artifactPath:
- `.ai-temp/scene-tests/artifacts/timer-stress-validation.json`

## Run

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/Timer/Tests/TimerStressValidation.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

当前 `Games/BrotatoLike` 尚未初始化 Godot project / runner 时，不能声明 scene-gate 通过；只能使用框架 `dotnet build` 和纯 C# `Tools/TimerSchedulerTdd` 作为临时证据。
