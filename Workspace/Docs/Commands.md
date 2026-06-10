# 常用命令参考

默认在框架仓执行：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
```

进入游戏仓、外层工作区或其他目录的命令会单独标注。

## 快速验证

框架最小验证：构建 C# 项目并校验 DataOS。

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
```

文档、SDD、AI 配置验证：修改 SDD、skill、rule、hook 或 SystemAgent 工具后使用。

```bash
python3 Workspace/SDD/sdd.py validate --all
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

全量 wrapper：依次跑框架 build、游戏 build、场景列表、主场景 smoke、日志分析。当前需要承载游戏仓存在 `Tools/run-build.sh` 和 `Tools/run-godot-scene.sh`。

```bash
bash Workspace/Tools/run-full-validation.sh
```

## 构建与专项 TDD

框架构建：日常 C# 编译检查。

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

按 solution 构建：需要检查解决方案级引用时使用。

```bash
dotnet build Brotato_my.sln --no-restore /clp:ErrorsOnly
```

Singleton 工具专项 TDD：验证 `SingletonInstanceGuard` 行为。

```bash
dotnet run --project Workspace/Tools/SingletonGuardTdd/SingletonGuardTdd.csproj
```

Timer 工具专项 TDD：验证 `TimerScheduler` 行为和基准输出。

```bash
dotnet run --project Workspace/Tools/TimerSchedulerTdd/TimerSchedulerTdd.csproj
```

## DataOS 数据更新

数据流向：

```text
seed SQL -> authoring.db -> runtime_snapshot.json -> DataKey_Generated.cs
```

完整更新：重建数据库、生成 runtime snapshot、生成 typed DataKey、再校验。

```bash
bash Data/DataOS/Tools/generate-all.sh
```

只更新 authoring 数据库：从 `core.sql`、`SlimeAINew.seed.sql`、`DataKeyDescriptors.seed.sql` 重建 `slimeainew.authoring.db`。

```bash
bash Data/DataOS/Tools/build-authoring-db.sh
```

只更新 runtime snapshot：从 authoring DB 输出运行时快照。

```bash
bash Data/DataOS/Tools/generate-runtime-snapshot.sh \
  Data/DataOS/Authoring/slimeainew.authoring.db \
  Data/DataOS/Snapshots/runtime_snapshot.json
```

只更新 DataKey handle：从 snapshot 生成 `DataKey_Generated.cs`。

```bash
python3 Data/DataOS/Tools/generate-data-key-handles.py \
  Data/DataOS/Snapshots/runtime_snapshot.json \
  Data/DataKey/Generated/DataKey_Generated.cs
```

只验证 DataOS：检查外键、descriptor、snapshot 和 generated handle 一致性。

```bash
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
```

固定生成时间：需要稳定 diff 时使用。

```bash
DATAOS_GENERATED_AT_UTC=1970-01-01T00:00:00Z \
bash Data/DataOS/Tools/generate-runtime-snapshot.sh \
  Data/DataOS/Authoring/slimeainew.authoring.db \
  Data/DataOS/Snapshots/runtime_snapshot.json
```

## 资源路径与 Catalog

迁移资源路径 dry-run：移动、重命名 Godot 资源前先看会改哪些文件。

```bash
python3 .ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --old "res://assets/Old" \
  --new "res://assets/New"
```

同时检查 `res://`、项目相对路径和绝对路径变体。

```bash
python3 .ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --old "res://assets/Old" \
  --new "res://assets/New" \
  --include-variants
```

确认 dry-run 后实际写入。

```bash
python3 .ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --old "res://assets/Old" \
  --new "res://assets/New" \
  --include-variants \
  --apply
```

在游戏仓内使用框架迁移脚本：先进入游戏仓，再用框架仓脚本绝对路径。

```bash
cd /home/slime/Code/SlimeAI/Games/<Game>
python3 /home/slime/Code/SlimeAI/SlimeAI/.ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py \
  --root . \
  --old "res://assets/Old" \
  --new "res://assets/New" \
  --include-variants
```

生成资源 catalog：扫描 `.tscn` / `.tres` 并更新 `Data/ResourceManagement/ResourcePaths.cs`。

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet run --project Workspace/Tools/ResourceGenerator/ResourceGenerator.csproj
```

## SDD

查看 SDD CLI。

```bash
python3 Workspace/SDD/sdd.py --help
```

列出 SDD。

```bash
python3 Workspace/SDD/sdd.py list
python3 Workspace/SDD/sdd.py list --state active
python3 Workspace/SDD/sdd.py list --tag systemagent
python3 Workspace/SDD/sdd.py list --json
```

列出项目级 SDD。

```bash
python3 Workspace/SDD/sdd.py project-list
python3 Workspace/SDD/sdd.py project-list --bucket projects
python3 Workspace/SDD/sdd.py project-list --bucket archived
```

查看单个 SDD 或项目。

```bash
python3 Workspace/SDD/sdd.py show SDD-0001
python3 Workspace/SDD/sdd.py project-show PRJ-0001
```

创建独立 SDD。

```bash
python3 Workspace/SDD/sdd.py new "任务标题" \
  --type feature \
  --scope Src/ECS \
  --area Src/ECS \
  --tag ecs
