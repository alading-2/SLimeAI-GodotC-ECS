from __future__ import annotations

from pathlib import Path
from typing import Any

from .config import PROJECT_BUCKETS, STATUSES
from .io import now, write_json, write_text
from .repository import collect_instances, collect_projects


def catalog_item(item: dict[str, Any], root: Path) -> dict[str, Any]:
    path = item["_path"]
    return {
        "id": item.get("id", path.name),
        "slug": item.get("slug", ""),
        "title": item.get("title", path.name),
        "status": item.get("status", item.get("_state_dir", "")),
        "type": item.get("type", ""),
        "updated_at": item.get("updated_at", ""),
        "scope": item.get("scope", ""),
        "affected_areas": item.get("affected_areas", []),
        "tags": item.get("tags", []),
        "project_id": item.get("project_id", ""),
        "project_order": item.get("project_order", ""),
        "current_task": item.get("progress", {}).get("current_task", ""),
        "path": str(path.relative_to(root.parent)),
    }


def project_catalog_item(item: dict[str, Any], root: Path) -> dict[str, Any]:
    path = item["_path"]
    return {
        "id": item.get("id", path.name),
        "slug": item.get("slug", ""),
        "title": item.get("title", path.name),
        "status": item.get("status", ""),
        "updated_at": item.get("updated_at", ""),
        "scope": item.get("scope", ""),
        "tags": item.get("tags", []),
        "current_sdd": item.get("current_sdd", ""),
        "bucket": item.get("_bucket", ""),
        "path": str(path.relative_to(root.parent)),
    }


def build_catalog(items: list[dict[str, Any]], root: Path) -> dict[str, Any]:
    return {
        "schema_version":
        2,
        "generated_at":
        now(),
        "root":
        str(root.relative_to(root.parent))
        if root.parent.exists() else str(root),
        "statuses":
        STATUSES,
        "project_buckets":
        PROJECT_BUCKETS,
        "projects":
        [project_catalog_item(item, root) for item in collect_projects(root)],
        "items": [catalog_item(item, root) for item in items],
    }


def write_catalog(root: Path, items: list[dict[str, Any]]) -> None:
    write_json(root / "catalog.json", build_catalog(items, root))


def build_index(items: list[dict[str, Any]], root: Path) -> str:
    counts = {status: 0 for status in STATUSES}
    for item in items:
        counts[item.get("status", item.get(
            "_state_dir", ""))] = counts.get(item.get("status", ""), 0) + 1
    projects = collect_projects(root)
    lines = ["# SDD Index", "", "## Summary", ""]
    for status in STATUSES:
        lines.append(f"- **{status}**: {counts.get(status, 0)}")
    lines.append(f"- **projects**: {len(projects)}")
    lines.extend(["", "## Projects", ""])
    if not projects:
        lines.append("无。")
    else:
        lines.extend([
            "| ID | Title | Status | Bucket | Current SDD | Path |",
            "| --- | --- | --- | --- | --- | --- |"
        ])
        for project in projects:
            lines.append(
                f"| {project.get('id', '')} | {project.get('title', '')} | {project.get('status', '')} | {project.get('_bucket', '')} | {project.get('current_sdd', '')} | {project['_path'].relative_to(root.parent)} |"
            )
    for status in STATUSES:
        lines.extend(["", f"## {status.title()}", ""])
        rows = [item for item in items if item.get("status") == status]
        if not rows:
            lines.append("无。")
            continue
        lines.extend([
            "| ID | Project | Title | Updated | Scope | Current Task |",
            "| --- | --- | --- | --- | --- | --- |"
        ])
        for item in rows:
            progress = item.get("progress", {})
            lines.append(
                f"| {item.get('id', '')} | {item.get('project_id', '')} | {item.get('title', '')} | {item.get('updated_at', '')} | {item.get('scope', '')} | {progress.get('current_task', '')} |"
            )
    by_scope: dict[str, list[dict[str, Any]]] = {}
    by_tag: dict[str, list[dict[str, Any]]] = {}
    for item in items:
        by_scope.setdefault(item.get("scope") or "未指定", []).append(item)
        for tag in item.get("tags", []):
            by_tag.setdefault(tag, []).append(item)
    lines.extend(["", "## By Scope", ""])
    if not by_scope:
        lines.append("无。")
    for scope, rows in sorted(by_scope.items()):
        lines.extend([f"### {scope}", ""])
        for item in rows:
            lines.append(f"- {item.get('id')} — {item.get('title')}")
        lines.append("")
    lines.extend(["## By Tag", ""])
    if not by_tag:
        lines.append("无。")
    for tag, rows in sorted(by_tag.items()):
        lines.extend([f"### {tag}", ""])
        for item in rows:
            lines.append(f"- {item.get('id')} — {item.get('title')}")
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def write_catalog_and_index(root: Path) -> list[dict[str, Any]]:
    items = collect_instances(root)
    write_catalog(root, items)
    write_text(root / "INDEX.md", build_index(items, root))
    return items
