from __future__ import annotations

import re
from pathlib import Path

from .errors import SDDCliError


def task_counts(tasks_path: Path) -> tuple[int, int, str]:
    if not tasks_path.exists():
        return 0, 0, "none"
    completed = 0
    total = 0
    current = "done"
    for line in tasks_path.read_text(encoding="utf-8").splitlines():
        match = re.match(r"^- \[([ xX])\] (T\d+\.\d+)", line)
        if not match:
            continue
        total += 1
        if match.group(1).lower() == "x":
            completed += 1
        elif current == "done":
            current = match.group(2)
    if total == 0:
        current = "none"
    return completed, total, current


def update_tasks_header(path: Path, metadata: dict[str, Any]) -> None:
    if not path.exists():
        return
    completed = metadata["progress"]["completed_tasks"]
    total = metadata["progress"]["total_tasks"]
    current = metadata["progress"]["current_task"]
    lines = path.read_text(encoding="utf-8").splitlines()
    new_lines: list[str] = []
    for line in lines:
        if line.startswith("- **Status**:"):
            new_lines.append(f"- **Status**: {metadata['status']}")
        elif line.startswith("- **Completed**:"):
            new_lines.append(f"- **Completed**: {completed}/{total}")
        elif line.startswith("- **Current**:"):
            new_lines.append(f"- **Current**: {current}")
        else:
            new_lines.append(line)
    path.write_text("\n".join(new_lines) + "\n", encoding="utf-8")


def next_task_id(tasks_path: Path) -> str:
    nums: list[tuple[int, int]] = []
    if tasks_path.exists():
        for chapter, item in re.findall(
                r"T(\d+)\.(\d+)", tasks_path.read_text(encoding="utf-8")):
            nums.append((int(chapter), int(item)))
    if not nums:
        return "T1.1"
    chapter, item = max(nums)
    return f"T{chapter}.{item + 1}"


def update_task_checkbox(tasks_path: Path, task_ref: str,
                         checked: bool) -> None:
    if not tasks_path.exists():
        raise SDDCliError(f"tasks.md 不存在: {tasks_path}")
    lines = tasks_path.read_text(encoding="utf-8").splitlines()
    found = False
    new_lines: list[str] = []
    marker = "x" if checked else " "
    for line in lines:
        if re.match(rf"^- \[[ xX]\] {re.escape(task_ref)}(\s|$)", line):
            new_lines.append(re.sub(r"^- \[[ xX]\]", f"- [{marker}]", line))
            found = True
        else:
            new_lines.append(line)
    if not found:
        raise SDDCliError(f"找不到任务: {task_ref}")
    tasks_path.write_text("\n".join(new_lines) + "\n", encoding="utf-8")
