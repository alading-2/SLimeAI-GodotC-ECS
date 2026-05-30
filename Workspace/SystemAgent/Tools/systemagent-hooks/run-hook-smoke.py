#!/usr/bin/env python3
from __future__ import annotations

import contextlib
import io
import json
import sys
import tempfile
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))

import systemagent_hook


class SmokeFailure(Exception):
    pass


@contextlib.contextmanager
def patched_io(event: str, tool: str, stdin_text: str):
    old_argv = sys.argv
    old_stdin = sys.stdin
    old_stdout = sys.stdout
    capture = io.StringIO()
    sys.argv = ["systemagent_hook.py", "--event", event, "--tool", tool]
    sys.stdin = io.StringIO(stdin_text)
    sys.stdout = capture
    try:
        yield capture
    finally:
        sys.argv = old_argv
        sys.stdin = old_stdin
        sys.stdout = old_stdout


@contextlib.contextmanager
def isolated_state():
    old_state_dir = systemagent_hook.STATE_DIR
    old_state_file = systemagent_hook.STATE_FILE
    with tempfile.TemporaryDirectory(
            prefix="systemagent-hook-smoke-") as temp_dir:
        state_dir = Path(temp_dir)
        systemagent_hook.STATE_DIR = state_dir
        systemagent_hook.STATE_FILE = state_dir / "hook-state.json"
        try:
            yield
        finally:
            systemagent_hook.STATE_DIR = old_state_dir
            systemagent_hook.STATE_FILE = old_state_file


def run_hook(event: str,
             tool: str = "claude",
             stdin_text: str = "") -> tuple[int, str]:
    with patched_io(event, tool, stdin_text) as capture:
        rc = systemagent_hook.main()
    return rc, capture.getvalue().strip()


def parse_json(stdout: str, case_name: str) -> dict[str, object]:
    if not stdout:
        raise SmokeFailure(
            f"{case_name}: expected JSON stdout, got empty output")
    try:
        value = json.loads(stdout)
    except json.JSONDecodeError as exc:
        raise SmokeFailure(
            f"{case_name}: stdout is not valid JSON: {stdout!r}") from exc
    if not isinstance(value, dict):
        raise SmokeFailure(f"{case_name}: JSON stdout must be an object")
    return value


def assert_hook_json(case_name: str,
                     event: str,
                     tool: str,
                     stdin_text: str = "") -> dict[str, object]:
    rc, stdout = run_hook(event, tool, stdin_text)
    if rc != 0:
        raise SmokeFailure(f"{case_name}: expected exit code 0, got {rc}")
    return parse_json(stdout, case_name)


def assert_no_output(case_name: str,
                     event: str,
                     tool: str,
                     stdin_text: str = "") -> None:
    rc, stdout = run_hook(event, tool, stdin_text)
    if rc != 0:
        raise SmokeFailure(f"{case_name}: expected exit code 0, got {rc}")
    if stdout:
        raise SmokeFailure(
            f"{case_name}: expected empty stdout, got {stdout!r}")


def assert_codex_json(case_name: str, value: dict[str, object]) -> None:
    if value.get("continue") is not True:
        raise SmokeFailure(
            f"{case_name}: Codex output must include continue=true")
    if "hookSpecificOutput" in value:
        raise SmokeFailure(
            f"{case_name}: Codex output must not include hookSpecificOutput")


def assert_claude_event(case_name: str, value: dict[str, object],
                        event: str) -> None:
    specific = value.get("hookSpecificOutput")
    if not isinstance(specific, dict):
        raise SmokeFailure(
            f"{case_name}: Claude non-Stop output must include hookSpecificOutput"
        )
    if specific.get("hookEventName") != event:
        raise SmokeFailure(
            f"{case_name}: hookEventName expected {event}, got {specific.get('hookEventName')}"
        )


def main() -> int:
    with isolated_state():
        claude_session = assert_hook_json("claude-sessionstart",
                                          "SessionStart", "claude")
        assert_claude_event("claude-sessionstart", claude_session,
                            "SessionStart")

        claude_post = assert_hook_json(
            "claude-posttooluse-validation",
            "PostToolUse",
            "claude",
            json.dumps({
                "tool_input": {
                    "command": "python3 Workspace/SDD/sdd.py validate SDD-0007"
                }
            }),
        )
        assert_claude_event("claude-posttooluse-validation", claude_post,
                            "PostToolUse")

        assert_no_output(
            "posttooluse-dedup-second",
            "PostToolUse",
            "claude",
            json.dumps({
                "tool_input": {
                    "command": "python3 Workspace/SDD/sdd.py validate SDD-0007"
                }
            }),
        )

        claude_stop = assert_hook_json("claude-stop-empty-stdin", "Stop",
                                       "claude", "")
        if claude_stop.get("continue") is not True:
            raise SmokeFailure(
                "claude-stop-empty-stdin: Stop output must include continue=true"
            )
        if "Skill-test" in json.dumps(claude_stop, ensure_ascii=False):
            raise SmokeFailure(
                "claude-stop-empty-stdin: Stop must not run or report skill-test"
            )

        codex_session = assert_hook_json("codex-sessionstart", "SessionStart",
                                         "codex")
        assert_codex_json("codex-sessionstart", codex_session)

        assert_no_output("codex-userpromptsubmit", "UserPromptSubmit", "codex")

        codex_post = assert_hook_json(
            "codex-posttooluse-sync",
            "PostToolUse",
            "codex",
            json.dumps({
                "tool_input": {
                    "command":
                    "bash Workspace/Tools/ai-config-sync/sync-ai-config.sh"
                }
            }),
        )
        assert_codex_json("codex-posttooluse-sync", codex_post)

        codex_stop = assert_hook_json("codex-stop-invalid-stdin", "Stop",
                                      "codex", "{not-json")
        assert_codex_json("codex-stop-invalid-stdin", codex_stop)

        original_event_messages = systemagent_hook._event_messages
        try:

            def raise_error(event: str, payload: dict[str,
                                                      object]) -> list[str]:
                raise RuntimeError("smoke forced failure")

            systemagent_hook._event_messages = raise_error
            fallback = assert_hook_json("stop-exception-fallback", "Stop",
                                        "codex")
            assert_codex_json("stop-exception-fallback", fallback)
        finally:
            systemagent_hook._event_messages = original_event_messages

    print("hook-smoke: passed")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except SmokeFailure as exc:
        print(f"hook-smoke: failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
