# AI Config Boundary Policy

## Source-of-truth boundary

`.ai-config/` 只维护 skill、rule 和 command 源。`.codex/skills/`、`.claude/skills/`、`.windsurf/skills/`、`.claude/commands/opsx/`、`CLAUDE.md`、`.windsurf/rules/windsurfrules.md` 是同步副本。Hook 和 subagent 是 `.claude/.codex` 直接运行配置，不从 `.ai-config` 生成。

## Allowed actions

- 改 skill：编辑 `.ai-config/skills/<category>/<name>/SKILL.md`。
- 改 rule：编辑 `.ai-config/rules/rules.md`。
- 改 command：编辑 `.ai-config/skills/<category>/<name>/SKILL.md` 中对应源。
- 改 hook/subagent：直接编辑 `.claude/settings.json`、`.claude/agents/`、`.codex/hooks.json`、`.codex/agents/` 或 `.codex/config.toml`。
- 改 `.ai-config` 后运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`。

## Forbidden actions

- 不直接修改同步副本作为源。
- SystemAgent wrapper skill 不复制 workflow、role、gate、policy 正文。
- `Workspace/SystemAgent/Rules/` 不反向生成 `.ai-config/skills/`。

## Required validation or reporting

运行 `Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only` 或记录无法运行的原因，并在最终汇报说明 sync/lint 结果。
