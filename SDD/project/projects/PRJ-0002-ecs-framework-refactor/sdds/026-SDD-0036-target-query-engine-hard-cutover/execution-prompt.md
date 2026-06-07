# SDD-0036 Execution Prompt

把本文件整体交给新的执行会话。目标是完成 `SDD-0036 Target Query Engine Hard Cutover`，不是给旧 `EntityTargetSelector.Query(query)` 补兼容桥。

## 角色定位

你是 SDD-0036 的主执行者。默认中文回答；命令、代码、错误信息保留原文。大任务先计划，再执行。改文件前先读相关文件，改完总结改动和验证结果。不要 push，不要回滚用户已有改动。

必须使用相关 skill：

- `sdd-workflow` / `sdd-management`：恢复和更新 SDD。
- `tools`：TargetSelector 属于 ECS Tools owner。
- `ability-system`：AbilityImpactTool、Ability target 调用点。
- `ai-system`：AI find/wander target 调用点。
- `feature-system`：Data/Feature ability handlers。
- `test-system` / `godot-scene-test`：TargetSelector tests 和场景验证。
- `ai-config-management` / `skill-test`：如果修改 `.ai-config` skill 源。

## 工作区

- **Framework Git Boundary**: `/home/slime/Code/SlimeAI/SlimeAI`
- **Game Validation Git Boundary**: `/home/slime/Code/SlimeAI/Games/BrotatoLike`
- **Project**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`
- **Current SDD**: `sdds/026-SDD-0036-target-query-engine-hard-cutover/`

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
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/Core/roadmap.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/README.md`
7. `design/Tool/其他Tool/01-现状证据与AI-first裁决.md`
8. `design/Tool/其他Tool/05-TargetSelector查询契约.md`
9. `design/Tool/其他Tool/06-实施路线与验证门禁.md`
10. `DocsAI/ECS/Tools/TargetSelector/README.md`
11. 本 SDD 的 `README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`progress.md`

## 核心裁决

- 新 current API 是 `TargetQueryEngine` / `TargetQueryResult`。
- 查询失败必须返回 diagnostics，不靠日志猜。
- `EntityTargetSelector.Query(query)` 不作为执行期、最终或 AI current 入口保留。
- Candidate source 必须封装；Ability/AI/Feature 不直接知道 NodeLifecycle。
- 随机必须可 seed/RNG 注入。
- 先做 query contract，不先上空间索引。

## 禁止结果

- 不让 Ability/AI handler 回到 `GetTree().GetNodesInGroup()` 或手写距离扫描。
- 不保留 list-only facade 作为 current 示例。
- 不在 query 内部用 `new Random()` 或 `Time.GetTicksMsec()` 随机播种。
- 不把 TargetSelector 合并进 Math。

## T1.1 Readiness Baseline

先只读，不改实现。记录摘要到 `progress.md`。

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
find Src/ECS/Tools/TargetSelector -maxdepth 3 -type f | sort
sed -n '1,260p' Src/ECS/Tools/TargetSelector/TargetSelectorQuery.cs
sed -n '1,320p' Src/ECS/Tools/TargetSelector/EntityTargetSelector.cs
sed -n '1,260p' Src/ECS/Tools/TargetSelector/PositionTargetSelector.cs
sed -n '1,220p' Src/ECS/Capabilities/Ability/System/AbilityImpactTool.cs
sed -n '1,220p' Src/ECS/Capabilities/AI/System/Actions/Combat/FindEnemyAction.cs
sed -n '1,220p' Src/ECS/Capabilities/AI/System/Actions/Movement/RandomWanderAction.cs
rg -n "EntityTargetSelector|PositionTargetSelector|TargetSelectorQuery|TargetSorting|GeometryType|TeamFilter" Src/ECS Data DocsAI/ECS
rg -n "new Random\\(|GD\\.Randf\\(|Time\\.GetTicksMsec\\(\\)" Src/ECS/Tools/TargetSelector Src/ECS/Capabilities Data/Feature
python3 Workspace/SDD/sdd.py validate SDD-0036
```

## 实现顺序

严格按 `tasks.md` 推进 T1.1 到 T1.11。每完成一项任务就更新 `tasks.md` 和 `progress.md`。

## 最终验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0036
git diff --check
```

Godot runner 可用时：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/TargetSelector/Tests/TargetSelectorTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
