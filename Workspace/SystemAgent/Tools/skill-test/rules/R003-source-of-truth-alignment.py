"""R003 — source-of-truth-alignment (critical)

SKILL.md 不得在正文中引用 .codex/.claude/.windsurf/skills/ 副本路径。
必须只引用 .ai-config/skills/ 源或其他实际路径。
"""
from __future__ import annotations
import re
import sys
from pathlib import Path
from typing import Any

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from lint import Violation  # noqa: E402

_COPY_PATH_RE = re.compile(
    r"`(\.codex/skills/|\.claude/skills/|\.windsurf/skills/)[^`\s]*\.(md|sh|yaml|yml|py|toml|json)`"
)


def name() -> str:
    return "source-of-truth-alignment"


def check(
    skill_files: list[Path],
    manifest: dict[str, Any],
    catalog: dict[str, Any],
    root: Path,
) -> list[Violation]:
    violations: list[Violation] = []
    for f in skill_files:
        try:
            text = f.read_text(encoding="utf-8")
        except Exception:
            continue
        for m in _COPY_PATH_RE.finditer(text):
            violations.append(
                Violation(
                    str(f),
                    "copy-path-reference",
                    f"引用了副本路径（应改为 .ai-config/skills/）: {m.group(0)!r}",
                    "critical",
                ))
    return violations
