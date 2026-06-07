#!/usr/bin/env python3
"""迁移项目目录或 Godot 资源路径引用。

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
        "--include-variants",
        action="store_true",
        help=(
            "Also replace obvious path variants under --root: res://, project-relative, "
            "and absolute paths. Always inspect dry-run before --apply."
        ),
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


def normalize_separators(value: str) -> str:
    return value.replace("\\", "/")


def to_res_path(value: str) -> str:
    normalized = normalize_separators(value).lstrip("/")
    return normalized if normalized.startswith("res://") else f"res://{normalized}"


def to_relative_path(value: str, root: Path) -> str:
    normalized = normalize_separators(value)
    if normalized.startswith("res://"):
        return normalized[6:]

    path = Path(value)
    if path.is_absolute():
        try:
            return normalize_separators(str(path.resolve().relative_to(root)))
        except ValueError:
            return normalized

    return normalized.lstrip("./")


def build_replacements(old: str, new: str, root: Path, include_variants: bool) -> list[tuple[str, str]]:
    replacements: list[tuple[str, str]] = [(old, new)]
    if not include_variants:
        return replacements

    old_relative = to_relative_path(old, root)
    new_relative = to_relative_path(new, root)
    old_res = to_res_path(old_relative)
    new_res = to_res_path(new_relative)
    old_absolute = normalize_separators(str((root / old_relative).resolve()))
    new_absolute = normalize_separators(str((root / new_relative).resolve()))

    for candidate in [
        (old_res, new_res),
        (old_relative, new_relative),
        (old_absolute, new_absolute),
    ]:
        if candidate[0] and candidate[0] != candidate[1] and candidate not in replacements:
            replacements.append(candidate)

    return replacements


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

    replacements = build_replacements(old, new, root, args.include_variants)
    changed: list[tuple[Path, int]] = []
    scanned = 0

    for path in iter_files(root, excluded_dirs, extensions):
        scanned += 1
        text = read_text(path)
        if text is None:
            continue

        count = 0
        updated_text = text
        for old_value, new_value in replacements:
            match_count = updated_text.count(old_value)
            if match_count == 0:
                continue
            count += match_count
            updated_text = updated_text.replace(old_value, new_value)

        if count == 0:
            continue

        changed.append((path, count))

        if args.apply:
            path.write_text(updated_text, encoding="utf-8")

    mode = "APPLY" if args.apply else "DRY-RUN"
    print(f"resource-path-migration: {mode}")
    print(f"root: {root}")
    print(f"old: {old}")
    print(f"new: {new}")
    if args.include_variants:
        print("replacement variants:")
        for old_value, new_value in replacements:
            print(f"  {old_value} -> {new_value}")
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
