#!/usr/bin/env python3
"""迁移 Godot 资源路径引用。

默认 dry-run，只报告会修改的文件；传入 --apply 后才写入。
"""
from __future__ import annotations

import argparse
from pathlib import Path


DEFAULT_EXCLUDE_DIRS = {
    ".git",
    ".godot",
    ".ai-temp",
    "bin",
    "obj",
    ".vs",
    ".idea",
    "Library",
}

DEFAULT_EXTENSIONS = {
    ".cs",
    ".tscn",
    ".tres",
    ".json",
    ".sql",
    ".md",
    ".csproj",
    ".godot",
    ".cfg",
    ".yaml",
    ".yml",
    ".txt",
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Replace old Godot resource paths with new paths in text files."
    )
    parser.add_argument("--old", required=True, help="Old path, e.g. res://assets/Old")
    parser.add_argument("--new", required=True, help="New path, e.g. res://assets/New")
    parser.add_argument(
        "--root",
        default=".",
        help="Search root. Defaults to current working directory.",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Write changes. Omit for dry-run.",
    )
    parser.add_argument(
        "--include-extension",
        action="append",
        default=[],
        help="Extra extension to include, e.g. --include-extension .gd",
    )
    parser.add_argument(
        "--exclude-dir",
        action="append",
        default=[],
        help="Extra directory name to exclude.",
    )
    return parser.parse_args()


def should_skip(path: Path, root: Path, excluded_dirs: set[str], extensions: set[str]) -> bool:
    try:
        rel_parts = path.relative_to(root).parts
    except ValueError:
        rel_parts = path.parts

    if any(part in excluded_dirs for part in rel_parts):
        return True

    if path.is_dir():
        return False

    return path.suffix not in extensions


def iter_files(root: Path, excluded_dirs: set[str], extensions: set[str]):
    for path in root.rglob("*"):
        if should_skip(path, root, excluded_dirs, extensions):
            continue
        if path.is_file():
            yield path


def read_text(path: Path) -> str | None:
    try:
        return path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        return None
    except OSError:
        return None


def main() -> int:
    args = parse_args()
    root = Path(args.root).resolve()
    old = args.old
    new = args.new

    if old == new:
        print("old and new are identical; nothing to do.")
        return 0

    excluded_dirs = DEFAULT_EXCLUDE_DIRS | set(args.exclude_dir)
    extensions = DEFAULT_EXTENSIONS | set(args.include_extension)

    changed: list[tuple[Path, int]] = []
    scanned = 0

    for path in iter_files(root, excluded_dirs, extensions):
        scanned += 1
        text = read_text(path)
        if text is None or old not in text:
            continue

        count = text.count(old)
        changed.append((path, count))

        if args.apply:
            path.write_text(text.replace(old, new), encoding="utf-8")

    mode = "APPLY" if args.apply else "DRY-RUN"
    print(f"resource-path-migration: {mode}")
    print(f"root: {root}")
    print(f"old: {old}")
    print(f"new: {new}")
    print(f"scanned files: {scanned}")
    print(f"matched files: {len(changed)}")

    for path, count in changed:
        rel = path.relative_to(root)
        print(f"{rel}: {count}")

    if not args.apply:
        print("No files were changed. Re-run with --apply to write changes.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
