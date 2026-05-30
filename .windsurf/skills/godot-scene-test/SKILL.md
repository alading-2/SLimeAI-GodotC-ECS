---
name: godot-scene-test
description: 需要通过 BrotatoLike 运行 Godot headless 场景、主场景 smoke 或分析 Godot 日志时使用。
---

# Godot 场景测试入口

## 运行位置

Godot 场景测试在第一个游戏仓库运行：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
```

## 必读入口

- `Src/ECS/Test/**` 旁 README — 框架级验证场景说明
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/DocsAI/GameProjectState.md`
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/Tools/run-godot-scene.sh`
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/Tools/analyze-godot-scene-logs.sh`

## 常用命令

```bash
Tools/run-build.sh
Tools/run-godot-scene.sh list
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --build --timeout 3 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

## 脚本入口

- `scripts/run-test.sh`
- `scripts/analyze-logs.sh`
- `scripts/godot-scene-runner.mjs`

## 当前事实源

- runner 源：`.ai-config/skills/godot/godot-scene-test/scripts/godot-scene-runner.mjs`
- analyzer 源：`.ai-config/skills/godot/godot-scene-test/scripts/analyze-logs.sh`
- BrotatoLike `Tools/run-godot-scene.sh` / `Tools/analyze-godot-scene-logs.sh` 是薄封装。
- 游戏仓里的 `SlimeAI/` 是框架仓 git submodule 镜像。框架仓新增或修改 `Src/Validation` 后，跑 Godot 前必须先选定承载游戏；当前初始开发阶段默认用 BrotatoLike，并直接同步到 `Games/BrotatoLike/SlimeAI/` 工作树。
- 不默认同步所有游戏仓。后续多游戏 / 成品阶段按每个游戏的框架版本策略更新 submodule 指针，再选择对应游戏跑验证。
- 承载游戏 wrapper 的 scan roots 必须包含游戏自身 `Src` 和框架镜像 `SlimeAI/Src`，否则框架侧验证场景不会出现在 `list/run-all`。
- 新日志结构是 `.ai-temp/scene-tests/runs/<date>/<time>/index.json` 加 per-scene attempt 目录。

## 规则

- 框架修改后如影响 GodotBridge / Capability bridge，必须回到 BrotatoLike 跑 headless smoke。
- 跑任何 Godot 场景前先执行 BrotatoLike `Tools/run-build.sh`；runner 的 `--build` 只能作为运行前再确认，不能替代显式游戏仓 build 门禁。
- 新功能涉及 Godot Node 生命周期、Physics、Input、Resource、UI、动画或游戏侧胶水时，必须新增独立验证场景；主场景 smoke 只是回归补充。
- 框架级新场景放在框架仓 `Src/Validation/<Area>/<Layer>/`，脚本放在 `Src/Validation/<Area>/<Layer>/`，旁置 `README.md`，输出 PASS/FAIL marker 和 JSON artifact。
- 新或改动的验证场景必须检查 `index.json`、per-scene `result.json` 和 scene artifact；artifact 中 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 必须非空。
- 跑框架级 Godot 场景前，当前阶段先直接同步到本轮承载游戏的 submodule 镜像，例如 BrotatoLike：`cp -a SlimeAI/Src/Validation/... Games/BrotatoLike/SlimeAI/Src/Validation/...` 和 `cp -a SlimeAI/Src/Validation/... Games/BrotatoLike/SlimeAI/Src/Validation/...`；以后多游戏版本管理成熟后再改成按游戏更新 submodule 指针。
- 承载游戏的 `.csproj` 需要排除框架源码但重新包含 `SlimeAI/Src/Validation/**/*.cs`，否则 submodule 场景脚本会因未编译而无法实例化。
- `Tools/run-godot-smoke.sh` 只是兼容入口，优先用统一 runner。
- 日志和截图 artifacts 保持在 `.ai-temp/scene-tests/runs`，优先读取 `index.json`、`result.json`、`combined.log` 和 `artifacts/logs/scene-log.jsonl`。

## UnitComposition 专项场景

```bash
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/GodotBridge/UnitComposition/UnitCompositionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
