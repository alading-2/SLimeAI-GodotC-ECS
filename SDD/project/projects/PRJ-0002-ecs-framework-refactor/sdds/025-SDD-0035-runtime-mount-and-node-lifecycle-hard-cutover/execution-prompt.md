# SDD-0035 Execution Prompt

把本文件整体交给新的执行会话。目标是完成 `SDD-0035 Runtime Mount And Node Lifecycle Hard Cutover`，不是只改文档，也不是给旧 `ParentManager` / `NodeLifecycleManager` 继续补兼容 facade。

## 角色定位

你是 SDD-0035 的主执行者。默认中文回答；命令、代码、错误信息保留原文。先读事实源，再计划，再实现。改文件前先读相关文件；改完必须总结改动和验证结果。不要 push，不要回滚用户已有改动。

必须使用相关 skill：

- `sdd-workflow` / `sdd-management`：恢复和更新 SDD。
- `tools`：ParentManager、ObjectPool mount 调用点属于 Tools 影响面。
- `ecs-entity` / `ecs-component`：EntityManager、ComponentRegistrar 注册边界。
- `ui-bind`：UIManager 注册边界。
- `test-system` / `godot-scene-test`：新增或运行 runtime 验证时使用。
- `ai-config-management` / `skill-test`：如果修改 `.ai-config` skill 源。

## 工作区

- **Framework Git Boundary**: `/home/slime/Code/SlimeAI/SlimeAI`
- **Project**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`
- **Current SDD**: `sdds/025-SDD-0035-runtime-mount-and-node-lifecycle-hard-cutover/`
- **Shared Design Package**: `design/Tool/其他Tool/`

执行 git 命令前必须确认边界：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
```

当前工作区可能已有 unrelated `__pycache__`、DocsAI 或其他文件改动。不要清理、回滚、覆盖或混入无关改动。

## 必读顺序

1. `AGENTS.md`
2. `DocsAI/README.md`
3. `DocsAI/ECS/README.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/Core/roadmap.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/Core/progress.md`
7. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/README.md`
8. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/01-现状证据与AI-first裁决.md`
9. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/04-NodeLifecycle与ParentManager边界.md`
10. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/06-实施路线与验证门禁.md`
11. 本 SDD 的 `README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`progress.md`

## 核心裁决

- 保留 ParentManager 的功能，不保旧 API 形状。
- 默认 runtime root 是 `/root/SlimeAIRuntime`。
- mount 必须 manifest 化，不靠自由字符串散落创建。
- `NodeLifecycle` 迁 Runtime registry 语义，只保底层注册和 diagnostics。
- Entity/UI/Component 对外查询走各自 manager。
- 业务路径不直接使用 NodeLifecycle 全局扫描。

## 禁止结果

- 不长期保留 `ParentManager.GetOrRegister(name, path)` 作为 current API。
- 不把 `ParentNames` 扩成业务关系常量表。
- 不让 TargetSelector 或 Ability/AI 继续直接扫 `NodeLifecycleManager.GetNodesByInterface<Node2D>()`。
- 不用 Godot group 替代 Entity/Data/owner/lifecycle 语义。
- 不把 ObjectPool parking state 写入 RuntimeMountRegistry。

## T1.1 Readiness Baseline

先只读，不改实现。记录摘要到 `progress.md`，不要复制完整 dirty 列表。

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
sed -n '1,260p' Src/ECS/Tools/ParentManager/ParentManager.cs
sed -n '1,220p' Src/ECS/Tools/ParentManager/ParentNames.cs
sed -n '1,260p' Src/ECS/Tools/NodeLifecycle/NodeLifecycleManager.cs
sed -n '1,220p' Src/ECS/Runtime/System/SystemManager.cs
sed -n '1,260p' Src/ECS/Runtime/Entity/Manager/EntityManager.cs
sed -n '1,220p' Src/ECS/UI/Core/UIManager.cs
rg -n "ParentManager\\.GetOrRegister|ParentManager\\.Register|ParentNames|EnsurePath" Src/ECS DocsAI/ECS
rg -n "NodeLifecycleManager\\.GetAllNodes|NodeLifecycleManager\\.GetNodesByInterface|NodeLifecycleManager\\.Register" Src/ECS DocsAI/ECS
python3 Workspace/SDD/sdd.py validate SDD-0035
```

## 实现顺序

严格按 `tasks.md` 执行 T1.1 到 T1.10。每完成一项任务，更新 `tasks.md` 和 `progress.md`。不要等最后一次性补状态。

## 最终验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0035
git diff --check
```

如果修改 `.ai-config`：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```
