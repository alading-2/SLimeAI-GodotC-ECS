from __future__ import annotations

import re
from pathlib import Path
from typing import Any

from .config import PROJECT_STATUSES, REQUIRED_FILES, STATUSES, TEMPLATE_MARKERS, WEAK_TEXT_VALUES
from .io import read_json
from .progress import extract_latest_resume
from .tasks import task_counts


def markdown_section(text: str, heading: str) -> str:
    match = re.search(rf"^## {re.escape(heading)}\n\n(.*?)(?=\n## |\Z)",
                      text,
                      flags=re.DOTALL | re.MULTILINE)
    return match.group(1).strip() if match else ""


def normalize_quality_text(text: str) -> str:
    return re.sub(r"[\s`*_。．.，,；;：:！!？?\-—/\\]+", "", text.strip().lower())


def is_weak_text(text: str) -> bool:
    normalized = normalize_quality_text(text)
    if normalized in WEAK_TEXT_VALUES:
        return True
    if len(normalized) < 4:
        return True
    weak_patterns = [
        r"^sdd已进入(done|active|pending|blocked)$",
        r"^已完成任务t\d+\d+$",
        r"^任务完成$",
        r"^继续处理下一个未完成任务$",
        r"^按tasksmd的current继续$",
    ]
    return any(re.match(pattern, normalized) for pattern in weak_patterns)


def read_existing_text(path: Path) -> str:
    return path.read_text(encoding="utf-8") if path.exists() else ""


def strip_fenced_code_blocks(text: str) -> str:
    return re.sub(r"```.*?```", "", text, flags=re.DOTALL)


def has_template_markers(path: Path) -> bool:
    texts: list[str] = []
    for rel in ["README.md", "tasks.md", "progress.md", "bdd.md", "notes.md"]:
        texts.append(strip_fenced_code_blocks(read_existing_text(path / rel)))
    design_dir = path / "design"
    if design_dir.exists():
        for file_path in sorted(design_dir.glob("*.md")):
            texts.append(
                strip_fenced_code_blocks(read_existing_text(file_path)))
    combined = "\n".join(texts)
    return any(marker in combined for marker in TEMPLATE_MARKERS)


def readme_summary_is_weak(text: str, title: str) -> bool:
    summary = markdown_section(text, "What This SDD Is About")
    if not summary:
        return True
    if normalize_quality_text(summary) == normalize_quality_text(title):
        return True
    if any(marker in summary for marker in TEMPLATE_MARKERS):
        return True
    return is_weak_text(summary)


def latest_resume_is_weak(progress_path: Path) -> bool:
    latest = extract_latest_resume(progress_path)
    conclusion = latest.get("last_conclusion", "")
    next_action = latest.get("next_action", "")
    return is_weak_text(conclusion) or is_weak_text(next_action)


def validation_summaries(metadata: dict[str, Any],
                         progress_text: str) -> list[str]:
    summaries = [
        str(item.get("summary", ""))
        for item in metadata.get("validation", []) if isinstance(item, dict)
    ]
    for match in re.finditer(
            r"^### P\d{3} .*? — validation\n\n(.*?)(?=\n### P\d{3} |\Z)",
            progress_text,
            flags=re.DOTALL | re.MULTILINE):
        evidence_match = re.search(r"^- \*\*Evidence\*\*: ?(.*)",
                                   match.group(1),
                                   flags=re.MULTILINE)
        evidence = evidence_match.group(1).strip() if evidence_match else ""
        if evidence:
            summaries.append(evidence)
    return summaries


def validation_summary_is_weak(summary: str) -> bool:
    if is_weak_text(summary):
        return True
    command_like = re.search(
        r"\b(python3?|bash|dotnet|godot|git|validate|test|tests|lint|build)\b",
        summary,
        flags=re.IGNORECASE)
    result_like = re.search(
        r"(0 error|0 warning|passed|pass|success|succeeded|failed|通过|失败|warning|error|tests? passed)",
        summary,
        flags=re.IGNORECASE)
    return not (command_like and result_like)


