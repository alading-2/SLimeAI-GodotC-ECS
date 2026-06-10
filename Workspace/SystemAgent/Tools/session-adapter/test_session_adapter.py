#!/usr/bin/env python3
"""session-adapter Codex digest 行为测试。"""

from __future__ import annotations

import json
import re
import subprocess
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).with_name("session_adapter.py").resolve()


def write_jsonl(path: Path, records: list[dict]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        "".join(json.dumps(record, ensure_ascii=False) + "\n" for record in records),
        encoding="utf-8",
    )


def session_meta(session_id: str, timestamp: str = "2026-06-09T07:28:46.105Z") -> dict:
    return {
        "timestamp": timestamp,
        "type": "session_meta",
        "payload": {
            "id": session_id,
            "timestamp": timestamp,
            "cwd": "/home/slime/Code/SlimeAI/SlimeAI",
            "base_instructions": {"text": "# AGENTS.md instructions\n大量启动噪声"},
        },
    }


def user_message(text: str, timestamp: str = "2026-06-09T07:29:00Z") -> dict:
    """构造 Codex 用户消息，避免每个用例重复 JSONL 结构。"""
    return {
        "timestamp": timestamp,
        "type": "response_item",
        "payload": {
            "type": "message",
            "role": "user",
            "content": [{"type": "input_text", "text": text}],
        },
    }


def assistant_message(
    text: str,
    *,
    phase: str = "commentary",
    timestamp: str = "2026-06-09T07:30:00Z",
) -> dict:
    """构造 Codex assistant 消息；phase=final 用来表达真实收尾。"""
    return {
        "timestamp": timestamp,
        "type": "response_item",
        "payload": {
            "type": "message",
            "role": "assistant",
            "phase": phase,
            "content": [{"type": "output_text", "text": text}],
        },
    }


def tool_call(call_id: str, command: str, *, timestamp: str = "2026-06-09T07:29:10Z") -> dict:
    """构造 exec_command 调用。"""
    return {
        "timestamp": timestamp,
        "type": "response_item",
        "payload": {
            "type": "function_call",
            "name": "exec_command",
            "call_id": call_id,
            "arguments": json.dumps({"cmd": command}),
        },
    }


def tool_output(call_id: str, output: str, *, timestamp: str = "2026-06-09T07:29:11Z") -> dict:
    """构造 exec_command 输出。"""
    return {
        "timestamp": timestamp,
        "type": "response_item",
        "payload": {
            "type": "function_call_output",
            "call_id": call_id,
            "output": output,
        },
    }


def apply_patch_call(call_id: str, patch: str = "*** Begin Patch\n*** Update File: a.md\n*** End Patch") -> dict:
    """构造 apply_patch 调用。"""
    return {
        "timestamp": "2026-06-09T07:29:20Z",
        "type": "response_item",
        "payload": {
            "type": "function_call",
            "name": "apply_patch",
            "call_id": call_id,
            "arguments": patch,
        },
    }


