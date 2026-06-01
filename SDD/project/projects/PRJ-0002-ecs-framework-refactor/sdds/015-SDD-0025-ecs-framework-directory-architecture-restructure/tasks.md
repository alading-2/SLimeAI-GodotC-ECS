# Tasks

## Progress

- **Status**: pending
- **Completed**: 0/8
- **Current**: T1.1

## Task List

- [ ] T1.1 Readiness baseline
  - **Scope**: 确认 Git boundary、dirty 范围、当前 `Src/ECS` / `DocsAI/ECS` / `Data` 旧路径引用和 SDD-0025 设计入口。
  - **Checks**: `Src/ECS/Base`、`DocsAI/ECS/System`、`DocsAI/ECS/Component`、`Data/EventType`、`Data/DataKey`、Godot `.tscn` `res://` 引用。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0025`

- [ ] T1.2 DocsAI 管理规则和索引先行
  - **Scope**: 更新 DocsAI 路由规则，确立 `Runtime / Capabilities / Foundations` 为新目录架构；不移动源码。
  - **Files**: `DocsAI/README.md`、`DocsAI/INDEX.md`、`DocsAI/ECS/README.md`、`DocsAI/管理/DocsAI统一管理与索引规则.md`、必要 owner skill。
  - **Validation**: `find DocsAI -type f -name '*.md' | sort`、`find Src/ECS -type f -name '*.md' | sort`、`python3 Workspace/SDD/sdd.py validate SDD-0025`

- [ ] T1.3 建立目标目录索引和迁移清单
  - **Scope**: 建立 `DocsAI/ECS/Runtime/`、`DocsAI/ECS/Capabilities/`、`DocsAI/ECS/Foundations/` 入口和目录迁移清单；源码目标目录只在执行切片中创建。
  - **Validation**: DocsAI 索引检查、SDD validate。

- [ ] T1.4 Runtime 内核迁移
  - **Scope**: 分批迁移 `Base/Event`、`Base/Data`、`Base/System/Core`、`Base/Entity/Core` 到 `Src/ECS/Runtime/`，同批更新 `.tscn`、using、namespace、DocsAI 和 skill。
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`、`Tools/run-build.sh`、Runtime 旧路径 grep gate。

- [ ] T1.5 第一批 Capability 迁移：Ability / Movement / Damage
  - **Scope**: 迁移最常用且分散最明显的 owner，包括 System、Component、Events、Tests、DataKey 归属和 DocsAI。
  - **Validation**: build/tests、Ability/Movement/Damage owner grep gate、必要 Godot scene。

- [ ] T1.6 第二批 Capability 迁移：Collision / Feature / Effect / Projectile / AI / Spawn / Unit
  - **Scope**: 迁移剩余 owner；处理跨 owner 依赖、DataOS generator 输出和测试场景。
  - **Validation**: build/tests、owner grep gate、必要 Godot scene。

- [ ] T1.7 DocsOld 迁入 Foundations
  - **Scope**: 将 DocsOld 重要概念文档原文复制到 `DocsAI/ECS/Foundations/`，新增来源索引，不改正文；是否删除 DocsOld 由用户另行确认。
  - **Validation**: `find DocsAI/ECS/Foundations -type f | sort`、`git status --short`

- [ ] T1.8 最终清理、DocsAI/skill 同步和验证
  - **Scope**: 清理旧入口或标记过渡残留，更新 SDD/project progress、DocsAI 索引、owner skill、AGENTS 同步副本。
  - **Validation**: `Tools/run-build.sh`、`Tools/run-tests.sh`、`python3 Workspace/SDD/sdd.py validate --all`、`bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`、`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`、必要 BrotatoLike Godot smoke。
