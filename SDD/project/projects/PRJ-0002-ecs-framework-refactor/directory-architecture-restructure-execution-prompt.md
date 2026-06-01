# ECS Framework Directory Architecture Restructure - Execution Prompt

你要执行 `SDD-0025 ECS Framework Directory Architecture Restructure`。

## 固定上下文

仓库：

```text
/home/slime/Code/SlimeAI/SlimeAI
```

项目：

```text
SDD/project/projects/PRJ-0002-ecs-framework-refactor
```

当前设计包：

```text
design/6.ECS框架目录架构大重构/
```

当前执行 SDD：

```text
sdds/015-SDD-0025-ecs-framework-directory-architecture-restructure/
```

## 必读顺序

1. `AGENTS.md`
2. `DocsAI/README.md`
3. `DocsAI/ECS框架与AIFirst方向决策.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/6.ECS框架目录架构大重构/README.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/6.ECS框架目录架构大重构/01-现状证据与AI-first裁决.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/6.ECS框架目录架构大重构/02-目标目录架构与归属规则.md`
7. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/6.ECS框架目录架构大重构/03-迁移切片与验证门禁.md`
8. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/015-SDD-0025-ecs-framework-directory-architecture-restructure/README.md`
9. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/015-SDD-0025-ecs-framework-directory-architecture-restructure/tasks.md`
10. 目标 owner 对应 skill，例如 ability-system、movement-system、damage-system、collision-system、feature-system。

## 架构裁决

最终方向是：

```text
Src/ECS/Runtime + Src/ECS/Capabilities
DocsAI/ECS/Runtime + DocsAI/ECS/Capabilities + DocsAI/ECS/Tools + DocsAI/ECS/UI
```

不要把当前仓迁到 `GameOS/`。  
不要删除 ECS 概念。  
不要恢复旧 Relationship / DataMeta / DataRegistry 兼容路线。  
不要把 Tools/UI 强行塞进 Capabilities；本 SDD 默认保留顶层，后续单独裁决。  
不要创建或恢复 `DocsAI/ECS/Foundations` 作为 current 路由层；历史概念材料按 owner `Concepts/`、Archive 或 Thinking 收口。
不要在 `Src/ECS` 新增框架 Markdown 文档。

## 执行循环

每个切片都按这个循环：

1. 读设计和 owner skill。
2. `git status --short`，记录当前 dirty 范围。
3. `rg` 旧路径引用，分桶记录。
4. 使用 `git mv` 或等价保留历史的方式移动文件。
5. 同批更新 `.tscn` 中 `res://` 路径、C# using/namespace、DocsAI 索引、skill 文档、DataOS generator 输出规则。
6. 运行切片验证。
7. 更新 `tasks.md` 和 `progress.md`。
8. 不混入用户已有未提交改动。

## 验证底线

每个源码迁移切片至少跑：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0025
```

跨多个 owner 或最终收口时跑：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate --all
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

涉及 Godot 场景路径时，按 owner 选择对应 headless scene；最终至少跑 BrotatoLike smoke。

## 成功标准

- `Src/ECS/Runtime` 承载 Runtime 内核。
- `Src/ECS/Capabilities/<Owner>` 承载 Runtime 之外功能 owner。
- `DocsAI/ECS/Runtime`、`DocsAI/ECS/Capabilities`、`DocsAI/ECS/Tools`、`DocsAI/ECS/UI` 是 AI 默认文档入口。
- `Foundation/Foundations` 不作为 current 路由层。
- 旧 `Base/` 和旧 DocsAI 分类不再作为默认入口。
- 所有路径变更都有构建、DataOS、SDD 和必要 Godot 验证证据。