class CodexDigestCommandTests(unittest.TestCase):
    def run_adapter(self, chat_root: Path, *args: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            ["python3", str(SCRIPT), "--chat-root", str(chat_root), *args],
            text=True,
            capture_output=True,
            check=False,
        )

    def test_short_codex_session_writes_locator_only_without_folder(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            chat_root = root / "chat"
            source = root / "rollout-2026-06-09T07-28-46-short-session.jsonl"
            write_jsonl(
                source,
                [
                    session_meta("short-session"),
                    {
                        "timestamp": "2026-06-09T07:29:00Z",
                        "type": "event_msg",
                        "payload": {"type": "user_message", "message": "你好"},
                    },
                ],
            )

            result = self.run_adapter(chat_root, "digest-codex", "--session", str(source))

            self.assertEqual(result.returncode, 0, result.stderr)
            index = json.loads((chat_root / "index.json").read_text(encoding="utf-8"))
            entry = index["entries"][0]
            self.assertEqual(index["schema_version"], 4)
            self.assertEqual(entry["digest_status"], "locator-only")
            self.assertEqual(entry["skip_reasons"], ["too_short"])
            self.assertNotIn("folder_path", entry)
            self.assertFalse(any(chat_root.glob("2026/06/09/*short-session*")))

    def test_codex_digest_generates_folder_events_and_tool_failures(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            chat_root = root / "chat"
            source = root / "rollout-2026-06-09T15-28-46-019eab48-a807-7a53-8517-8113be876303.jsonl"
            write_jsonl(
                source,
                [
                    session_meta("019eab48-a807-7a53-8517-8113be876303"),
                    {
                        "timestamp": "2026-06-09T07:29:00Z",
                        "type": "response_item",
                        "payload": {
                            "type": "message",
                            "role": "user",
                            "content": [
                                {
                                    "type": "input_text",
                                    "text": "请更新 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py，并运行 python3 -m py_compile。",
                                }
                            ],
                        },
                    },
                    {
                        "timestamp": "2026-06-09T07:29:10Z",
                        "type": "response_item",
                        "payload": {
                            "type": "function_call",
                            "name": "exec_command",
                            "call_id": "call_read",
                            "arguments": json.dumps(
                                {
                                    "cmd": "sed -n '1,20p' Workspace/SystemAgent/Tools/session-adapter/session_adapter.py"
                                }
                            ),
                        },
                    },
                    {
                        "timestamp": "2026-06-09T07:29:11Z",
                        "type": "response_item",
                        "payload": {
                            "type": "function_call_output",
                            "call_id": "call_read",
                            "output": "Process exited with code 0\nOutput:\n...",
                        },
                    },
                    {
                        "timestamp": "2026-06-09T07:29:20Z",
                        "type": "response_item",
                        "payload": {
                            "type": "function_call",
                            "name": "apply_patch",
                            "call_id": "call_patch",
                            "arguments": "*** Begin Patch\n*** Update File: Workspace/SystemAgent/Tools/session-adapter/session_adapter.py\n*** End Patch",
                        },
                    },
                    {
                        "timestamp": "2026-06-09T07:29:21Z",
                        "type": "response_item",
                        "payload": {
                            "type": "function_call_output",
                            "call_id": "call_patch",
                            "output": "Success. Updated the following files:\nM Workspace/SystemAgent/Tools/session-adapter/session_adapter.py",
                        },
                    },
                    {
                        "timestamp": "2026-06-09T07:29:30Z",
                        "type": "response_item",
                        "payload": {
                            "type": "function_call",
                            "name": "exec_command",
                            "call_id": "call_validate",
                            "arguments": json.dumps(
                                {
                                    "cmd": "python3 -m py_compile Workspace/SystemAgent/Tools/session-adapter/session_adapter.py"
                                }
                            ),
                        },
                    },
                    {
                        "timestamp": "2026-06-09T07:29:31Z",
                        "type": "response_item",
                        "payload": {
                            "type": "function_call_output",
                            "call_id": "call_validate",
                            "output": "Process exited with code 1\nError: invalid syntax",
                        },
                    },
                    {
                        "timestamp": "2026-06-09T07:30:00Z",
                        "type": "response_item",
                        "payload": {
                            "type": "message",
                            "role": "assistant",
                            "content": [
                                {
                                    "type": "output_text",
                                    "text": "已更新 session_adapter.py；验证命令 python3 -m py_compile 失败，原因是 invalid syntax。",
                                }
                            ],
                        },
                    },
                ],
            )

            result = self.run_adapter(chat_root, "digest-codex", "--session", str(source))

            self.assertEqual(result.returncode, 0, result.stderr)
            index = json.loads((chat_root / "index.json").read_text(encoding="utf-8"))
            entry = index["entries"][0]
            self.assertEqual(entry["digest_status"], "digest")
            self.assertEqual(entry["tool_failed_count"], 1)
            self.assertEqual(entry["validation_signals"], 1)
            folder = chat_root / Path(entry["folder_path"]).relative_to("Workspace/DocsAI/ChatHistory")
            self.assertRegex(folder.name, r"2026-06-09-15-28-codex-.*-019eab48a8077$")

            expected = [
                "manifest.json",
                "raw/transcript.visible.md",
                "raw/source-locator.md",
                "derived/events.jsonl",
                "derived/ai-context.md",
                "derived/summary.md",
                "derived/user-requests.md",
                "derived/assistant-results.md",
                "derived/tools.md",
                "derived/files.md",
                "derived/validation.md",
                "derived/interruptions.md",
                "derived/noise.md",
                "derived/tool-failures.md",
            ]
            for relative in expected:
                self.assertTrue((folder / relative).exists(), relative)

            events = [
                json.loads(line)
                for line in (folder / "derived/events.jsonl").read_text(encoding="utf-8").splitlines()
            ]
            self.assertTrue(any(event["event_kind"] == "tool_call" for event in events))
            self.assertTrue(any(event["tool_status"] == "failed" for event in events))
            self.assertIn("invalid syntax", (folder / "derived/tool-failures.md").read_text(encoding="utf-8"))
            self.assertNotIn("大量启动噪声", (folder / "derived/ai-context.md").read_text(encoding="utf-8"))

            list_result = self.run_adapter(chat_root, "list-digests", "--failed-tools", "--status", "digest")
            self.assertEqual(list_result.returncode, 0, list_result.stderr)
            self.assertIn("019eab48", list_result.stdout)

    def test_sdd_cli_category_does_not_inflate_verification_loops(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            chat_root = root / "chat"
            source = root / "rollout-2026-06-09T16-00-00-command-category.jsonl"
            write_jsonl(
                source,
                [
                    session_meta("command-category"),
                    user_message("按 SDD-0041 执行 session-adapter 分类修复。"),
                    apply_patch_call("patch"),
                    tool_output("patch", "Success. Updated the following files:\nM Workspace/SystemAgent/Tools/session-adapter/session_adapter.py"),
                    tool_call("sdd_validate", "python3 Workspace/SDD/sdd.py validate SDD-0041"),
                    tool_output("sdd_validate", "Process exited with code 0\nSDD validate: SDD-0041\nChecks: 0 error(s), 0 warning(s)"),
                    tool_call("sdd_show", "python3 Workspace/SDD/sdd.py show SDD-0041"),
                    tool_output("sdd_show", "Process exited with code 0\n# SDD-0041"),
                    tool_call("git_status", "git status --short"),
                    tool_output("git_status", "Process exited with code 0\n M file.py"),
                    tool_call("unit", "python3 -m unittest Workspace/SystemAgent/Tools/session-adapter/test_session_adapter.py"),
                    tool_output("unit", "Process exited with code 0\nOK"),
                    assistant_message("分类修复完成，验证通过。", phase="final"),
                ],
            )

            result = self.run_adapter(chat_root, "digest-codex", "--session", str(source))

            self.assertEqual(result.returncode, 0, result.stderr)
            index = json.loads((chat_root / "index.json").read_text(encoding="utf-8"))
            entry = index["entries"][0]
            self.assertEqual(entry["schema_version"], 4)
            self.assertEqual(entry["validation_signals"], 2)
            self.assertEqual(entry["command_category_counts"]["edit"], 1)
            self.assertEqual(entry["command_category_counts"]["validation"], 2)
            self.assertEqual(entry["command_category_counts"]["git_inspection"], 1)
            self.assertEqual((entry["efficiency"] or {})["verification_loops"], 1)
            folder = chat_root / Path(entry["folder_path"]).relative_to("Workspace/DocsAI/ChatHistory")
            events = [
                json.loads(line)
                for line in (folder / "derived/events.jsonl").read_text(encoding="utf-8").splitlines()
            ]
            by_summary = {event["summary"]: event for event in events if event["event_kind"] == "tool_call"}
            self.assertEqual(by_summary["python3 Workspace/SDD/sdd.py validate SDD-0041"]["command_category"], "validation")
            self.assertEqual(by_summary["python3 Workspace/SDD/sdd.py show SDD-0041"]["command_category"], "read")
            self.assertEqual(by_summary["git status --short"]["command_category"], "git_inspection")

    def test_digest_goal_outcome_skips_resume_continue_and_duplicates(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            chat_root = root / "chat"
            source = root / "rollout-2026-06-09T17-00-00-goal-cleanup.jsonl"
            duplicated_goal = "按照 SDD-0041 执行 session-adapter digest 准确性修复。"
            write_jsonl(
                source,
                [
                    session_meta("goal-cleanup"),
                    user_message("A previous agent produced the plan below. Use it as context, but do not treat this as the user goal."),
                    user_message("continue"),
                    user_message(duplicated_goal),
                    user_message(duplicated_goal),
                    tool_call("read", "sed -n '1,20p' Workspace/SystemAgent/Tools/session-adapter/session_adapter.py"),
                    tool_output("read", "Process exited with code 0\nOutput"),
                    assistant_message("我会继续读取上下文。"),
                    assistant_message("我会继续读取上下文。"),
                    assistant_message("已完成 SDD-0041 session-adapter digest 修复，验证命令通过。", phase="final"),
                ],
            )

            result = self.run_adapter(chat_root, "digest-codex", "--session", str(source))

            self.assertEqual(result.returncode, 0, result.stderr)
            index = json.loads((chat_root / "index.json").read_text(encoding="utf-8"))
            entry = index["entries"][0]
            folder = chat_root / Path(entry["folder_path"]).relative_to("Workspace/DocsAI/ChatHistory")
            ai_context = (folder / "derived/ai-context.md").read_text(encoding="utf-8")
            requests = (folder / "derived/user-requests.md").read_text(encoding="utf-8")
            assistant_results = (folder / "derived/assistant-results.md").read_text(encoding="utf-8")

            self.assertIn("SDD-0041", entry["title"])
            self.assertNotIn("A previous agent produced the plan below", entry["title"])
            self.assertIn(duplicated_goal, ai_context)
            self.assertNotIn("## User Goal\n\ncontinue", ai_context)
            self.assertIn("已完成 SDD-0041", ai_context)
            self.assertEqual(requests.count(duplicated_goal), 1)
            self.assertEqual(assistant_results.count("我会继续读取上下文。"), 1)

    def test_tool_failure_records_category_retry_recovery_and_impact(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            chat_root = root / "chat"
            source = root / "rollout-2026-06-09T18-00-00-failure-recovery.jsonl"
            write_jsonl(
                source,
                [
                    session_meta("failure-recovery"),
                    user_message("修复 session-adapter failure 输出。"),
                    tool_call("bad", "sed -n '1,20p' missing.md"),
                    tool_output("bad", "Process exited with code 2\nsed: can't read missing.md: No such file or directory"),
                    tool_call("good", "sed -n '1,20p' missing.md"),
                    tool_output("good", "Process exited with code 0\n# recovered"),
                    assistant_message("已绕过路径问题并完成修复。", phase="final"),
                ],
            )

            result = self.run_adapter(chat_root, "digest-codex", "--session", str(source))

            self.assertEqual(result.returncode, 0, result.stderr)
            index = json.loads((chat_root / "index.json").read_text(encoding="utf-8"))
            entry = index["entries"][0]
            folder = chat_root / Path(entry["folder_path"]).relative_to("Workspace/DocsAI/ChatHistory")
            failures = (folder / "derived/tool-failures.md").read_text(encoding="utf-8")

            self.assertIn("failure_category", failures)
            self.assertIn("path_error", failures)
            self.assertIn("retry_count", failures)
            self.assertIn("recovered=yes", failures)
            self.assertIn("final_impact=worked_around", failures)

    def test_stale_report_lists_missing_codex_sources(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source_root = root / "sessions" / "2026" / "06" / "10"
            chat_root = root / "chat"
            first = source_root / "rollout-2026-06-10T09-00-00-indexed.jsonl"
            missing = source_root / "rollout-2026-06-10T10-00-00-missing.jsonl"
            write_jsonl(first, [session_meta("indexed", "2026-06-10T09:00:00Z"), user_message("已索引会话")])
            write_jsonl(missing, [session_meta("missing", "2026-06-10T10:00:00Z"), user_message("缺失会话")])
            chat_root.mkdir(parents=True, exist_ok=True)
            (chat_root / "index.json").write_text(
                json.dumps(
                    {
                        "schema_version": 4,
                        "entries": [
                            {
                                "id": "codex:indexed",
                                "source_tool": "codex",
                                "source_adapter": "session-adapter.codex-digest",
                                "session_id": "indexed",
                                "digest_status": "digest",
                                "source_path": str(first),
                            }
                        ],
                    },
                    ensure_ascii=False,
                    indent=2,
                )
                + "\n",
                encoding="utf-8",
            )

            result = self.run_adapter(
                chat_root,
                "stale-report",
                "--source-root",
                str(source_root),
                "--json",
            )

            self.assertEqual(result.returncode, 0, result.stderr)
            report = json.loads(result.stdout)
            self.assertEqual(report["source_count"], 2)
            self.assertEqual(report["digest_count"], 1)
            self.assertEqual(report["coverage"], "stale")
            self.assertEqual(report["missing_session_ids"], ["missing"])


if __name__ == "__main__":
    unittest.main()
