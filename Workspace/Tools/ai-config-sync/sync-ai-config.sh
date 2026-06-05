#!/bin/bash
# 统一同步 AI 配置：以 .ai-config/ 为唯一源，分发到 Codex、Claude、Devin、Trae
# .ai-config/skills/ 内部可按领域分子目录，同步时自动打平到目标顶层
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"

# 确保所有目标目录存在（迁移或新克隆后可能缺失）
mkdir -p "$ROOT/.codex/skills"
mkdir -p "$ROOT/.claude/skills"
mkdir -p "$ROOT/.claude/commands/opsx"
mkdir -p "$ROOT/.devin/skills"
mkdir -p "$ROOT/.devin/rules"
mkdir -p "$ROOT/.trae/skills"
mkdir -p "$(dirname "$ROOT/AGENTS.md")"
mkdir -p "$(dirname "$ROOT/CLAUDE.md")"

sync_skills_flat() {
    local source="$1"
    local target="$2"
    local exclude_pattern="${3:-}"
    declare -A seen_skills=()

    echo "  Clearing $target ..."
    rm -rf "$target"/*
    mkdir -p "$target"

    echo "  Copying skills (flatten) ..."
    find "$source" -mindepth 2 -name "SKILL.md" | sort | while read -r skill_md; do
        skill_dir=$(dirname "$skill_md")
        skill_name=$(basename "$skill_dir")

        if [[ -n "$exclude_pattern" && "$skill_name" == $exclude_pattern ]]; then
            echo "    SKIP (excluded): $skill_name"
            continue
        fi

        # 同名 skill 目标先清理，避免重复同步时出现 skill/skill 嵌套目录。
        if [[ -n "${seen_skills[$skill_name]:-}" ]]; then
            echo "ERROR: duplicate skill basename '$skill_name': ${seen_skills[$skill_name]} and $skill_dir" >&2
            echo "       Move/rename one source under .ai-config/skills before syncing." >&2
            return 1
        fi
        seen_skills[$skill_name]="$skill_dir"

        # 同名 skill 目标先清理，避免重复同步时出现 skill/skill 嵌套目录。
        rm -rf "$target/$skill_name"
        mkdir -p "$target/$skill_name"
        cp -R "$skill_dir"/. "$target/$skill_name"/
        echo "    COPY: $skill_name"
    done
}

echo "==> [1/8] Syncing skills: .ai-config/skills/ -> .codex/skills/"
sync_skills_flat "$ROOT/.ai-config/skills" "$ROOT/.codex/skills" ""

echo ""
echo "==> [2/8] Syncing skills: .ai-config/skills/ -> .claude/skills/ (exclude openspec-*)"
sync_skills_flat "$ROOT/.ai-config/skills" "$ROOT/.claude/skills" "openspec-*"

echo ""
echo "==> [3/8] Syncing skills: .ai-config/skills/ -> .devin/skills/"
sync_skills_flat "$ROOT/.ai-config/skills" "$ROOT/.devin/skills" ""

echo ""
echo "==> [4/8] Syncing skills: .ai-config/skills/ -> .trae/skills/"
sync_skills_flat "$ROOT/.ai-config/skills" "$ROOT/.trae/skills" ""

echo ""
echo "==> [5/8] Syncing rules: .ai-config/rules/rules.md -> AGENTS.md"
cp "$ROOT/.ai-config/rules/rules.md" "$ROOT/AGENTS.md"

echo ""
echo "==> [6/8] Syncing rules: .ai-config/rules/rules.md -> CLAUDE.md"
cp "$ROOT/.ai-config/rules/rules.md" "$ROOT/CLAUDE.md"

echo ""
echo "==> [7/8] Syncing rules: .ai-config/rules/rules.md -> .devin/rules/devinrules.md"
{
  echo "---"
  echo "trigger: always_on"
  echo "---"
  echo ""
  cat "$ROOT/.ai-config/rules/rules.md"
} > "$ROOT/.devin/rules/devinrules.md"

echo ""
echo "==> [8/8] Regenerating Claude commands from .ai-config/openspec skills"
python3 "$ROOT/Workspace/Tools/ai-config-sync/generate-claude-commands.py"

echo ""
echo "=== SystemAgent skill-test (advisory) ==="
date -u +%Y-%m-%dT%H:%M:%SZ > "$ROOT/Workspace/SystemAgent/Registry/.last-sync"
bash "$ROOT/Workspace/SystemAgent/Tools/skill-test/lint.sh" static changed --no-fail --summary-only

echo ""
echo "==> Done."
