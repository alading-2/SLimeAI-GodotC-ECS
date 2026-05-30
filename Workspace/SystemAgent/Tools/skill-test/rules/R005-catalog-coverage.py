"""R005 — catalog-coverage (advisory)

skills.yaml 登记的 skill ID 集合必须等于
.ai-config/skills/ 实际 SKILL.md 目录名集合。
"""
from __future__ import annotations
import sys
from pathlib import Path
from typing import Any

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from lint import Violation  # noqa: E402


def name() -> str:
    return "catalog-coverage"


def check(
    skill_files: list[Path],
    manifest: dict[str, Any],
    catalog: dict[str, Any],
    root: Path,
) -> list[Violation]:
    violations: list[Violation] = []
    if not catalog:
        violations.append(
            Violation(
                "Workspace/SystemAgent/Registry/skills.yaml",
                "catalog-missing",
                "catalog 文件不存在或为空",
                "advisory",
            ))
        return violations

    catalog_ids = {s["id"] for s in catalog.get("skills", [])}
    actual_ids = {
        f.parent.name
        for f in (root / ".ai-config" / "skills").rglob("SKILL.md")
    }

    missing_from_catalog = sorted(actual_ids - catalog_ids)
    extra_in_catalog = sorted(catalog_ids - actual_ids)

    for sid in missing_from_catalog:
        violations.append(
            Violation(
                "Workspace/SystemAgent/Registry/skills.yaml",
                "missing-in-catalog",
                f"实际 skill 未在 catalog 中登记: {sid}",
                "advisory",
            ))
    for sid in extra_in_catalog:
        violations.append(
            Violation(
                "Workspace/SystemAgent/Registry/skills.yaml",
                "extra-in-catalog",
                f"catalog 登记了不存在的 skill: {sid}",
                "advisory",
            ))
    return violations
