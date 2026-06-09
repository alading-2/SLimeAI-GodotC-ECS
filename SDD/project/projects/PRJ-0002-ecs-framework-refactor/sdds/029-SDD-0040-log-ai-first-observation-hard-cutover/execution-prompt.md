# SDD-0040 Execution Prompt

你是 SDD-0040 的主执行者。目标是完成 `Log AI-first Observation Hard Cutover`，不是只把 `GD.PrintRich` 替换成 `Console.WriteLine`。

## 必读

1. `AGENTS.md`
2. `DocsAI/README.md`
3. `DocsAI/ECS/README.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/029-SDD-0040-log-ai-first-observation-hard-cutover/README.md`
7. 同目录下 `design/main.md`、`tasks.md`、`bdd.md`、`progress.md`、`notes.md`
8. `DocsAI/ECS/Tools/Logger/README.md`
9. `.ai-config/skills/godot/godot-scene-test/SKILL.md`

## 核心裁决

- Log 是 AI-first Observation 入口，不是字符串打印工具。
- 默认详细事实写 C# buffered JSONL file。
- 默认可见输出走 C# stdout summary。
- Validation / runner 主事实源是 artifact + structured log + exit code。
- `GD.PrintRich` / `GD.PushWarning` / `GD.PushError` 只属于 optional `GodotEditorSink`，默认关闭。
- `Success` 不再作为 severity；使用 `severity / outcome / validationStatus` 拆分。
- `logctl` 既负责运行控制，也负责离线 `analyze/query/ingest/suggest`。
- `godot-scene-test` 长期只运行场景、保存 run dir、调用 `logctl` 和读取 gate report。

## 执行步骤

从 `tasks.md` 的 T1.1 开始，严格按 T1.1 到 T1.10 推进。每完成一项任务，更新 `tasks.md` 和 `progress.md`。

第一步只做 readiness baseline，不直接改源码：

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

## 验证门禁

最低验证：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate SDD-0040
git diff --check
```

如果改 `.ai-config/skills/`，必须额外运行：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

Godot runner 可用时，进入承载游戏仓验证；若游戏仓不是有效 git boundary、runner 缺失或 `godot` CLI 不可用，记录 blocker，不能伪造场景验证通过。
