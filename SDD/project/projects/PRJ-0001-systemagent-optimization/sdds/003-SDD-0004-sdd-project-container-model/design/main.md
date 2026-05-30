# SDD Project Container Model

## Goal

引入项目级 SDD 容器，使一个长期项目能够包含多个有序子 SDD，并共享项目级设计资料。真实状态必须来自 `project.json.status` 或 `sdd.json.status`，目录只表达组织位置或归档位置。

## Context

当前 SDD MVP 采用 `SDD/pending|active|blocked|done/SDD-xxxx/` 单层结构。这个模型对独立任务简单，但对 SystemAgent Optimization 这类多阶段项目不直观：共享设计资料会重复，路线图散落，状态容易被目录和 metadata 双重表达造成漂移。

用户确认的新结构是 `SDD/project/` 命名空间：`SDD/project/projects/` 存放当前项目，`SDD/project/archived/` 存放已归档项目。项目内部用 `sdds/001-SDD-xxxx/` 表达项目内顺序。

## Design

CLI 新增项目命令：`project-new` 创建项目容器，`project-list` 列出项目，`project-show` 展示项目入口，`project-archive` 显式归档项目。`new --project PRJ-0001` 会把新 SDD 创建到项目 `sdds/` 下，并写入 `project_id`、`project_order` 和 `shared_design_refs`。

`collect_instances()` 同时扫描 legacy 状态目录和项目子 SDD。`collect_projects()` 专门扫描 `project/projects` 与 `project/archived`。`catalog.json` schema 升级到 2，新增 `projects` 字段；`INDEX.md` 新增 Projects 区域，并在 SDD 表中显示 Project。

状态流转改为 metadata-first：`start`、`block`、`done` 不再移动 SDD 目录，只更新 `sdd.json.status`、README、tasks 和 progress。`validate` 的 `SDD004` 只检查状态是否合法，不再检查父目录名。项目基础结构和 `project.json.status` 由 `SDD026` 校验。

## Risks

主要风险是旧 `SDD/pending|active|blocked|done` 目录仍存在，历史资料和 catalog 可能短期混用。为降低风险，本次保持 legacy 目录兼容读取，不强制迁移旧 SDD；项目迁移通过新项目实例逐步收敛。

## Verification

必须通过 `python3 -m unittest discover Workspace/SDD/tests`、`python3 -m py_compile Workspace/SDD/sdd.py Workspace/SDD/tests/test_sdd_cli.py`、`python3 Workspace/SDD/sdd.py validate --all`。改动 SDD skill 统一源后还必须运行同步脚本和 skill lint。
