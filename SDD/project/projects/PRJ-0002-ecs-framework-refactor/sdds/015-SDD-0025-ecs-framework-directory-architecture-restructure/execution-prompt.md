# SDD-0025 Execution Prompt

你正在执行 `SDD-0025 ECS Framework Directory Architecture Restructure`。

## 当前目标

把 SlimeAI ECS 框架目录从技术层分散结构迁移到：

```text
Src/ECS/Runtime
Src/ECS/Capabilities
DocsAI/ECS/Runtime
DocsAI/ECS/Capabilities
DocsAI/ECS/Foundations
```

保留 ECS 概念，不迁到 `GameOS/`。

## 必读

1. `../../design/6.ECS框架目录架构大重构/README.md`
2. `../../design/6.ECS框架目录架构大重构/01-现状证据与AI-first裁决.md`
3. `../../design/6.ECS框架目录架构大重构/02-目标目录架构与归属规则.md`
4. `../../design/6.ECS框架目录架构大重构/03-迁移切片与验证门禁.md`
5. `../../directory-architecture-restructure-execution-prompt.md`
6. `README.md`
7. `tasks.md`
8. `progress.md`

## 禁止

- 不直接迁到 `/GameOS`。
- 不删除 ECS 语义。
- 不把 Runtime 变成业务大杂烩。
- 不把 Tools/UI 强行塞进 Capabilities。
- 不手动移动会被 DataOS generator 覆盖的 generated 文件。
- 不混入当前工作区已有 `.uid` / `__pycache__` 改动。
- 不在 `Src/ECS` 新增长期 Markdown 文档。

## 执行起点

从 T1.1 开始：

```bash
git rev-parse --show-toplevel
git status --short
find Src/ECS -maxdepth 3 -type d | sort
find DocsAI/ECS -maxdepth 3 -type d | sort
rg -n "Src/ECS/Base|DocsAI/ECS/System|DocsAI/ECS/Component|Data/EventType|Data/DataKey" Src DocsAI Data SDD -g '*'
python3 Workspace/SDD/sdd.py validate SDD-0025
```

只记录 baseline，不移动文件。

## 执行顺序

1. DocsAI 规则先行。
2. 建立目标目录索引和迁移清单。
3. Runtime 内核迁移。
4. Ability / Movement / Damage 迁移。
5. Collision / Feature / Effect / Projectile / AI / Spawn / Unit 迁移。
6. DocsOld 原文复制到 Foundations。
7. 最终旧路径清理和全量验证。

每个阶段完成后更新 `tasks.md`、`progress.md` 和项目级 `progress.md`。

