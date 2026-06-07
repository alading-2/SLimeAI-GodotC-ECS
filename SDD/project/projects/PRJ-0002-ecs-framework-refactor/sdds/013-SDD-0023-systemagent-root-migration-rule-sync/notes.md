# Notes

## References

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/4.SystemAgent目录更改到SlimeAI里面/README.md`
- `.ai-config/rules/rules.md`
- `Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- `Workspace/SDD/Src/config.py`
- `Workspace/SDD/Src/templates.py`
- `DocsNew/README.md`
- `AGENTS.md`
- `execution-prompt.md`

## Grep Gates

Current-entry grep gate:

```bash
rg -n "/home/slime/Code/SlimeAI([^/]|$)|SlimeAI/Src/ECS|SlimeAI/DocsNew|SlimeAI/DocsAI|DocsAI/INDEX|当前 OpenSpec change" \
  AGENTS.md CLAUDE.md .ai-config DocsNew Workspace/SDD/Src/templates.py \
  -g '*.md' -g '*.sh' -g '*.py'
```

Allowed historical areas:

- `.history/**`
- `Workspace/DocsAI/ChatHistory/**`
- completed SDD evidence / execution prompts, unless linked as current entry

## Open Questions

- 是否在本 SDD 中同步清理 `Workspace/SystemAgent/Rules/**` 的 GameOS / DocsAI 历史语义，还是只修当前入口与高风险 skill。
- `.history/.ai-config`、`.history/SDD`、`.history/Workspace` 是否需要后续单独归档说明。
