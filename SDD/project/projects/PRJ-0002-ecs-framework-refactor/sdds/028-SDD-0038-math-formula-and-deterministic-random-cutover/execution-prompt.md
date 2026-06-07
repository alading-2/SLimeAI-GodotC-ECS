# SDD-0038 Execution Prompt

把本文件整体交给新的执行会话。目标是完成 `SDD-0038 Math Formula And Deterministic Random Cutover`，不是继续把所有公式堆进 `MyMath`。

## 角色定位

你是 SDD-0038 的主执行者。默认中文回答；命令、代码、错误信息保留原文。大任务先计划，再执行。改文件前先读相关文件，改完总结改动和验证结果。不要 push，不要回滚用户已有改动。

必须使用相关 skill：

- `sdd-workflow` / `sdd-management`：恢复和更新 SDD。
- `tools`：Math 属于 ECS Tools owner。
- `damage-system`：护甲/伤害公式。
- `ability-system`：冷却/充能/技能概率公式。
- `movement-system`：Bezier/Curve movement 调用点。
- `test-system` / `godot-scene-test`：Math tests 和场景验证。
- `ai-config-management` / `skill-test`：如果修改 `.ai-config` skill 源。

## 工作区

- **Framework Git Boundary**: `/home/slime/Code/SlimeAI/SlimeAI`
- **Game Validation Git Boundary**: `/home/slime/Code/SlimeAI/Games/BrotatoLike`
- **Project**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`
- **Current SDD**: `sdds/028-SDD-0038-math-formula-and-deterministic-random-cutover/`

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
7. `design/Tool/其他Tool/03-Math目标架构与验证.md`
8. `design/Tool/其他Tool/06-实施路线与验证门禁.md`
9. `DocsAI/ECS/Tools/Math/`
10. 本 SDD 的 `README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`progress.md`

## 核心裁决

- Math 功能保留。
- 不保留 `MyMath` 作为所有公式的杂项 current 入口。
- 随机必须可复现；测试不依赖不可控统计波动。
- 纯几何归 Math，查询语义归 TargetSelector。
- 不引入第三方数学库，不迁到 `System.Numerics`。

## 禁止结果

- 不让 Math 读取 Entity.Data 或调用 EventBus / SceneTree。
- 不把 `GeometryType` / `TargetSelectorQuery` 放进 Math。
- 不继续在测试里依赖 `GD.Randf()` 统计概率。
- 不把旧公式注释的多个版本都留成 current 参考。

## T1.1 Readiness Baseline

先只读，不改实现。记录摘要到 `progress.md`。

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
find Src/ECS/Tools/Math -maxdepth 4 -type f | sort
sed -n '1,320p' Src/ECS/Tools/Math/MyMath.cs
find Src/ECS/Tools/Math/Tests -maxdepth 3 -type f | sort
rg -n "MyMath|CheckProbability|GD\\.Randf\\(|new Random\\(|Time\\.GetTicksMsec\\(" Src/ECS Data DocsAI/ECS
rg -n "Geometry2D|GeometryCalculator|TargetSelectorQuery|GeometryType" Src/ECS/Tools DocsAI/ECS/Tools
python3 Workspace/SDD/sdd.py validate SDD-0038
```

## 实现顺序

严格按 `tasks.md` 推进 T1.1 到 T1.10。每完成一项任务就更新 `tasks.md` 和 `progress.md`。

## 最终验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0038
git diff --check
```

Godot runner 可用时：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/Math/Tests/MyMathTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
