"""R001 — frontmatter-required-fields (critical)

每个 SKILL.md 顶部 YAML frontmatter 必须包含 name 和 description 字段。
"""
from __future__ import annotations
import re
import sys
from pathlib import Path
from typing import Any

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from lint import Violation  # noqa: E402


def name() -> str:
    return "frontmatter-required-fields"


def check(
    skill_files: list[Path],
    manifest: dict[str, Any],
    catalog: dict[str, Any],
    root: Path,
) -> list[Violation]:
    required = {"name", "description"}
    violations: list[Violation] = []
    for f in skill_files:
        try:
            text = f.read_text(encoding="utf-8")
        except Exception as e:
            violations.append(Violation(str(f), "read-error", str(e), "critical"))
            continue
        m = re.match(r"^---\s*\n(.*?)\n---", text, re.DOTALL)
        if not m:
            violations.append(
                Violation(str(f), "missing-frontmatter", "frontmatter 块缺失", "critical")
            )
            continue
        fm: dict[str, str] = {}
        for line in m.group(1).splitlines():
            kv = re.match(r"^(\w[\w-]*):\s*(.*)", line)
            if kv:
                fm[kv.group(1)] = kv.group(2).strip()
        missing = sorted(required - set(fm.keys()))
        if missing:
            violations.append(
                Violation(str(f), "missing-fields", f"缺少: {missing}", "critical")
            )
    return violations
