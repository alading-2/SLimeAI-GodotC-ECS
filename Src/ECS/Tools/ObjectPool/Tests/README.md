# ObjectPool Tests

ObjectPool 测试目录只保留 AI-first 自动验证入口：

| 目录 | 用途 | 门禁 |
| --- | --- | --- |
| `Contracts/` | Runtime contract checks，验证池容量、统计、重复归还、静态归还、active snapshot 和测试池隔离。 | 自动回归入口。 |
| `Validation/CollisionIsolation/` | Godot collision validation，验证 `ParkedInTree`、activation-frame embargo、parking grid、fallback control 和结构化 artifact。 | scene gate 入口。 |

运行 ObjectPool 专项验证时优先使用：

```bash
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/ObjectPool/Tests/Contracts/ObjectPoolContractRuntimeTest.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/ObjectPool/Tests/Validation/CollisionIsolation/ObjectPoolCollisionIsolationValidation.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

历史 UI demo 已删除；ObjectPool 不再保留依赖 UI、鼠标、随机数或人工观察的测试场景。
