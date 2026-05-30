# SystemAgent Optimization Project Design

## Goal

本项目把 SystemAgent 优化从散落的 Idea 文档提升为可执行、可恢复、可校验的项目级 SDD 容器。项目级目录保存共享设计，子 SDD 保存单个可执行任务的设计、任务、进度、BDD 和验证证据。

## Context

当前 `SDD/<state>/SDD-xxxx/` 单层模型适合独立任务，但不适合一组有顺序、共享设计和长期路线图的任务。SystemAgent 优化已经包含 OpenSpec 退场、SDD CLI 信息质量加固、项目容器、hook 稳定性、workflow/skill 分层等多个相关任务，需要项目级组织。

## Model

项目使用 `SDD/project/projects/PRJ-0001-systemagent-optimization/`。项目完成后使用 `project-archive` 移动到 `SDD/project/archived/`。真实状态来自 `project.json.status`，目录只表达组织和归档位置。

项目下的 `design/` 保存共享设计资料；`roadmap.md` 维护子 SDD 顺序；`sdds/` 保存有序子 SDD，例如 `001-SDD-0004-sdd-project-container-model/`。子 SDD 的真实状态来自自身 `sdd.json.status`。

## Migration Boundary

本项目先复制 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/` 的设计资料进入项目 `design/`，保留来源说明。后续子 SDD 应优先引用项目级设计，不再依赖 Idea 目录作为长期事实源。

## Verification

项目级验证以 `python3 Workspace/SDD/sdd.py validate --all`、SDD CLI 单测、skill 同步与 skill lint 为准。单个子 SDD 完成前必须记录验证命令和结果摘要。