def has_trace_entry(text: str, summaries: list[str]) -> bool:
    combined = "\n".join([text, *summaries])
    if re.search(r"\bcommit\s+`?[0-9a-f]{7,40}`?",
                 combined,
                 flags=re.IGNORECASE):
        return True
    if "artifact" in combined.lower() or "artifacts/" in combined:
        return True
    if "Key Areas" in combined or "Key Files" in combined:
        return True
    return any(not validation_summary_is_weak(summary)
               for summary in summaries)


def progress_entry_count(progress_text: str) -> int:
    return len(re.findall(r"^### P\d{3}\b", progress_text, flags=re.MULTILINE))


def has_excessive_key_files(progress_text: str) -> bool:
    for match in re.finditer(r"- \*\*Key Files\*\*:\n((?:  - .+\n?)+)",
                             progress_text):
        lines = [
            line for line in match.group(1).splitlines()
            if line.startswith("  - ")
        ]
        if len(lines) > 8:
            return True
    return False


def artifacts_are_unreferenced(path: Path, progress_text: str,
                               notes_text: str) -> bool:
    artifacts_dir = path / "artifacts"
    if not artifacts_dir.exists():
        return False
    files = [item for item in artifacts_dir.iterdir() if item.is_file()]
    if len(files) <= 1:
        return False
    combined = progress_text + "\n" + notes_text
    return not any(item.name in combined for item in files)


def design_is_thin(path: Path) -> bool:
    """检查 design/ 是否过于单薄：只有 INDEX.md + main.md 且 main.md 行数过少。"""
    design_dir = path / "design"
    if not design_dir.exists():
        return True
    md_files = [f for f in design_dir.glob("*.md") if f.name != "INDEX.md"]
    if len(md_files) == 0:
        return True
    if len(md_files) == 1 and md_files[0].name == "main.md":
        lines = md_files[0].read_text(encoding="utf-8").splitlines()
        non_blank = [line for line in lines if line.strip()]
        if len(non_blank) < 20:
            return True
    return False


def design_has_external_refs(path: Path) -> bool:
    """检查 main.md 是否引用了外部路径但对应文件未在 design/ 下存在。"""
    main_path = path / "design" / "main.md"
    if not main_path.exists():
        return False
    text = main_path.read_text(encoding="utf-8")
    external_patterns = [
        r"Workspace/DocsAI/",
        r"Workspace/SystemAgent/",
        r"Resources/",
        r"\.ai-config/",
    ]
    has_ref = any(re.search(pattern, text) for pattern in external_patterns)
    if not has_ref:
        return False
    design_dir = path / "design"
    md_files = [f for f in design_dir.glob("*.md") if f.name != "INDEX.md"]
    if len(md_files) <= 1:
        return True
    return False


def notes_are_too_long_without_index(notes_text: str) -> bool:
    lines = notes_text.splitlines()
    subheadings = [
        line for line in lines
        if line.startswith("## ") or line.startswith("### ")
    ]
    return len(lines) > 120 and not subheadings


