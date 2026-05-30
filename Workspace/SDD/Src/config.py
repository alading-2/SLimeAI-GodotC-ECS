from __future__ import annotations

from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
CLI_PATH = REPO_ROOT / "Workspace" / "SDD" / "sdd.py"
DEFAULT_ROOT = REPO_ROOT / "SDD"
STATUSES = ["pending", "active", "blocked", "done"]
PROJECT_STATUSES = ["pending", "active", "blocked", "done"]
PROJECT_BUCKETS = ["projects", "archived"]
REQUIRED_FILES = [
    "README.md",
    "sdd.json",
    "tasks.md",
    "progress.md",
    "bdd.md",
    "notes.md",
    "design/INDEX.md",
]
TEMPLATE_MARKERS = [
    "一句话说明这个 SDD 要解决什么问题",
    "说明这个 SDD 要解决的问题",
    "列出必要上下文、现有事实源和约束",
    "描述最终设计、取舍和影响范围",
    "列出完成后必须执行的验证",
    "YYYY-MM-DD",
    "Example Title",
    "等待补充设计与任务",
]
WEAK_TEXT_VALUES = {
    "",
    "ok",
    "done",
    "passed",
    "pass",
    "none",
    "n/a",
    "na",
    "完成",
    "已完成",
    "无",
    "无需",
}


def resolve_root(value: str | None) -> Path:
    if not value:
        return DEFAULT_ROOT
    path = Path(value)
    if path.is_absolute():
        return path
    return REPO_ROOT / path
