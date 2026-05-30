from __future__ import annotations

import argparse
import re
import shutil
from pathlib import Path
from typing import Any

from .catalog import write_catalog_and_index
from .config import REPO_ROOT, STATUSES
from .errors import SDDCliError
from .io import now, slugify, today, write_json, write_text
from .progress import extract_latest_resume, replace_latest_resume
from .repository import (
    instance_dir_name,
    load_instance,
    locate_project,
    next_id,
    next_project_order,
    project_child_dir_name,
)
from .root_ops import ensure_root
from .tasks import task_counts, update_tasks_header
from .templates import (
    build_design_index,
    build_progress,
    build_readme,
    build_tasks,
    template_bdd,
    template_main_design,
    template_notes,
)


def create_instance(root: Path, title: str,
                    args: argparse.Namespace) -> dict[str, Any]:
    ensure_root(root)
    project: dict[str, Any] | None = None
    project_order: int | None = None
    if getattr(args, "project", None):
        project = locate_project(root, args.project)
        if project.get("_bucket") == "archived":
            raise SDDCliError(f"不能在已归档项目中创建 SDD: {args.project}")
        project_order = next_project_order(project["_path"])
    sdd_id = next_id(root)
    slug = slugify(title)
    created = today()
    timestamp = now()
    areas = args.area or []
    if not areas and args.scope:
        areas = [args.scope]
    metadata = {
        "id": sdd_id,
        "slug": slug,
        "title": title,
        "status": "pending",
        "type": args.type,
        "created_at": created,
        "updated_at": created,
        "scope": args.scope,
        "git_boundaries": args.git_boundary or [str(REPO_ROOT)],
        "affected_areas": areas,
        "tags": args.tag or [],
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
    if project is not None and project_order is not None:
        metadata["project_id"] = project["id"]
        metadata["project_order"] = project_order
        metadata["shared_design_refs"] = ["../../design/INDEX.md"]
        instance = (project["_path"] / "sdds" /
                    project_child_dir_name(project_order, sdd_id, slug))
    else:
        instance = root / "pending" / instance_dir_name(sdd_id, slug)
    if instance.exists():
        raise SDDCliError(f"目标 SDD 已存在: {instance}")
    instance.mkdir(parents=True)
    (instance / "design").mkdir()
    (instance / "artifacts").mkdir()
    latest = {
        "current_task": "T1.1",
        "last_conclusion": f"{sdd_id} 已创建，用于跟踪 {title}。",
        "next_action": "阅读 README、design/INDEX.md 和 tasks.md 后继续推进。",
        "open_blockers": "none",
    }
    write_text(instance / "README.md", build_readme(metadata, latest))
    write_json(instance / "sdd.json", metadata)
    write_text(instance / "design" / "INDEX.md",
               build_design_index("main.md", title, created))
    write_text(instance / "design" / "main.md", template_main_design(title))
    write_text(instance / "tasks.md", build_tasks("pending", sdd_id))
    write_text(instance / "progress.md",
               build_progress(sdd_id, title, timestamp))
    write_text(instance / "bdd.md", template_bdd())
    write_text(instance / "notes.md", template_notes())
    write_catalog_and_index(root)
    loaded = load_instance(instance)
    if loaded is None:
        raise SDDCliError(f"创建后无法读取 SDD: {instance}")
    return loaded


def patch_markdown_field(text: str, label: str, value: str) -> str:
    pattern = rf"(^- \*\*{re.escape(label)}\*\*: ).*$"
    replacement = rf"\g<1>{value}"
    return re.sub(pattern, replacement, text, count=1, flags=re.MULTILINE)


def patch_readme_fields(instance: Path, metadata: dict[str, Any],
                        blockers: str) -> None:
    readme_path = instance / "README.md"
    if not readme_path.exists():
        latest = extract_latest_resume(instance / "progress.md")
        write_text(readme_path, build_readme(metadata, latest))
        return
    text = readme_path.read_text(encoding="utf-8")
    progress = metadata.get("progress", {})
    fields = [
        ("Status", metadata.get("status", "")),
        ("Updated", metadata.get("updated_at", "")),
        ("Type", metadata.get("type", "")),
        ("Scope", metadata.get("scope", "")),
        ("Current Task", progress.get("current_task", "none")),
        ("Open Blockers", blockers),
    ]
    for label, value in fields:
        text = patch_markdown_field(text, label, str(value))
    readme_path.write_text(text.rstrip() + "\n", encoding="utf-8")


def refresh_progress_metadata(instance: Path, metadata: dict[str,
                                                             Any]) -> None:
    completed, total, current = task_counts(instance / "tasks.md")
    percent = int((completed / total) * 100) if total else 0
    metadata["progress"] = {
        "current_task": current,
        "completed_tasks": completed,
        "total_tasks": total,
        "percent": percent,
    }


def save_metadata(instance: Path, metadata: dict[str, Any]) -> None:
    metadata["updated_at"] = today()
    metadata.pop("_path", None)
    metadata.pop("_state_dir", None)
    write_json(instance / "sdd.json", metadata)


def save_instance(instance: Path,
                  metadata: dict[str, Any],
                  conclusion: str,
                  next_action: str,
                  blockers: str = "none") -> None:
    save_metadata(instance, metadata)
    update_tasks_header(instance / "tasks.md", metadata)
    replace_latest_resume(instance / "progress.md", metadata, conclusion,
                          next_action, blockers)
    patch_readme_fields(instance, metadata, blockers)


def move_instance(root: Path, item: dict[str, Any],
                  target_status: str) -> Path:
    if target_status not in STATUSES:
        raise SDDCliError(f"未知状态: {target_status}")
    current = item["_path"]
    target = root / target_status / current.name
    if current == target:
        return current
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        raise SDDCliError(f"目标路径已存在: {target}")
    shutil.move(str(current), str(target))
    return target
