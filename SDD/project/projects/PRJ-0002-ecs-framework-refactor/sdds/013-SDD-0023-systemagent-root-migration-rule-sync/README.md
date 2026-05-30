# SDD-0023 SystemAgent Root Migration Rule Sync

## Index Card

- **Status**: done
- **Created**: 2026-05-30
- **Updated**: 2026-05-30
- **Type**: workflow
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ai-config/systemagent
- **Tags**: systemagent, ai-config, sdd

## What This SDD Is About

`SDD/`、`Workspace/`、`.ai-config/`、`.claude/`、`.codex`、`.windsurf/` 已迁入 `SlimeAI/` 框架仓。本 SDD 负责把迁入后的规则、skill、SDD 模板、DocsNew 入口和验证门禁从旧工作区语义收口为框架仓语义。

本任务不改 ECS runtime 业务代码，不处理 Entity / Relationship hard cutover；它先保证 AI 从 `/home/slime/Code/SlimeAI/SlimeAI` 打开时能读到正确入口、正确路径和正确同步规则。

## Reading Order

1. `../../design/4.SystemAgent目录更改到SlimeAI里面/README.md` — 项目级迁移后规则更新设计
2. `design/main.md` — 本 SDD 执行设计
3. `tasks.md` — 当前任务拆分、顺序和验证要求
4. `bdd.md` — 行为验收场景
5. `progress.md` — 最近结论和恢复点
6. `notes.md` — 参考、开放问题和 grep 入口
7. `execution-prompt.md` — 可直接交给新会话执行的完整提示词

## Current Resume

- **Current Task**: T1.1
- **Last Conclusion**: SDD-0023 已创建并补齐任务级设计，用于执行 `SlimeAI/` 内 SystemAgent / AI config 根迁移后的规则同步与路径收口。
- **Next Action**: 从 T1.1 开始，先建立 readiness baseline，再重写 `.ai-config/rules/rules.md` 并通过 sync 生成 `AGENTS.md` / `CLAUDE.md` / Windsurf rules。
- **Open Blockers**: none
