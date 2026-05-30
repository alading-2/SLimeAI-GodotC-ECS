from __future__ import annotations

import re
from pathlib import Path
from typing import Any

from .config import PROJECT_BUCKETS, STATUSES
from .errors import SDDCliError
from .io import read_json


def load_instance(path: Path) -> dict[str, Any] | None:
    metadata_path = path / "sdd.json"
    if not metadata_path.exists():
        return None
    try:
        metadata = read_json(metadata_path)
    except Exception:
        return None
    metadata["_path"] = path
    metadata["_state_dir"] = path.parent.name
    return metadata


def load_project(path: Path) -> dict[str, Any] | None:
    metadata_path = path / "project.json"
    if not metadata_path.exists():
        return None
    try:
        metadata = read_json(metadata_path)
    except Exception:
        return None
    metadata["_path"] = path
    metadata["_bucket"] = path.parent.name
    return metadata


def collect_instances(root: Path,
                      include_invalid: bool = False) -> list[dict[str, Any]]:
    items: list[dict[str, Any]] = []
    for state in STATUSES:
        state_dir = root / state
        if not state_dir.exists():
            continue
        for child in sorted(state_dir.iterdir()):
            if not child.is_dir():
                continue
            metadata = load_instance(child)
            if metadata is not None:
                items.append(metadata)
            elif include_invalid:
                items.append({
                    "id": child.name,
                    "title": child.name,
                    "status": state,
                    "_path": child,
                    "_state_dir": state
                })
    for project in collect_projects(root):
        sdds_dir = project["_path"] / "sdds"
        if not sdds_dir.exists():
            continue
        for child in sorted(sdds_dir.iterdir()):
            if not child.is_dir():
                continue
            metadata = load_instance(child)
            if metadata is not None:
                metadata.setdefault("project_id", project.get("id"))
                metadata["_project_path"] = project["_path"]
                items.append(metadata)
            elif include_invalid:
                items.append({
                    "id": child.name,
                    "title": child.name,
                    "status": "pending",
                    "project_id": project.get("id"),
                    "_path": child,
                    "_state_dir": "project"
                })
    return items


def collect_projects(root: Path,
                     include_invalid: bool = False) -> list[dict[str, Any]]:
    items: list[dict[str, Any]] = []
    for bucket in PROJECT_BUCKETS:
        bucket_dir = root / "project" / bucket
        if not bucket_dir.exists():
            continue
        for child in sorted(bucket_dir.iterdir()):
            if not child.is_dir():
                continue
            metadata = load_project(child)
            if metadata is not None:
                items.append(metadata)
            elif include_invalid:
                items.append({
                    "id": child.name,
                    "title": child.name,
                    "status": "pending",
                    "_path": child,
                    "_bucket": bucket
                })
    return items


def locate_instance(root: Path, ident: str) -> dict[str, Any]:
    matches: list[dict[str, Any]] = []
    for item in collect_instances(root, include_invalid=True):
        path = item["_path"]
        if item.get(
                "id") == ident or path.name == ident or path.name.startswith(
                    f"{ident}-") or re.match(
                        rf"^\d{{3}}-{re.escape(ident)}(?:-|$)", path.name):
            matches.append(item)
    if not matches:
        raise SDDCliError(f"找不到 SDD: {ident}")
    if len(matches) > 1:
        paths = ", ".join(
            str(m["_path"].relative_to(root.parent)) for m in matches)
        raise SDDCliError(f"SDD 标识不唯一: {ident} -> {paths}")
    return matches[0]


def locate_project(root: Path, ident: str) -> dict[str, Any]:
    matches: list[dict[str, Any]] = []
    for item in collect_projects(root, include_invalid=True):
        path = item["_path"]
        if item.get(
                "id") == ident or path.name == ident or path.name.startswith(
                    f"{ident}-"):
            matches.append(item)
    if not matches:
        raise SDDCliError(f"找不到项目: {ident}")
    if len(matches) > 1:
        paths = ", ".join(
            str(m["_path"].relative_to(root.parent)) for m in matches)
        raise SDDCliError(f"项目标识不唯一: {ident} -> {paths}")
    return matches[0]


def next_id(root: Path) -> str:
    max_num = 0
    for item in collect_instances(root, include_invalid=True):
        text = str(item.get("id", item["_path"].name))
        match = re.search(r"SDD-(\d{4})", text)
        if match:
            max_num = max(max_num, int(match.group(1)))
    return f"SDD-{max_num + 1:04d}"


def next_project_id(root: Path) -> str:
    max_num = 0
    for item in collect_projects(root, include_invalid=True):
        text = str(item.get("id", item["_path"].name))
        match = re.search(r"PRJ-(\d{4})", text)
        if match:
            max_num = max(max_num, int(match.group(1)))
    return f"PRJ-{max_num + 1:04d}"


def instance_dir_name(sdd_id: str, slug: str) -> str:
    return f"{sdd_id}-{slug}"


def project_child_dir_name(order: int, sdd_id: str, slug: str) -> str:
    return f"{order:03d}-{sdd_id}-{slug}"


def next_project_order(project_path: Path) -> int:
    sdds_dir = project_path / "sdds"
    max_order = 0
    if sdds_dir.exists():
        for child in sdds_dir.iterdir():
            if not child.is_dir():
                continue
            match = re.match(r"(\d{3})-SDD-\d{4}", child.name)
            if match:
                max_order = max(max_order, int(match.group(1)))
    return max_order + 1