def validate_instance(root: Path,
                      item: dict[str, Any]) -> tuple[list[str], list[str]]:
    errors: list[str] = []
    warnings: list[str] = []
    path = item["_path"]
    for rel in REQUIRED_FILES:
        if not (path / rel).exists():
            errors.append(f"ERROR SDD001 missing-required-file: {path / rel}")
    metadata_path = path / "sdd.json"
    try:
        metadata = read_json(metadata_path)
    except Exception as exc:
        errors.append(f"ERROR SDD002 invalid-json: {metadata_path}: {exc}")
        return errors, warnings
    directory_name = path.name
    metadata_id = metadata.get("id", "")
    if not (directory_name.startswith(metadata_id) or re.match(
            rf"^\d{{3}}-{re.escape(metadata_id)}(?:-|$)", directory_name)):
        errors.append(f"ERROR SDD003 id-directory-mismatch: {metadata_path}")
    status = metadata.get("status")
    if status not in STATUSES:
        errors.append(
            f"ERROR SDD004 invalid-status: {metadata_path}: {status}")
    main_design = metadata.get("links", {}).get("main_design")
    if main_design and not (path / main_design).exists():
        errors.append(
            f"ERROR SDD001 missing-main-design: {path / main_design}")
    readme = path / "README.md"
    readme_text = ""
    if readme.exists():
        readme_text = readme.read_text(encoding="utf-8")
        for label, expected in [
            ("Status", metadata.get("status", "")),
            ("Updated", metadata.get("updated_at", "")),
            ("Scope", metadata.get("scope", "")),
        ]:
            if f"**{label}**: {expected}" not in readme_text:
                warnings.append(
                    f"WARN SDD005 readme-field-mismatch: {readme}: {label}")
        if len(readme_text.splitlines()) > 100:
            warnings.append(f"WARN SDD020 readme-too-long: {readme}")
        if readme_summary_is_weak(readme_text, metadata.get("title", "")):
            warnings.append(f"WARN SDD016 weak-readme-summary: {readme}")
    completed, total, current = task_counts(path / "tasks.md")
    progress = metadata.get("progress", {})
    if total == 0:
        warnings.append(f"WARN SDD006 no-task-checkbox: {path / 'tasks.md'}")
    if progress.get("completed_tasks") != completed or progress.get(
            "total_tasks") != total:
        warnings.append(
            f"WARN SDD006 task-progress-mismatch: {path / 'tasks.md'}")
    design_index = path / "design" / "INDEX.md"
    if design_index.exists():
        text = design_index.read_text(encoding="utf-8")
        if "main" not in text.lower() and "current" not in text.lower():
            warnings.append(
                f"WARN SDD007 missing-main-design-marker: {design_index}")
    progress_path = path / "progress.md"
    progress_text = read_existing_text(progress_path)
    notes_text = read_existing_text(path / "notes.md")
    if progress_path.exists() and "## Latest Resume" not in progress_text:
        warnings.append(f"WARN SDD008 missing-latest-resume: {progress_path}")
    elif progress_path.exists() and latest_resume_is_weak(progress_path):
        warnings.append(f"WARN SDD017 weak-latest-resume: {progress_path}")
    if progress_entry_count(progress_text) > 5 and latest_resume_is_weak(
            progress_path):
        warnings.append(
            f"WARN SDD021 verbose-progress-weak-resume: {progress_path}")
    if artifacts_are_unreferenced(path, progress_text, notes_text):
        warnings.append(
            f"WARN SDD022 unreferenced-artifacts: {path / 'artifacts'}")
    if has_excessive_key_files(progress_text):
        warnings.append(f"WARN SDD023 excessive-key-files: {progress_path}")
    if notes_are_too_long_without_index(notes_text):
        warnings.append(
            f"WARN SDD024 long-notes-without-index: {path / 'notes.md'}")
    if design_is_thin(path):
        if status == "done":
            warnings.append(
                f"WARN SDD025 thin-design-in-done: {path / 'design'}")
        else:
            warnings.append(f"WARN SDD025 thin-design: {path / 'design'}")
    if design_has_external_refs(path):
        warnings.append(
            f"WARN SDD025 design-refs-external: {path / 'design' / 'main.md'}")
    has_templates = has_template_markers(path)
    if has_templates:
        if status == "done":
            errors.append(f"ERROR SDD015 template-residue-in-done: {path}")
        else:
            warnings.append(f"WARN SDD015 template-residue: {path}")
    bdd = metadata.get("bdd", {})
    bdd_path = path / "bdd.md"
    if bdd_path.exists():
        text = bdd_path.read_text(encoding="utf-8")
        if bdd.get("required") is True and "Scenario:" not in text:
            errors.append(f"ERROR SDD011 missing-bdd-scenario: {bdd_path}")
        if bdd.get("required") is False and "Reason" not in text:
            warnings.append(f"WARN SDD011 missing-bdd-reason: {bdd_path}")
    if status == "blocked":
        if "blocker" not in progress_text.lower(
        ) and "阻塞" not in progress_text:
            errors.append(
                f"ERROR SDD012 missing-blocker-record: {progress_path}")
    if status == "done":
        if total and completed != total:
            errors.append(
                f"ERROR SDD014 done-has-open-tasks: {path / 'tasks.md'}: {completed}/{total}"
            )
        if "validation" not in progress_text.lower(
        ) and "验证" not in progress_text:
            errors.append(
                f"ERROR SDD013 missing-validation-record: {progress_path}")
        summaries = validation_summaries(metadata, progress_text)
        if any(validation_summary_is_weak(summary) for summary in summaries):
            warnings.append(
                f"WARN SDD018 weak-validation-summary: {progress_path}")
        if summaries and not has_trace_entry(progress_text, summaries):
            warnings.append(
                f"WARN SDD019 missing-trace-entry: {progress_path}")
    return errors, warnings


