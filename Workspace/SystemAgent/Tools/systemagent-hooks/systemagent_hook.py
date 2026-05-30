#!/usr/bin/env python3
"""SlimeAI systemagent advisory hook.

精简版：SessionStart 说一次路由提醒，PostToolUse 只在 Bash 后做 gate 检查，
不再每次文件写入都触发 retrospective inject。
使用轻量状态文件追踪"距上次验证多少步"，在 Stop 时强提醒。
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[3]
STATE_DIR = Path(
    os.environ.get("SLIMEAI_SYSTEMAGENT_HOOK_STATE_DIR",
                   ROOT / ".omc" / "state"))
STATE_FILE = STATE_DIR / "hook-state.json"

# 连续无验证的工具调用数阈值
VERIFY_GAP_WARN = 15
NOTICE_COOLDOWN_SECONDS = 3600


def _read_payload() -> dict[str, Any]:
    raw = sys.stdin.read()
    if not raw.strip():
        return {}
    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        return {}


def _read_state() -> dict[str, Any]:
    try:
        if STATE_FILE.exists():
            state = json.loads(STATE_FILE.read_text())
            if isinstance(state, dict):
                return state
    except Exception:
        pass
    return {}


def _write_state(state: dict[str, Any]) -> None:
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    try:
        STATE_FILE.write_text(json.dumps(state, ensure_ascii=False))
    except Exception:
        pass


def _notice_due(state: dict[str, Any], key: str) -> bool:
    now = time.time()
    notices = state.setdefault("last_notice_utc", {})
    if not isinstance(notices, dict):
        notices = {}
        state["last_notice_utc"] = notices
    last = notices.get(key)
    if not isinstance(last,
                      (int, float)) or now - last >= NOTICE_COOLDOWN_SECONDS:
        notices[key] = now
        return True
    return False


def _append_notice(state: dict[str, Any], key: str, message: str,
                   messages: list[str]) -> None:
    if _notice_due(state, key):
        messages.append(message)


def _post_tool_messages(payload: dict[str, Any]) -> list[str]:
    text = json.dumps(payload, ensure_ascii=False) if payload else ""

    validation_tokens = (
        "Tools/run-build.sh",
        "Tools/run-tests.sh",
        "run-godot-scene.sh",
        "analyze-godot-scene-logs.sh",
        "Workspace/SDD/sdd.py validate",
        "sdd.py validate",
        "sync-ai-config.sh",
    )
    sensitive_tokens = (
        ".ai-config/",
        ".claude/settings.json",
        ".claude/agents/",
        ".codex/hooks.json",
        ".codex/agents/",
        ".codex/config.toml",
        "Workspace/SystemAgent/Tools/systemagent-hooks",
        "Workspace/SystemAgent/Tools/skill-test",
    )
    has_validation = any(token in text for token in validation_tokens)
    has_sensitive_config = any(token in text for token in sensitive_tokens)

    messages: list[str] = []
    state = _read_state()
    state["tool_count_since_verify"] = state.get("tool_count_since_verify",
                                                 0) + 1

    if has_validation:
        state["tool_count_since_verify"] = 0
        state["last_verify_utc"] = time.time()
        _append_notice(
            state,
            "post_tool_validation",
            "PostToolUse: 已运行验证/同步相关命令；请读取输出或 artifact，必要时更新 SDD tasks/progress 与 retrospective。",
            messages,
        )

    if "run-godot-scene.sh" in text:
        _append_notice(
            state,
            "godot_scene_gate",
            "Godot scene gate: 检查 index.json、result.json 和 scene artifact；expectedInputs/expectedObservations/passCriteria/failCriteria/artifactPath 必须非空。",
            messages,
        )

    if "sync-ai-config.sh" in text or has_sensitive_config:
        _append_notice(
            state,
            "config_gate",
            "AI config gate: 确认只由 .ai-config 同步 skill/rule/command；hook/subagent 仍应直接维护 .claude/.codex，并运行 hook smoke 或 skill-test。",
            messages,
        )

    _write_state(state)
    return messages


def _event_messages(event: str, payload: dict[str, Any]) -> list[str]:
    event_key = event.lower()

    if event_key in {"sessionstart", "session-start"}:
        _write_state({
            "tool_count_since_verify": 0,
            "last_verify_utc": time.time()
        })
        return [
            "SlimeAI SystemAgent: 先路由 Workspace/SystemAgent/README.md，再按 Route / Actor / Rule 进入。",
            "配置边界: 见 Workspace/SystemAgent/Rules/AIConfig.md；skill/rule/command 改 .ai-config 后运行 sync。",
            "Review gate: 见 Workspace/SystemAgent/Rules/ReviewGates.md；验收格式见 Workspace/SystemAgent/Tools/BDDSceneFormat.md。",
            "外部资源: 见 Workspace/SystemAgent/Rules/Boundary.md。",
            "Git/Subagent: 见 Workspace/SystemAgent/Rules/Git.md 和 Subagent.md。",
        ]

    if event_key in {"userpromptsubmit", "user-prompt-submit", "prompt"}:
        # 不再每次说话都输出路由提醒 — 已迁移到 SessionStart
        return []

    if event_key in {"posttooluse", "post-tool-use"}:
        return _post_tool_messages(payload)

    if event_key in {"stop", "subagentstop", "subagent-stop"}:
        state = _read_state()
        gap = state.get("tool_count_since_verify", 0)
        msgs = [
            "Completion gate: final 前检查 git status、验证命令、SDD tasks/progress、artifact 和 SystemAgent retrospective。",
        ]
        if gap >= VERIFY_GAP_WARN:
            msgs.append(f"Verify gap: {gap} 次工具调用未验证。强烈建议在结束前运行验证命令。")
        return msgs

    return []


_HOOK_EVENT_NAMES: dict[str, str] = {
    "sessionstart": "SessionStart",
    "session-start": "SessionStart",
    "userpromptsubmit": "UserPromptSubmit",
    "user-prompt-submit": "UserPromptSubmit",
    "posttooluse": "PostToolUse",
    "post-tool-use": "PostToolUse",
    "stop": "Stop",
    "subagentstop": "SubagentStop",
    "subagent-stop": "SubagentStop",
}


def _build_output(event: str, tool: str,
                  messages: list[str]) -> dict[str, Any]:
    event_key = event.lower()
    text = "\n".join(f"- {m}" for m in messages)
    is_stop_event = event_key in {"stop", "subagentstop", "subagent-stop"}

    output: dict[str, Any] = {
        "systemMessage": f"[SlimeAI systemagent advisory]\n{text}",
    }

    if is_stop_event:
        output["continue"] = True
    else:
        if tool == "codex":
            output["continue"] = True
        else:
            hook_event_name = _HOOK_EVENT_NAMES.get(event_key, event)
            output["hookSpecificOutput"] = {
                "hookEventName": hook_event_name,
                "additionalContext": text,
            }

    return output


def _fallback_output(event: str, tool: str) -> dict[str, Any]:
    return _build_output(
        event,
        tool,
        ["SystemAgent hook fallback: hook 内部异常，已降级为非阻塞 advisory。"],
    )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="SlimeAI systemagent advisory hook")
    parser.add_argument("--event", required=True, help="Hook event name")
    parser.add_argument("--tool",
                        default="claude",
                        choices=["claude", "codex"],
                        help="调用方工具（claude 或 codex），决定输出格式")
    args = parser.parse_args()

    try:
        payload = _read_payload()
        messages = _event_messages(args.event, payload)
        if not messages:
            return 0
        output = _build_output(args.event, args.tool, messages)
    except Exception:
        output = _fallback_output(args.event, args.tool)

    print(json.dumps(output, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
