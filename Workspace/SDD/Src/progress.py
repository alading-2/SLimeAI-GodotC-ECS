from __future__ import annotations

import re
from pathlib import Path
from typing import Any

from .io import now, write_text
from .templates import build_progress, template_progress


def markdown_section(text: str, heading: str) -> str:
    match = re.search(rf"^## {re.escape(heading)}\n\n(.*?)(?=\n## |\Z)",
                      text,
                      flags=re.DOTALL | re.MULTILINE)
    return match.group(1).strip() if match else ""


def replace_markdown_section(text: str, heading: str, body: str) -> str:
    section = f"## {heading}\n\n{body.strip()}\n"
    if re.search(rf"^## {re.escape(heading)}\n\n", text, flags=re.MULTILINE):
        return re.sub(rf"^## {re.escape(heading)}\n\n.*?(?=\n## |\Z)",
                      section.rstrip(),
                      text,
                      count=1,
                      flags=re.DOTALL | re.MULTILINE)
    if text.startswith("# Progress"):
        return text.rstrip() + "\n\n" + section
    return "# Progress\n\n" + section


def parse_panel_fields(section: str) -> dict[str, str]:
    fields: dict[str, str] = {}
    for line in section.splitlines():
        item = re.match(r"- \*\*(.*?)\*\*: ?(.*)", line)
        if not item:
            continue
        key = item.group(1).strip().lower().replace(" ", "_")
        fields[key] = item.group(2).strip()
    return fields


def extract_state(progress_path: Path) -> dict[str, str]:
    result = {
        "status": "",
        "current": "",
        "next": "",
        "blocker": "",
    }
    if not progress_path.exists():
        return result
    text = progress_path.read_text(encoding="utf-8")
    state = parse_panel_fields(markdown_section(text, "State"))
    if state:
        result["status"] = state.get("status", "")
        result["current"] = state.get("current", state.get("current_task", ""))
        result["next"] = state.get("next", state.get("next_action", ""))
        result["blocker"] = state.get("blocker", state.get("open_blockers", ""))
        return result
    latest = parse_panel_fields(markdown_section(text, "Latest Resume"))
    result["status"] = latest.get("status", "")
    result["current"] = latest.get("current_task", "")
    result["next"] = latest.get("next_action", "")
    result["blocker"] = latest.get("open_blockers", "")
    return result


def extract_latest_decision(progress_path: Path) -> str:
    if not progress_path.exists():
        return ""
    text = progress_path.read_text(encoding="utf-8")
    decisions = markdown_section(text, "Decisions")
    candidates: list[str] = []
    for line in decisions.splitlines():
        if not line.startswith("- "):
            continue
        value = line[2:].strip()
        if value and value.lower() != "none":
            candidates.append(value)
    if not candidates:
        return ""
    latest = candidates[-1]
    return re.sub(r"^\d{4}-\d{2}-\d{2}(?: \d{2}:\d{2})?:\s*", "",
                  latest).strip()


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
    if any(result.values()):
        return result
    state = extract_state(progress_path)
    result["current_task"] = state.get("current", "")
    result["last_conclusion"] = extract_latest_decision(progress_path)
    result["next_action"] = state.get("next", "")
    result["open_blockers"] = state.get("blocker", "")
    return result


def replace_state(progress_path: Path, metadata: dict[str, Any],
                  next_action: str, blockers: str) -> None:
    if not progress_path.exists():
        write_text(progress_path,
                   build_progress(metadata["id"], metadata["title"], now()))
    text = progress_path.read_text(encoding="utf-8")
    progress = metadata.get("progress", {})
    body = f"""- **Status**: {metadata.get('status', '')}
- **Current**: {progress.get('current_task', 'none')}
- **Next**: {next_action}
- **Blocker**: {blockers}"""
    text = replace_markdown_section(text, "State", body)
    progress_path.write_text(text.rstrip() + "\n", encoding="utf-8")


def append_panel_entry(progress_path: Path, heading: str, text: str,
                       prefix: str | None = None) -> None:
    if not progress_path.exists():
        write_text(progress_path, template_progress())
    content = progress_path.read_text(encoding="utf-8")
    existing = markdown_section(content, heading)
    lines = [
        line for line in existing.splitlines()
        if line.strip() and line.strip().lower() not in {"- none", "- pending"}
    ]
    label = f"{prefix}: " if prefix else ""
    lines.append(f"- {now()}: {label}{text}")
    content = replace_markdown_section(content, heading, "\n".join(lines))
    progress_path.write_text(content.rstrip() + "\n", encoding="utf-8")


def append_decision(instance: Path, kind: str, text: str) -> None:
    prefix = None if kind == "decision" else kind
    append_panel_entry(instance / "progress.md", "Decisions", text, prefix)


def record_validation(instance: Path, summary: str) -> None:
    append_panel_entry(instance / "progress.md", "Validation", summary, None)
