#!/usr/bin/env python3
"""只读整理本地 AI CLI 会话为 SlimeAI ChatHistory sidecar。"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[4]
DEFAULT_CODBASH_ROOT = REPO_ROOT / "Workspace/Resources/tool/codbash"
DEFAULT_CHAT_ROOT = REPO_ROOT / "Workspace/DocsAI/ChatHistory"
DEFAULT_CODEX_MONTH_ROOT = Path("~/.codex/sessions/2026/06").expanduser()
SUPPORTED_SOURCE_TOOLS = {"claude", "claude-ext", "codex", "opencode"}
ENCRYPTED_PLACEHOLDER_KEYS = {"encrypted_content"}
TRANSCRIPT_HEADING_TIMESTAMP_RE = re.compile(
    r"^(### \d{6})\s+\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z(\s+.+)$",
    re.MULTILINE,
)
PROCESS_EXIT_RE = re.compile(r"Process exited with code\s+(-?\d+)", re.IGNORECASE)
EXIT_CODE_RE = re.compile(r"\bExit code:?\s*(-?\d+)", re.IGNORECASE)
FAILED_TEXT_RE = re.compile(
    r"\b(traceback|exception|failed|failure|timeout|timed out|protocol mismatch|invalid syntax|syntax error)\b|"
    r"\b(error|fatal):",
    re.IGNORECASE,
)
SUCCESS_TEXT_RE = re.compile(
    r"Process exited with code 0|Exit code:?\s*0\b|0 error / 0 warning|"
    r"\b(success|passed|validated|exported|generated|created|updated)\b",
    re.IGNORECASE,
)
# 命令分类改为显式类别，避免用两个宽泛正则把 read/validation/edit 混在一起。
COMMAND_CATEGORIES = [
    "read",
    "edit",
    "sdd_state_write",
    "validation",
    "git_inspection",
    "git_write",
    "external_probe",
    "unknown",
]
SDD_READ_COMMANDS = {"show", "list", "project-show", "project-list"}
SDD_WRITE_COMMANDS = {
    "new",
    "start",
    "task",
    "note",
    "block",
    "done",
    "design-import",
    "index",
    "init-root",
    "project-new",
    "project-archive",
}
SDD_VALIDATION_COMMANDS = {"validate", "doctor"}
RESUME_BOILERPLATE_RE = re.compile(
    r"^\s*(A previous agent produced the plan below|Previous agent summary|"
    r"We need continue from|This session is being continued)",
    re.IGNORECASE,
)
CONTINUE_ONLY_RE = re.compile(r"^\s*(continue|继续|继续。|继续执行|继续吧)\s*$", re.IGNORECASE)
FINAL_CONCLUSION_RE = re.compile(
    r"(^|\n)\s*(已完成|本轮完成|实现完成|验证通过|结论[:：]|总结[:：]|"
    r"complete|completed|implemented|validation passed|blocked|failed)\b",
    re.IGNORECASE,
)
PATH_RE = re.compile(
    r"(?:(?:Workspace|SDD|DocsAI|Src|Data|Games|Tools|\.ai-config|\.codex|\.claude|\.trae|"
    r"\.opencode|Brotato_my\.csproj)[\w./@+=:,~\-\u4e00-\u9fff]*|"
    r"[\w./@+=:,~\-\u4e00-\u9fff]+\.(?:py|md|cs|json|toml|yaml|yml|sh|csproj|ts|tsx|js|mjs|gd|tscn|uid))"
)


class SessionAdapterError(RuntimeError):
    """用户可修复的 session adapter 错误。"""


@dataclass(frozen=True)
class AdapterPaths:
    repo_root: Path
    codbash_root: Path
    chat_root: Path


@dataclass
class CodexToolCall:
    call_id: str
    tool_name: str
    arguments: Any
    command: str
    event_index: int
    raw_ref: str
    status: str = "unknown"
    output: str = ""
    output_event_index: int = 0
    output_raw_ref: str = ""
    failure_reason: str = ""
    command_category: str = "unknown"
    failure_category: str = "unknown"
    retry_count: int = 0
    recovered: str = "unknown"
    final_impact: str = "unknown"
    validation_signal: bool = False
    code_edit_signal: bool = False
    files_read: set[str] = field(default_factory=set)
    files_modified: set[str] = field(default_factory=set)
    files_inferred: set[str] = field(default_factory=set)


@dataclass
class CodexDigestAnalysis:
    metadata: dict[str, Any]
    source_path: Path
    source_sha256: str
    source_bytes: int
    source_lines: int
    records: list[dict[str, Any]]
    events: list[dict[str, Any]]
    tool_calls_detail: list[CodexToolCall]
    user_requests: list[dict[str, Any]]
    assistant_messages_detail: list[dict[str, Any]]
    validation_commands: list[dict[str, Any]]
    file_entries: dict[str, dict[str, Any]]
    sdd_ids: list[str]
    topics: list[str]
    meaningful_user_turns: int
    assistant_messages: int
    tool_calls: int
    tool_outputs: int
    tool_success_count: int
    tool_failed_count: int
    tool_unknown_count: int
    turn_aborted_count: int
    thread_rolled_back_count: int
    interrupted: bool
    code_edit_signals: dict[str, int]
    sdd_state_write_signals: int
    command_category_counts: dict[str, int]
    validation_signals: int
    final_conclusion: bool
    duration_seconds: int
    noise_ratio_estimate: float
    digest_status: str
    priority: str
    skip_reasons: list[str]
    digest_reasons: list[str]
    topic_slug: str
    # 效率指标
    verification_loops: int = 0
    file_read_counts: dict[str, int] = field(default_factory=dict)
    repeated_reads_gt3: list[str] = field(default_factory=list)
    avg_validation_per_edit: float = 0.0


def run_command(args: list[str], cwd: Path | None = None) -> subprocess.CompletedProcess[str]:
    return subprocess.run(args, cwd=cwd, text=True, capture_output=True, check=False)


def resolve_repo(path: str) -> Path:
    candidate = Path(path).expanduser()
    if not candidate.is_absolute():
        candidate = (Path.cwd() / candidate).resolve()
    result = run_command(["git", "-C", str(candidate), "rev-parse", "--show-toplevel"])
    if result.returncode == 0:
        return Path(result.stdout.strip()).resolve()
    return candidate.resolve()


def repo_relative(path: Path) -> str:
    try:
        return str(path.resolve().relative_to(REPO_ROOT))
    except ValueError:
        return str(path)


def require_codbash_root(codbash_root: Path) -> None:
    data_js = codbash_root / "src/data.js"
    if not data_js.exists():
        raise SessionAdapterError(
            "找不到 codbash 本地源码入口："
            f"{data_js}\n"
            "请确认 Workspace/Resources/tool/codbash 已存在，或使用 --codbash-root 指定路径。"
        )


def load_from_codbash(paths: AdapterPaths, mode: str, session_id: str = "") -> dict[str, Any]:
    require_codbash_root(paths.codbash_root)
    script = r"""
const path = require('path');
const root = process.argv[1];
const mode = process.argv[2] || 'list';
const sessionId = process.argv[3] || '';
const data = require(path.join(root, 'src', 'data.js'));

function enrich(session, includeDetail) {
  const out = {
    id: session.id || '',
    tool: session.tool || 'unknown',
    project: session.project || '',
    project_short: session.project_short || '',
    session_name: session.session_name || '',
    first_message: session.first_message || '',
    first_ts: session.first_ts || 0,
    last_ts: session.last_ts || 0,
    first_time: session.first_time || '',
    last_time: session.last_time || '',
    messages: session.messages || 0,
    detail_messages: session.detail_messages || 0,
    user_messages: session.user_messages || 0,
    git_root: session.git_root || '',
    file_size: session.file_size || 0,
    source_path: '',
    source_format: '',
    detail: { messages: [] }
  };
  try {
    const found = data.findSessionFile(session.id, session.project || '');
    if (found) {
      out.source_path = found.file || '';
      out.source_format = found.format || '';
    }
  } catch (err) {
    out.source_error = String(err && err.message || err);
  }
  if (includeDetail) {
    try {
      const detail = data.loadSessionDetail(session.id, session.project || '');
      out.detail = { messages: detail.messages || [] };
    } catch (err) {
      out.detail_error = String(err && err.message || err);
    }
  }
  return out;
}

