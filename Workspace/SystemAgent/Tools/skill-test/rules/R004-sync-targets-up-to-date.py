"""R004 — sync-targets-up-to-date (advisory)

.ai-config/skills/<cat>/<name>/SKILL.md 与三 IDE 副本的 MD5 必须一致。
不一致时输出 advisory 提示跑 sync 脚本。
"""
from __future__ import annotations
import hashlib
import sys
from pathlib import Path
from typing import Any

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from lint import Violation  # noqa: E402


def name() -> str:
    return "sync-targets-up-to-date"


def _md5(p: Path) -> str:
    return hashlib.md5(p.read_bytes()).hexdigest()


def check(
    skill_files: list[Path],
    manifest: dict[str, Any],
    catalog: dict[str, Any],
    root: Path,
) -> list[Violation]:
    violations: list[Violation] = []
    copy_roots = [
        root / ".codex" / "skills",
        root / ".claude" / "skills",
        root / ".windsurf" / "skills",
    ]
    for f in skill_files:
        skill_name = f.parent.name
        src_md5 = _md5(f)
        for cr in copy_roots:
            copy = cr / skill_name / "SKILL.md"
            if not copy.exists():
                continue
            if _md5(copy) != src_md5:
                violations.append(
                    Violation(
                        str(f),
                        "sync-mismatch",
                        f"副本与源不一致: {copy.relative_to(root)}；请跑 sync-ai-config.sh",
                        "advisory",
                    )
                )
    return violations
