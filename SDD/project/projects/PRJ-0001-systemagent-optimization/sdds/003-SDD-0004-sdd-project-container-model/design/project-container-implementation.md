# Project Container Implementation Notes

## Purpose

本文件补充 `main.md` 的实现细节，确保任务级 `design/` 不只是薄入口。项目级共享资料保存在父项目 `design/`，本 SDD 只记录本次 CLI 改造的任务级决策。

## CLI Surface

新增项目命令包括 `project-new`、`project-list`、`project-show` 和 `project-archive`。这些命令围绕 `project.json` 工作，不复用 `sdd.json`，避免项目和任务实例混淆。

`new --project PRJ-0001` 创建项目子 SDD，目录名采用 `001-SDD-0004-slug`。其中 `001` 是项目内顺序，`SDD-0004` 仍是全局 SDD ID。

## Scanning Model

`collect_projects()` 只扫描 `SDD/project/projects/` 与 `SDD/project/archived/`。`collect_instances()` 保留 legacy 状态目录扫描，并额外扫描每个项目的 `sdds/`。这让旧 SDD 不需要立即迁移，同时新项目结构可被 `list`、`show`、`validate` 和 `index` 统一识别。

## Metadata Status Model

`start`、`block`、`done` 不再调用目录移动逻辑。命令只更新 `sdd.json.status`、README、tasks 和 progress。项目归档用 `project-archive` 显式移动项目目录，并同步 `project.json.status`。

## Validation Model

`SDD004` 只校验 `sdd.json.status` 是否属于允许状态，不再校验父目录。项目结构使用 `SDD026` 校验，包括 `project.json` 合法性、必备文件和归档目录中的状态一致性提醒。

## Catalog Model

`catalog.json` schema 升级为 `2`，保留 `items` 表示 SDD，新增 `projects` 表示项目。`INDEX.md` 新增 Projects 区域，SDD 表增加 Project 列。