def validate_project(root: Path,
                     item: dict[str, Any]) -> tuple[list[str], list[str]]:
    errors: list[str] = []
    warnings: list[str] = []
    path = item["_path"]
    # 基础文件（不依赖 project.json）
    static_required = [
        "README.md",
        "project.json",
    ]
    for rel in static_required:
        if not (path / rel).exists():
            errors.append(f"ERROR SDD026 missing-project-file: {path / rel}")
    # 读取 project.json，从 links 获取动态路径
    metadata_path = path / "project.json"
    try:
        metadata = read_json(metadata_path)
    except Exception as exc:
        errors.append(
            f"ERROR SDD026 invalid-project-json: {metadata_path}: {exc}")
        return errors, warnings
    links = metadata.get("links", {})
    dynamic_required = {
        "design_index": "design/INDEX.md",
        "roadmap": "Core/roadmap.md",
        "progress": "Core/progress.md",
        "notes": "Core/notes.md",
    }
    for key, default_rel in dynamic_required.items():
        rel = links.get(key, default_rel)
        if not (path / rel).exists():
            errors.append(f"ERROR SDD026 missing-project-file: {path / rel}")
    project_id = metadata.get("id", "")
    if not path.name.startswith(project_id):
        errors.append(
            f"ERROR SDD026 project-id-directory-mismatch: {metadata_path}")
    status = metadata.get("status")
    if status not in PROJECT_STATUSES:
        errors.append(
            f"ERROR SDD026 invalid-project-status: {metadata_path}: {status}")
    bucket = item.get("_bucket", "")
    if bucket == "archived" and status not in {"done"}:
        warnings.append(
            f"WARN SDD026 archived-project-status: {metadata_path}: {status}")
    if bucket == "projects" and status == "done":
        warnings.append(
            f"WARN SDD026 done-project-not-archived: {metadata_path}")
    return errors, warnings


def validate_global(
        root: Path, items: list[dict[str,
                                     Any]]) -> tuple[list[str], list[str]]:
    errors: list[str] = []
    warnings: list[str] = []
    seen: dict[str, Path] = {}
    for item in items:
        sdd_id = item.get("id", "")
        path = item["_path"]
        if sdd_id in seen:
            errors.append(
                f"ERROR SDD009 duplicate-id: {sdd_id}: {seen[sdd_id]} and {path}"
            )
        seen[sdd_id] = path
    catalog_path = root / "catalog.json"
    if catalog_path.exists():
        try:
            catalog = read_json(catalog_path)
            catalog_paths = {
                entry.get("path")
                for entry in catalog.get("items", [])
            }
            actual_paths = {
                str(item["_path"].relative_to(root.parent))
                for item in items
            }
            missing = actual_paths - catalog_paths
            stale = catalog_paths - actual_paths
            for item_path in sorted(missing):
                warnings.append(
                    f"WARN SDD009 catalog-missing-instance: {item_path}")
            for item_path in sorted(stale):
                errors.append(
                    f"ERROR SDD009 catalog-stale-instance: {item_path}")
        except Exception as exc:
            errors.append(
                f"ERROR SDD009 invalid-catalog: {catalog_path}: {exc}")
    active_count = sum(1 for item in items if item.get("status") == "active")
    if active_count > 5:
        warnings.append(f"WARN SDD010 too-many-active: {active_count}")
    return errors, warnings
