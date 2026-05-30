# Tasks

## Progress

- **Status**: pending
- **Completed**: 8/8
- **Current**: T1.1

## Task List

- [x] T1.1 Readiness baseline
  - **Scope**: 确认当前 Git 边界、迁移后目录、设计文档、旧路径命中和无关 dirty 状态。
  - **Validation**: `git status --short`、目标路径 `find` / `rg` 摘要记录到 `progress.md`。

- [x] T1.2 重写 `.ai-config/rules/rules.md`
  - **Scope**: 改为 `SlimeAI ECS 框架仓规则`，入口链、事实源边界、Git 边界、ECS 红线、Data 规则和验证入口全部使用框架仓内语义。
  - **Validation**: 人工检查 rules 不再把旧根作为当前工作区，不再把 `DocsAI` 作为当前入口。

- [x] T1.3 运行 AI config sync 并检查生成副本
  - **Scope**: 由 sync 生成 `AGENTS.md`、`CLAUDE.md`、`.windsurf/rules/windsurfrules.md` 和三工具 skill 副本。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`。

- [x] T1.4 修正 SDD / DocsNew 当前入口
  - **Scope**: 更新 `Workspace/SDD/Src/templates.py` Git Boundary；修正 `DocsNew/README.md` 入口漂移和相对路径。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate --all`。

- [x] T1.5 修正第一批高风险 skill 路径
  - **Scope**: `ai-config-management`、`project-index`、`godot-scene-test`、`ecs-data` 等当前会误导路径或验证的 skill 源。
  - **Validation**: sync + skill-test；grep gate 不出现非历史的旧根 / 双层路径 / 已删除 DocsAI 当前入口。

- [x] T1.6 更新项目级 SDD 状态
  - **Scope**: `project.json` current_sdd、`roadmap.md`、`progress.md` 登记 SDD-0023。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate --all`。

- [x] T1.7 最终验证
  - **Scope**: SDD、AI config、skill-test、Markdown whitespace 和路径 grep gate。
  - **Validation**: `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`、`python3 Workspace/SDD/sdd.py validate --all`、`git diff --check`。

- [x] T1.8 Handoff
  - **Scope**: 更新本 SDD `progress.md` Latest Resume；说明已完成、未处理历史区、下一步是否进入 Entity hard cutover。
  - **Validation**: Latest Resume 可让新会话恢复。
