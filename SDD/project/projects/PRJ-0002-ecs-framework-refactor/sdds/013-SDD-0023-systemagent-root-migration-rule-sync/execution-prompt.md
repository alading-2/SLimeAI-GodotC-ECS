# One-Shot Execution Prompt: Complete SDD-0023 SystemAgent Root Migration Rule Sync

把下面整段作为新会话的一次性执行提示词使用。目标是完成 SDD-0023，把已经迁入 `SlimeAI/` 框架仓的 `SDD/`、`Workspace/`、`.ai-config/`、`.claude/`、`.codex/`、`.windsurf/` 从旧工作区语义收口为框架仓语义。

```text
你在 /home/slime/Code/SlimeAI/SlimeAI 框架仓内工作。默认中文回答；命令、代码、错误信息保留原文。改文件前先读相关文件；涉及文件修改时前后运行 git status --short；改完总结改动和验证结果。不要 push。不要随意加依赖、大重构或跨 git 边界混提交。

任务目标：
完成 SDD-0023 SystemAgent Root Migration Rule Sync。目录迁移已经完成：SDD/、Workspace/、.ai-config/、.claude/、.codex/、.windsurf/ 已在 /home/slime/Code/SlimeAI/SlimeAI 内。当前目标不是继续搬目录，而是把 rules / skill / SDD template / DocsNew 从旧工作区语义收口到 SlimeAI 框架仓语义。

必须先读：
1. /home/slime/Code/SlimeAI/SlimeAI/AGENTS.md
2. /home/slime/Code/SlimeAI/SlimeAI/SDD/INDEX.md
3. /home/slime/Code/SlimeAI/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md
4. /home/slime/Code/SlimeAI/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/4.SystemAgent目录更改到SlimeAI里面/README.md
5. /home/slime/Code/SlimeAI/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/013-SDD-0023-systemagent-root-migration-rule-sync/README.md
6. /home/slime/Code/SlimeAI/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/013-SDD-0023-systemagent-root-migration-rule-sync/design/main.md
7. /home/slime/Code/SlimeAI/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/013-SDD-0023-systemagent-root-migration-rule-sync/tasks.md
8. /home/slime/Code/SlimeAI/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/013-SDD-0023-systemagent-root-migration-rule-sync/bdd.md
9. /home/slime/Code/SlimeAI/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/013-SDD-0023-systemagent-root-migration-rule-sync/progress.md
10. /home/slime/Code/SlimeAI/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/013-SDD-0023-systemagent-root-migration-rule-sync/notes.md
11. /home/slime/Code/SlimeAI/SlimeAI/.ai-config/rules/rules.md
12. /home/slime/Code/SlimeAI/SlimeAI/Workspace/Tools/ai-config-sync/sync-ai-config.sh
13. /home/slime/Code/SlimeAI/SlimeAI/Workspace/SDD/Src/templates.py
14. /home/slime/Code/SlimeAI/SlimeAI/DocsNew/README.md

Git 边界：
- 当前 SDD、Workspace、.ai-config 和框架源码都在 /home/slime/Code/SlimeAI/SlimeAI。
- 外层 /home/slime/Code/SlimeAI 只作为资源与多仓容器，不是当前 AI 配置根。
- 游戏仓仍在 /home/slime/Code/SlimeAI/Games/*，只有 Godot 场景验证或游戏侧规则明确需要时才进入。
- Games/*/SlimeAI/ 是框架 submodule 镜像，禁止直接做框架业务改动。
- 当前工作区可能已有无关 .uid 删除、pycache 或源码改动；不得回滚、覆盖或混入无关修改。

核心裁决：
- 不恢复 DocsAI/ 作为当前事实源。
- 不把 OpenSpec / Workspace/DocsAI 作为默认入口。
- 不把 SystemAgent 作为 ECS 业务事实源第一入口；SystemAgent 只作为流程、gate、hook、skill-test 工具。
- 不直接修改 .claude/skills、.codex/skills、.windsurf/skills 作为源；skill/rule/command 只改 .ai-config。
- 不机械全局替换 /home/slime/Code/SlimeAI；游戏仓、Resources、历史 evidence 仍可能合法指向外层路径。
- 不机械删除所有 SlimeAI/ 前缀；游戏仓 submodule 场景路径可能需要 res://SlimeAI/...，但框架仓内部当前入口应使用 Src/ECS、DocsNew、SDD、Workspace。

实施顺序必须按 SDD-0023 tasks.md：

1. T1.1 Readiness baseline
   - 运行 git status --short。
   - 确认 SDD/、Workspace/、.ai-config、.claude、.codex、.windsurf 均在当前框架仓。
   - 记录 .ai-config/rules/rules.md、AGENTS.md、DocsNew/README.md、Workspace/SDD/Src/templates.py、godot-scene-test skill 的旧路径命中。
   - 写入 SDD-0023 progress.md。

2. T1.2 重写 .ai-config/rules/rules.md
   - 标题改为 SlimeAI ECS 框架仓规则。
   - 定位改为 /home/slime/Code/SlimeAI/SlimeAI 是当前框架仓与 AI 主目录。
   - 默认入口改为 AGENTS.md -> DocsNew/README.md -> SDD/project/projects/PRJ-0002... -> Src/ECS/** 旁文档 -> owner skill -> 验证脚本。
   - 事实源边界写清：DocsNew、SDD、Src/ECS 旁文档、.ai-config、Workspace/SystemAgent、生成副本。
   - 删除或降级旧语义：AI-first GameOS 工作区、旧根工作区、当前 OpenSpec change、DocsAI/INDEX.md。
   - 整合 ECS 红线和 Data 当前规则：DataOS descriptor、runtime snapshot、generated handle、catalog-bound Data，不恢复 DataMeta / DataRegistry / DataKey_Compatibility 事实源。

3. T1.3 运行 AI config sync 并检查生成副本
   - 运行 bash Workspace/Tools/ai-config-sync/sync-ai-config.sh。
   - 确认生成 AGENTS.md、CLAUDE.md、.windsurf/rules/windsurfrules.md。
   - 确认技能副本生成到当前仓 .claude/.codex/.windsurf，不生成到外层根。
   - 不手工编辑生成副本。

4. T1.4 修正 SDD / DocsNew 当前入口
   - Workspace/SDD/Src/templates.py 默认 Git Boundary 改为 /home/slime/Code/SlimeAI/SlimeAI。
   - DocsNew/README.md 修正入口漂移：真实文件是 ECS框架与AIFirst方向决策.md 和 ECS/Data/Data系统说明.md。
   - DocsNew 内指向 SDD / Workspace 的相对路径按迁移后结构修正。
   - 运行 python3 Workspace/SDD/sdd.py validate --all。

5. T1.5 修正第一批高风险 skill 路径
   - 优先修 .ai-config/skills/core/ai-config-management/SKILL.md。
   - 优先修 .ai-config/skills/core/project-index/SKILL.md。
   - 优先修 .ai-config/skills/godot/godot-scene-test/SKILL.md。
   - 优先修 .ai-config/skills/godot/godot-scene-test/scripts/analyze-logs.sh。
   - 优先修 .ai-config/skills/ecs/ecs-data/SKILL.md。
   - 对 GameOS / DocsAI 历史引用做最小必要修正：当前入口必须指向 DocsNew、SDD、Src/ECS；确属历史参考的标 historical，不纳入默认入口。

6. T1.6 更新项目级 SDD 状态
   - 确认 project.json current_sdd 为 SDD-0023。
   - 确认 roadmap.md、progress.md、README.md 已登记 SDD-0023。
   - 若执行过程中 tasks 状态变化，同步 sdd.json progress 与 tasks.md。

7. T1.7 最终验证
   - 运行 bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only。
   - 运行 python3 Workspace/SDD/sdd.py validate SDD-0023。
   - 运行 python3 Workspace/SDD/sdd.py validate --all。
   - 运行 git diff --check。
   - 运行路径 grep gate，确认当前入口不再出现非历史旧根 / 双层路径 / 已删除 DocsAI 当前入口。

8. T1.8 Handoff
   - 勾选 tasks.md 已完成项。
   - 更新 SDD-0023 progress.md Latest Resume 和 Timeline。
   - 说明未处理范围：.history、ChatHistory、历史 SDD evidence、外层资源目录。
   - 若全部完成，准备将 SDD-0023 标记 done；否则保留 pending/active 并明确 next action。

关键文件优先检查：
- .ai-config/rules/rules.md
- AGENTS.md
- CLAUDE.md
- .windsurf/rules/windsurfrules.md
- Workspace/Tools/ai-config-sync/sync-ai-config.sh
- Workspace/SDD/Src/config.py
- Workspace/SDD/Src/templates.py
- DocsNew/README.md
- .ai-config/skills/core/ai-config-management/SKILL.md
- .ai-config/skills/core/project-index/SKILL.md
- .ai-config/skills/ecs/ecs-data/SKILL.md
- .ai-config/skills/godot/godot-scene-test/SKILL.md
- .ai-config/skills/godot/godot-scene-test/scripts/analyze-logs.sh
- SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/013-SDD-0023-systemagent-root-migration-rule-sync/

最低验证命令：
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
python3 Workspace/SDD/sdd.py validate SDD-0023
python3 Workspace/SDD/sdd.py validate --all
git diff --check

路径 grep gate：
cd /home/slime/Code/SlimeAI/SlimeAI
rg -n "/home/slime/Code/SlimeAI([^/]|$)|SlimeAI/Src/ECS|SlimeAI/DocsNew|SlimeAI/DocsAI|DocsAI/INDEX|当前 OpenSpec change" \
  AGENTS.md CLAUDE.md .ai-config DocsNew Workspace/SDD/Src/templates.py \
  -g '*.md' -g '*.sh' -g '*.py'

允许命中只能是：
- .history/**。
- Workspace/DocsAI/ChatHistory/**。
- 已完成 SDD 的历史 evidence / execution prompt。
- 明确标记 historical / legacy 且不参与当前入口的说明。
- 游戏仓 submodule 或 Godot res://SlimeAI/... 场景路径，如确实用于承载游戏中的框架镜像验证。

完成标准：
- tasks.md T1.1 至 T1.8 全部完成或有明确 blocker。
- .ai-config/rules/rules.md 是框架仓规则，sync 后 AGENTS.md / CLAUDE.md / Windsurf rules 同义。
- SDD CLI 默认 root 使用当前仓 SDD/，新模板 Git Boundary 正确。
- DocsNew 当前入口无漂移。
- 第一批高风险 skill 不再把旧根、已删除 DocsAI 或双层 SlimeAI/Src 当当前入口。
- skill-test、SDD validate、git diff --check 有新鲜通过证据。
```

