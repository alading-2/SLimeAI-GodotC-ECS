# SDD-0037 Execution Prompt

把本文件整体交给新的执行会话。目标是完成 `SDD-0037 Resource Loading And Common Utilities Hard Cutover`，不是继续维护 `CommonTool` 杂物箱，也不是把 ResourceManagement 描述成自动修复资源路径的管理器。

## 角色定位

你是 SDD-0037 的主执行者。默认中文回答；命令、代码、错误信息保留原文。大任务先计划，再执行。改文件前先读相关文件，改完总结改动和验证结果。不要 push，不要回滚用户已有改动。

必须使用相关 skill：

- `sdd-workflow` / `sdd-management`：恢复和更新 SDD。
- `tools`：ResourceLoading、Common Utilities 属于 ECS Tools owner。
- `resource-path-migration`：资源路径移动或验证旧路径残留。
- `data-authoring` / `ecs-data`：如果触及 DataOS resource refs。
- `test-system`：diagnostics / CLI / scene validation。
- `ai-config-management` / `skill-test`：如果修改 `.ai-config` skill 源。

## 工作区

- **Framework Git Boundary**: `/home/slime/Code/SlimeAI/SlimeAI`
- **Project**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`
- **Current SDD**: `sdds/027-SDD-0037-resource-loading-and-common-utilities-hard-cutover/`

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
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/README.md`
6. `design/Tool/其他Tool/01-现状证据与AI-first裁决.md`
7. `design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md`
8. `design/Tool/其他Tool/06-实施路线与验证门禁.md`
9. `DocsAI/ECS/Tools/ResourceManagement/README.md`
10. `.ai-config/skills/core/resource-path-migration/SKILL.md`
11. 本 SDD 的 `README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`progress.md`

## 核心裁决

- `res://` 本身不是问题。
- 业务 Capability 不直接裸 `GD.Load` / `ResourceLoader.Load`。
- ResourceManagement 不作为长期“资源管理器”概念保留，默认收敛为 `ResourceLoading`。
- strict lookup 删除 contains fallback。
- `LoadPath` 必须携带 source/owner/usage。
- `CommonTool.LoadPackedScene` 迁入 ResourceLoading；CommonTool 不作为 current owner。
- Common Utilities 放 `Src/ECS/Tools/CommonUtilities/`，且必须有 owner 边界和禁止项。

## 禁止结果

- 不恢复 `CommonTool.SomeHelper()` 杂物箱。
- 不把 ResourceLoading 做成重型资产系统。
- 不声称 ResourceLoading 能自动防止资源移动报错。
- 不跨 git boundary 混改游戏仓和框架仓资源。
- 不把游戏仓资源写入框架仓 catalog 作为长期事实源。

## T1.1 Readiness Baseline

先只读，不改实现。记录摘要到 `progress.md`。

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
find Data/ResourceManagement Tools/ResourceGenerator -maxdepth 3 -type f | sort
sed -n '1,260p' Data/ResourceManagement/ResourceManagement.cs
sed -n '1,260p' Data/ResourceManagement/ResourceCatalog.cs
sed -n '1,220p' Src/ECS/Tools/CommonTool.cs
rg -n "CommonTool\\.|GD\\.Load|ResourceLoader\\.Load" Src/ECS Data DocsAI/ECS
rg -n "Contains\\(name, StringComparison\\.OrdinalIgnoreCase\\)|LegacyContainsFallback" Data/ResourceManagement Src/ECS
python3 Workspace/SDD/sdd.py validate SDD-0037
```

## 实现顺序

严格按 `tasks.md` 推进 T1.1 到 T1.11。每完成一项任务就更新 `tasks.md` 和 `progress.md`。

## 最终验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate SDD-0037
git diff --check
```

如果修改 `.ai-config`：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```
