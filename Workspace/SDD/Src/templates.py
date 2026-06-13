from __future__ import annotations

from typing import Any

from .config import REPO_ROOT
from .io import now, today


def root_readme() -> str:
    return """# SDD

SDD 是 SlimeAI 工作区的中大型任务上下文胶囊系统，用于保存设计、任务、进度、行为约束和验证证据。

## 目录职责

- `pending/`：已创建但尚未开始执行的 SDD。
- `active/`：正在执行的 SDD。
- `blocked/`：因缺信息、失败或外部条件暂停的 SDD。
- `done/`：已完成并记录验证结果的 SDD。
- `project/projects/`：当前项目级 SDD 容器。
- `project/archived/`：已归档项目级 SDD 容器。
- `templates/`：手动创建 SDD 时使用的模板。
- `INDEX.md`：CLI 生成的人类可读索引。
- `catalog.json`：CLI 生成的机器可读索引。

## 常用命令

```bash
python3 Workspace/SDD/sdd.py new "Task Title"
python3 Workspace/SDD/sdd.py list
python3 Workspace/SDD/sdd.py show SDD-0001
python3 Workspace/SDD/sdd.py validate --all
python3 Workspace/SDD/sdd.py index
```

## 使用原则

- 小任务不强制创建 SDD。
- 中大型任务、跨模块重构、AI 配置治理和长期恢复任务应创建 SDD。
- 单个 SDD 的 `README.md` 是入口卡片，不承载完整设计正文。
- 完整设计放入项目 `design/` 或任务 `design/`，`progress.md` 只保留状态面板和验证摘要。
"""


def template_readme() -> str:
    return """# SDD-0000 Example Title

## Index Card

- **Status**: pending
- **Created**: YYYY-MM-DD
- **Updated**: YYYY-MM-DD
- **Type**: workflow
- **Scope**: Workspace/SDD
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - Workspace/SDD
- **Tags**: sdd

## What This SDD Is About

一句话说明这个 SDD 要解决什么问题。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `progress.md` — State / Decisions / Validation 状态面板
4. `bdd.md` — FeatureSpec 引用、行为摘录或不适用说明
5. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: T1.1
- **Last Conclusion**: SDD 已创建，等待补充设计与任务。
- **Next Action**: 阅读设计并更新任务。
- **Open Blockers**: none
"""


def template_metadata() -> dict[str, Any]:
    return {
        "id": "SDD-0000",
        "slug": "example-title",
        "title": "Example Title",
        "status": "pending",
        "type": "workflow",
        "created_at": "YYYY-MM-DD",
        "updated_at": "YYYY-MM-DD",
        "scope": "Workspace/SDD",
        "git_boundaries": [str(REPO_ROOT)],
        "affected_areas": ["Workspace/SDD"],
        "tags": ["sdd"],
        "progress": {
            "current_task": "T1.1",
            "completed_tasks": 0,
            "total_tasks": 1,
            "percent": 0,
        },
        "bdd": {
            "required": True,
            "reason": "This SDD changes CLI or workflow behavior.",
        },
        "links": {
            "design_index": "design/INDEX.md",
            "main_design": "design/main.md",
            "tasks": "tasks.md",
            "progress": "progress.md",
            "bdd": "bdd.md",
            "notes": "notes.md",
        },
    }


def template_design_index() -> str:
    return """# Design Index

## Main Design

- `main.md`

## Documents

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `main.md` | main | current | YYYY-MM-DD | 主设计文档 |
"""


def template_main_design(title: str = "Example Title") -> str:
    return f"""# {title}

## Goal

说明这个 SDD 要解决的问题。

## Context

列出必要上下文、现有事实源和约束。

## Design

描述最终设计、取舍和影响范围。

## Verification

列出完成后必须执行的验证。
"""


def template_tasks() -> str:
    return """# Tasks

## Progress

- **Status**: pending
- **Completed**: 0/1
- **Current**: T1.1

## Task List

- [ ] T1.1 建立 SDD 入口、设计、任务和验证记录
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0000`
"""


def template_progress() -> str:
    return """# Progress

## State

- **Status**: pending
- **Current**: T1.1
- **Next**: 阅读设计并更新任务。
- **Blocker**: none

## Decisions

- none

## Validation

- pending
"""


def template_bdd() -> str:
    return """# BDD

## Applicability

- **Required**: true
- **Reason**: This SDD changes CLI or workflow behavior.
- **Source**: `design/main.md`
- **Executed features**: T1.1

## Scenarios

### Scenario: Restore task context from an SDD

Given an SDD exists with README, tasks, progress, design and bdd files
When an AI or user opens the SDD README
Then they can identify status, scope, affected areas, reading order and next action
"""


def template_notes() -> str:
    return """# Notes

## References

- 无。

## Open Questions

- 无。
"""


