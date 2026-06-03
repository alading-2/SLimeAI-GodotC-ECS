# SystemCoreRuntimeTest

expectedInputs:
- Godot headless scene `res://SlimeAI/Src/ECS/Runtime/System/Tests/SystemCore/SystemCoreRuntimeTest.tscn` or framework-local `res://Src/ECS/Runtime/System/Tests/SystemCore/SystemCoreRuntimeTest.tscn`.
- DataOS runtime snapshot contains current `system.config` and `system.preset` records.
- `SystemRegistry` module initializers have registered the current Runtime / Capability / Tools / UI systems.

expectedObservations:
- `SystemPreflight` reports zero errors for current runtime snapshot and descriptors.
- `SystemManager.Execute` blocks FrontEnd damage command with stable `FlowStateMismatch` reason code.
- `SystemDiagnosticsSnapshot` serializes to JSON and includes schemaVersion, projectState, activePreset, counts, entries, preflightIssues and recentTrace.
- Required systems cannot be disabled or removed through SystemManager management APIs.

passCriteria:
- Scene exits with code 0.
- Stdout contains System Core PASS logs and no FAIL logs.
- Artifact `status` is `PASS`.
- Artifact contains non-empty standard answer fields.
- Artifact has `schemaVersion=1`, `configCount>=14`, `registeredDescriptorCount>=14`, `loadedCount>0` and diagnostics entries for core systems.

failCriteria:
- Scene exits non-zero or prints any FAIL log.
- `SystemPreflight.ErrorCount` is non-zero.
- Diagnostics JSON is missing, cannot parse, has empty standard answer fields, or lacks stable blocked reason code.
- Required system management guard regresses.

artifactPath:
- `.ai-temp/scene-tests/artifacts/system-core-diagnostics.json`

## Run

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Runtime/System/Tests/SystemCore/SystemCoreRuntimeTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

当前承载游戏没有可用 runner 或本机没有 Godot CLI 时，不能声明 scene gate 通过；只能记录具体 blocker，并用 `dotnet build`、DataOS validate、SDD validate 和文件级检查作为临时证据。
