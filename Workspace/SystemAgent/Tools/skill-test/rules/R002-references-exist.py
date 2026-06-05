"""R002 — references-exist (critical)

SKILL.md 中所有路径引用（Foo/Bar.md、tools/script.sh 等）必须真实存在。
按 skill 文件所在目录和工作区根两处尝试解析。
"""
from __future__ import annotations
import re
import sys
from pathlib import Path
from typing import Any

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from lint import Violation  # noqa: E402

# 匹配 `Some/Path/File.ext` 风格的路径（backtick 包裹或裸引用）
_PATH_RE = re.compile(
    r"`([A-Za-z_.][A-Za-z0-9_.\-/]+\.(md|sh|yaml|yml|py|toml|json))`")

# 忽略明显的占位符或模板路径
_IGNORE_PATTERNS = re.compile(
    r"<|>|\*|\{|\}|example|<change>|<Game>|<Area>|<Layer>|<Scene>")


def name() -> str:
    return "references-exist"


def check(
    skill_files: list[Path],
    manifest: dict[str, Any],
    catalog: dict[str, Any],
    root: Path,
) -> list[Violation]:
    violations: list[Violation] = []
    # skill 文件可能引用这些仓库内的相对路径
    extra_roots = [
        root / "SlimeAI",
        root / "Games" / "BrotatoLike",
        root / "Games" / "BrotatoLike" / "SlimeAI",
    ]
    for f in skill_files:
        try:
            text = f.read_text(encoding="utf-8")
        except Exception:
            continue
        skill_dir = f.parent
        for m in _PATH_RE.finditer(text):
            ref = m.group(1)
            if _IGNORE_PATTERNS.search(ref):
                continue
            # 跳过无目录前缀的单文件名（如 README.md、tasks.md 等模板占位符）
            if "/" not in ref:
                continue
            # 只检查已知工作区根前缀的路径；其余视为输出 artifact 路径，不检查
            _KNOWN_PREFIXES = (
                "Workspace/",
                "SlimeAI/",
                "Games/",
                ".ai-config/",
                "openspec/",
                "Resources/",
                ".claude/",
                ".codex/",
                ".devin/",
                ".trae/",
            )
            if not any(ref.startswith(p) for p in _KNOWN_PREFIXES):
                continue
            # 依次尝试多个基路径
            bases = [skill_dir, root] + extra_roots
            if any((base / ref).exists() for base in bases):
                continue
            violations.append(
                Violation(str(f), "missing-reference", f"引用不存在: {ref}",
                          "critical"))
    return violations
