# SystemAgent Root Migration Rule Sync

## Goal

把已经迁入 `SlimeAI/` 框架仓的 `SDD/`、`Workspace/`、`.ai-config/`、`.claude/`、`.codex/`、`.windsurf/` 从旧工作区语义收口为框架仓语义。

完成后，AI 从 `/home/slime/Code/SlimeAI/SlimeAI` 打开时，应能通过 `AGENTS.md -> DocsNew -> SDD/PRJ-0002 -> Src/ECS/** -> owner skill -> 验证脚本` 恢复任务，而不是被旧 `/home/slime/Code/SlimeAI` 工作区、旧 `DocsAI`、旧 OpenSpec 或双层 `SlimeAI/Src` 路径误导。

非目标：

- 不改 ECS runtime 业务代码。
- 不启动 Entity / Relationship hard cutover。
- 不清理 `.history/**` 或历史 ChatHistory / Plans 中的旧路径。
- 不恢复 `DocsAI/`。

## Context

已确认事实：

- `SlimeAI/SDD`、`SlimeAI/Workspace`、`SlimeAI/.ai-config`、`SlimeAI/.claude`、`SlimeAI/.codex`、`SlimeAI/.windsurf` 已存在。
- `Workspace/Tools/ai-config-sync/sync-ai-config.sh` 位于 `SlimeAI/Workspace/Tools/ai-config-sync/`，其 `../../..` root 推导已自然指向 `SlimeAI/`。
- `Workspace/SDD/Src/config.py` 通过 `parents[3]` 推导 `REPO_ROOT`，在新位置下也指向 `SlimeAI/`。
- `.ai-config/rules/rules.md` 仍是旧工作区 / GameOS 规则，包含旧根 `/home/slime/Code/SlimeAI`、`SlimeAI/Src/ECS`、`SlimeAI/DocsNew`、`SlimeAI/DocsAI` 等迁移前表达。
- `SlimeAI/AGENTS.md` 仍保留旧纠偏入口、OpenSpec change 和 `DocsAI/INDEX.md`。
- `DocsNew/README.md` 存在入口漂移，表格引用 `01-*` / `02-*`，实际文件是 `ECS框架与AIFirst方向决策.md` 和 `ECS/Data/Data系统说明.md`。
- `Workspace/SDD/Src/templates.py` SDD 模板仍写 `Git Boundary: /home/slime/Code/SlimeAI`。
- `.ai-config/skills/godot/godot-scene-test/SKILL.md` 和 `scripts/analyze-logs.sh` 有旧 `.ai-config` / `DocsAI` 路径残留。

关键约束：

- skill / rule / command 只改 `.ai-config` 源，不直接维护生成副本。
- 生成副本由 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 负责。
- 当前 Git 边界是 `/home/slime/Code/SlimeAI/SlimeAI`，外层 `Games/*`、`Resources/*` 只在明确需要时访问。
- 已有 `.uid` 删除、pycache 等 dirty 状态不是本 SDD 产生，执行时不得混入无关修改。

## Design

### 入口语义

迁移后默认入口链：

```text
AGENTS.md
  -> DocsNew/README.md
  -> SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md
  -> SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md
  -> Src/ECS/** 旁文档
  -> owner skill
  -> Tools/run-build.sh / Tools/run-tests.sh / Godot scene test
```

SystemAgent 只作为流程工具，不作为 ECS 业务事实源第一入口。

### rules 同步

`.ai-config/rules/rules.md` 重写为 `SlimeAI ECS 框架仓规则`。同步后生成：

```text
AGENTS.md
CLAUDE.md
.windsurf/rules/windsurfrules.md
```

规则必须改掉旧工作区语义：

- 当前目录从 `/home/slime/Code/SlimeAI` 改为 `/home/slime/Code/SlimeAI/SlimeAI`。
- 框架内路径从 `SlimeAI/Src/ECS` 改为 `Src/ECS`。
- 框架内路径从 `SlimeAI/DocsNew` 改为 `DocsNew`。
- `DocsAI/` 明确为已删除旧入口，不恢复。
- OpenSpec 只作为历史兼容，不作为默认入口。

### Docs / SDD / template 收口

- `DocsNew/README.md` 修正文件名和相对路径。
- `Workspace/SDD/Src/templates.py` 的默认 Git Boundary 改为 `/home/slime/Code/SlimeAI/SlimeAI`。
- 项目级 PRJ-0002 `roadmap.md`、`progress.md`、`project.json` 切到 SDD-0023。

### skill 收口

第一批只修会误导当前任务入口或验证的高风险 skill：

- `.ai-config/skills/core/ai-config-management/SKILL.md`
- `.ai-config/skills/core/project-index/SKILL.md`
- `.ai-config/skills/godot/godot-scene-test/SKILL.md`
- `.ai-config/skills/godot/godot-scene-test/scripts/analyze-logs.sh`
- `.ai-config/skills/ecs/ecs-data/SKILL.md`

更大范围的 GameOS / DocsAI 历史引用可进入后续任务，不阻塞本 SDD 的 rules sync。

## Verification

执行中和完成前至少运行：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
python3 Workspace/SDD/sdd.py validate --all
git diff --check
```

规则与路径 grep gate：

```bash
rg -n "/home/slime/Code/SlimeAI([^/]|$)|SlimeAI/Src/ECS|SlimeAI/DocsNew|SlimeAI/DocsAI|DocsAI/INDEX|当前 OpenSpec change" \
  AGENTS.md CLAUDE.md .ai-config DocsNew Workspace/SDD/Src/templates.py \
  -g '*.md' -g '*.sh' -g '*.py'
```

允许历史命中：

- `.history/**`
- `Workspace/DocsAI/ChatHistory/**`
- 已完成 SDD 的历史 evidence / execution prompt，除非被当前入口直接引用。
