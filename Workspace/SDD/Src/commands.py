from __future__ import annotations

import argparse
import json
import re
import shutil
from pathlib import Path

from .catalog import catalog_item, project_catalog_item, write_catalog_and_index
from .config import CLI_PATH, PROJECT_BUCKETS, PROJECT_STATUSES, STATUSES, resolve_root
from .errors import SDDCliError
from .instance_ops import create_instance, refresh_progress_metadata, save_instance
from .io import now, read_json, today, write_json, write_text
from .progress import append_decision, extract_latest_resume, extract_state, record_validation
from .project_ops import create_project
from .repository import collect_instances, collect_projects, locate_instance, locate_project
from .root_ops import ensure_root
from .tasks import next_task_id, task_counts, update_task_checkbox
from .templates import build_project_readme, template_design_index
from .validation import validate_global, validate_instance, validate_project


def command_init_root(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    ensure_root(root)
    print(f"SDD root ready: {root}")
    return 0


def command_new(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    item = create_instance(root, args.title, args)
    print(f"Created {item['id']}: {item['_path'].relative_to(root.parent)}")
    return 0


def command_project_new(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    item = create_project(root, args.title, args)
    print(f"Created {item['id']}: {item['_path'].relative_to(root.parent)}")
    return 0


def command_project_list(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    if not root.exists():
        print(f"SDD root not found: {root}")
        return 0
    items = collect_projects(root)
    if args.state:
        items = [item for item in items if item.get("status") == args.state]
    if args.bucket:
        items = [item for item in items if item.get("_bucket") == args.bucket]
    if args.json:
        print(
            json.dumps([project_catalog_item(item, root) for item in items],
                       ensure_ascii=False,
                       indent=2))
        return 0
    print(
        "ID        State    Bucket     Updated     Scope                  Title"
    )
    for item in items:
        print(
            f"{item.get('id', ''):<9} {item.get('status', ''):<8} {item.get('_bucket', ''):<10} {item.get('updated_at', ''):<11} {item.get('scope', ''):<22} {item.get('title', '')}"
        )
    return 0


def command_project_show(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    item = locate_project(root, args.ident)
    if args.json:
        print(
            json.dumps(project_catalog_item(item, root),
                       ensure_ascii=False,
                       indent=2))
        return 0
    readme = item["_path"] / "README.md"
    if readme.exists():
        print(readme.read_text(encoding="utf-8").rstrip())
    else:
        print(f"{item.get('id', '')} {item.get('title', '')}")
    return 0


def command_project_archive(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    item = locate_project(root, args.ident)
    if item.get("_bucket") == "archived":
        print(
            f"Project already archived: {item['_path'].relative_to(root.parent)}"
        )
        return 0
    current = item["_path"]
    target = root / "project" / "archived" / current.name
    if target.exists():
        raise SDDCliError(f"目标路径已存在: {target}")
    metadata = read_json(current / "project.json")
    metadata["status"] = args.status
    metadata["updated_at"] = today()
    write_json(current / "project.json", metadata)
    write_text(current / "README.md", build_project_readme(metadata))
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.move(str(current), str(target))
    write_catalog_and_index(root)
    print(f"Archived {metadata['id']}: {target.relative_to(root.parent)}")
    return 0


def command_list(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    if not root.exists():
        print(f"SDD root not found: {root}")
        return 0
    items = collect_instances(root)
    if args.state:
        items = [item for item in items if item.get("status") == args.state]
    if args.scope:
        items = [item for item in items if item.get("scope") == args.scope]
    if args.tag:
        items = [item for item in items if args.tag in item.get("tags", [])]
    if args.json:
        print(
            json.dumps([catalog_item(item, root) for item in items],
                       ensure_ascii=False,
                       indent=2))
        return 0
    print(
        "ID        State    Project   Updated     Scope                  Title"
    )
    for item in items:
        print(
            f"{item.get('id', ''):<9} {item.get('status', ''):<8} {item.get('project_id', ''):<9} {item.get('updated_at', ''):<11} {item.get('scope', ''):<22} {item.get('title', '')}"
        )
    return 0


def command_show(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    item = locate_instance(root, args.ident)
    path = item["_path"]
    if args.json:
        latest = extract_latest_resume(path / "progress.md")
        data = catalog_item(item, root)
        data["latest_resume"] = latest
        print(json.dumps(data, ensure_ascii=False, indent=2))
        return 0
    readme = path / "README.md"
    progress = path / "progress.md"
    if readme.exists():
        print(readme.read_text(encoding="utf-8").rstrip())
    state = extract_state(progress)
    print("\n## State")
    for key, label in [
        ("status", "Status"),
        ("current", "Current"),
        ("next", "Next"),
        ("blocker", "Blocker"),
    ]:
        if state.get(key):
            print(f"- **{label}**: {state[key]}")
    return 0


def command_index(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    ensure_root(root)
    items = write_catalog_and_index(root)
    print(
        f"Indexed {len(items)} SDD(s): {root / 'INDEX.md'}, {root / 'catalog.json'}"
    )
    return 0


def command_validate(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    if not root.exists():
        print(f"ERROR SDD000 root-missing: {root}")
        return 1
    if args.ident and args.all:
        print("ERROR SDD000 invalid-arguments: validate 不能同时指定 id 和 --all")
        return 1
    if args.ident:
        items = [locate_instance(root, args.ident)]
    else:
        items = collect_instances(root)
    all_errors: list[str] = []
    all_warnings: list[str] = []
    for item in items:
        errors, warnings = validate_instance(root, item)
        all_errors.extend(errors)
        all_warnings.extend(warnings)
    for item in collect_projects(root):
        errors, warnings = validate_project(root, item)
        all_errors.extend(errors)
        all_warnings.extend(warnings)
    errors, warnings = validate_global(root, collect_instances(root))
    all_errors.extend(errors)
    all_warnings.extend(warnings)
    if args.json:
        print(
            json.dumps({
                "errors": all_errors,
                "warnings": all_warnings
            },
                       ensure_ascii=False,
                       indent=2))
    else:
        target = args.ident or "--all"
        print(f"SDD validate: {target}")
        for line in all_errors + all_warnings:
            print(line)
        print(
            f"Checks: {len(all_errors)} error(s), {len(all_warnings)} warning(s)"
        )
    return 1 if all_errors else 0


def command_doctor(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    checks: list[str] = []
    checks.append(f"PASS cli: {CLI_PATH}")
    if root.exists():
        checks.append(f"PASS root: {root}")
    else:
        checks.append(f"WARN root-missing: {root}")
    for state in STATUSES:
        if (root / state).exists():
            checks.append(f"PASS state-dir: {state}")
        else:
            checks.append(f"WARN state-dir-missing: {state}")
    for rel in [
            "README.md", "INDEX.md", "catalog.json", "templates/README.md",
            "templates/sdd.json", "project/projects", "project/archived"
    ]:
        if (root / rel).exists():
            checks.append(f"PASS file: {rel}")
        else:
            checks.append(f"WARN file-missing: {rel}")
    print("\n".join(checks))
    return 0


def command_start(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    item = locate_instance(root, args.ident)
    if item.get("status") == "done":
        raise SDDCliError("done 状态默认不允许重新 start，请创建新的 SDD 引用原任务。")
    new_path = item["_path"]
    metadata = read_json(new_path / "sdd.json")
    metadata["status"] = "active"
    metadata.pop("blockers", None)
    refresh_progress_metadata(new_path, metadata)
    current = metadata.get("progress", {}).get("current_task", "none")
    next_action = (f"继续执行 {current}。"
                   if current not in {"done", "none"} else
                   "检查 tasks.md，确认是否需要记录最终验证。")
    save_instance(new_path, metadata, "SDD 已进入 active。", next_action)
    write_catalog_and_index(root)
    print(f"Started {metadata['id']}: {new_path.relative_to(root.parent)}")
    return 0


def command_block(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    item = locate_instance(root, args.ident)
    new_path = item["_path"]
    metadata = read_json(new_path / "sdd.json")
    metadata["status"] = "blocked"
    metadata["blockers"] = [args.reason]
    refresh_progress_metadata(new_path, metadata)
    append_decision(new_path, "blocker", args.reason)
    save_instance(new_path, metadata, args.reason, "解除 blocker 后运行 start。",
                  args.reason)
    write_catalog_and_index(root)
    print(f"Blocked {metadata['id']}: {args.reason}")
    return 0


def command_done(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    item = locate_instance(root, args.ident)
    completed, total, current = task_counts(item["_path"] / "tasks.md")
    if total and completed != total:
        raise SDDCliError(
            f"未完成任务，不能进入 done: {completed}/{total}, current={current}")
    new_path = item["_path"]
    metadata = read_json(new_path / "sdd.json")
    metadata["status"] = "done"
    metadata.setdefault("validation", []).append({
        "at": now(),
        "summary": args.validation
    })
    refresh_progress_metadata(new_path, metadata)
    metadata["progress"]["current_task"] = "done"
    latest = extract_latest_resume(new_path / "progress.md")
    conclusion = args.conclusion or latest.get(
        "last_conclusion") or "任务已完成并记录验证摘要。"
    next_action = args.next_action or "无需继续；如有新问题创建新 SDD 引用本任务。"
    record_validation(new_path, args.validation)
    save_instance(new_path, metadata, conclusion, next_action)
    write_catalog_and_index(root)
    print(f"Done {metadata['id']}: {args.validation}")
    return 0


def command_note(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    item = locate_instance(root, args.ident)
    path = item["_path"]
    metadata = read_json(path / "sdd.json")
    refresh_progress_metadata(path, metadata)
    if args.type == "validation":
        record_validation(path, args.text)
    else:
        append_decision(path, args.type, args.text)
    save_instance(
        path, metadata, args.text,
        f"保持当前任务 {metadata.get('progress', {}).get('current_task', 'none')}，按最新记录继续。",
        metadata.get("blockers", ["none"])[0]
        if metadata.get("status") == "blocked" else "none")
    write_catalog_and_index(root)
    print(f"Noted {metadata['id']}: {args.text}")
    return 0


def command_design_import(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    item = locate_instance(root, args.ident)
    path = item["_path"]
    source = Path(args.source)
    if not source.exists():
        raise SDDCliError(f"源文件不存在: {source}")
    if not source.is_file():
        raise SDDCliError(f"源路径不是文件: {source}")
    design_dir = path / "design"
    if not design_dir.exists():
        design_dir.mkdir(parents=True)
    dest = design_dir / source.name
    if dest.exists() and not args.force:
        raise SDDCliError(f"目标已存在: {dest}，使用 --force 覆盖")
    shutil.copy2(str(source), str(dest))
    # 更新 design/INDEX.md
    index_path = design_dir / "INDEX.md"
    if index_path.exists():
        index_text = index_path.read_text(encoding="utf-8")
    else:
        index_text = template_design_index()
    title = item.get("title", args.ident)
    today_str = today()
    role = args.role or "reference"
    notes = f"来源：{source}"
    if args.notes:
        notes = f"{notes}；{args.notes}"
    # 在 Documents 表中追加新行
    new_row = (
        f"| `{source.name}` | {role} | current | {today_str} | {notes} |")
    if "| --- |" in index_text:
        lines = index_text.splitlines()
        header_idx = None
        for idx, line in enumerate(lines):
            if "| --- |" in line:
                header_idx = idx
                break
        if header_idx is not None:
            lines.insert(header_idx + 1, new_row)
            index_text = "\n".join(lines)
    else:
        index_text = index_text.rstrip() + "\n\n" + new_row + "\n"
    index_path.write_text(index_text.rstrip() + "\n", encoding="utf-8")
    write_catalog_and_index(root)
    print(f"Imported {source.name} into {item['id']}/design/")
    return 0


def command_task(args: argparse.Namespace) -> int:
    root = resolve_root(args.root)
    item = locate_instance(root, args.ident)
    path = item["_path"]
    tasks_path = path / "tasks.md"
    metadata = read_json(path / "sdd.json")
    action = args.action
    if action == "list":
        for line in tasks_path.read_text(encoding="utf-8").splitlines():
            if re.match(r"^- \[[ xX]\] T\d+\.\d+", line):
                print(line)
        return 0
    if action == "add":
        if not args.text:
            raise SDDCliError("task add 需要 --text")
        task_id = args.task_ref or next_task_id(tasks_path)
        with tasks_path.open("a", encoding="utf-8") as handle:
            handle.write(f"\n- [ ] {task_id} {args.text}\n")
        conclusion = f"已新增任务 {task_id}。"
    else:
        if not args.task_ref:
            raise SDDCliError(f"task {action} 需要任务编号")
        checked = action in {"done", "check"}
        update_task_checkbox(tasks_path, args.task_ref, checked)
        conclusion = f"已{'完成' if checked else '取消完成'}任务 {args.task_ref}。"
    refresh_progress_metadata(path, metadata)
    current = metadata.get("progress", {}).get("current_task", "none")
    next_action = (f"继续执行 {current}。"
                   if current not in {"done", "none"} else
                   "所有任务已勾选，运行 done 时写入最终验证摘要。")
    save_instance(
        path, metadata, conclusion, next_action,
        metadata.get("blockers", ["none"])[0]
        if metadata.get("status") == "blocked" else "none")
    write_catalog_and_index(root)
    print(f"Task updated {metadata['id']}: {conclusion}")
    return 0
