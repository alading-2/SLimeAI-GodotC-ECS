# SystemAgent OpenSpec Retirement

## Goal

让 SystemAgent 的默认中大型任务入口从 OpenSpec 切换为 SDD，并确保默认路由、workflow、role、protocol、gate、policy、rule 和 wrapper skill 都不再把 OpenSpec 当作当前事实源或执行记忆。

本阶段不删除 `openspec/` 目录，也不删除 `.ai-config/skills/openspec/` 兼容 skill。OpenSpec 仅作为历史资产和显式兼容入口保留。

## Context

- SDD MVP 已落地：`Workspace/SDD/` 是 CLI、模板和校验规则事实源，根目录 `SDD/` 是任务实例事实源。
- SystemAgent 正文事实源在 `Workspace/SystemAgent/`。
- skill/rule/command 统一源在 `.ai-config/`，同步副本不能直接编辑。
- OpenSpec 过去承担中大型任务计划、执行记忆、归档和 baseline 角色；本任务只退休其默认入口，不清除历史资产。
- `openspec-*` skill 因 catalog coverage 和历史兼容需要继续登记，但角色必须标明 legacy compatibility，不能作为默认入口。

## Design

采用 SDD-first、OpenSpec-compatible 的过渡设计：

1. `Workspace/SystemAgent/` 默认入口只指向 SDD。
2. 删除 SystemAgent 正文中的 OpenSpec 专属协议文件，避免继续被 must-read 或 manifest 当作当前协议入口。
3. 将 workflow、role、gate、policy 中的长任务、执行记忆、baseline、archive 前检查等表述替换为 SDD `tasks.md`、`progress.md`、`design/`、`bdd.md` 和 `python3 Workspace/SDD/sdd.py validate`。
4. `.ai-config/rules/rules.md` 切换为 SDD 工作流和 OpenSpec 兼容边界；通过同步脚本生成 AGENTS/CLAUDE/Windsurf 副本。
5. `.ai-config/skills/ai`、`systemagent`、`core`、`godot`、`ecs` 中会影响默认路由的 OpenSpec 表述替换为 SDD-first。
6. `.ai-config/skills/openspec/*` 保留，不改写为 SDD，因为它们是显式兼容命令本身。
7. `Workspace/SystemAgent/Catalog/systemagent-catalog.yaml` 保留 `openspec-*` 条目，但 role 改为 `legacy-compatibility-*`，与 `sdd-workflow` 的默认 entry 区分。

## Verification

- `python3 Workspace/SDD/sdd.py validate SDD-0002`
- `python3 Workspace/SDD/sdd.py validate --all`
- `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`
- `grep` 检查 `Workspace/SystemAgent` 残留 OpenSpec 引用，允许 `systemagent-catalog.yaml` 中 legacy compatibility 条目。
- `grep` 检查 `.ai-config/skills` 非 `openspec/` 目录残留，允许历史基线路径、兼容边界说明和搜索范围。
