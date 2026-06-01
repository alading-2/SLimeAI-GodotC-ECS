# ECS Framework Directory Architecture Restructure

## Goal

把当前 `Src/ECS` 和 `DocsAI/ECS` 从“ECS 技术层平铺 + 功能 owner 分散”的结构，重构为：

```text
Runtime 内核 + Capabilities 功能 owner
```

完成后，AI 默认按 Capability 路由功能任务，仍按 ECS 语义理解每个 Capability 内部的 Component / Data / Event / System / Tests 边界。

## Context

完整设计已随本 SDD 复制到当前 `design/` 目录：

```text
README.md
01-现状证据与AI-first裁决.md
02-目标目录架构与归属规则.md
03-迁移切片与验证门禁.md
```

关键上下文：

- 当前方向文档已经裁决为 AI-first ECS，不是纯 GameOS。
- AiFirst 的 `GameOS/Capabilities` 结构对 AI 路由有价值，但不能照搬其“不是传统 ECS”的定位。
- 当前 `Src/ECS/Base` 混合 Runtime 基础设施和业务功能 System，长期会误导 AI。
- 当前 `DocsAI/ECS` 按 Entity/Data/Event/System/Component 分类，对 ECS 概念友好，但对 Ability、Movement、Damage 等功能任务不够直接。
- 当前 `Data/` 顶层包含 DataOS authoring、generated DataKey、EventType 和 gameplay data，迁移时必须先区分 generator 事实源，不能手动移动会被生成器覆盖的文件。
- 当前工作区已有用户未提交 `.uid` 删除/重命名和 `__pycache__`，本 SDD 执行时必须避开无关改动。

## Design

目标结构：

```text
Src/ECS/
├── Runtime/
├── Capabilities/
├── Tools/
└── UI/

DocsAI/ECS/
├── Runtime/
├── Capabilities/
├── Tools/
└── UI/
```

Runtime 初始承载：

- `Entity`
- `Data`
- `Event`
- `System`

Capabilities 初始承载：

- `Ability`
- `Damage`
- `Movement`
- `Collision`
- `Feature`
- `Effect`
- `Projectile`
- `AI`
- `Spawn`
- `Unit`

Tools 和 UI 暂时保持顶层，不在本 SDD 中强行塞进 Capabilities。

历史概念材料不再集中到 `DocsAI/ECS/Foundations/`。与具体 owner 强相关的内容进入对应 owner 的 `Concepts/`；只是历史背景或已失效方案的内容进入 `DocsAI/Archive/` 或 `DocsAI/思考/` 并标注非执行入口。

## Execution Strategy

本 SDD 分阶段执行：

1. Readiness baseline：记录 dirty 范围和旧路径引用，不改源码。
2. DocsAI 规则先行：先更新文档路由事实源。
3. 目标目录和迁移清单：建立 Runtime / Capabilities / Tools / UI 文档入口。
4. Runtime 内核迁移：Event、Data、System Core、Entity Core。
5. 第一批 Capability：Ability、Movement、Damage。
6. 第二批 Capability：Collision、Feature、Effect、Projectile、AI、Spawn、Unit。
7. 历史概念材料按 owner 收口：分散到 `Concepts/` 或 Archive/Thinking。
8. 最终清理和验证：旧路径 gate、构建、测试、SDD、skill sync、必要 Godot scene。

## Verification

设计/SDD 阶段验证：

```bash
python3 Workspace/SDD/sdd.py validate SDD-0025
python3 Workspace/SDD/sdd.py validate --all
```

执行阶段基础验证：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
```

DocsAI / skill 验证：

```bash
find DocsAI -type f -name '*.md' | sort
find Src/ECS -type f -name '*.md' | sort
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

Godot 场景验证按受影响 owner 选择，最终至少运行 BrotatoLike smoke。
