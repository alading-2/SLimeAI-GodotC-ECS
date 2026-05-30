from __future__ import annotations

import argparse
from pathlib import Path
from typing import Any

from .catalog import write_catalog_and_index
from .errors import SDDCliError
from .io import now, slugify, today, write_json, write_text
from .repository import load_project, next_project_id
from .root_ops import ensure_root
from .templates import (
    build_project_progress,
    build_project_readme,
    build_project_roadmap,
    template_main_design,
    template_notes,
    template_project_design_index,
)


def create_project(root: Path, title: str,
                   args: argparse.Namespace) -> dict[str, Any]:
    ensure_root(root)
    project_id = next_project_id(root)
    slug = slugify(title)
    created = today()
    timestamp = now()
    metadata = {
        "id": project_id,
        "slug": slug,
        "title": title,
        "status": "active",
        "created_at": created,
        "updated_at": created,
        "scope": args.scope,
        "tags": args.tag or [],
        "current_sdd": "none",
        "sdds": [],
        "links": {
            "design_index": "design/INDEX.md",
            "roadmap": "roadmap.md",
            "progress": "progress.md",
            "notes": "notes.md",
            "sdds": "sdds",
        },
    }
    project = root / "project" / "projects" / f"{project_id}-{slug}"
    if project.exists():
        raise SDDCliError(f"目标项目已存在: {project}")
    (project / "design").mkdir(parents=True)
    (project / "sdds").mkdir()
    write_text(project / "README.md", build_project_readme(metadata))
    write_json(project / "project.json", metadata)
    write_text(project / "design" / "INDEX.md",
               template_project_design_index().replace("YYYY-MM-DD", created))
    write_text(project / "design" / "main.md", template_main_design(title))
    write_text(project / "roadmap.md", build_project_roadmap(metadata))
    write_text(project / "progress.md",
               build_project_progress(project_id, title, timestamp))
    write_text(project / "notes.md", template_notes())
    write_catalog_and_index(root)
    loaded = load_project(project)
    if loaded is None:
        raise SDDCliError(f"创建后无法读取项目: {project}")
    return loaded