const sessions = data.loadSessions();
if (mode === 'get') {
  const match = sessions.find(s => s.id === sessionId || (s.id || '').startsWith(sessionId));
  if (!match) {
    console.error(JSON.stringify({ error: 'session not found', session_id: sessionId }));
    process.exit(2);
  }
  console.log(JSON.stringify({ session: enrich(match, true) }));
} else {
  console.log(JSON.stringify({ sessions: sessions.map(s => enrich(s, false)) }));
}
"""
    result = run_command(["node", "-e", script, str(paths.codbash_root), mode, session_id], cwd=paths.repo_root)
    if result.returncode != 0:
        stderr = result.stderr.strip()
        raise SessionAdapterError(f"调用 codbash 失败：{stderr or result.stdout.strip()}")
    try:
        return json.loads(result.stdout)
    except json.JSONDecodeError as exc:
        raise SessionAdapterError(f"codbash 输出不是 JSON：{exc}") from exc


def is_session_in_repo(session: dict[str, Any], repo: Path) -> bool:
    repo = repo.resolve()
    candidates = [session.get("git_root"), session.get("project")]
    for value in candidates:
        if not value:
            continue
        try:
            path = Path(str(value)).expanduser().resolve()
        except OSError:
            continue
        if path == repo:
            return True
        try:
            path.relative_to(repo)
            return True
        except ValueError:
            pass
    return False


def normalize_source_tool(tool: str) -> str:
    if tool == "claude-ext":
        return "claude"
    if tool in SUPPORTED_SOURCE_TOOLS:
        return tool
    return tool or "unknown"


def session_title(session: dict[str, Any]) -> str:
    raw = (session.get("session_name") or session.get("first_message") or "").strip()
    first_line = next((line.strip() for line in raw.splitlines() if line.strip()), "")
    return first_line or f"session-{session.get('id', 'unknown')}"


def slugify(text: str, max_chars: int = 36) -> str:
    cleaned = re.sub(r"[\x00-\x1f\x7f]", "", text).strip().lower()
    cleaned = re.sub(r"[\\/:*?\"<>|`$!#]+", "", cleaned)
    cleaned = re.sub(r"\s+", "-", cleaned)
    cleaned = re.sub(r"-{2,}", "-", cleaned).strip("-._ ")
    if not cleaned:
        cleaned = "session"
    return cleaned[:max_chars].strip("-._ ") or "session"


def parse_timestamp(value: Any) -> datetime | None:
    if not value:
        return None
    if isinstance(value, (int, float)):
        number = float(value)
        if number > 10_000_000_000:
            number = number / 1000
        return datetime.fromtimestamp(number, tz=timezone.utc).astimezone()
    if not isinstance(value, str):
        return None
    text = value.strip()
    if not text:
        return None
    if text.endswith("Z"):
        text = text[:-1] + "+00:00"
    try:
        return datetime.fromisoformat(text).astimezone()
    except ValueError:
        return None


def local_dt_from_ms(value: Any) -> datetime:
    try:
        ms = int(value)
    except (TypeError, ValueError):
        ms = 0
    if ms <= 0:
        return datetime.now().astimezone()
    return datetime.fromtimestamp(ms / 1000).astimezone()


def iso_from_ms(value: Any) -> str:
    return local_dt_from_ms(value).isoformat(timespec="seconds")


def sidecar_name(session: dict[str, Any]) -> str:
    started = local_dt_from_ms(session.get("first_ts"))
    source_tool = normalize_source_tool(str(session.get("tool") or "unknown"))
    slug = slugify(session_title(session))
    session_id = str(session.get("id") or "unknown")
    short_id = session_id.split("-")[0] if "-" in session_id else session_id[:8]
    return f"{started:%Y-%m-%d-%H%M}-{source_tool}-{slug}-{short_id}.md"


def full_export_name(started: datetime, title: str, session_id: str) -> str:
    short_id = re.sub(r"[^0-9a-zA-Z]", "", session_id)[:13] or "unknown"
    return f"{started:%Y-%m-%d-%H%M}-codex-{slugify(title, 48)}-{short_id}.md"


def truncate(text: str, limit: int = 900) -> str:
    text = (text or "").strip()
    if len(text) <= limit:
        return text
    return text[: limit - 20].rstrip() + "\n\n... [truncated]"


def clean_snippet(text: str, limit: int = 180) -> str:
    return truncate(re.sub(r"\s+", " ", text or "").strip(), limit)


def compact_text(text: str, limit: int = 48) -> str:
    cleaned = re.sub(r"\s+", " ", text or "").strip()
    return cleaned[:limit].rstrip() if len(cleaned) > limit else cleaned


def strip_transcript_heading_timestamps(text: str) -> str:
    """去掉可见 transcript 标题里的逐条时间戳，保留编号和记录类型。"""
    return TRANSCRIPT_HEADING_TIMESTAMP_RE.sub(r"\1\2", text)


def is_bootstrap_message(text: str) -> bool:
    stripped = (text or "").lstrip()
    return (
        stripped.startswith("# AGENTS.md instructions")
        or stripped.startswith("<permissions instructions>")
        or stripped.startswith("<collaboration_mode>")
        or stripped.startswith("<skill>\n<name>")
    )


def is_resume_boilerplate_message(text: str) -> bool:
    """识别 Codex resume/compact 注入，避免把恢复胶囊当用户目标。"""
    return bool(RESUME_BOILERPLATE_RE.search(text or ""))


def normalize_conversation_text(text: str) -> str:
    """把相邻消息比较压缩成稳定 key，用于去重而不改变原文输出。"""
    return re.sub(r"\s+", " ", text or "").strip()


def is_continue_only_text(text: str) -> bool:
    """识别纯 continue；它可以保留为请求记录，但不覆盖真实 User Goal。"""
    return bool(CONTINUE_ONLY_RE.match(text or ""))


def dedupe_adjacent_conversation(items: list[dict[str, Any]]) -> list[dict[str, Any]]:
    """只去重相邻重复消息，保留非相邻重复作为真实对话证据。"""
    deduped: list[dict[str, Any]] = []
    last_key = ""
    for item in items:
        key = normalize_conversation_text(str(item.get("text") or ""))
        if key and key == last_key:
            continue
        deduped.append(item)
        last_key = key
    return deduped


def choose_user_goal(user_requests: list[dict[str, Any]], fallback: str = "") -> str:
    """选择 AI digest 的真实目标：优先最后一个非 boilerplate、非纯 continue 请求。"""
    for item in reversed(user_requests):
        text = str(item.get("text") or "").strip()
        if text and not is_continue_only_text(text) and not is_resume_boilerplate_message(text):
            return text
    for item in reversed(user_requests):
        text = str(item.get("text") or "").strip()
        if text:
            return text
    return "" if is_resume_boilerplate_message(fallback) else (fallback or "")


def is_final_assistant_message(text: str, phase: str = "") -> bool:
    """优先使用 Codex phase=final；缺 phase 时只接受较窄的最终结论表达。"""
    if str(phase or "").lower() == "final":
        return True
    return bool(FINAL_CONCLUSION_RE.search(text or ""))


def choose_outcome(assistant_messages: list[dict[str, Any]]) -> str:
    """Outcome 只能来自 final-like assistant；找不到时显式写 incomplete。"""
    for item in reversed(assistant_messages):
        if is_final_assistant_message(str(item.get("text") or ""), str(item.get("phase") or "")):
            return str(item.get("text") or "").strip() or "incomplete"
    return "incomplete"


def first_sdd_command(command: str) -> str:
    """抽取 sdd.py 子命令，支持 `python3 Workspace/SDD/sdd.py validate` 形式。"""
    match = re.search(r"(?:^|\s)(?:python3\s+)?(?:\S*/)?sdd\.py\s+([a-z-]+)\b", command or "", re.IGNORECASE)
    return match.group(1).lower() if match else ""


def classify_command_category(tool_name: str, command: str) -> str:
    """按 SDD-0041 新契约把工具命令分类到稳定类别。"""
    text = (command or "").strip()
    lower = text.lower()
    if tool_name == "apply_patch" or "apply_patch" in lower or re.search(r"^\*\*\* (update|add|delete|move) file", text, re.MULTILINE | re.IGNORECASE):
        return "edit"

    sdd_command = first_sdd_command(text)
    if sdd_command in SDD_READ_COMMANDS:
        return "read"
    if sdd_command in SDD_VALIDATION_COMMANDS:
        return "validation"
    if sdd_command in SDD_WRITE_COMMANDS:
        return "sdd_state_write"

    if re.search(r"\bgit\s+(add|commit|push|merge|rebase|checkout|switch|reset|clean|tag)\b", lower):
        return "git_write"
    if re.search(r"\bgit\s+(status|diff|log|show|rev-parse|branch)\b", lower):
        return "git_inspection"

    if re.search(r"\b(command\s+-v|which\s+|type\s+|[^;&|]+\s+--help\b|[^;&|]+\s+-h\b|[^;&|]+\s+--version\b)", lower):
        return "external_probe"
    if re.search(r"\b(pytest|unittest|py_compile|dotnet\s+build|validate-dataos|validate\b|lint\.sh|skill-test|run-godot-scene|analyze-godot|build-solutions)\b", lower):
        return "validation"
    if re.search(r"\b(cat\s+>|tee\s+|write_text|open\(.*['\"]w|sed\s+-i|dotnet\s+format|ruff\s+.*--fix|sync-ai-config\.sh)\b", lower):
        return "edit"
    if re.search(r"^(sed|rg|find|ls|cat|jq|wc|nl|tree|eza|bat|head|tail)\b", lower):
        return "read"
    return "unknown"


def classify_failure_category(call: CodexToolCall) -> str:
    """把失败工具调用归因到可行动类别，供 Retrospective 直接消费。"""
    output = (call.output or "").lower()
    command = (call.command or "").lower()
    combined = f"{command}\n{output}"
    if "no such file or directory" in combined or "can't read" in combined or "cannot access" in combined:
        return "path_error"
    if (
        "invalid context" in combined
        or "patch failed" in combined
        or "could not apply patch" in combined
        or "failed to find expected lines" in combined
    ):
        return "patch_context_mismatch"
    if "unrecognized arguments" in combined or re.search(r"\busage:", combined):
        return "command_misuse"
    if "command not found" in combined or "not found:" in combined:
        return "tool_unavailable"
    if "timed out" in combined or "timeout" in combined:
        return "timeout"
    if "protocol mismatch" in combined or "fetch failed" in combined or "network" in combined:
        return "network_or_fetch"
    if command.startswith("rg ") and call.failure_reason.startswith("exit code 1"):
        return "search_no_result"
    if call.command_category == "validation" or re.search(r"\b(build|py_compile|pytest|unittest|lint)\b", command):
        return "build_failure"
    return "unknown"


def retry_key(call: CodexToolCall) -> str:
    """同一命令视为同一恢复目标；后续可扩展为 path-level target。"""
    if call.tool_name == "apply_patch":
        patch_paths = sorted(extract_paths(call.output)) or sorted(call.files_modified)
        if patch_paths:
            return f"apply_patch:{patch_paths[0].lower()}"
        return f"apply_patch:{call.event_index}"
    return re.sub(r"\s+", " ", call.command or "").strip().lower()


def annotate_failure_analysis(tool_calls_detail: list[CodexToolCall], final_conclusion: bool) -> None:
    """补全 failure_category/retry/recovered/final_impact，避免 failure 文件只有 raw output。"""
    for index, call in enumerate(tool_calls_detail):
        if call.status != "failed":
            continue
        key = retry_key(call)
        later_same = [later for later in tool_calls_detail[index + 1 :] if retry_key(later) == key]
        later_success = any(later.status == "success" for later in later_same)
        call.failure_category = classify_failure_category(call)
        call.retry_count = len(later_same)
        if later_success:
            call.recovered = "yes"
            call.final_impact = "worked_around"
        elif final_conclusion and (call.command_category == "external_probe" or call.failure_category == "search_no_result"):
            call.recovered = "unknown"
            call.final_impact = "not_relevant"
        elif final_conclusion:
            call.recovered = "unknown"
            call.final_impact = "unknown"
        else:
            call.recovered = "no"
            call.final_impact = "blocked"


def extract_content_text(content: Any) -> str:
    if isinstance(content, str):
        return content
    if not isinstance(content, list):
        return ""
    parts: list[str] = []
    for item in content:
        if not isinstance(item, dict):
            continue
        text = item.get("text")
        if isinstance(text, str):
            parts.append(text)
    return "\n".join(parts)


def choose_fence(text: str) -> str:
    fence = "```"
    while fence in text:
        fence += "`"
    return fence


def fenced(text: Any, language: str = "text") -> str:
    if isinstance(text, str):
        rendered = strip_transcript_heading_timestamps(text)
    else:
        rendered = json.dumps(sanitize_hidden_payload(text), ensure_ascii=False, indent=2)
    fence = choose_fence(rendered)
    return f"{fence}{language}\n{rendered}\n{fence}"


def sha256_text(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8", errors="replace")).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def sanitize_hidden_payload(value: Any) -> Any:
    if isinstance(value, dict):
        sanitized: dict[str, Any] = {}
        for key, item in value.items():
            if key in ENCRYPTED_PLACEHOLDER_KEYS and isinstance(item, str):
                sanitized[key] = {
                    "omitted": "encrypted content is not readable in Markdown export",
                    "bytes": len(item.encode("utf-8", errors="replace")),
                    "sha256": sha256_text(item),
                }
            else:
                sanitized[key] = sanitize_hidden_payload(item)
        return sanitized
    if isinstance(value, list):
        return [sanitize_hidden_payload(item) for item in value]
    if isinstance(value, str):
        return strip_transcript_heading_timestamps(value)
    return value


def render_content(content: Any) -> str:
    if isinstance(content, str):
        return strip_transcript_heading_timestamps(content)
    if not isinstance(content, list):
        return fenced(sanitize_hidden_payload(content), "json")

    sections: list[str] = []
    for index, item in enumerate(content, 1):
        if not isinstance(item, dict):
            sections.append(fenced(sanitize_hidden_payload(item), "json"))
            continue
        item_type = str(item.get("type") or f"part-{index}")
        text = item.get("text")
        if isinstance(text, str):
            sections.append(strip_transcript_heading_timestamps(text))
            continue
        sections.append(f"Content part `{item_type}`:\n\n{fenced(sanitize_hidden_payload(item), 'json')}")
    return "\n\n".join(sections)


def render_json_record(title: str, value: Any) -> str:
    return f"{title}\n\n{fenced(sanitize_hidden_payload(value), 'json')}"


def detail_messages(session: dict[str, Any]) -> list[dict[str, Any]]:
    detail = session.get("detail") or {}
    messages = detail.get("messages") or []
    return [m for m in messages if isinstance(m, dict)]


def first_message(session: dict[str, Any], messages: list[dict[str, Any]]) -> str:
    for message in messages:
        if message.get("role") == "user" and not str(message.get("content", "")).startswith("<turn_aborted>"):
            return str(message.get("content") or "")
    return str(session.get("first_message") or "")


def last_message(messages: list[dict[str, Any]], role: str) -> str:
    for message in reversed(messages):
        content = str(message.get("content") or "")
        if message.get("role") == role and not content.startswith("<turn_aborted>"):
            return content
    return ""


def collect_tool_evidence(messages: list[dict[str, Any]]) -> list[str]:
    evidence: list[str] = []
    seen: set[str] = set()
    for message in messages:
        for tool in message.get("tools") or []:
            if not isinstance(tool, dict):
                continue
            label = ":".join(str(tool.get(k, "")) for k in ("type", "server", "tool") if tool.get(k))
            if label and label not in seen:
                seen.add(label)
                evidence.append(f"- `{label}`")
    if evidence:
        return evidence[:20]
    return [
        "- Summary-level export did not expose complete tool calls.",
        "- For Codex high-fidelity tool outputs, run `codlogs-sessions --md <session.jsonl> --include-tool-results` when `source_path` is available.",
    ]


def collect_snippets(messages: list[dict[str, Any]], patterns: list[str], limit: int = 6) -> list[str]:
    regex = re.compile("|".join(patterns), re.IGNORECASE)
    snippets: list[str] = []
    for message in messages:
        content = str(message.get("content") or "")
        if message.get("role") == "user" and len(content) > 1000:
            continue
        if regex.search(content):
            snippets.append(f"- {clean_snippet(content)}")
        if len(snippets) >= limit:
            break
    return snippets


def markdown_for_session(session: dict[str, Any], sidecar_rel: str) -> str:
    messages = detail_messages(session)
    title = session_title(session)
    source_tool = normalize_source_tool(str(session.get("tool") or "unknown"))
    source_path = str(session.get("source_path") or "")
    evidence_level = "summary"
    validation = collect_snippets(
        messages,
        ["validate", "py_compile", "pytest", "unittest", "dotnet build", "git diff --check", "Checks:"],
    )
    decisions = collect_snippets(messages, ["裁决", "结论", "Conclusion", "Decision", "默认方案", "推荐方案"], limit=6)
    final_state = last_message(messages, "assistant")
    latest_request = last_message(messages, "user")
    first_prompt = first_message(session, messages)

    if not validation:
        validation = ["- not recorded in summary-level export"]
    if not decisions:
        decisions = ["- not recorded in summary-level export"]

    source_locator = source_path or "unknown"
    codlogs_hint = ""
    if source_tool == "codex" and source_path:
        codlogs_hint = (
            "\n- Codex high-fidelity hint: "
            f"`node Workspace/Resources/tool/codlogs/codlogs-sessions.cjs --md {source_path} --include-tool-results`"
        )
    if source_tool == "opencode":
        codlogs_hint = "\n- OpenCode export hint: `opencode export <sessionID>`"

    lines = [
        f"# {title}",
        "",
        "## Metadata",
        "",
        f"- Source Tool: `{source_tool}`",
        "- Source Adapter: `codbash`",
        f"- Session ID: `{session.get('id', '')}`",
        f"- Source Path: `{source_locator}`",
        f"- CWD: `{session.get('project') or session.get('git_root') or ''}`",
        f"- Started: {iso_from_ms(session.get('first_ts'))}",
        f"- Updated: {iso_from_ms(session.get('last_ts'))}",
        f"- Evidence Level: `{evidence_level}`",
        f"- ChatHistory Path: `{sidecar_rel}`",
        codlogs_hint,
        "",
        "## First Prompt",
        "",
        truncate(first_prompt, 1400) or "not recorded",
        "",
        "## User Goal",
        "",
        clean_snippet(first_prompt, 500) or "not recorded",
        "",
        "## Decisions",
        "",
        *decisions,
        "",
        "## Tool Evidence",
        "",
        *collect_tool_evidence(messages),
        "",
        "## Key Command Outputs",
        "",
        "- not stored in full; this sidecar intentionally keeps only summary-level evidence.",
        "",
        "## Files Touched",
        "",
        "- unknown from summary-level export",
        "",
        "## Validation Evidence",
        "",
        *validation,
        "",
        "## Open Questions",
        "",
        "- Review source session or SDD progress for unresolved user decisions.",
        "",
        "## Final State",
        "",
        truncate(final_state, 1200) or "not recorded",
        "",
        "## Resume Prompt",
        "",
        "继续这个会话时，先读取本文件和 source session locator；不要把本 sidecar 当完整 transcript。",
        "",
        f"- Session: `{source_tool}:{session.get('id', '')}`",
        f"- Latest Request: {clean_snippet(latest_request, 300) or 'not recorded'}",
    ]
    return "\n".join(line for line in lines if line is not None).rstrip() + "\n"


def read_index(index_path: Path) -> dict[str, Any]:
    if not index_path.exists():
        return {"schema_version": 1, "updated_at": "", "entries": []}
    try:
        data = json.loads(index_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise SessionAdapterError(f"ChatHistory index JSON 无法解析：{index_path}: {exc}") from exc
    if not isinstance(data.get("entries"), list):
        data["entries"] = []
    return data


def write_index_entry(paths: AdapterPaths, session: dict[str, Any], sidecar_path: Path) -> None:
    index_path = paths.chat_root / "index.json"
    data = read_index(index_path)
    source_tool = normalize_source_tool(str(session.get("tool") or "unknown"))
    entry_id = f"{source_tool}:{session.get('id', '')}"
    sidecar_rel = repo_relative(sidecar_path)
    entry = {
        "id": entry_id,
        "source_tool": source_tool,
        "source_adapter": "codbash",
        "session_id": session.get("id", ""),
        "title": session_title(session),
        "slug": slugify(session_title(session)),
        "cwd": session.get("project") or session.get("git_root") or "",
        "started_at": iso_from_ms(session.get("first_ts")),
        "updated_at": iso_from_ms(session.get("last_ts")),
        "chat_history_path": sidecar_rel,
        "source_path": session.get("source_path") or "",
        "evidence_level": "summary",
        "tags": ["systemagent", "session-adapter"],
    }
    entries = [item for item in data["entries"] if item.get("id") != entry_id]
    entries.append(entry)
    entries.sort(key=lambda item: item.get("updated_at", ""), reverse=True)
    data["entries"] = entries
    data["updated_at"] = datetime.now().astimezone().isoformat(timespec="seconds")
    index_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def upsert_index_entry(chat_root: Path, entry: dict[str, Any]) -> None:
    index_path = chat_root / "index.json"
    data = read_index(index_path)
    entries = [item for item in data["entries"] if item.get("id") != entry.get("id")]
    entries.append(entry)
    entries.sort(key=lambda item: item.get("updated_at", item.get("started_at", "")), reverse=True)
    data["schema_version"] = max(int(data.get("schema_version") or 1), int(entry.get("schema_version") or 2))
    data["entries"] = entries
    data["updated_at"] = datetime.now().astimezone().isoformat(timespec="seconds")
    index_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def relative_chat_path(path: Path, chat_root: Path) -> str:
    """ChatHistory index 固定写 repo 相对路径，临时根用于测试时仍保持同样形状。"""
    try:
        relative = path.resolve().relative_to(chat_root.resolve())
        return str(Path("Workspace/DocsAI/ChatHistory") / relative)
    except ValueError:
        return repo_relative(path)


def short_session_id(session_id: str) -> str:
    # Codex UUID 前 8 位可能在同一分钟连续会话中重复；13 位保持可读且避免 folder 覆盖。
    return re.sub(r"[^0-9a-zA-Z]", "", session_id)[:13] or "unknown"


def parse_tool_arguments(arguments: Any) -> Any:
    if isinstance(arguments, str):
        text = arguments.strip()
        if not text:
            return ""
        try:
            return json.loads(text)
        except json.JSONDecodeError:
            return arguments
    return arguments


def tool_command(tool_name: str, arguments: Any) -> str:
    parsed = parse_tool_arguments(arguments)
    if isinstance(parsed, dict):
        cmd = parsed.get("cmd") or parsed.get("command")
        if isinstance(cmd, str) and cmd.strip():
            return cmd.strip()
        if tool_name == "apply_patch":
            return "apply_patch"
        compact = json.dumps(parsed, ensure_ascii=False, sort_keys=True)
        return clean_snippet(compact, 240)
    if isinstance(parsed, str):
        if tool_name == "apply_patch":
            first = next((line for line in parsed.splitlines() if line.startswith("*** ")), "")
            return first or "apply_patch"
        return clean_snippet(parsed, 240)
    if tool_name == "apply_patch":
        return "apply_patch"
    return clean_snippet(str(parsed), 240)


def extract_text_from_payload(payload: dict[str, Any]) -> str:
    payload_type = str(payload.get("type") or "")
    if payload_type in {"user_message", "agent_message"}:
        return str(payload.get("message") or "")
    if payload_type == "message":
        return extract_content_text(payload.get("content"))
    if payload_type == "function_call_output":
        return str(payload.get("output") or "")
    if payload_type in {"function_call", "custom_tool_call"}:
        return tool_command(str(payload.get("name") or payload.get("tool_name") or "unknown"), payload.get("arguments"))
    return ""


def is_meaningful_user_text(text: str) -> bool:
    stripped = (text or "").strip()
    if not stripped:
        return False
    if stripped.startswith("<turn_aborted>"):
        return False
    if is_resume_boilerplate_message(stripped):
        return False
    return not is_bootstrap_message(stripped)


def detect_tool_status(tool_name: str, output: str) -> tuple[str, str]:
    text = output or ""
    if not text.strip():
        return "unknown", "no output"
    if "[truncated" in text.lower() or "output truncated" in text.lower():
        return "unknown", "output truncated"
    for regex in (PROCESS_EXIT_RE, EXIT_CODE_RE):
        match = regex.search(text)
        if match:
            code = int(match.group(1))
            if code == 0:
                return "success", "exit code 0"
            return "failed", f"exit code {code}"
    if FAILED_TEXT_RE.search(text):
        return "failed", clean_snippet(FAILED_TEXT_RE.search(text).group(0), 120)  # type: ignore[union-attr]
    if SUCCESS_TEXT_RE.search(text):
        return "success", "success marker"
    return "unknown", "no stable status marker"


def classify_phase(text: str, payload_type: str) -> str:
    lower = (text or "").lower()
    if is_bootstrap_message(text) or payload_type in {"session_meta", "turn_context"}:
        return "bootstrap"
    if "deepthink" in lower or "深度思考" in text:
        return "deepthink"
    if "plan" in lower or "计划" in text or "design" in lower:
        return "planning"
    if "validate" in lower or "py_compile" in lower or "验证" in text:
        return "validation"
    if "apply_patch" in lower or "修改" in text or "实现" in text:
        return "implementation"
    if payload_type in {"turn_aborted", "thread_rolled_back"}:
        return "interrupted"
    return "unknown"


def extract_paths(text: str) -> set[str]:
    paths: set[str] = set()
    for match in PATH_RE.finditer(text or ""):
        value = match.group(0).strip("`'\"()[]{}<>.,;:")
        if len(value) < 3:
            continue
        if value.startswith("http"):
            continue
        paths.add(value)
    return paths


def extract_sdd_ids(text: str) -> set[str]:
    return {match.upper() for match in re.findall(r"\bSDD-\d{4}\b", text or "", re.IGNORECASE)}


def add_file_entry(files: dict[str, dict[str, Any]], path: str, access: str, evidence: str, raw_ref: str) -> None:
    item = files.setdefault(path, {"path": path, "read": 0, "modified": 0, "inferred": 0, "evidence": []})
    if access == "read":
        item["read"] += 1
    elif access == "modified":
        item["modified"] += 1
    elif access == "inferred":
        item["inferred"] += 1
    if len(item["evidence"]) < 5:
        item["evidence"].append({"access": access, "evidence": evidence, "raw_ref": raw_ref})


def derive_topics(title: str, texts: list[str], files: set[str], sdd_ids: set[str]) -> list[str]:
    joined = " ".join([title, *texts[:8], " ".join(sorted(files)[:20])]).lower()
    topics: list[str] = []
    candidates = [
        ("systemagent", ["systemagent", "workspace/systemagent"]),
        ("session-adapter", ["session-adapter", "chat-history", "chathistory"]),
        ("sdd", ["sdd-", "workspace/sdd", "sdd/project"]),
        ("docsai", ["docsai"]),
        ("ecs", ["src/ecs", "ecs/"]),
        ("dataos", ["dataos"]),
        ("log", ["logger", "logctl", "10.log"]),
        ("godot", ["godot", ".tscn"]),
        ("codex", ["codex", ".codex"]),
    ]
    for topic, markers in candidates:
        if any(marker in joined for marker in markers):
            topics.append(topic)
    for sdd_id in sorted(sdd_ids):
        topics.append(sdd_id.lower())
    return list(dict.fromkeys(topics))[:12]


def choose_topic_slug(title: str, topics: list[str], sdd_ids: list[str], files: set[str]) -> str:
    if sdd_ids:
        tail = ""
        if "log" in topics:
            tail = "-log"
        elif "session-adapter" in topics:
            tail = "-session-adapter"
        return slugify(f"{sdd_ids[0].lower()}{tail}", 48)
    if "session-adapter" in topics:
        return "chat-history-digest"
    for path in sorted(files):
        if "session-adapter" in path:
            return "chat-history-digest"
        if "10.Log" in path or "Logger" in path:
            return "log"
    return slugify(title, 48)


def event_summary(text: str, fallback: str) -> str:
    return clean_snippet(text, 220) or fallback


def build_base_event(
    *,
    metadata: dict[str, Any],
    event_index: int,
    record_type: str,
    payload_type: str,
    event_kind: str,
    role: str = "",
    text: str = "",
    tool_name: str = "",
    tool_status: str = "not_applicable",
    command_category: str = "unknown",
    evidence_level: str = "explicit",
) -> dict[str, Any]:
    raw_ref = f"raw/transcript.visible.md#{event_index:06d}"
    return {
        "schema_version": 2,
        "session_id": metadata["session_id"],
        "event_index": event_index,
        "source_record_type": record_type,
        "source_payload_type": payload_type,
        "role": role,
        "event_kind": event_kind,
        "phase": classify_phase(text, payload_type),
        "summary": event_summary(text, event_kind),
        "text_ref": raw_ref,
        "tool_name": tool_name,
        "tool_status": tool_status,
        "command_category": command_category,
        "evidence_level": evidence_level,
    }


def find_codex_session(session_or_path: str, source_root: Path = DEFAULT_CODEX_MONTH_ROOT) -> Path:
    candidate = Path(session_or_path).expanduser()
    if candidate.exists():
        return candidate.resolve()
    search_root = source_root.expanduser().resolve()
    if not search_root.exists():
        raise SessionAdapterError(f"Codex source root 不存在：{search_root}")
    matches = sorted(search_root.glob(f"**/*{session_or_path}*.jsonl"))
    if not matches:
        matches = sorted(path for path in search_root.glob("**/rollout-*.jsonl") if session_or_path in path.stem)
    if not matches:
        raise SessionAdapterError(f"找不到 Codex session：{session_or_path}")
    if len(matches) > 1:
        exact = [path for path in matches if path.stem.endswith(session_or_path)]
        if len(exact) == 1:
            return exact[0].resolve()
        raise SessionAdapterError(f"Codex session 匹配多个文件，请传完整路径或更长 id：{session_or_path}")
    return matches[0].resolve()


def legacy_paths_for_session(chat_root: Path, metadata: dict[str, Any]) -> list[str]:
    legacy: list[str] = []
    session_id = metadata["session_id"]
    short = re.sub(r"[^0-9a-zA-Z]", "", session_id)[:13]
    for path in sorted(chat_root.glob(f"**/*{short}*.md")):
        if "/raw/" in str(path) or "/derived/" in str(path):
            continue
        legacy.append(relative_chat_path(path, chat_root))
    return legacy


def detect_verification_loops(tool_calls_detail: list[CodexToolCall]) -> int:
    """检测验证循环次数：edit/SDD state write 后连续 2+ 个 validation signal 算一个循环。"""
    loops = 0
    validation_since_edit = 0
    has_pending_edit = False

    for call in tool_calls_detail:
        if call.code_edit_signal:
            if has_pending_edit and validation_since_edit >= 2:
                loops += 1
            has_pending_edit = True
            validation_since_edit = 0
        elif call.validation_signal and has_pending_edit:
            validation_since_edit += 1

    if has_pending_edit and validation_since_edit >= 2:
        loops += 1

    return loops


def detect_file_read_amplification(
    tool_calls_detail: list[CodexToolCall],
) -> tuple[dict[str, int], list[str]]:
    """统计每个文件的读取次数，返回 (读取次数 dict, 读取>3次的文件列表)。"""
    read_counts: dict[str, int] = {}

    for call in tool_calls_detail:
        if call.code_edit_signal:
            continue
        for path in call.files_read:
            read_counts[path] = read_counts.get(path, 0) + 1

    repeated = sorted(
        [p for p, c in read_counts.items() if c > 3],
        key=lambda p: read_counts[p],
        reverse=True,
    )
    return read_counts, repeated


def analyze_codex_digest(source_path: Path, *, skip_interrupted: bool = False) -> CodexDigestAnalysis:
    metadata = scan_codex_session_metadata(source_path)
    source_sha = sha256_file(source_path)
    source_bytes = source_path.stat().st_size
    source_lines = int(metadata["line_count"])
    records: list[dict[str, Any]] = []
    events: list[dict[str, Any]] = []
    calls_by_id: dict[str, CodexToolCall] = {}
    tool_calls_detail: list[CodexToolCall] = []
    user_requests: list[dict[str, Any]] = []
    assistant_messages_detail: list[dict[str, Any]] = []
    validation_commands: list[dict[str, Any]] = []
    file_entries: dict[str, dict[str, Any]] = {}
    sdd_ids: set[str] = set()
    all_texts: list[str] = []
    noise_bytes = 0
    visible_bytes = 0
    turn_aborted_count = 0
    thread_rolled_back_count = 0
    tool_outputs = 0
    assistant_messages = 0
    final_conclusion = False
    last_assistant_text = ""

    with source_path.open("r", encoding="utf-8", errors="replace") as stream:
        for event_index, line in enumerate(stream, 1):
            try:
                record = json.loads(line)
            except json.JSONDecodeError as exc:
                record = {"type": "json_decode_error", "payload": {"type": "json_decode_error", "message": str(exc)}}
            records.append(record)
            record_type = str(record.get("type") or "unknown")
            payload = record.get("payload")
            if not isinstance(payload, dict):
                events.append(
                    build_base_event(
                        metadata=metadata,
                        event_index=event_index,
                        record_type=record_type,
                        payload_type=record_type,
                        event_kind="unknown",
                    )
                )
                continue

            payload_type = str(payload.get("type") or record_type)
            text = extract_text_from_payload(payload)
            if text:
                all_texts.append(text)
                visible_bytes += len(text.encode("utf-8", errors="replace"))
                sdd_ids.update(extract_sdd_ids(text))
            if (
                record_type == "session_meta"
                or payload_type == "turn_context"
                or is_bootstrap_message(text)
                or "base_instructions" in payload
            ):
                noise_bytes += len(line.encode("utf-8", errors="replace"))

            if payload_type == "turn_aborted" or text.strip().startswith("<turn_aborted>"):
                turn_aborted_count += 1
                events.append(
                    build_base_event(
                        metadata=metadata,
                        event_index=event_index,
                        record_type=record_type,
                        payload_type=payload_type,
                        event_kind="interruption",
                        text=text or "turn_aborted",
                    )
                )
                continue
            if payload_type == "thread_rolled_back":
                thread_rolled_back_count += 1
                events.append(
                    build_base_event(
                        metadata=metadata,
                        event_index=event_index,
                        record_type=record_type,
                        payload_type=payload_type,
                        event_kind="rollback",
                        text=text or "thread_rolled_back",
                    )
                )
                continue

            if payload_type == "user_message":
                if is_meaningful_user_text(text):
                    user_requests.append({"event_index": event_index, "text": text, "raw_ref": f"raw/transcript.visible.md#{event_index:06d}"})
                events.append(
                    build_base_event(
                        metadata=metadata,
                        event_index=event_index,
                        record_type=record_type,
                        payload_type=payload_type,
                        event_kind="user_request" if is_meaningful_user_text(text) else "noise",
                        role="user",
                        text=text,
                        evidence_level="explicit" if is_meaningful_user_text(text) else "weak",
                    )
                )
                continue

            if payload_type == "message":
                role = str(payload.get("role") or "")
                if role == "user":
                    if is_meaningful_user_text(text):
                        user_requests.append(
                            {"event_index": event_index, "text": text, "raw_ref": f"raw/transcript.visible.md#{event_index:06d}"}
                        )
                    event_kind = "user_request" if is_meaningful_user_text(text) else "noise"
                elif role == "assistant":
                    phase = str(payload.get("phase") or "")
                    assistant_messages += 1
                    last_assistant_text = text
                    assistant_messages_detail.append(
                        {
                            "event_index": event_index,
                            "text": text,
                            "phase": phase,
                            "raw_ref": f"raw/transcript.visible.md#{event_index:06d}",
                        }
                    )
                    event_kind = "assistant_message"
                    if is_final_assistant_message(text, phase):
                        final_conclusion = True
                        event_kind = "assistant_final"
                else:
                    event_kind = "unknown"
                events.append(
                    build_base_event(
                        metadata=metadata,
                        event_index=event_index,
                        record_type=record_type,
                        payload_type=payload_type,
                        event_kind=event_kind,
                        role=role,
                        text=text,
                    )
                )
                for path in extract_paths(text):
                    add_file_entry(file_entries, path, "inferred" if role == "assistant" else "read", "message reference", f"raw/transcript.visible.md#{event_index:06d}")
                continue

            if payload_type == "agent_message":
                phase = str(payload.get("phase") or "")
                assistant_messages += 1
                last_assistant_text = text
                assistant_messages_detail.append(
                    {
                        "event_index": event_index,
                        "text": text,
                        "phase": phase,
                        "raw_ref": f"raw/transcript.visible.md#{event_index:06d}",
                    }
                )
                event_kind = "assistant_message"
                if is_final_assistant_message(text, phase):
                    final_conclusion = True
                    event_kind = "assistant_final"
                events.append(
                    build_base_event(
                        metadata=metadata,
                        event_index=event_index,
                        record_type=record_type,
                        payload_type=payload_type,
                        event_kind=event_kind,
                        role="assistant",
                        text=text,
                    )
                )
                for path in extract_paths(text):
                    add_file_entry(file_entries, path, "inferred", "assistant reference", f"raw/transcript.visible.md#{event_index:06d}")
                continue

            if payload_type in {"function_call", "custom_tool_call"}:
                tool_name = str(payload.get("name") or payload.get("tool_name") or "unknown")
                call_id = str(payload.get("call_id") or payload.get("id") or f"call-{event_index}")
                command = tool_command(tool_name, payload.get("arguments"))
                command_category = classify_command_category(tool_name, command)
                raw_ref = f"raw/transcript.visible.md#{event_index:06d}"
                call = CodexToolCall(
                    call_id=call_id,
                    tool_name=tool_name,
                    arguments=payload.get("arguments"),
                    command=command,
                    event_index=event_index,
                    raw_ref=raw_ref,
                    command_category=command_category,
                    validation_signal=command_category == "validation",
                    code_edit_signal=command_category in {"edit", "sdd_state_write"},
                )
                paths = extract_paths(command)
                if call.code_edit_signal:
                    call.files_modified.update(paths)
                    for path in paths:
                        add_file_entry(file_entries, path, "modified", f"{tool_name} call", raw_ref)
                else:
                    call.files_read.update(paths)
                    for path in paths:
                        add_file_entry(file_entries, path, "read", f"{tool_name} call", raw_ref)
                calls_by_id[call_id] = call
                tool_calls_detail.append(call)
                if call.validation_signal:
                    validation_commands.append({"event_index": event_index, "tool": tool_name, "command": command, "status": "unknown", "raw_ref": raw_ref})
                sdd_ids.update(extract_sdd_ids(command))
                events.append(
                    build_base_event(
                        metadata=metadata,
                        event_index=event_index,
                        record_type=record_type,
                        payload_type=payload_type,
                        event_kind="tool_call",
                        role="assistant",
                        text=command,
                        tool_name=tool_name,
                        tool_status="unknown",
                        command_category=command_category,
                    )
                )
                continue

            if payload_type in {"function_call_output", "custom_tool_call_output"}:
                tool_outputs += 1
                call_id = str(payload.get("call_id") or payload.get("id") or "")
                output = str(payload.get("output") or "")
                status, reason = detect_tool_status("", output)
                raw_ref = f"raw/transcript.visible.md#{event_index:06d}"
                call = calls_by_id.get(call_id)
                tool_name = call.tool_name if call else "unknown"
                if call is not None:
                    call.output = output
                    call.status = status
                    call.failure_reason = reason if status == "failed" else ""
                    call.output_event_index = event_index
                    call.output_raw_ref = raw_ref
                    if call.validation_signal:
                        for item in reversed(validation_commands):
                            if item["event_index"] == call.event_index:
                                item["status"] = status
                                item["failure_reason"] = call.failure_reason
                                item["output_raw_ref"] = raw_ref
                                break
                    output_paths = extract_paths(output)
                    if call.code_edit_signal and output_paths:
                        call.files_modified.update(output_paths)
                        for path in output_paths:
                            add_file_entry(file_entries, path, "modified", "tool output", raw_ref)
                events.append(
                    build_base_event(
                        metadata=metadata,
                        event_index=event_index,
                        record_type=record_type,
                        payload_type=payload_type,
                        event_kind="tool_output",
                        role="tool",
                        text=output,
                        tool_name=tool_name,
                        tool_status=status,
                    )
                )
                sdd_ids.update(extract_sdd_ids(output))
                continue

            event_kind = "session_meta" if record_type == "session_meta" else "noise" if payload_type == "turn_context" else "unknown"
            events.append(
                build_base_event(
                    metadata=metadata,
                    event_index=event_index,
                    record_type=record_type,
                    payload_type=payload_type,
                    event_kind=event_kind,
                    text=text,
                    evidence_level="weak" if event_kind == "noise" else "explicit",
                )
            )

    for call in tool_calls_detail:
        if call.output_event_index == 0:
            call.status = "unknown"
            call.failure_reason = "missing output"
        for event in events:
            if event["event_kind"] == "tool_call" and event["event_index"] == call.event_index:
                event["tool_status"] = call.status
                break

    user_requests = dedupe_adjacent_conversation(user_requests)
    assistant_messages_detail = dedupe_adjacent_conversation(assistant_messages_detail)
    final_conclusion = any(
        is_final_assistant_message(str(item.get("text") or ""), str(item.get("phase") or ""))
        for item in assistant_messages_detail
    )
    chosen_goal = choose_user_goal(user_requests, str(metadata.get("first_user") or ""))
    if chosen_goal:
        metadata["title"] = session_title(
            {"session_name": "", "first_message": chosen_goal, "id": metadata["session_id"]}
        )
    annotate_failure_analysis(tool_calls_detail, final_conclusion)

    tool_success_count = sum(1 for call in tool_calls_detail if call.status == "success")
    tool_failed_count = sum(1 for call in tool_calls_detail if call.status == "failed")
    tool_unknown_count = sum(1 for call in tool_calls_detail if call.status == "unknown")
    command_category_counts = {category: 0 for category in COMMAND_CATEGORIES}
    for call in tool_calls_detail:
        command_category_counts[call.command_category] = command_category_counts.get(call.command_category, 0) + 1
    code_edit_explicit = sum(1 for call in tool_calls_detail if call.command_category == "edit")
    sdd_state_write_signals = sum(1 for call in tool_calls_detail if call.command_category == "sdd_state_write")
    change_signals = code_edit_explicit + sdd_state_write_signals
    inferred_edits = sum(
        1
        for message in assistant_messages_detail
        if re.search(r"已修改|已更新|已生成|modified|updated|generated", message["text"], re.IGNORECASE)
    )
    validation_signals = command_category_counts.get("validation", 0)
    interrupted = turn_aborted_count > 0 or thread_rolled_back_count > 0
    meaningful_user_turns = len(user_requests)
    duration_seconds = 0
    if metadata["started"] and metadata["updated"]:
        duration_seconds = max(0, int((metadata["updated"] - metadata["started"]).total_seconds()))
    total_signal_bytes = max(1, visible_bytes + noise_bytes)
    noise_ratio = round(min(1.0, noise_bytes / total_signal_bytes), 2)
    topics = derive_topics(metadata["title"], all_texts, set(file_entries), sdd_ids)
    sdd_id_list = sorted(sdd_ids)
    topic_slug = choose_topic_slug(metadata["title"], topics, sdd_id_list, set(file_entries))

    skip_reasons: list[str] = []
    digest_reasons: list[str] = []
    if (
        meaningful_user_turns < 2
        and len(tool_calls_detail) < 5
        and change_signals == 0
        and validation_signals == 0
        and not final_conclusion
    ):
        skip_reasons.append("too_short")
    if skip_interrupted and interrupted and not final_conclusion and change_signals == 0 and validation_signals == 0:
        skip_reasons.extend(reason for reason in ["interrupted", "no_final_conclusion"] if reason not in skip_reasons)

    if tool_calls_detail:
        digest_reasons.append("has_tool_calls")
    if sdd_id_list:
        digest_reasons.append("has_sdd_reference")
    if final_conclusion:
        digest_reasons.append("has_final_conclusion")
    if validation_signals:
        digest_reasons.append("has_validation_signal")
    if code_edit_explicit:
        digest_reasons.append("has_code_edit_signal")
    if sdd_state_write_signals:
        digest_reasons.append("has_sdd_state_write_signal")
    if interrupted:
        digest_reasons.append("has_interruption_risk")

    digest_status = "locator-only" if skip_reasons else "digest"
    if change_signals and (validation_signals or final_conclusion):
        priority = "high"
    elif final_conclusion or len(tool_calls_detail) >= 5:
        priority = "normal"
    else:
        priority = "low"
    if interrupted and not final_conclusion:
        priority = "low"

    # 效率指标
    verification_loops = detect_verification_loops(tool_calls_detail)
    file_read_counts, repeated_reads_gt3 = detect_file_read_amplification(tool_calls_detail)
    avg_val_per_edit = round(validation_signals / max(1, change_signals), 1) if change_signals > 0 else 0.0

    return CodexDigestAnalysis(
        metadata=metadata,
        source_path=source_path,
        source_sha256=source_sha,
        source_bytes=source_bytes,
        source_lines=source_lines,
        records=records,
        events=events,
        tool_calls_detail=tool_calls_detail,
        user_requests=user_requests,
        assistant_messages_detail=assistant_messages_detail,
        validation_commands=validation_commands,
        file_entries=file_entries,
        sdd_ids=sdd_id_list,
        topics=topics,
        meaningful_user_turns=meaningful_user_turns,
        assistant_messages=assistant_messages,
        tool_calls=len(tool_calls_detail),
        tool_outputs=tool_outputs,
        tool_success_count=tool_success_count,
        tool_failed_count=tool_failed_count,
        tool_unknown_count=tool_unknown_count,
        turn_aborted_count=turn_aborted_count,
        thread_rolled_back_count=thread_rolled_back_count,
        interrupted=interrupted,
        code_edit_signals={"explicit": code_edit_explicit, "inferred": inferred_edits, "unknown": 0},
        sdd_state_write_signals=sdd_state_write_signals,
        command_category_counts=command_category_counts,
        validation_signals=validation_signals,
        final_conclusion=final_conclusion,
        duration_seconds=duration_seconds,
        noise_ratio_estimate=noise_ratio,
        digest_status=digest_status,
        priority=priority,
        skip_reasons=skip_reasons,
        digest_reasons=digest_reasons,
        topic_slug=topic_slug,
        verification_loops=verification_loops,
        file_read_counts=file_read_counts,
        repeated_reads_gt3=repeated_reads_gt3[:10],
        avg_validation_per_edit=avg_val_per_edit,
    )


def codex_digest_folder(chat_root: Path, analysis: CodexDigestAnalysis) -> Path:
    started: datetime = analysis.metadata["started"]
    name = (
        f"{started:%Y-%m-%d-%H-%M}-codex-"
        f"{analysis.topic_slug}-{short_session_id(analysis.metadata['session_id'])}"
    )
    return chat_root / f"{started:%Y}" / f"{started:%m}" / f"{started:%d}" / name


def markdown_list(items: list[str], empty: str = "- none") -> list[str]:
    return items if items else [empty]


def render_source_locator(analysis: CodexDigestAnalysis) -> str:
    lines = [
        "# Source Locator",
        "",
        f"- Source Tool: `codex`",
        f"- Session ID: `{analysis.metadata['session_id']}`",
        f"- Source Path: `{analysis.source_path}`",
        f"- Source SHA256: `{analysis.source_sha256}`",
        f"- Source Bytes: {analysis.source_bytes}",
        f"- Source Lines: {analysis.source_lines}",
        f"- Started: {analysis.metadata['started'].isoformat(timespec='seconds')}",
        f"- Updated: {analysis.metadata['updated'].isoformat(timespec='seconds')}",
        "",
        "## Notes",
        "",
        "- 原始 Codex JSONL 不复制进仓库。",
        "- `raw/transcript.visible.md` 只保留 Codex JSONL 中可见内容。",
        "- `encrypted_content` 不会被解密；需要字节级证据时读取 Source Path。",
    ]
    return "\n".join(lines).rstrip() + "\n"


def render_ai_context(analysis: CodexDigestAnalysis, entry: dict[str, Any]) -> str:
    latest_user = choose_user_goal(analysis.user_requests, str(analysis.metadata.get("first_user") or ""))
    latest_assistant = choose_outcome(analysis.assistant_messages_detail)
    failure_line = (
        f"- Tool failures: {analysis.tool_failed_count} -> `{entry.get('tool_failure_path')}`"
        if analysis.tool_failed_count
        else "- Tool failures: 0"
    )
    interruption = (
        f"- Interrupted: yes, turn_aborted={analysis.turn_aborted_count}, "
        f"thread_rolled_back={analysis.thread_rolled_back_count}"
        if analysis.interrupted
        else "- Interrupted: no"
    )
    lines = [
        f"# {analysis.metadata['title']}",
        "",
        "## Verdict",
        "",
        f"- digest_status: `{analysis.digest_status}`",
        f"- priority: `{analysis.priority}`",
        f"- skip_reasons: {', '.join(analysis.skip_reasons) or 'none'}",
        f"- digest_reasons: {', '.join(analysis.digest_reasons) or 'none'}",
        "",
        "## User Goal",
        "",
        truncate(latest_user or "not recorded", 800),
        "",
        "## Outcome",
        "",
        truncate(latest_assistant or "incomplete", 900),
        "",
        "## Files / SDD / DocsAI",
        "",
        *markdown_list([f"- `{path}`" for path in list(analysis.file_entries)[:12]]),
        *([f"- SDD IDs: {', '.join(analysis.sdd_ids)}"] if analysis.sdd_ids else []),
        "",
        "## Tool And Validation Evidence",
        "",
        f"- Tool calls: {analysis.tool_calls} success={analysis.tool_success_count} failed={analysis.tool_failed_count} unknown={analysis.tool_unknown_count}",
        f"- Validation signals: {analysis.validation_signals}",
        failure_line,
        "",
        "## Interruptions",
        "",
        interruption,
        "",
        "## Resume Prompt",
        "",
        "继续该会话时，先读本文件，再读 `derived/summary.md`、`derived/validation.md` 和必要的失败工具摘要；只有需要完整证据时才读 raw transcript。",
    ]
    return "\n".join(lines).rstrip() + "\n"


def render_summary(analysis: CodexDigestAnalysis) -> str:
    timeline: list[str] = []
    for item in analysis.user_requests[:6]:
        timeline.append(f"- user #{item['event_index']:06d}: {clean_snippet(item['text'], 180)}")
    for item in analysis.assistant_messages_detail[-4:]:
        timeline.append(f"- assistant #{item['event_index']:06d}: {clean_snippet(item['text'], 180)}")
    lines = [
        f"# {analysis.metadata['title']}",
        "",
        "## Metadata",
        "",
        f"- Session ID: `{analysis.metadata['session_id']}`",
        f"- Source: `{analysis.source_path}`",
        f"- Started: {analysis.metadata['started'].isoformat(timespec='seconds')}",
        f"- Updated: {analysis.metadata['updated'].isoformat(timespec='seconds')}",
        f"- Duration Seconds: {analysis.duration_seconds}",
        "",
        "## Digest Gate",
        "",
        f"- Status: `{analysis.digest_status}`",
        f"- Priority: `{analysis.priority}`",
        f"- Skip Reasons: {', '.join(analysis.skip_reasons) or 'none'}",
        f"- Digest Reasons: {', '.join(analysis.digest_reasons) or 'none'}",
        "",
        "## Counts",
        "",
        f"- Meaningful User Turns: {analysis.meaningful_user_turns}",
        f"- Assistant Messages: {analysis.assistant_messages}",
        f"- Tool Calls: {analysis.tool_calls}",
        f"- Tool Outputs: {analysis.tool_outputs}",
        f"- Tool Status: success={analysis.tool_success_count}, failed={analysis.tool_failed_count}, unknown={analysis.tool_unknown_count}",
        f"- Validation Signals: {analysis.validation_signals}",
        f"- Code Edit Signals: {analysis.code_edit_signals}",
        "",
        "## Timeline",
        "",
        *markdown_list(timeline),
        "",
        "## Open Risk",
        "",
        "- 中断会话可能缺少最终交接；详见 `derived/interruptions.md`。",
        "- 工具输出只在 raw transcript 保留完整内容，digest 文件只写摘要和 raw ref。",
    ]
    return "\n".join(lines).rstrip() + "\n"


def render_user_requests(analysis: CodexDigestAnalysis) -> str:
    lines = ["# User Requests", ""]
    for item in analysis.user_requests:
        lines.extend([f"## #{item['event_index']:06d}", "", f"- Raw Ref: `{item['raw_ref']}`", "", truncate(item["text"], 1200), ""])
    if not analysis.user_requests:
        lines.append("- none")
    return "\n".join(lines).rstrip() + "\n"


def render_assistant_results(analysis: CodexDigestAnalysis) -> str:
    lines = ["# Assistant Results", ""]
    for item in analysis.assistant_messages_detail[-12:]:
        kind = "final-like" if is_final_assistant_message(item["text"] or "", str(item.get("phase") or "")) else "message"
        lines.extend(
            [
                f"## #{item['event_index']:06d} `{kind}`",
                "",
                f"- Raw Ref: `{item['raw_ref']}`",
                "",
                truncate(item["text"], 1000),
                "",
            ]
        )
    if not analysis.assistant_messages_detail:
        lines.append("- none")
    return "\n".join(lines).rstrip() + "\n"


def render_tools(analysis: CodexDigestAnalysis) -> str:
    lines = [
        "# Tools",
        "",
        "## Summary",
        "",
        f"- Tool Calls: {analysis.tool_calls}",
        f"- Tool Outputs: {analysis.tool_outputs}",
        f"- Success: {analysis.tool_success_count}",
        f"- Failed: {analysis.tool_failed_count}",
        f"- Unknown: {analysis.tool_unknown_count}",
        "",
        "## Calls",
        "",
        "| Event | Tool | Category | Status | Command / Action | Raw Ref |",
        "| --- | --- | --- | --- | --- | --- |",
    ]
    for call in analysis.tool_calls_detail:
        lines.append(
            f"| {call.event_index:06d} | `{call.tool_name}` | `{call.command_category}` | `{call.status}` | "
            f"{clean_snippet(call.command, 120)} | `{call.raw_ref}` |"
        )
    if not analysis.tool_calls_detail:
        lines.append("| - | - | - | - | - | - |")
    lines.extend(["", "## Notes", "", "- 完整 output 保留在 `raw/transcript.visible.md`；本文件不复制大段输出。"])
    return "\n".join(lines).rstrip() + "\n"


def render_tool_failures(analysis: CodexDigestAnalysis) -> str:
    lines = [
        "# Tool Failures",
        "",
        "## Summary",
        "",
        f"- Failed: {analysis.tool_failed_count}",
        f"- Unknown: {analysis.tool_unknown_count}",
        "",
        "## Failures",
        "",
        "| Event | Tool | Command / Action | Failure Analysis | Exit / Error | Raw Ref |",
        "| --- | --- | --- | --- | --- | --- |",
    ]
    for call in analysis.tool_calls_detail:
        if call.status != "failed":
            continue
        error_summary = call.failure_reason
        output_summary = clean_snippet(call.output, 120)
        if output_summary and output_summary not in error_summary:
            error_summary = f"{error_summary}; {output_summary}" if error_summary else output_summary
        analysis_summary = (
            f"failure_category={call.failure_category}; "
            f"retry_count={call.retry_count}; "
            f"recovered={call.recovered}; "
            f"final_impact={call.final_impact}"
        )
        lines.append(
            f"| {call.output_event_index or call.event_index:06d} | `{call.tool_name}` | "
            f"{clean_snippet(call.command, 100)} | {analysis_summary} | "
            f"{clean_snippet(error_summary, 140)} | `{call.output_raw_ref or call.raw_ref}` |"
        )
    return "\n".join(lines).rstrip() + "\n"


def render_files(analysis: CodexDigestAnalysis) -> str:
    lines = [
        "# Files",
        "",
        "| Path | Read | Modified Explicit | Inferred | Evidence |",
        "| --- | --- | --- | --- | --- |",
    ]
    for path, item in sorted(analysis.file_entries.items()):
        evidence = "; ".join(f"{ev['access']} {ev['raw_ref']}" for ev in item["evidence"][:3])
        lines.append(
            f"| `{path}` | {item['read']} | {item['modified']} | {item['inferred']} | {clean_snippet(evidence, 160)} |"
        )
    if not analysis.file_entries:
        lines.append("| - | 0 | 0 | 0 | none |")
    lines.extend(
        [
            "",
            "## Classification",
            "",
            "- `explicit`: apply_patch、明确写入命令或工具输出显示文件更新。",
            "- `inferred`: assistant/message 提到文件或修改，但缺少直接写入证据。",
            "- `unknown`: 当前 Codex JSONL 无法判断。",
        ]
    )
    return "\n".join(lines).rstrip() + "\n"


def render_validation(analysis: CodexDigestAnalysis) -> str:
    lines = [
        "# Validation",
        "",
        f"- Validation Signals: {analysis.validation_signals}",
        "",
        "| Event | Tool | Status | Command | Failure Summary | Raw Ref |",
        "| --- | --- | --- | --- | --- | --- |",
    ]
    for item in analysis.validation_commands:
        lines.append(
            f"| {item['event_index']:06d} | `{item['tool']}` | `{item.get('status', 'unknown')}` | "
            f"{clean_snippet(item['command'], 120)} | {clean_snippet(item.get('failure_reason', ''), 100)} | "
            f"`{item.get('output_raw_ref') or item['raw_ref']}` |"
        )
    if not analysis.validation_commands:
        lines.append("| - | - | - | not recorded | - | - |")
    if analysis.validation_signals == 0:
        lines.extend(["", "## Unverified", "", "- 未在 Codex JSONL 中检测到稳定验证命令。"])
    return "\n".join(lines).rstrip() + "\n"


def render_interruptions(analysis: CodexDigestAnalysis) -> str:
    interrupted_events = [event for event in analysis.events if event["event_kind"] in {"interruption", "rollback"}]
    lines = [
        "# Interruptions",
        "",
        f"- Interrupted: {str(analysis.interrupted).lower()}",
        f"- turn_aborted_count: {analysis.turn_aborted_count}",
        f"- thread_rolled_back_count: {analysis.thread_rolled_back_count}",
        f"- final_conclusion: {str(analysis.final_conclusion).lower()}",
        f"- code_edit_signals: {analysis.code_edit_signals}",
        f"- validation_signals: {analysis.validation_signals}",
        "",
        "## Events",
        "",
    ]
    lines.extend(
        markdown_list(
            [
                f"- #{event['event_index']:06d} `{event['event_kind']}` {event['summary']} -> `{event['text_ref']}`"
                for event in interrupted_events[:20]
            ]
        )
    )
    return "\n".join(lines).rstrip() + "\n"


def render_noise(analysis: CodexDigestAnalysis) -> str:
    noise_events = [event for event in analysis.events if event["event_kind"] in {"noise", "session_meta", "system_context"}]
    lines = [
        "# Noise",
        "",
        f"- noise_ratio_estimate: {analysis.noise_ratio_estimate}",
        f"- noise_events: {len(noise_events)}",
        "",
        "## Detected Noise",
        "",
        "- `session_meta.base_instructions`",
        "- AGENTS / developer / skill 注入",
        "- `turn_context` 环境上下文",
        "- encrypted reasoning placeholder",
        "- 超大 tool output，完整内容只在 raw transcript 中按需读取",
        "",
        "## Raw Ranges",
        "",
    ]
    lines.extend(markdown_list([f"- #{event['event_index']:06d} `{event['source_payload_type']}`" for event in noise_events[:30]]))
    return "\n".join(lines).rstrip() + "\n"


def render_efficiency(analysis: CodexDigestAnalysis) -> str:
    """生成 derived/efficiency.md：会话效率分析。"""
    code_edit_count = analysis.code_edit_signals.get("explicit", 0)
    change_count = code_edit_count + analysis.sdd_state_write_signals
    # 验证循环明细：找出每个 edit 后的验证序列
    loop_details: list[str] = []
    validation_since_edit = 0
    has_pending_edit = False
    last_edit_event = 0
    last_edit_target = ""

    for call in analysis.tool_calls_detail:
        if call.command_category in {"edit", "sdd_state_write"}:
            if has_pending_edit and validation_since_edit >= 2:
                target = last_edit_target if last_edit_target and last_edit_target != "None" else "apply_patch"
                loop_details.append(
                    f"| {last_edit_event:06d} | {clean_snippet(target, 80)} | "
                    f"{validation_since_edit} | verification loop |"
                )
            has_pending_edit = True
            validation_since_edit = 0
            last_edit_event = call.event_index
            last_edit_target = call.command if call.command and call.command != "None" else call.tool_name
        elif call.validation_signal and has_pending_edit:
            validation_since_edit += 1

    if has_pending_edit and validation_since_edit >= 2:
        target = last_edit_target if last_edit_target and last_edit_target != "None" else "apply_patch"
        loop_details.append(
            f"| {last_edit_event:06d} | {clean_snippet(target, 80)} | "
            f"{validation_since_edit} | verification loop |"
        )

    # 文件读放大明细
    read_detail_lines: list[str] = []
    for path in analysis.repeated_reads_gt3[:10]:
        count = analysis.file_read_counts.get(path, 0)
        read_detail_lines.append(f"| `{path}` | {count} | — | — |")

    # 效率等级标注
    loop_label = "过多" if analysis.verification_loops > 5 else "偏高" if analysis.verification_loops > 3 else "正常"
    read_label = "严重" if len(analysis.repeated_reads_gt3) > 5 else "偏高" if len(analysis.repeated_reads_gt3) > 3 else "正常"
    ratio_label = "过高" if analysis.avg_validation_per_edit > 2.5 else "偏高" if analysis.avg_validation_per_edit > 1.5 else "正常"

    lines = [
        "# Efficiency Analysis",
        "",
        "## Summary",
        "",
        f"- Edit Count: {code_edit_count}",
        f"- SDD State Write Count: {analysis.sdd_state_write_signals}",
        f"- Change Count For Loop: {change_count}",
        f"- Validation Count: {analysis.validation_signals}",
        f"- Avg Validation Per Change: {analysis.avg_validation_per_edit} ({ratio_label})",
        f"- Verification Loops: {analysis.verification_loops} ({loop_label})",
        f"- Repeated File Reads (>3): {len(analysis.repeated_reads_gt3)} ({read_label})",
        f"- Command Categories: {analysis.command_category_counts}",
        "",
        "## Verification Loops",
        "",
        '以下 edit 或 SDD 状态写入触发了 2+ 次连续验证，可能可以合并为"批量修改后统一验证"：',
        "",
        "| Change Event | Change Target | Validation Count | Note |",
        "| --- | --- | --- | --- |",
        *markdown_list(loop_details, empty="| - | - | - | 无验证循环 |"),
        "",
        "## File Read Amplification",
        "",
        "以下文件被读取超过 3 次：",
        "",
        "| File | Read Count | Read Ranges | Note |",
        "| --- | --- | --- | --- |",
        *markdown_list(read_detail_lines, empty="| - | 0 | - | 无读放大 |"),
        "",
        "## Thresholds",
        "",
        "| 指标 | 正常 | 警告 | 异常 |",
        "| --- | --- | --- | --- |",
        "| 验证循环次数 | 0-3 | 3-5 | >5 |",
        "| 同文件读取次数 | 1-3 | 4-5 | >5 |",
        "| 平均验证/变更比 | 1.0-1.5 | 1.5-2.5 | >2.5 |",
        "",
        "## Notes",
        "",
        "- 验证循环不一定是问题：SDD 设计文档等场景确实需要逐步验证。",
        "- 文件读放大不一定是问题：context switch 时重新读取是合理的，但 >5 次值得检查。",
        "- 本文件只记录事实，不做自动优化建议。",
    ]
    return "\n".join(lines).rstrip() + "\n"


def build_codex_digest_entry(
    analysis: CodexDigestAnalysis,
    chat_root: Path,
    folder: Path | None,
    legacy_paths: list[str],
) -> dict[str, Any]:
    entry: dict[str, Any] = {
        "schema_version": 4,
        "id": f"codex:{analysis.metadata['session_id']}",
        "source_tool": "codex",
        "source_adapter": "session-adapter.codex-digest",
        "session_id": analysis.metadata["session_id"],
        "title": analysis.metadata["title"],
        "topic_slug": analysis.topic_slug,
        "legacy_paths": legacy_paths,
        "digest_status": analysis.digest_status,
        "priority": analysis.priority,
        "skip_reasons": analysis.skip_reasons,
        "digest_reasons": analysis.digest_reasons,
        "meaningful_user_turns": analysis.meaningful_user_turns,
        "assistant_messages": analysis.assistant_messages,
        "tool_calls": analysis.tool_calls,
        "tool_outputs": analysis.tool_outputs,
        "tool_success_count": analysis.tool_success_count,
        "tool_failed_count": analysis.tool_failed_count,
        "tool_unknown_count": analysis.tool_unknown_count,
        "tool_failure_path": "",
        "turn_aborted_count": analysis.turn_aborted_count,
        "thread_rolled_back_count": analysis.thread_rolled_back_count,
        "interrupted": analysis.interrupted,
        "code_edit_signals": analysis.code_edit_signals,
        "sdd_state_write_signals": analysis.sdd_state_write_signals,
        "command_category_counts": analysis.command_category_counts,
        "validation_signals": analysis.validation_signals,
        "final_conclusion": analysis.final_conclusion,
        "duration_seconds": analysis.duration_seconds,
        "noise_ratio_estimate": analysis.noise_ratio_estimate,
        "recommended_reading_order": [
            "derived/ai-context.md",
            "derived/summary.md",
            "derived/validation.md",
            "derived/tools.md",
            "raw/transcript.visible.md",
        ],
        "topics": analysis.topics,
        "sdd_ids": analysis.sdd_ids,
        "files_mentioned": list(sorted(analysis.file_entries))[:80],
        "source_path": str(analysis.source_path),
        "source_sha256": analysis.source_sha256,
        "source_bytes": analysis.source_bytes,
        "source_lines": analysis.source_lines,
        "cwd": analysis.metadata["session_meta"].get("cwd", ""),
        "started_at": analysis.metadata["started"].isoformat(timespec="seconds"),
        "updated_at": analysis.metadata["updated"].isoformat(timespec="seconds"),
        "tags": ["systemagent", "session-adapter", "codex", "digest"],
        "efficiency": {
            "verification_loops": analysis.verification_loops,
            "repeated_file_reads": analysis.repeated_reads_gt3[:10],
            "avg_validation_per_edit": analysis.avg_validation_per_edit,
            "edit_count": analysis.code_edit_signals.get("explicit", 0),
            "sdd_state_write_count": analysis.sdd_state_write_signals,
            "validation_count": analysis.validation_signals,
        },
    }
    if folder is not None:
        entry["folder_path"] = relative_chat_path(folder, chat_root)
        entry["digest_path"] = relative_chat_path(folder / "derived/ai-context.md", chat_root)
        entry["raw_transcript_path"] = relative_chat_path(folder / "raw/transcript.visible.md", chat_root)
        if analysis.tool_failed_count > 0:
            entry["tool_failure_path"] = relative_chat_path(folder / "derived/tool-failures.md", chat_root)
    return entry


def write_codex_digest(source_path: Path, chat_root: Path, *, skip_interrupted: bool = False) -> dict[str, Any]:
    chat_root.mkdir(parents=True, exist_ok=True)
    analysis = analyze_codex_digest(source_path, skip_interrupted=skip_interrupted)
    legacy_paths = legacy_paths_for_session(chat_root, analysis.metadata)
    folder = codex_digest_folder(chat_root, analysis) if analysis.digest_status == "digest" else None
    entry = build_codex_digest_entry(analysis, chat_root, folder, legacy_paths)

    if folder is None:
        upsert_index_entry(chat_root, entry)
        return entry

    raw_dir = folder / "raw"
    derived_dir = folder / "derived"
    raw_dir.mkdir(parents=True, exist_ok=True)
    derived_dir.mkdir(parents=True, exist_ok=True)

    manifest = {
        "schema_version": 1,
        "generator": "session-adapter.codex-digest",
        "generated_at": datetime.now().astimezone().isoformat(timespec="seconds"),
        "entry": entry,
    }
    (folder / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    write_codex_full_markdown(source_path, raw_dir / "transcript.visible.md", chat_root)
    (raw_dir / "source-locator.md").write_text(render_source_locator(analysis), encoding="utf-8")
    with (derived_dir / "events.jsonl").open("w", encoding="utf-8", newline="\n") as out:
        for event in analysis.events:
            out.write(json.dumps(event, ensure_ascii=False, sort_keys=True) + "\n")
    (derived_dir / "ai-context.md").write_text(render_ai_context(analysis, entry), encoding="utf-8")
    (derived_dir / "summary.md").write_text(render_summary(analysis), encoding="utf-8")
    (derived_dir / "user-requests.md").write_text(render_user_requests(analysis), encoding="utf-8")
    (derived_dir / "assistant-results.md").write_text(render_assistant_results(analysis), encoding="utf-8")
    (derived_dir / "tools.md").write_text(render_tools(analysis), encoding="utf-8")
    (derived_dir / "files.md").write_text(render_files(analysis), encoding="utf-8")
    (derived_dir / "validation.md").write_text(render_validation(analysis), encoding="utf-8")
    (derived_dir / "interruptions.md").write_text(render_interruptions(analysis), encoding="utf-8")
    (derived_dir / "noise.md").write_text(render_noise(analysis), encoding="utf-8")
    (derived_dir / "efficiency.md").write_text(render_efficiency(analysis), encoding="utf-8")
    if analysis.tool_failed_count > 0:
        (derived_dir / "tool-failures.md").write_text(render_tool_failures(analysis), encoding="utf-8")
    upsert_index_entry(chat_root, entry)
    return entry


def scan_codex_session_metadata(source_path: Path) -> dict[str, Any]:
    session_meta: dict[str, Any] = {}
    first_user = ""
    event_counts: dict[str, int] = {}
    payload_counts: dict[str, int] = {}
    line_count = 0
    first_timestamp: datetime | None = None
    last_timestamp: datetime | None = None

    with source_path.open("r", encoding="utf-8", errors="replace") as stream:
        for line in stream:
            line_count += 1
            try:
                record = json.loads(line)
            except json.JSONDecodeError:
                event_counts["json_decode_error"] = event_counts.get("json_decode_error", 0) + 1
                continue

            record_type = str(record.get("type") or "unknown")
            event_counts[record_type] = event_counts.get(record_type, 0) + 1
            timestamp = parse_timestamp(record.get("timestamp"))
            if timestamp is not None:
                first_timestamp = first_timestamp or timestamp
                last_timestamp = timestamp

            payload = record.get("payload")
            if isinstance(payload, dict):
                payload_type = str(payload.get("type") or "")
                if payload_type:
                    payload_counts[payload_type] = payload_counts.get(payload_type, 0) + 1
                if record_type == "session_meta" and not session_meta:
                    session_meta = payload
                    meta_timestamp = parse_timestamp(payload.get("timestamp"))
                    if meta_timestamp is not None:
                        first_timestamp = first_timestamp or meta_timestamp
                if not first_user and payload.get("type") == "user_message":
                    candidate = str(payload.get("message") or "")
                    if candidate and not is_bootstrap_message(candidate) and not is_resume_boilerplate_message(candidate):
                        first_user = candidate
                if not first_user and payload.get("type") == "message" and payload.get("role") == "user":
                    candidate = extract_content_text(payload.get("content"))
                    if candidate and not is_bootstrap_message(candidate) and not is_resume_boilerplate_message(candidate):
                        first_user = candidate

    session_id = str(session_meta.get("id") or source_path.stem.split("-")[-1])
    started = parse_timestamp(session_meta.get("timestamp")) or first_timestamp or datetime.fromtimestamp(
        source_path.stat().st_mtime
    ).astimezone()
    title = session_title(
        {
            "session_name": session_meta.get("name") or "",
            "first_message": first_user or f"codex-session-{session_id}",
            "id": session_id,
        }
    )
    return {
        "session_id": session_id,
        "title": title,
        "first_user": first_user,
        "started": started,
        "updated": last_timestamp or started,
        "session_meta": session_meta,
        "event_counts": event_counts,
        "payload_counts": payload_counts,
        "line_count": line_count,
    }


def render_codex_record(index: int, record: dict[str, Any]) -> str:
    record_type = str(record.get("type") or "unknown")
    payload = record.get("payload")
    if not isinstance(payload, dict):
        return f"### {index:06d} {record_type}\n\n{fenced(sanitize_hidden_payload(payload), 'json')}"

    payload_type = str(payload.get("type") or record_type)
    header = f"### {index:06d} {payload_type}"

    if record_type == "response_item" and payload_type == "message":
        role = str(payload.get("role") or "unknown")
        body = render_content(payload.get("content"))
        return f"{header} `{role}`\n\n{body or '_empty message_'}"

    if record_type == "response_item" and payload_type == "reasoning":
        summary = payload.get("summary")
        content = payload.get("content")
        encrypted = payload.get("encrypted_content")
        lines = [
            f"{header}",
            "",
            "Reasoning is recorded by Codex as hidden or encrypted payload when it is not explicitly visible.",
        ]
        if summary:
            lines.extend(["", "**Summary**", "", fenced(summary, "json")])
        if content:
            lines.extend(["", "**Visible Content**", "", render_content(content)])
        if isinstance(encrypted, str):
            lines.extend(
                [
                    "",
                    "**Encrypted Content**",
                    "",
                    f"- bytes: {len(encrypted.encode('utf-8', errors='replace'))}",
                    f"- sha256: `{sha256_text(encrypted)}`",
                    "- markdown: omitted because it is encrypted and not useful for AI-visible transcript analysis",
                ]
            )
        return "\n".join(lines)

    if record_type == "response_item" and payload_type == "function_call":
        call_name = payload.get("name") or payload.get("tool_name") or "unknown"
        call_id = payload.get("call_id") or payload.get("id") or ""
        arguments = payload.get("arguments")
        return "\n".join(
            [
                f"{header} `{call_name}`",
                "",
                f"- call_id: `{call_id}`",
                "",
                "**Arguments**",
                "",
                fenced(arguments or "", "json" if isinstance(arguments, (dict, list)) else "text"),
            ]
        )

    if record_type == "response_item" and payload_type == "function_call_output":
        call_id = payload.get("call_id") or payload.get("id") or ""
        output = payload.get("output")
        return "\n".join(
            [
                f"{header}",
                "",
                f"- call_id: `{call_id}`",
                "",
                "**Output**",
                "",
                fenced(output or "", "text"),
            ]
        )

    if record_type == "response_item" and payload_type == "custom_tool_call":
        call_name = payload.get("name") or payload.get("tool_name") or "custom_tool"
        return render_json_record(f"{header} `{call_name}`", payload)

    if record_type == "response_item" and payload_type == "custom_tool_call_output":
        return render_json_record(header, payload)

    if record_type == "event_msg" and payload_type == "agent_message":
        phase = payload.get("phase") or ""
        message = strip_transcript_heading_timestamps(str(payload.get("message") or ""))
        return f"{header} `{phase}`\n\n{message}"

    if record_type == "event_msg" and payload_type == "user_message":
        message = strip_transcript_heading_timestamps(str(payload.get("message") or ""))
        return f"{header}\n\n{message}"

    return render_json_record(header, payload)


def write_codex_full_markdown(source_path: Path, output_path: Path, chat_root: Path) -> dict[str, Any]:
    metadata = scan_codex_session_metadata(source_path)
    source_sha = sha256_file(source_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    rel_path = repo_relative(output_path)

    with output_path.open("w", encoding="utf-8", newline="\n") as out:
        out.write(f"# {metadata['title']}\n\n")
        out.write("## Metadata\n\n")
        out.write("- Source Tool: `codex`\n")
        out.write("- Source Adapter: `session-adapter.codex-full-visible`\n")
        out.write(f"- Session ID: `{metadata['session_id']}`\n")
        out.write(f"- Source Path: `{source_path}`\n")
        out.write(f"- Source SHA256: `{source_sha}`\n")
        out.write(f"- Source Bytes: {source_path.stat().st_size}\n")
        out.write(f"- Source Lines: {metadata['line_count']}\n")
        out.write(f"- CWD: `{metadata['session_meta'].get('cwd', '')}`\n")
        out.write(f"- Started: {metadata['started'].isoformat(timespec='seconds')}\n")
        out.write(f"- Updated: {metadata['updated'].isoformat(timespec='seconds')}\n")
        out.write("- Evidence Level: `visible-transcript`\n")
        out.write(f"- ChatHistory Path: `{rel_path}`\n")
        out.write("\n")
        out.write("## Fidelity Notes\n\n")
        out.write("- 本文件保留 Codex JSONL 中可见的 message、tool call、tool output、event payload 和 turn context，不对可见文本做摘要截断。\n")
        out.write("- Codex 的隐藏推理以 `encrypted_content` 保存时无法还原为可读文本；本导出只保留 bytes 与 sha256，占位不等于完整思考过程。\n")
        out.write("- 原始 JSONL 不复制进仓库；需要字节级完整证据时读取 `Source Path`。\n")
        out.write("\n")
        out.write("## Event Counts\n\n")
        out.write(fenced({"record_types": metadata["event_counts"], "payload_types": metadata["payload_counts"]}, "json"))
        out.write("\n\n## Transcript\n\n")

        rendered_index = 0
        with source_path.open("r", encoding="utf-8", errors="replace") as stream:
            for line_number, line in enumerate(stream, 1):
                try:
                    record = json.loads(line)
                except json.JSONDecodeError as exc:
                    rendered_index += 1
                    out.write(
                        f"### {rendered_index:06d} line-{line_number} json_decode_error\n\n"
                        f"{fenced(str(exc), 'text')}\n\n"
                    )
                    continue
                rendered_index += 1
                out.write(render_codex_record(rendered_index, record))
                out.write("\n\n")

    entry = {
        "id": f"codex:{metadata['session_id']}",
        "source_tool": "codex",
        "source_adapter": "session-adapter.codex-full-visible",
        "session_id": metadata["session_id"],
        "title": metadata["title"],
        "slug": slugify(metadata["title"]),
        "cwd": metadata["session_meta"].get("cwd", ""),
        "started_at": metadata["started"].isoformat(timespec="seconds"),
        "updated_at": metadata["updated"].isoformat(timespec="seconds"),
        "chat_history_path": rel_path,
        "source_path": str(source_path),
        "source_sha256": source_sha,
        "source_bytes": source_path.stat().st_size,
        "source_lines": metadata["line_count"],
        "evidence_level": "visible-transcript",
        "tags": ["systemagent", "session-adapter", "codex", "full-visible"],
    }
    upsert_index_entry(chat_root, entry)
    return entry


def create_sidecar(paths: AdapterPaths, session_id: str) -> Path:
    data = load_from_codbash(paths, "get", session_id)
    session = data["session"]
    paths.chat_root.mkdir(parents=True, exist_ok=True)
    sidecar_path = paths.chat_root / sidecar_name(session)
    sidecar_rel = repo_relative(sidecar_path)
    sidecar_path.write_text(markdown_for_session(session, sidecar_rel), encoding="utf-8")
    write_index_entry(paths, session, sidecar_path)
    return sidecar_path


def command_list(args: argparse.Namespace) -> int:
    repo = resolve_repo(args.repo)
    paths = AdapterPaths(repo_root=REPO_ROOT, codbash_root=args.codbash_root.resolve(), chat_root=args.chat_root.resolve())
    data = load_from_codbash(paths, "list")
    sessions = [s for s in data.get("sessions", []) if isinstance(s, dict)]
    if not args.all:
        sessions = [s for s in sessions if is_session_in_repo(s, repo)]
    sessions = sessions[: args.limit]
    if args.json:
        print(json.dumps({"repo": str(repo), "sessions": sessions}, ensure_ascii=False, indent=2))
        return 0
    print(f"Found {len(sessions)} session(s) for {repo}")
    for session in sessions:
        title = clean_snippet(session_title(session), 56)
        source_tool = normalize_source_tool(str(session.get("tool") or "unknown"))
        print(
            f"{source_tool:<10} {str(session.get('id', ''))[:13]:<13} "
            f"{session.get('last_time', ''):<16} {title}  {session.get('project_short') or session.get('project') or ''}"
        )
    if not sessions:
        print("No matching sessions. Use --all to inspect sessions outside this repo.")
    return 0


def command_index(args: argparse.Namespace) -> int:
    repo = resolve_repo(args.repo)
    paths = AdapterPaths(repo_root=repo, codbash_root=args.codbash_root.resolve(), chat_root=args.chat_root.resolve())
    sidecar_path = create_sidecar(paths, args.session)
    print(f"ChatHistory sidecar: {repo_relative(sidecar_path)}")
    print(f"ChatHistory index: {repo_relative(paths.chat_root / 'index.json')}")
    return 0


def command_export_codex_month(args: argparse.Namespace) -> int:
    source_root = args.source_root.expanduser().resolve()
    chat_root = args.chat_root.expanduser().resolve()
    if not source_root.exists():
        raise SessionAdapterError(f"Codex month source root 不存在：{source_root}")

    files = sorted(source_root.glob("**/rollout-*.jsonl"))
    if args.limit:
        files = files[: args.limit]
    if not files:
        raise SessionAdapterError(f"没有找到 Codex JSONL：{source_root}/**/rollout-*.jsonl")

    exported: list[dict[str, Any]] = []
    for source_path in files:
        metadata = scan_codex_session_metadata(source_path)
        started: datetime = metadata["started"]
        output_dir = chat_root / f"{started:%Y}" / f"{started:%m}" / f"{started:%d}"
        output_path = output_dir / full_export_name(started, metadata["title"], metadata["session_id"])
        entry = write_codex_full_markdown(source_path, output_path, chat_root)
        exported.append(entry)
        print(f"exported {repo_relative(output_path)}")

    print(f"Exported {len(exported)} Codex session(s) from {source_root}")
    print(f"ChatHistory index: {repo_relative(chat_root / 'index.json')}")
    return 0


def command_digest_codex(args: argparse.Namespace) -> int:
    chat_root = args.chat_root.expanduser().resolve()
    source_root = args.source_root.expanduser().resolve()
    source_path = find_codex_session(args.session, source_root=source_root)
    entry = write_codex_digest(source_path, chat_root, skip_interrupted=bool(args.skip_interrupted))
    if entry["digest_status"] == "digest":
        print(f"digest {entry['id']} -> {entry['folder_path']}")
    else:
        print(f"locator-only {entry['id']} skip={','.join(entry['skip_reasons'])}")
    print(f"ChatHistory index: {repo_relative(chat_root / 'index.json')}")
    return 0


def command_digest_codex_month(args: argparse.Namespace) -> int:
    source_root = args.source_root.expanduser().resolve()
    chat_root = args.chat_root.expanduser().resolve()
    if not source_root.exists():
        raise SessionAdapterError(f"Codex month source root 不存在：{source_root}")
    files = sorted(source_root.glob("**/rollout-*.jsonl"))
    if args.limit:
        files = files[: args.limit]
    if not files:
        raise SessionAdapterError(f"没有找到 Codex JSONL：{source_root}/**/rollout-*.jsonl")

    digest_count = 0
    locator_count = 0
    failed_tools_count = 0
    for source_path in files:
        entry = write_codex_digest(source_path, chat_root, skip_interrupted=bool(args.skip_interrupted))
        if entry["digest_status"] == "digest":
            digest_count += 1
            print(f"digest {entry['id']} -> {entry.get('folder_path', '')}")
        else:
            locator_count += 1
            print(f"locator-only {entry['id']} skip={','.join(entry.get('skip_reasons', []))}")
        if int(entry.get("tool_failed_count") or 0) > 0:
            failed_tools_count += 1

    print(
        f"Digested {len(files)} Codex session(s): "
        f"digest={digest_count} locator-only={locator_count} failed-tools={failed_tools_count}"
    )
    print(f"ChatHistory index: {repo_relative(chat_root / 'index.json')}")
    return 0


def entry_matches_topic(entry: dict[str, Any], topic: str) -> bool:
    if not topic:
        return True
    needle = topic.lower()
    haystacks = [
        str(entry.get("title") or ""),
        str(entry.get("topic_slug") or ""),
        " ".join(str(item) for item in entry.get("topics") or []),
        " ".join(str(item) for item in entry.get("sdd_ids") or []),
        " ".join(str(item) for item in entry.get("files_mentioned") or []),
    ]
    return any(needle in text.lower() for text in haystacks)


def command_list_digests(args: argparse.Namespace) -> int:
    chat_root = args.chat_root.expanduser().resolve()
    data = read_index(chat_root / "index.json")
    entries = [entry for entry in data.get("entries", []) if isinstance(entry, dict)]
    if args.status:
        entries = [entry for entry in entries if entry.get("digest_status") == args.status]
    if args.priority:
        entries = [entry for entry in entries if entry.get("priority") == args.priority]
    if args.failed_tools:
        entries = [entry for entry in entries if int(entry.get("tool_failed_count") or 0) > 0]
    if args.interrupted:
        entries = [entry for entry in entries if bool(entry.get("interrupted"))]
    if args.topic:
        entries = [entry for entry in entries if entry_matches_topic(entry, args.topic)]
    if args.efficiency_loop > 0:
        entries = [
            entry for entry in entries
            if int((entry.get("efficiency") or {}).get("verification_loops") or 0) >= args.efficiency_loop
        ]
    if args.json:
        print(json.dumps({"schema_version": data.get("schema_version"), "entries": entries}, ensure_ascii=False, indent=2))
        return 0
    print(f"Found {len(entries)} digest entry(s)")
    for entry in entries[: args.limit]:
        status = entry.get("digest_status", "")
        priority = entry.get("priority", "")
        failed = int(entry.get("tool_failed_count") or 0)
        interrupted = " interrupted" if entry.get("interrupted") else ""
        eff = entry.get("efficiency") or {}
        loops = int(eff.get("verification_loops") or 0)
        loop_tag = f" loops={loops}" if loops > 0 else ""
        print(
            f"{status:<12} {priority:<6} failed={failed:<2} "
            f"{str(entry.get('session_id', ''))[:13]:<13} "
            f"{entry.get('topic_slug') or entry.get('slug') or ''}  "
            f"{clean_snippet(str(entry.get('title') or ''), 80)}{interrupted}{loop_tag}"
        )
    return 0


def build_stale_report(source_root: Path, chat_root: Path, current_session: str = "") -> dict[str, Any]:
    """比较 Codex 原始 JSONL 与 ChatHistory index，生成覆盖率报告。"""
    source_root = source_root.expanduser().resolve()
    chat_root = chat_root.expanduser().resolve()
    if not source_root.exists():
        raise SessionAdapterError(f"Codex source root 不存在：{source_root}")

    source_files = sorted(source_root.glob("**/rollout-*.jsonl"))
    sources: list[dict[str, Any]] = []
    for source_path in source_files:
        metadata = scan_codex_session_metadata(source_path)
        sources.append(
            {
                "session_id": metadata["session_id"],
                "source_path": str(source_path),
                "started_at": metadata["started"].isoformat(timespec="seconds"),
                "updated_at": metadata["updated"].isoformat(timespec="seconds"),
            }
        )

    data = read_index(chat_root / "index.json")
    entries = [entry for entry in data.get("entries", []) if isinstance(entry, dict)]
    codex_entries = {
        str(entry.get("session_id") or "").strip(): entry
        for entry in entries
        if str(entry.get("source_tool") or "") == "codex" and str(entry.get("session_id") or "").strip()
    }
    missing = [item for item in sources if item["session_id"] not in codex_entries]
    indexed = [item for item in sources if item["session_id"] in codex_entries]
    current_missing = bool(current_session and any(item["session_id"].startswith(current_session) for item in missing))
    if not sources:
        coverage = "unknown"
    elif not missing:
        coverage = "complete"
    elif current_missing:
        coverage = "partial-current"
    else:
        coverage = "stale"

    return {
        "schema_version": 1,
        "coverage": coverage,
        "source_root": str(source_root),
        "chat_root": str(chat_root),
        "source_count": len(sources),
        "digest_count": len(indexed),
        "missing_count": len(missing),
        "missing_session_ids": [item["session_id"] for item in missing],
        "missing_sources": missing,
        "current_session": current_session,
        "index_schema_version": data.get("schema_version"),
    }


def command_stale_report(args: argparse.Namespace) -> int:
    report = build_stale_report(args.source_root, args.chat_root, current_session=args.current_session)
    if args.json:
        print(json.dumps(report, ensure_ascii=False, indent=2))
        return 0
    print(f"Coverage: {report['coverage']}")
    print(f"Sources: {report['source_count']}  Digests: {report['digest_count']}  Missing: {report['missing_count']}")
    for item in report["missing_sources"][: args.limit]:
        print(f"- missing {item['session_id']}  {item['source_path']}")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="SlimeAI Cross-agent Session Adapter")
    parser.add_argument("--codbash-root", type=Path, default=DEFAULT_CODBASH_ROOT, help="本地 codbash 源码根目录")
    parser.add_argument("--chat-root", type=Path, default=DEFAULT_CHAT_ROOT, help="ChatHistory 输出目录")
    sub = parser.add_subparsers(dest="command", required=True)

    list_parser = sub.add_parser("list", help="列出当前仓最近会话")
    list_parser.add_argument("--repo", default=".", help="用于过滤会话的仓库路径")
    list_parser.add_argument("--limit", type=int, default=20, help="最多输出会话数量")
    list_parser.add_argument("--all", action="store_true", help="不按 repo 过滤")
    list_parser.add_argument("--json", action="store_true", help="输出 JSON")
    list_parser.set_defaults(func=command_list)

    for name in ("index", "summarize"):
        index_parser = sub.add_parser(name, help="生成或刷新 ChatHistory sidecar")
        index_parser.add_argument("--repo", default=".", help="仓库路径")
        index_parser.add_argument("--session", required=True, help="session id 或前缀")
        index_parser.set_defaults(func=command_index)

    export_parser = sub.add_parser("export-codex-month", help="按日期目录导出 Codex 月度可见完整 transcript")
    export_parser.add_argument("--source-root", type=Path, default=DEFAULT_CODEX_MONTH_ROOT, help="Codex 月度 sessions 目录")
    export_parser.add_argument("--limit", type=int, default=0, help="最多导出文件数量，0 表示全部")
    export_parser.set_defaults(func=command_export_codex_month)

    digest_parser = sub.add_parser("digest-codex", help="生成单个 Codex JSONL 的 AI-first digest")
    digest_parser.add_argument("--session", required=True, help="Codex session id、前缀或 JSONL 路径")
    digest_parser.add_argument("--source-root", type=Path, default=DEFAULT_CODEX_MONTH_ROOT, help="按 id 查找时使用的 Codex sessions 根目录")
    interrupt = digest_parser.add_mutually_exclusive_group()
    interrupt.add_argument("--include-interrupted", dest="skip_interrupted", action="store_false", help="包含中断会话（单会话默认）")
    interrupt.add_argument("--skip-interrupted", dest="skip_interrupted", action="store_true", help="跳过无结论、无代码、无验证的中断会话")
    digest_parser.set_defaults(func=command_digest_codex, skip_interrupted=False)

    digest_month_parser = sub.add_parser("digest-codex-month", help="批量生成 Codex 月度 AI-first digest")
    digest_month_parser.add_argument("--source-root", type=Path, default=DEFAULT_CODEX_MONTH_ROOT, help="Codex 月度 sessions 目录")
    digest_month_parser.add_argument("--limit", type=int, default=0, help="最多处理文件数量，0 表示全部")
    digest_month_parser.add_argument("--skip-interrupted", action="store_true", help="跳过无结论、无代码、无验证的中断会话")
    digest_month_parser.set_defaults(func=command_digest_codex_month)

    list_digest_parser = sub.add_parser("list-digests", help="筛选 ChatHistory digest index")
    list_digest_parser.add_argument("--status", choices=["digest", "locator-only"], help="按 digest_status 过滤")
    list_digest_parser.add_argument("--priority", choices=["low", "normal", "high"], help="按 priority 过滤")
    list_digest_parser.add_argument("--failed-tools", action="store_true", help="只列出有失败工具调用的 digest")
    list_digest_parser.add_argument("--interrupted", action="store_true", help="只列出有中断或回滚的会话")
    list_digest_parser.add_argument("--efficiency-loop", type=int, default=0, metavar="N", help="只列出验证循环次数 >= N 的会话")
    list_digest_parser.add_argument("--topic", default="", help="按 title/topic/files 文本过滤")
    list_digest_parser.add_argument("--limit", type=int, default=50, help="最多输出数量")
    list_digest_parser.add_argument("--json", action="store_true", help="输出 JSON")
    list_digest_parser.set_defaults(func=command_list_digests)

    stale_parser = sub.add_parser("stale-report", help="检查 Codex source JSONL 与 ChatHistory digest index 覆盖差异")
    stale_parser.add_argument("--source-root", type=Path, default=DEFAULT_CODEX_MONTH_ROOT, help="Codex sessions 源目录")
    stale_parser.add_argument("--current-session", default="", help="当前会话 id 或前缀；缺失时 coverage 标为 partial-current")
    stale_parser.add_argument("--limit", type=int, default=50, help="最多输出缺失来源数量")
    stale_parser.add_argument("--json", action="store_true", help="输出 JSON")
    stale_parser.set_defaults(func=command_stale_report)
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    try:
        return int(args.func(args))
    except SessionAdapterError as exc:
        print(f"session-adapter: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
