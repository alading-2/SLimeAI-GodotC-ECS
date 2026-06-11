---
name: godot-scene-test
description: 需要通过承载游戏运行 Godot headless 场景、主场景 smoke 或分析 Godot 日志时使用。
---

# Godot 场景测试入口

## 运行位置

Godot 场景测试在提供 `Tools/run-godot-scene.sh` 的承载游戏仓库运行。当前 `Games/BrotatoLike` 只有文档入口，执行前必须先确认目标游戏 runner 存在。

```bash
cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
```

## 必读入口

- `Src/ECS/Test/**` 旁 README — 框架级验证场景说明
- `/home/slime/Code/SlimeAI/Games/<GameWithRunner>/DocsAI/GameProjectState.md`
- `/home/slime/Code/SlimeAI/Games/<GameWithRunner>/Tools/run-godot-scene.sh`
- `/home/slime/Code/SlimeAI/Games/<GameWithRunner>/Tools/analyze-godot-scene-logs.sh`（薄 wrapper；优先调用 `logctl analyze` 并读取 gate report）

## 常用命令

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
Tools/run-godot-scene.sh list
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --build --timeout 3 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
Workspace/Tools/logctl/logctl analyze --run-dir <latest-run-dir> --out <latest-run-dir>/analysis
Workspace/Tools/logctl/logctl query --analysis-dir <latest-run-dir>/analysis owner=Ability
Workspace/Tools/logctl/logctl query --analysis-dir <latest-run-dir>/analysis severity>=Warn
Workspace/Tools/logctl/logctl query --file <latest-run-dir>/analysis/raw/entries.jsonl sourceFile=Src/ECS/Capabilities/Ability/System/AbilitySystem.cs
Workspace/Tools/logctl/logctl profile show --config-dir Config/Log
```

## 脚本入口

- `scripts/run-test.sh`
- `scripts/analyze-logs.sh`（薄 wrapper；长期日志拆分规则属于 Log CLI）
- `scripts/godot-scene-runner.mjs`

## 当前事实源

- runner 源：`.ai-config/skills/godot/godot-scene-test/scripts/godot-scene-runner.mjs`
- analyzer wrapper 源：`.ai-config/skills/godot/godot-scene-test/scripts/analyze-logs.sh`
- 日志整理和 AI 分析入口属于 `Workspace/Tools/logctl/logctl analyze/query`；`godot-scene-test` 只负责运行 Godot、保存 run dir、调用 Log CLI、读取 gate report。
- AI 默认读取 `analysis/summary.md`（若存在）、`analysis/ai-context.md`、`analysis/flows/index.md`、`analysis/noise/templates.md`、`analysis/noise/top-contexts.md`、`analysis/missing-fields/index.md` 和 `analysis/failures/index.md`；不要把 `raw/scene-log.jsonl` 直接作为默认提示词输入。
- 若 `analysis/summary.md` 不存在或 `ai-context.md` 只含 run metadata，应记录为 `Log CLI issue` / `Log gap`，再用 `logctl query` 缩小范围；不要退回复制全量 raw。
- 目标游戏若提供 `Tools/run-godot-scene.sh` / `Tools/analyze-godot-scene-logs.sh`，应作为 runner / Log CLI 的薄封装。
- 游戏仓里的 `SlimeAI/` 是框架仓 git submodule 镜像。框架仓新增或修改 `Src/Validation` 后，跑 Godot 前必须先选定承载游戏；当前初始开发阶段默认用 BrotatoLike，并直接同步到 `Games/BrotatoLike/SlimeAI/` 工作树。
- 不默认同步所有游戏仓。后续多游戏 / 成品阶段按每个游戏的框架版本策略更新 submodule 指针，再选择对应游戏跑验证。
- 承载游戏 wrapper 的 scan roots 必须包含游戏自身 `Src` 和框架镜像 `SlimeAI/Src`，否则框架侧验证场景不会出现在 `list/run-all`。
- 新日志结构是 `.ai-temp/scene-tests/runs/<date>/<time>/index.json` 加 per-scene attempt 目录。
- runner 必须为每个 attempt 注入 `SLIMEAI_LOG_RUN_DIR`，让 C# structured JSONL 和 artifacts 写入当前 run dir。
- runner 选择非默认 profile 时必须显式注入 `SLIMEAI_LOG_PROFILE` 和 `SLIMEAI_LOG_OVERRIDES`；运行前可用 `logctl profile show --config-dir Config/Log` 检查 `GodotEditorSink` 是否仍默认关闭、budget/rules 是否可解析。
- runner 判定优先级必须是 `artifact` / `structured-log` / `exit-code`，旧 stdout `PASS` / `FAIL` marker 只能作为 `stdout-pattern-fallback` 过渡信号，并写入 `resultSource`。
- Godot `GD.Print` / `GD.PushError` / `GD.PushWarning` 只属于可选 editor/debug sink，不应作为新验证场景的主事实源。

## 规则

- 框架修改后如影响 GodotBridge / Capability bridge，必须回到可运行的承载游戏跑 headless smoke。
- 跑任何 Godot 场景前先执行承载游戏的显式 build（若该游戏提供 build runner）；runner 的 `--build` 只能作为运行前再确认，不能替代显式游戏仓 build 门禁。
- 新功能涉及 Godot Node 生命周期、Physics、Input、Resource、UI、动画或游戏侧胶水时，必须新增独立验证场景；主场景 smoke 只是回归补充。
- 框架级新场景放在框架仓 `Src/Validation/<Area>/<Layer>/`，脚本放在 `Src/Validation/<Area>/<Layer>/`，旁置 `README.md`，输出 JSON artifact 和结构化日志；不要新增 PASS/FAIL marker，只有旧场景可被 runner 标记为 `stdout-pattern-fallback`。
- 新或改动的验证场景必须检查 `index.json`、per-scene `result.json` 和 scene artifact；artifact 中 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 必须非空。
- 跑框架级 Godot 场景前，当前阶段先直接同步到本轮承载游戏的 submodule 镜像，例如 BrotatoLike：`cp -a SlimeAI/Src/Validation/... Games/BrotatoLike/SlimeAI/Src/Validation/...` 和 `cp -a SlimeAI/Src/Validation/... Games/BrotatoLike/SlimeAI/Src/Validation/...`；以后多游戏版本管理成熟后再改成按游戏更新 submodule 指针。
- 承载游戏的 `.csproj` 需要排除框架源码但重新包含 `SlimeAI/Src/Validation/**/*.cs`，否则 submodule 场景脚本会因未编译而无法实例化。
- `Tools/run-godot-smoke.sh` 只是兼容入口，优先用统一 runner。
- 日志和截图 artifacts 保持在 `.ai-temp/scene-tests/runs`，优先读取 `index.json`、`result.json`、scene artifact、`analysis/summary.md`、`analysis/ai-context.md`、`analysis/flows/index.md`、`analysis/noise/templates.md`、`analysis/noise/top-contexts.md`、`analysis/missing-fields/index.md` 和 `analysis/failures/index.md`；`raw/scene-log.jsonl`、`analysis/raw/entries.jsonl` 和 `combined.log` 只作为 query/raw 证据和 fallback 排查入口。
- 需要筛选某个 owner / operation / entityId 时，不要复制 console 文本给 AI；调用 `logctl query --analysis-dir <run>/analysis ...` 查询 flow conclusion 和 success template。`query --analysis-dir` 不会在语义索引为空时回退 raw；需要按 `sourceFile` 或原始字段下钻时，显式调用 `logctl query --file <run>/analysis/raw/entries.jsonl ...`。
- 如果 gate report 显示 `validationEntries=0` 且 `artifacts=0`，只能说明未观察到 structured failure，不能当作行为已通过完整验证；新验证场景必须补 Validation artifact。

## UnitComposition 专项场景

旧 GameOS Validation 专项场景已不是当前事实源；当前可复验入口是游戏侧 UnitComposition 场景和主场景 smoke。若新增框架级专项场景，应放在 `Src/Validation/...` 并同步到承载游戏镜像后再运行。

```bash
Tools/run-godot-scene.sh run res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
