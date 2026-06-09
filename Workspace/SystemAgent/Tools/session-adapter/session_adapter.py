#!/usr/bin/env python3
"""只读整理本地 AI CLI 会话为 SlimeAI ChatHistory sidecar。"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[4]
DEFAULT_CODBASH_ROOT = REPO_ROOT / "Workspace/Resources/tool/codbash"
DEFAULT_CHAT_ROOT = REPO_ROOT / "Workspace/DocsAI/ChatHistory"
SUPPORTED_SOURCE_TOOLS = {"claude", "claude-ext", "codex", "opencode"}


class SessionAdapterError(RuntimeError):
    """用户可修复的 session adapter 错误。"""


@dataclass(frozen=True)
class AdapterPaths:
    repo_root: Path
    codbash_root: Path
    chat_root: Path


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


def truncate(text: str, limit: int = 900) -> str:
    text = (text or "").strip()
    if len(text) <= limit:
        return text
    return text[: limit - 20].rstrip() + "\n\n... [truncated]"


def clean_snippet(text: str, limit: int = 180) -> str:
    return truncate(re.sub(r"\s+", " ", text or "").strip(), limit)


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
