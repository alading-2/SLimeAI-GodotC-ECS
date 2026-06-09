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
            self.assertEqual(index["schema_version"], 3)
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


if __name__ == "__main__":
    unittest.main()
