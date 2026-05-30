# SDD Changelog

## 2026-05-25 — CLI Source Modularization

### Added

- 新增 `Workspace/SDD/Src/` Python package，按职责承载 SDD CLI 的命令、配置、模板、仓储、索引、校验、任务、进度和写入逻辑。
- 新增入口结构回归测试，确保 `Workspace/SDD/sdd.py` 只保留 `build_parser()` 与 `main()`。

### Changed

- `Workspace/SDD/sdd.py` 改为稳定 CLI 入口，只负责命令行参数定义、命令绑定和异常处理。
- `Workspace/SDD/README.md` 新增源码布局说明。

### Validation

- `python3 -m py_compile Workspace/SDD/sdd.py Workspace/SDD/Src/*.py`
- `python3 -m unittest discover Workspace/SDD/tests`
- `python3 Workspace/SDD/sdd.py doctor`
- `python3 Workspace/SDD/sdd.py validate --all`
- `git diff --check`

## 2026-05-25 — Project Containers and Metadata Status Model

### Added

- 新增 `SDD/project/` 项目命名空间。
- 新增 `SDD/project/projects/` 当前项目目录和 `SDD/project/archived/` 项目归档目录。
- 新增项目 CLI：`project-new`、`project-list`、`project-show`、`project-archive`。
- 新增 `new --project <project-id>`，用于创建项目内有序子 SDD。
- 新增项目校验规则 `SDD026`。

### Changed

- `sdd.json.status` 成为 SDD 状态事实源，不再要求与父目录名一致。
- `start`、`block`、`done` 只更新 metadata、README、tasks 和 progress，不再移动 SDD 目录。
- `catalog.json` schema 升级到 `2`，新增 `projects` 与项目字段。
- `INDEX.md` 新增项目总览，并在 SDD 表中显示 `Project`。
- `project-new` 生成的 `roadmap.md` 精简为文档为中心的两表模型：`Design Progress`（文档完成追踪）+ `Next SDDs`（下一步计划），替代原先的三表（SDD Execution Plan / Design-to-SDD Traceability / Not Yet Created SDDs）。
- 项目根 `progress.md` 模板的 `Project Status Board` 精简为 SDD / Status / Design Docs / Current Result 四列。
- SDD 系统历史任务迁入 `PRJ-0001`：`001-SDD-0001`、`002-SDD-0003`、`003-SDD-0004`，项目级 SDD 专项设计归入 `design/SDD/`。
- 项目进度事实源收敛为项目根 `progress.md`；`design/执行情况.md` 不再作为项目记录维护。
- 子 SDD 不再重复保存项目级 SDD 详细设计；子 SDD `design/` 只保留任务级摘要，并通过 `shared_design_refs` 引用项目 `design/SDD/`。
- `PRJ-0001` 的 `roadmap.md` 已补充设计文档到 SDD 的映射、未创建 SDD 清单和推荐执行顺序。

### Removed

- 删除旧 `Workspace/DocsAI/Idea/SystemAgent优化/` 过渡目录；设计资料已迁移到 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/` 统一维护。

### Validation

- `python3 -m unittest discover Workspace/SDD/tests`
- `python3 Workspace/SDD/sdd.py validate --all`
- `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all`
