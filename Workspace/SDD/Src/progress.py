from __future__ import annotations

import re
from pathlib import Path

from .io import now, write_text
from .templates import build_progress, template_progress


def extract_latest_resume(progress_path: Path) -> dict[str, str]:
    result = {
        "updated": "",
        "current_task": "",
        "last_conclusion": "",
        "next_action": "",
        "open_blockers": "",
    }
    if not progress_path.exists():
        return result
    text = progress_path.read_text(encoding="utf-8")
    match = re.search(r"## Latest Resume\n\n(.*?)(\n## |\Z)", text, re.DOTALL)
    if not match:
        return result
    for line in match.group(1).splitlines():
        item = re.match(r"- \*\*(.*?)\*\*: ?(.*)", line)
        if not item:
            continue
        key = item.group(1).strip().lower().replace(" ", "_")
        value = item.group(2).strip()
        if key == "current_task":
            result["current_task"] = value
        elif key == "last_conclusion":
            result["last_conclusion"] = value
        elif key == "next_action":
            result["next_action"] = value
        elif key == "open_blockers":
            result["open_blockers"] = value
        elif key == "updated":
            result["updated"] = value
    return result


def replace_latest_resume(progress_path: Path, metadata: dict[str, Any],
                          conclusion: str, next_action: str,
                          blockers: str) -> None:
    if not progress_path.exists():
        write_text(progress_path,
                   build_progress(metadata["id"], metadata["title"], now()))
    text = progress_path.read_text(encoding="utf-8")
    latest = f"""## Latest Resume

- **Updated**: {now()}
- **Current Task**: {metadata.get('progress', {}).get('current_task', 'none')}
- **Last Conclusion**: {conclusion}
- **Next Action**: {next_action}
- **Open Blockers**: {blockers}
"""
    if "## Latest Resume" in text:
        text = re.sub(r"## Latest Resume\n\n.*?(?=\n## |\Z)",
                      latest.rstrip(),
                      text,
                      flags=re.DOTALL)
    else:
        text = text + "\n" + latest
    progress_path.write_text(text.rstrip() + "\n", encoding="utf-8")


def next_progress_id(progress_path: Path) -> str:
    if not progress_path.exists():
        return "P001"
    nums = [
        int(x) for x in re.findall(r"### P(\d{3})",
                                   progress_path.read_text(encoding="utf-8"))
    ]
    return f"P{(max(nums) + 1) if nums else 1:03d}"


def append_progress(instance: Path, kind: str, context: str, conclusion: str,
                    evidence: str, impact: str, resume: str) -> None:
    progress_path = instance / "progress.md"
    if not progress_path.exists():
        write_text(progress_path, template_progress())
    pid = next_progress_id(progress_path)
    entry = f"""
### {pid} — {now()} — {kind}

- **Context**: {context}
- **Conclusion**: {conclusion}
- **Evidence**: {evidence}
- **Impact**: {impact}
- **Resume**: {resume}
"""
    with progress_path.open("a", encoding="utf-8") as handle:
        handle.write(entry)
