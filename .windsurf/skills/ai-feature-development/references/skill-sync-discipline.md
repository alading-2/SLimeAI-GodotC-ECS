# Skill Sync Discipline

> Baseline 由 `Workspace/SystemAgent/Policies/AIConfigBoundary.md` 与 `ai-config-management` skill 管理。

读取时机：修改任何 skill、reference、rule、command 或 AI 工具副本时读取。

## 唯一可写源

- Skill：`.ai-config/skills/<category>/<name>/SKILL.md`
- Skill references / agents metadata：`.ai-config/skills/<category>/<name>/references/`、`agents/`
- Rules：`.ai-config/rules/rules.md`
- Commands：`.ai-config/skills/<category>/<name>/SKILL.md`

hook / subagent 不属于本同步规则：

- Claude hook：`.claude/settings.json`
- Claude subagent：`.claude/agents/*.md`
- Codex hook：`.codex/hooks.json`
- Codex subagent：`.codex/agents/*.toml`、`.codex/config.toml`

这些是工具项目运行配置，直接维护，不放进 `.ai-config`。

禁止直接手改：

- `.codex/skills/`
- `.claude/skills/`
- `.windsurf/skills/`
- `.claude/commands/opsx/`
- `CLAUDE.md`
- `.windsurf/rules/windsurfrules.md`

这些路径只允许由同步脚本生成。

## 同步命令

在工作区根运行：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
```

当前脚本事实源在 `Workspace/Tools/ai-config-sync/`。旧 `Tools/ai-config-sync/` 已不是当前入口；如果脚本路径被移动、删除或重写，先确认这是用户改动，再按当前事实源执行，不要擅自恢复旧路径。

## 验证

```bash
find .ai-config/skills/ai/ai-feature-development -type f | sort
find .codex/skills/ai-feature-development -type f | sort
find .claude/skills/ai-feature-development -type f | sort
find .windsurf/skills/ai-feature-development -type f | sort
git status --short
```

副本文件清单必须与 `.ai-config` source 对应。若副本有额外手工差异，回到 `.ai-config` 源修正后重新 sync。

## SDD 规则

- 修改 `SKILL.md` 段落骨架、reference 清单或同步规则：必须走 SDD。
- reference typo、链接、命令示例小修：可直接 PR，但要说明来源和验证。
- 完成前确认 tasks 中有 sync 步骤和副本清单验证。