def template_project_design_index() -> str:
    return """# Project Design Index

## Main Design

- `main.md`

## Documents

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `main.md` | main | current | YYYY-MM-DD | 项目共享设计 |
"""


def build_project_readme(metadata: dict[str, Any]) -> str:
    tags = ", ".join(metadata.get("tags", [])) or "none"
    return f"""# {metadata['id']} {metadata['title']}

## Index Card

- **Status**: {metadata['status']}
- **Created**: {metadata['created_at']}
- **Updated**: {metadata['updated_at']}
- **Scope**: {metadata.get('scope', '')}
- **Current SDD**: {metadata.get('current_sdd', 'none')}
- **Tags**: {tags}

## What This Project Is About

{metadata['title']}

## Reading Order

1. `design/INDEX.md` — 项目共享设计索引
2. `Core/roadmap.md` — 设计文档到 SDD 的映射、执行顺序、依赖和状态
3. `Core/progress.md` — 项目级关键结论和恢复点
4. `sdds/` — 项目内有序 SDD
5. `Core/notes.md` — 参考与开放问题
"""


def build_project_roadmap(metadata: dict[str, Any]) -> str:
    return f"""# Roadmap

## Purpose

项目级执行路线图，追踪 `design/` 下每份文档的完成情况和对应 SDD。多份文档可以合并为一个 SDD，不要求一对一。

## Design Progress

| Design Document | Done | SDD | Notes |
| --- | --- | --- | --- |
| `design/main.md` | — | — | 项目主设计，共享上下文 |

## Next SDDs

| Priority | Design Docs | Goal |
| --- | --- | --- |
| P0 | `design/main.md` | 用 `python3 Workspace/SDD/sdd.py new "<title>" --project {metadata['id']}` 创建第一个子 SDD |
"""


def build_project_progress(project_id: str, title: str, timestamp: str) -> str:
    return f"""# Project Progress

## State

- **Status**: active
- **Current SDD**: none
- **Next**: 阅读 project.json、design/INDEX.md 和 Core/roadmap.md，补充设计到 SDD 的映射后继续推进。
- **Blocker**: none

## Project Status Board

| SDD | Status | Design Docs | Current Result |
| --- | --- | --- | --- |
| none | — | `design/main.md` | 已创建项目容器 |

## Decisions

- {timestamp}: {project_id} 已创建，用于组织 {title}。

## Validation

- pending
"""


def build_readme(metadata: dict[str, Any],
                 latest: dict[str, str] | None = None) -> str:
    latest = latest or {}
    affected = "\n".join(
        f"  - {area}"
        for area in metadata.get("affected_areas", [])) or "  - 未指定"
    tags = ", ".join(metadata.get("tags", [])) or "none"
    boundaries = metadata.get("git_boundaries", []) or [str(REPO_ROOT)]
    return f"""# {metadata['id']} {metadata['title']}

## Index Card

- **Status**: {metadata['status']}
- **Created**: {metadata['created_at']}
- **Updated**: {metadata['updated_at']}
- **Type**: {metadata.get('type', 'workflow')}
- **Scope**: {metadata.get('scope', '')}
- **Git Boundary**: {boundaries[0]}
- **Affected Areas**:
{affected}
- **Tags**: {tags}

## What This SDD Is About

{metadata['title']}

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `progress.md` — State / Decisions / Validation 状态面板
4. `bdd.md` — FeatureSpec 引用、行为摘录或不适用说明
5. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: {latest.get('current_task', metadata.get('progress', {}).get('current_task', 'T1.1'))}
- **Last Conclusion**: {latest.get('last_conclusion', 'SDD 已创建，等待继续推进。')}
- **Next Action**: {latest.get('next_action', '阅读设计并更新任务。')}
- **Open Blockers**: {latest.get('open_blockers', 'none')}
"""


def build_design_index(main_file: str, title: str, updated: str) -> str:
    return f"""# Design Index

## Main Design

- `{main_file}`

## Documents

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `{main_file}` | main | current | {updated} | {title} 主设计 |
"""


def build_tasks(status: str, sdd_id: str) -> str:
    return f"""# Tasks

## Progress

- **Status**: {status}
- **Completed**: 0/1
- **Current**: T1.1

## Task List

- [ ] T1.1 建立 SDD 入口、设计、任务和验证记录
  - **Validation**: `python3 Workspace/SDD/sdd.py validate {sdd_id}`
"""


def build_progress(sdd_id: str, title: str, timestamp: str) -> str:
    return f"""# Progress

## State

- **Status**: pending
- **Current**: T1.1
- **Next**: 阅读 README、design/INDEX.md 和 tasks.md 后继续推进。
- **Blocker**: none

## Decisions

- {timestamp}: {sdd_id} 已创建，用于跟踪 {title}。

## Validation

- pending
"""
