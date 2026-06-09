# SDD-0040 Execution Prompt

把本文件整体交给新的执行会话。目标是完成 `SDD-0040 Log AI-first Observation Hard Cutover`，不是只把 `GD.PrintRich` 替换成 `Console.WriteLine`。

## 角色定位

你是 SDD-0040 的主执行者。默认中文回答；命令、代码、错误信息保留原文。大任务先计划，再执行。改文件前先读相关文件，改完总结改动和验证结果。不要 push，不要回滚用户已有改动。

必须使用相关 skill：

- `sdd-workflow` / `sdd-management`：恢复和更新 SDD。
- `tools`：Log 属于 ECS Tools owner。
- `test-system` / `godot-scene-test`：ValidationSession、场景 runner 和日志 artifact 验证。
- `ai-config-management` / `skill-test`：修改 `.ai-config/skills/godot/godot-scene-test` 或其他 skill 源后必须同步验证。
- 触及 owner flow 时按实际 owner 使用 `ability-system`、`damage-system`、`movement-system`、`projectile-effect-system`、`runtime-command-buffer`、`ecs-system` 等对应 skill。

## 工作区

- **Framework Git Boundary**: `/home/slime/Code/SlimeAI/SlimeAI`
- **Game Validation Git Boundary**: `/home/slime/Code/SlimeAI/Games/BrotatoLike`（只有 runner 存在且是有效 git 边界时才运行）
- **Project**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`
- **Current SDD**: `sdds/029-SDD-0040-log-ai-first-observation-hard-cutover/`

执行 git 命令前必须确认边界：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
```

## 必读顺序

1. `AGENTS.md`
2. `DocsAI/README.md`
3. `DocsAI/ECS/README.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md`
6. 本 SDD 的 `README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`progress.md`、`notes.md`
7. `DocsAI/ECS/Tools/Logger/README.md`
8. `.ai-config/skills/godot/godot-scene-test/SKILL.md`
9. 当前代码：`Src/ECS/Tools/Logger/Log.cs`、`Src/ECS/Tools/Logger/Tests/`、`Src/ECS/**/Tests/**/*.cs`
10. runner 脚本：`.ai-config/skills/godot/godot-scene-test/scripts/godot-scene-runner.mjs`、`analyze-logs.sh`

## 核心裁决

- Log 是 AI-first Observation 入口，不是字符串打印工具。
- 默认详细事实写 C# buffered JSONL file。
- 默认可见输出走 C# stdout summary。
- Validation / runner 主事实源是 artifact + structured log + exit code。
- `GD.PrintRich` / `GD.PushWarning` / `GD.PushError` 只属于 optional `GodotEditorSink`，默认关闭。
- `Success` 不再作为 severity；使用 `severity / outcome / validationStatus` 拆分。
- `logctl` 既负责运行控制，也负责离线 `analyze/query/ingest/suggest`。
- `godot-scene-test` 长期只运行场景、保存 run dir、调用 `logctl` 和读取 gate report。

## 禁止结果

- 不把每条详细日志都逐条 `Console.WriteLine`。
- 不让 runner 长期依赖 `[PASS]`、`[FAIL]`、`FAIL:` 等 stdout pattern。
- 不让 `godot-scene-test` 维护第二套 owner/phase/flow/failure/noise analyzer。
- 不把 AI 默认分析流程退回“读完整 stdout”。
- 不在没有 owner Log 文档或 README `## Log` 的情况下新增高频日志。
- 不引入第三方 logging/observability 依赖。

## T1.1 Readiness Baseline

先只读，不改实现。记录摘要到 `progress.md`。

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
sed -n '1,320p' Src/ECS/Tools/Logger/Log.cs
find Src/ECS/Tools/Logger -maxdepth 4 -type f | sort
find Src/ECS -path '*Tests*' -type f -name '*.cs' | sort
rg -n "GD\.Print\(\"PASS\"|GD\.PushError\(\"FAIL\"|\[PASS\]|\[FAIL\]|LogLevel\.Success|GD\.PrintRich|GD\.PushWarning|GD\.PushError" Src/ECS -g "*.cs"
rg -n "FAILURE_PATTERNS|\\[PASS\\]|\\[FAIL\\]|FAIL:|Exception" .ai-config/skills/godot/godot-scene-test/scripts
rg -n "logctl|LogEntry|OperationTrace|ValidationSession|resultSource|stdout-pattern-fallback" Src/ECS DocsAI/ECS SDD/project/projects/PRJ-0002-ecs-framework-refactor .ai-config/skills/godot/godot-scene-test
python3 Workspace/SDD/sdd.py validate SDD-0040
```

## 实现顺序

严格按 `tasks.md` 推进 T1.1 到 T1.10。每完成一项任务就更新 `tasks.md` 和 `progress.md`。

## 验证门禁

最低验证：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate SDD-0040
git diff --check
```

如果改 `.ai-config/skills/`：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

Godot runner 可用时：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
# Log hard cutover 后：
logctl analyze --run-dir <latest-run-dir> --out <latest-run-dir>/analysis
logctl query --analysis-dir <latest-run-dir>/analysis owner=Ability
```

若游戏仓不是有效 git boundary、runner 缺失或 `godot` CLI 不可用，必须记录 blocker，不能伪造场景验证通过。