```

创建项目子 SDD。

```bash
python3 Workspace/SDD/sdd.py new "任务标题" \
  --project PRJ-0002 \
  --scope Src/ECS
```

创建项目容器。

```bash
python3 Workspace/SDD/sdd.py project-new "项目标题" \
  --scope Workspace/SystemAgent \
  --tag systemagent
```

开始、阻塞、完成 SDD。

```bash
python3 Workspace/SDD/sdd.py start SDD-0001
python3 Workspace/SDD/sdd.py block SDD-0001 "阻塞原因"
python3 Workspace/SDD/sdd.py done SDD-0001 \
  --validation "python3 Workspace/SDD/sdd.py validate SDD-0001: 0 error / 0 warning" \
  --conclusion "完成结论" \
  --next-action "后续动作"
```

管理任务清单。

```bash
python3 Workspace/SDD/sdd.py task SDD-0001 list
python3 Workspace/SDD/sdd.py task SDD-0001 add --text "Run validation"
python3 Workspace/SDD/sdd.py task SDD-0001 done T1.1
python3 Workspace/SDD/sdd.py task SDD-0001 todo T1.1
```

追加进度记录。

```bash
python3 Workspace/SDD/sdd.py note SDD-0001 --type decision "记录决策"
python3 Workspace/SDD/sdd.py note SDD-0001 --type validation "记录验证命令和结果"
```

校验和重建索引。

```bash
python3 Workspace/SDD/sdd.py validate SDD-0001
python3 Workspace/SDD/sdd.py validate --all
python3 Workspace/SDD/sdd.py index
python3 Workspace/SDD/sdd.py doctor
```

## AI 配置与 SystemAgent

同步 AI 配置：只改 `.ai-config/skills` 或 `.ai-config/rules` 后执行；会同步到 Claude、Codex、Devin、Trae、OpenCode 副本，并跑 changed skill lint。

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
```

扫描所有 skill。

```bash
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all
```

只扫描 git 修改过的 skill。

```bash
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static changed
```

advisory 模式：只看摘要，不让 lint 失败中断当前验证链。

```bash
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static changed --no-fail --summary-only
```

Hook smoke：修改 `.claude/settings.json`、`.codex/hooks.json` 或 `Workspace/SystemAgent/Tools/systemagent-hooks/` 后执行。

```bash
python3 Workspace/SystemAgent/Tools/systemagent-hooks/run-hook-smoke.py
```

## 会话整理

查看 session-adapter 命令。

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py --help
```

列出当前仓最近 AI 会话：依赖本地 `Workspace/Resources/tool/codbash`。

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list --repo . --limit 10
```

为指定会话生成或刷新摘要级 ChatHistory sidecar。

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py index --session <session-id>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py summarize --session <session-id>
```

导出 Codex 当前月份可见 transcript。

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py export-codex-month \
  --source-root "$HOME/.codex/sessions/$(date +%Y/%m)" \
  --limit 20
```

生成单个 Codex digest。

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py digest-codex \
  --session <id-or-jsonl-path>
```

批量生成 Codex digest：跳过无结论、无代码、无验证的中断会话。

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py digest-codex-month \
  --source-root "$HOME/.codex/sessions/$(date +%Y/%m)" \
  --limit 20 \
  --skip-interrupted
```

查询 digest index。

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list-digests --status digest --limit 20
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list-digests --status digest --failed-tools
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list-digests --topic "DataOS"
```

输出位置。

```text
Workspace/DocsAI/ChatHistory/
```

## Godot 场景验证

当前 `Games/BrotatoLike/` 尚未初始化 Godot project 和 runner。以下命令只适用于已经包含 `Tools/run-godot-scene.sh` 的承载游戏仓。

```bash
cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
```

列出可验证场景。

```bash
Tools/run-godot-scene.sh list
```

运行单个场景。

```bash
Tools/run-godot-scene.sh run res://Scenes/Main.tscn \
  --timeout 10 \
  --log-dir .ai-temp/scene-tests/runs
```

运行主场景 smoke。

```bash
Tools/run-godot-scene.sh run-main-smoke \
  --timeout 10 \
  --log-dir .ai-temp/scene-tests/runs
```

运行 manifest 中的全部场景。

```bash
Tools/run-godot-scene.sh run-all \
  --build \
  --continue-on-fail \
  --manifest DocsAI/ValidationManifest.json
```

分析 Godot 场景日志。

```bash
Tools/analyze-godot-scene-logs.sh
```

## Git 边界与子模块

确认当前 Git 边界。

```bash
git rev-parse --show-toplevel
git status --short
```

框架仓提交。

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
git add <files>
git commit -m "What changed and why"
git push
```

更新游戏仓中的 `SlimeAI` submodule：只在游戏仓执行。

```bash
cd /home/slime/Code/SlimeAI/Games/<Game>
git submodule update --remote SlimeAI
git add SlimeAI
git commit -m "Update SlimeAI submodule"
git push
```

不要在 `Games/<Game>/SlimeAI/` 里直接改框架代码。
