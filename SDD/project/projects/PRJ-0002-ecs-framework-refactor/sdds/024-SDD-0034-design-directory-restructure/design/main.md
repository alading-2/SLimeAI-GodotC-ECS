# Design Directory Restructure

## Goal

把 PRJ-0002 的项目级管理文件和设计材料从混杂根目录整理成可恢复、可导航、可校验的目录结构。完成后，项目入口只保留 `README.md`、`project.json`、`design/` 和 `sdds/` 等顶层事实源；长期进度、路线图、备注和执行提示词归入 `Core/`；设计资料按语义归入 `design/Runtime/`、`design/Foundation/` 和 `design/Tool/`。

## Context

- PRJ-0002 是 ECS framework refactor 的项目级 SDD，既有设计目录包含编号目录、早期分析文档、工具设计和项目管理文件。
- 重组前，`roadmap.md`、`progress.md`、`notes.md` 和多个 execution prompt 分散在项目根目录，设计目录内既有运行时架构材料，也有早期问题分析材料。
- SDD CLI 原本对项目容器的 `roadmap.md`、`progress.md`、`notes.md` 路径存在硬编码假设，目录重组必须同步更新 `project.json.links` 和 validator 读取逻辑。
- 这是已完成的目录治理任务，不改变 ECS runtime 行为，不迁移游戏资产，不改变子 SDD 的业务结论。

## Design

### 目录分层

- `Core/`：保存项目级管理与执行恢复材料，包括 `roadmap.md`、`progress.md`、`notes.md` 和 execution prompt。这样项目根目录不再混放过程文件。
- `design/Runtime/`：保存编号运行时设计目录和 `ECS框架优化/`，用于恢复 ECS Runtime / Capability 架构背景。
- `design/Foundation/`：保存早期问题分析和重构原则，例如旧 ECS 框架问题总览、Data 系统问题、字符串键名统一问题、优先级与拆分建议、完全重构原则。
- `design/Tool/`：继续承载工具类设计材料，不与 Runtime / Foundation 混排。

### 路径迁移

- 将项目根管理文件移动到 `Core/` 后，`project.json.links` 必须指向新的 `Core/roadmap.md`、`Core/progress.md` 和 `Core/notes.md`。
- 将编号设计目录移动到 `design/Runtime/` 后，项目 `design/INDEX.md`、`Core/roadmap.md`、`Core/progress.md` 和子 SDD 内引用必须同步到新路径。
- 将早期散落分析文档移动到 `design/Foundation/` 后，旧顶层路径只允许作为历史说明或迁移记录存在，不能继续作为默认入口。

### Validator 适配

- `Workspace/SDD/Src/validation.py` 需要读取项目 `project.json.links` 中声明的动态路径，而不是硬编码项目根的 `roadmap.md`、`progress.md`、`notes.md`。
- 该适配只改变项目容器验证路径解析，不改变单个 SDD 实例的必备文件契约。

### 取舍

- 本任务按 hard cutover 处理路径，不保留双路径兼容层。
- 旧文件路径只作为只读历史证据或引用迁移检查输入，不作为后续默认入口。
- 子 SDD 的业务内容不重写，只更新路径引用，避免把目录治理扩大为设计重写。

## Verification

- `rg` 搜索迁移前的旧路径，确认默认入口和引用已更新到 `Core/`、`design/Runtime/` 或 `design/Foundation/`。
- `git status --short` 检查移动、重命名和文件范围，避免跨 Git 边界或混入非目录治理改动。
- `python3 Workspace/SDD/sdd.py validate SDD-0034` 检查当前 SDD 必备文件、任务进度、BDD、design index 和 validation summary。
- `python3 Workspace/SDD/sdd.py validate --all` 检查项目容器动态路径和全局 SDD 索引。

## Completion Notes

本 SDD 后续因校验规则升级补实当前设计正文：原执行已经完成目录重组，但 `design/main.md` 仍保留脚手架文本，导致 done 状态下触发 `SDD015 template-residue-in-done` 和 `SDD025 thin-design-in-done`。本设计正文用于补齐该 SDD 的可恢复设计证据，不改变已完成的目录迁移范围。
