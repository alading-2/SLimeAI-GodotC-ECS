#!/usr/bin/env python3
from __future__ import annotations

import argparse

from Src.commands import (
    command_init_root,
    command_new,
    command_project_new,
    command_project_list,
    command_project_show,
    command_project_archive,
    command_list,
    command_show,
    command_index,
    command_validate,
    command_doctor,
    command_start,
    command_block,
    command_done,
    command_note,
    command_design_import,
    command_task
)
from Src.config import DEFAULT_ROOT, PROJECT_BUCKETS, PROJECT_STATUSES, STATUSES
from Src.errors import SDDCliError


def build_parser() -> argparse.ArgumentParser:
    root_parent = argparse.ArgumentParser(add_help=False)
    root_parent.add_argument("--root",
                             default=str(DEFAULT_ROOT),
                             help="SDD 根目录，默认是工作区根 SDD/")
    parser = argparse.ArgumentParser(description="SlimeAI SDD CLI")
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("init-root", parents=[root_parent],
                   help="创建 SDD 根目录结构").set_defaults(func=command_init_root)

    p_new = sub.add_parser("new",
                           parents=[root_parent],
                           help="创建新的 pending SDD")
    p_new.add_argument("title")
    p_new.add_argument("--type", default="workflow")
    p_new.add_argument("--scope", default="Workspace/SDD")
    p_new.add_argument("--area", action="append")
    p_new.add_argument("--tag", action="append")
    p_new.add_argument("--git-boundary", action="append")
    p_new.add_argument(
        "--project", help="项目 ID；指定后创建到 SDD/project/projects/<project>/sdds/")
    p_new.set_defaults(func=command_new)

    p_project_new = sub.add_parser("project-new",
                                   parents=[root_parent],
                                   help="创建项目级 SDD 容器")
    p_project_new.add_argument("title")
    p_project_new.add_argument("--scope", default="Workspace/SDD")
    p_project_new.add_argument("--tag", action="append")
    p_project_new.set_defaults(func=command_project_new)

    p_project_list = sub.add_parser("project-list",
                                    parents=[root_parent],
                                    help="列出项目级 SDD 容器")
    p_project_list.add_argument("--state", choices=PROJECT_STATUSES)
    p_project_list.add_argument("--bucket", choices=PROJECT_BUCKETS)
    p_project_list.add_argument("--json", action="store_true")
    p_project_list.set_defaults(func=command_project_list)

    p_project_show = sub.add_parser("project-show",
                                    parents=[root_parent],
                                    help="显示项目级 SDD 容器")
    p_project_show.add_argument("ident")
    p_project_show.add_argument("--json", action="store_true")
    p_project_show.set_defaults(func=command_project_show)

    p_project_archive = sub.add_parser("project-archive",
                                       parents=[root_parent],
                                       help="将项目级 SDD 容器移动到 project/archived")
    p_project_archive.add_argument("ident")
    p_project_archive.add_argument("--status",
                                   default="done",
                                   choices=PROJECT_STATUSES)
    p_project_archive.set_defaults(func=command_project_archive)

    p_list = sub.add_parser("list", parents=[root_parent], help="列出 SDD")
    p_list.add_argument("--state", choices=STATUSES)
    p_list.add_argument("--scope")
    p_list.add_argument("--tag")
    p_list.add_argument("--json", action="store_true")
    p_list.set_defaults(func=command_list)

    p_show = sub.add_parser("show", parents=[root_parent], help="显示单个 SDD")
    p_show.add_argument("ident")
    p_show.add_argument("--json", action="store_true")
    p_show.set_defaults(func=command_show)

    sub.add_parser(
        "index", parents=[root_parent],
        help="重建 INDEX.md 和 catalog.json").set_defaults(func=command_index)

    p_validate = sub.add_parser("validate",
                                parents=[root_parent],
                                help="校验 SDD")
    p_validate.add_argument("ident", nargs="?")
    p_validate.add_argument("--all", action="store_true")
    p_validate.add_argument("--json", action="store_true")
    p_validate.set_defaults(func=command_validate)

    sub.add_parser("doctor", parents=[root_parent],
                   help="检查 CLI 与根目录状态").set_defaults(func=command_doctor)

    p_start = sub.add_parser("start",
                             parents=[root_parent],
                             help="将 pending/blocked SDD 移入 active")
    p_start.add_argument("ident")
    p_start.set_defaults(func=command_start)

    p_block = sub.add_parser("block",
                             parents=[root_parent],
                             help="将 SDD 标记为 blocked")
    p_block.add_argument("ident")
    p_block.add_argument("reason")
    p_block.set_defaults(func=command_block)

    p_done = sub.add_parser("done",
                            parents=[root_parent],
                            help="将 SDD 标记为 done")
    p_done.add_argument("ident")
    p_done.add_argument("--validation", required=True)
    p_done.add_argument("--conclusion")
    p_done.add_argument("--next-action")
    p_done.set_defaults(func=command_done)

    p_note = sub.add_parser("note",
                            parents=[root_parent],
                            help="追加 progress 记录")
    p_note.add_argument("ident")
    p_note.add_argument("text")
    p_note.add_argument("--type",
                        default="decision",
                        choices=[
                            "decision", "finding", "validation", "blocker",
                            "resume", "change"
                        ])
    p_note.set_defaults(func=command_note)

    p_import = sub.add_parser("design-import",
                              parents=[root_parent],
                              help="将外部设计文档复制到 SDD design/ 目录")
    p_import.add_argument("ident")
    p_import.add_argument("source", help="外部设计文档路径")
    p_import.add_argument("--role",
                          default="reference",
                          help="文档角色，默认 reference")
    p_import.add_argument("--notes", help="补充说明")
    p_import.add_argument("--force", action="store_true", help="覆盖已存在的目标文件")
    p_import.set_defaults(func=command_design_import)

    p_task = sub.add_parser("task",
                            parents=[root_parent],
                            help="管理 tasks.md checkbox")
    p_task.add_argument("ident")
    p_task.add_argument(
        "action", choices=["list", "add", "done", "todo", "check", "uncheck"])
    p_task.add_argument("task_ref", nargs="?")
    p_task.add_argument("--text")
    p_task.set_defaults(func=command_task)

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    try:
        return args.func(args)
    except SDDCliError as exc:
        print(f"ERROR SDD000 {exc}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
