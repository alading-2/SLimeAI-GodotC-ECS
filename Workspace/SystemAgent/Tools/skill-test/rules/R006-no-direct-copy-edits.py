"""R006 — no-direct-copy-edits (advisory)

检查三 IDE 副本目录的最近文件修改时间是否晚于
Workspace/SystemAgent/Registry/.last-sync 时间戳。
如果晚于，可能存在直接修改副本的行为。
"""
from __future__ import annotations
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from lint import Violation  # noqa: E402


def name() -> str:
    return "no-direct-copy-edits"


def check(
    skill_files: list[Path],
    manifest: dict[str, Any],
    catalog: dict[str, Any],
    root: Path,
) -> list[Violation]:
    violations: list[Violation] = []
    last_sync_file = root / "Workspace" / "SystemAgent" / "Config" / ".last-sync"
    if not last_sync_file.exists():
        # .last-sync 不存在时跳过（尚未跑过 sync）
        return violations

    try:
        from datetime import timedelta
        last_sync_str = last_sync_file.read_text().strip()
        last_sync_dt = datetime.fromisoformat(
            last_sync_str.replace("Z", "+00:00"))
        # 加 2 秒宽限：sync 写完副本再写 .last-sync 存在亚秒时差
        last_sync_dt = last_sync_dt + timedelta(seconds=2)
    except Exception:
        return violations

    copy_roots = [
        root / ".codex" / "skills",
        root / ".claude" / "skills",
        root / ".devin" / "skills",
        root / ".trae" / "skills",
    ]
    for cr in copy_roots:
        if not cr.exists():
            continue
        for skill_file in cr.rglob("SKILL.md"):
            mtime = datetime.fromtimestamp(skill_file.stat().st_mtime,
                                           tz=timezone.utc)
            if mtime > last_sync_dt:
                violations.append(
                    Violation(
                        str(skill_file.relative_to(root)),
                        "direct-copy-edit",
                        f"副本修改时间 {mtime.isoformat()} 晚于 .last-sync {last_sync_str}；请确认是否直接改副本（应改 .ai-config/ 源）",
                        "advisory",
                    ))
    return violations
