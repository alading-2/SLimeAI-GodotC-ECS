#!/bin/bash
# 统一同步 AI 配置：以 .ai-config/ 为唯一源，按 sync-targets.json 配置分发
# .ai-config/skills/ 内部可按领域分子目录，同步时自动打平到目标顶层
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
CONFIG="$ROOT/.ai-config/sync-targets.json"

if [[ ! -f "$CONFIG" ]]; then
    echo "ERROR: sync config not found: $CONFIG" >&2
    exit 1
fi

# ── skill 同步（打平） ──────────────────────────────────────────────
sync_skills_flat() {
    local source="$1"
    local target="$2"
    local exclude_json="$3"  # JSON array, e.g. '["openspec-*"]'
    declare -A seen_skills=()

    echo "  Clearing $target ..."
    rm -rf "$target"/*
    mkdir -p "$target"

    echo "  Copying skills (flatten) ..."
    find "$source" -mindepth 2 -name "SKILL.md" | sort | while read -r skill_md; do
        skill_dir=$(dirname "$skill_md")
        skill_name=$(basename "$skill_dir")

        # 检查是否在排除列表中
        excluded=$(echo "$exclude_json" | jq -r --arg name "$skill_name" \
            '[ .[] | select($name | test(.)) ] | length')
        if [[ "$excluded" -gt 0 ]]; then
            echo "    SKIP (excluded): $skill_name"
            continue
        fi

        # 同名 skill 检测
        if [[ -n "${seen_skills[$skill_name]:-}" ]]; then
            echo "ERROR: duplicate skill basename '$skill_name': ${seen_skills[$skill_name]} and $skill_dir" >&2
            echo "       Move/rename one source under .ai-config/skills before syncing." >&2
            return 1
        fi
        seen_skills[$skill_name]="$skill_dir"

        rm -rf "$target/$skill_name"
        mkdir -p "$target/$skill_name"
        cp -R "$skill_dir"/. "$target/$skill_name"/
        echo "    COPY: $skill_name"
    done
}

# ── rule 同步（支持 prepend） ───────────────────────────────────────
sync_rule() {
    local source="$1"
    local target="$2"
    local prepend_json="$3"  # JSON array of strings

    if [[ ! -f "$source" ]]; then
        echo "  SKIP (source not found): $source"
        return
    fi

    mkdir -p "$(dirname "$target")"

    # 写入 prepend 行（如有 frontmatter 差异）
    prepend_count=$(echo "$prepend_json" | jq 'length')
    if [[ "$prepend_count" -gt 0 ]]; then
        echo "$prepend_json" | jq -r '.[]' > "$target"
        echo "" >> "$target"
        cat "$source" >> "$target"
    else
        cp "$source" "$target"
    fi

    echo "  SYNC: $source -> $target"
}

# ── 主流程 ─────────────────────────────────────────────────────────
echo "==> Reading sync config: $CONFIG"

# 同步 skills
skill_count=$(jq '.skills | length' "$CONFIG")
for ((i = 0; i < skill_count; i++)); do
    target=$(jq -r ".skills[$i].target" "$CONFIG")
    exclude=$(jq -c ".skills[$i].exclude // []" "$CONFIG")
    echo ""
    echo "==> [Skill $((i+1))/$skill_count] .ai-config/skills/ -> $target"
    sync_skills_flat "$ROOT/.ai-config/skills" "$ROOT/$target" "$exclude"
done

# 同步 rules
rule_count=$(jq '.rules | length' "$CONFIG")
for ((i = 0; i < rule_count; i++)); do
    source=$(jq -r ".rules[$i].source" "$CONFIG")
    target=$(jq -r ".rules[$i].target" "$CONFIG")
    prepend=$(jq -c ".rules[$i].prepend // []" "$CONFIG")
    echo ""
    echo "==> [Rule $((i+1))/$rule_count] $source -> $target"
    sync_rule "$ROOT/$source" "$ROOT/$target" "$prepend"
done

# skill-test
echo ""
echo "=== SystemAgent skill-test (advisory) ==="
date -u +%Y-%m-%dT%H:%M:%SZ > "$ROOT/Workspace/SystemAgent/Registry/.last-sync"
bash "$ROOT/Workspace/SystemAgent/Tools/skill-test/lint.sh" static changed --no-fail --summary-only

echo ""
echo "==> Done."
