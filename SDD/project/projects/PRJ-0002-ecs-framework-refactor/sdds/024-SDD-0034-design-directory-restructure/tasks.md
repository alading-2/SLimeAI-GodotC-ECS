# Tasks

## Progress

- **Status**: done
- **Completed**: 13/13
- **Current**: done

## Phase 1: Core 目录

- [x] T1.1 创建 `Core/` 目录，`git mv` roadmap.md、progress.md、notes.md 到 `Core/`
- [x] T1.2 `git mv` 3 个 execution-prompt.md 到 `Core/`

## Phase 2: design/Runtime + Foundation 整合

- [x] T2.1 `git mv` design/ 下编号目录（1-8）到 `design/Runtime/`
- [x] T2.2 `git mv` design/ECS框架优化/ 到 `design/Runtime/`
- [x] T2.3 创建 `design/Foundation/`，`git mv` 散落 md（00、01、03、04、06）到 `Foundation/`

## Phase 3: 路径引用更新

- [x] T3.1 更新 `design/INDEX.md` — 所有路径加 `Runtime/` 或 `Foundation/` 前缀
- [x] T3.2 更新 `README.md` — Reading Order 路径 + Core/ 文件路径
- [x] T3.3 更新 `Core/progress.md` — Timeline 中引用的 design 路径
- [x] T3.4 更新 `Core/roadmap.md` — Design Progress 表路径
- [x] T3.5 更新 `project.json` — links 字段
- [x] T3.6 更新 `sdds/` 下子 SDD 文件中引用的 design 路径

## Phase 4: 验证

- [x] T4.1 `rg` 搜索旧路径确认无残留引用 + `git status --short` 确认改动范围
- [x] T4.2 `python3 Workspace/SDD/sdd.py validate SDD-0034` — 0 error, 4 warning（均为 SDD-0034 自身 meta-task 警告）

## 额外修复

- [x] 更新 `Workspace/SDD/Src/validation.py` — `validate_project` 支持从 `project.json.links` 读取动态文件路径，不再硬编码 `roadmap.md`/`progress.md`/`notes.md`
