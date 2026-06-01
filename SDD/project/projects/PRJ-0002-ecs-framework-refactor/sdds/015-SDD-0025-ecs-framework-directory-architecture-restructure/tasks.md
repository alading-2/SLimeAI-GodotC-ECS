# Tasks

## Progress

- **Status**: done
- **Completed**: 8/8
- **Current**: done

## Task List

- [x] T1.1 Readiness baseline
  - **Scope**: 确认 Git boundary、dirty 范围、当前 `Src/ECS` / `DocsAI/ECS` / `Data` 旧路径引用和 SDD-0025 设计入口。
  - **Checks**: `Src/ECS/Base`、`DocsAI/ECS/System`、`DocsAI/ECS/Component`、`Data/EventType`、`Data/DataKey`、Godot `.tscn` `res://` 引用。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0025`

- [x] T1.2 DocsAI 管理规则和索引先行
  - **Scope**: 更新 DocsAI 路由规则，确立 `Runtime / Capabilities / Tools / UI` 为当前目录架构；不移动源码。
  - **Files**: `DocsAI/README.md`、`DocsAI/INDEX.md`、`DocsAI/ECS/README.md`、`DocsAI/管理/DocsAI统一管理与索引规则.md`、必要 owner skill。
  - **Validation**: `find DocsAI -type f -name '*.md' | sort`、`find Src/ECS -type f -name '*.md' | sort`、`python3 Workspace/SDD/sdd.py validate SDD-0025`

- [x] T1.3 建立目标目录索引和迁移清单
  - **Scope**: 建立 `DocsAI/ECS/Runtime/`、`DocsAI/ECS/Capabilities/`、`DocsAI/ECS/Tools/`、`DocsAI/ECS/UI/` 入口和目录迁移清单；源码目标目录只在执行切片中创建。
  - **Validation**: DocsAI 索引检查、SDD validate。

- [x] T1.4 Runtime 内核迁移
  - **Scope**: 分批迁移 `Base/Event`、`Base/Data`、`Base/System/Core`、`Base/Entity/Core` 到 `Src/ECS/Runtime/`，同批更新 `.tscn`、using、namespace、DocsAI 和 skill。
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`、Runtime 旧路径 grep gate。

- [x] T1.5 第一批 Capability 迁移：Ability / Movement / Damage
  - **Scope**: 迁移最常用且分散最明显的 owner，包括 System、Component、Events、Tests、DataKey 归属和 DocsAI。
  - **Validation**: build/tests、Ability/Movement/Damage owner grep gate、必要 Godot scene。

- [x] T1.6 第二批 Capability 迁移：Collision / Feature / Effect / Projectile / AI / Spawn / Unit
  - **Scope**: 迁移剩余 owner；处理跨 owner 依赖、DataOS generator 输出和测试场景。
  - **Validation**: build/tests、owner grep gate、必要 Godot scene。

- [x] T1.7 历史概念材料按 owner 收口
  - **Scope**: 不保留 `DocsAI/ECS/Foundations/` 当前入口；历史概念材料按 owner 分散到 `Concepts/`，或进入 `DocsAI/Archive/` / `DocsAI/思考/`。
  - **Validation**: `find DocsAI/ECS -maxdepth 2 -type d | sort`、`git status --short`

- [x] T1.8 最终清理、DocsAI/skill 同步和验证
  - **Scope**: 清理旧入口或标记过渡残留，更新 SDD/project progress、DocsAI 索引、owner skill、AGENTS 同步副本。
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`、`bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`、`python3 Workspace/SDD/sdd.py validate --all`、`bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`、`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`、必要 BrotatoLike Godot smoke。
