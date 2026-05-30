from __future__ import annotations

from pathlib import Path

from .catalog import build_index, write_catalog
from .config import PROJECT_BUCKETS, STATUSES
from .io import write_if_missing, write_json
from .templates import (
    root_readme,
    template_bdd,
    template_design_index,
    template_main_design,
    template_metadata,
    template_notes,
    template_progress,
    template_readme,
    template_tasks,
)


def ensure_root(root: Path) -> None:
    root.mkdir(parents=True, exist_ok=True)
    for state in STATUSES:
        (root / state).mkdir(parents=True, exist_ok=True)
        write_if_missing(root / state / ".gitkeep", "")
    for bucket in PROJECT_BUCKETS:
        project_dir = root / "project" / bucket
        project_dir.mkdir(parents=True, exist_ok=True)
        write_if_missing(project_dir / ".gitkeep", "")
    (root / "templates" / "design").mkdir(parents=True, exist_ok=True)
    write_if_missing(root / "README.md", root_readme())
    write_if_missing(root / "INDEX.md", build_index([], root))
    if not (root / "catalog.json").exists():
        write_catalog(root, [])
    write_if_missing(root / "templates" / "README.md", template_readme())
    write_json_if_missing(root / "templates" / "sdd.json", template_metadata())
    write_if_missing(root / "templates" / "design" / "INDEX.md",
                     template_design_index())
    write_if_missing(root / "templates" / "design" / "main.md",
                     template_main_design())
    write_if_missing(root / "templates" / "tasks.md", template_tasks())
    write_if_missing(root / "templates" / "progress.md", template_progress())
    write_if_missing(root / "templates" / "bdd.md", template_bdd())
    write_if_missing(root / "templates" / "notes.md", template_notes())


def write_json_if_missing(path: Path, data: dict[str, Any]) -> None:
    if not path.exists():
        write_json(path, data)
