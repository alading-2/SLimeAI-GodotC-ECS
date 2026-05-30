# SlimeAI 工作区规则

## 定位

这是 AI-first GameOS 框架工作区。优先目标是让 AI 能稳定路由、验证和复盘：入口少、事实源少、命令可重复、artifact 可检查。

默认路径：

```text
AGENTS.md -> Workspace/SystemAgent/README.md -> SDD/INDEX.md -> 当前 SDD design -> SlimeAI/Src/ECS/** 旁文档 -> owner skill / 项目脚本 -> 验证 artifact
```

## Git说明
当前目录 `/home/slime/Code/SlimeAI` 是工作区根，也是 AI 配置、Workspace DocsAI、SDD 和历史 OpenSpec 资产的 Git 仓库。

主要 Git 边界包括：
- `/home/slime/Code/SlimeAI`（工作区配置 / Workspace DocsAI / SDD / 历史 OpenSpec 资产）
- `SlimeAI/`（框架仓）
- `Games/*/`（各游戏仓）
- `Games/BrotatoLike/SlimeAI/`（submodule，独立 git，只读镜像）

**单向数据流**：框架改动只在 `SlimeAI/` 仓提交；游戏仓通过更新 submodule 指针拉取新版本。submodule 操作详情、故障排查见 `Workspace/DocsAI/GitSubmoduleWorkflow.md`。

执行 git status、git diff、commit、branch 等操作前，必须先进入对应仓库目录，确认当前 Git 边界。不要在工作区根目录重新初始化 Git。

## 必读入口

- 框架文档事实源：当前以 `SlimeAI/Src/ECS/**` 旁文档、`SlimeAI/DocsNew/` 和相关 SDD design 为准；`SlimeAI/DocsAI/` 已删除，不再作为入口
- SystemAgent 工作流事实源：`Workspace/SystemAgent/README.md`、`Workspace/SystemAgent/README.md`
- 当前状态：`SDD/INDEX.md`、当前项目 `README.md` / `progress.md`，以及对应 `SlimeAI/Src/ECS/**` 旁文档
- SDD 任务上下文：`SDD/INDEX.md`、`SDD/active/<sdd>/`
- 游戏侧状态：`Games/BrotatoLike/DocsAI/INDEX.md`
- Godot 场景测试：`SlimeAI/Src/ECS/Test/**` 旁 README、`Games/BrotatoLike/Tools/run-godot-scene.sh`
- 工作区跨仓库工作流：`Workspace/DocsAI/INDEX.md`

## 事实源边界

- `SlimeAI/DocsAI/` 已删除，不再保存框架长期知识；不要恢复、引用或继续局部修补。当前框架知识临时回到 `SlimeAI/Src/ECS/**` 旁文档、`SlimeAI/DocsNew/` 和 SDD design，后续再统一收敛。
- `Workspace/SystemAgent/` 保存 SystemAgent workflow、role、protocol、gate、policy、catalog、config 和工具正文的长期事实源；`Workspace/DocsAI/AgentWorkflow/` 如保留仅作迁移指针。
- `.ai-config/skills/*` 是唯一可维护 skill 源，保存 skill 路由、命令、reference 和脚本入口；`.codex/.claude/.windsurf/skills` 是同步副本。
- `.ai-config/rules/rules.md` 是 rule 源；`CLAUDE.md`、`.windsurf/rules/windsurfrules.md` 是同步副本；`AGENTS.md` 是 Codex 根规则入口，需与 rule 源保持同义。
- `.claude/settings.json`、`.claude/agents/`、`.codex/hooks.json`、`.codex/agents/`、`.codex/config.toml` 是 hook/subagent 运行配置，直接维护，不走 `.ai-config` 同步。
- `.ai-config/skills/godot/godot-scene-test/scripts/` 是 Godot 场景测试 runner/analyzer 的维护源，工具 skill 副本由同步脚本生成。
- `Games/BrotatoLike/DocsAI/` 只保存游戏项目文档，不吞并到框架 DocsAI。
- `SDD/` 是中大型任务上下文事实源；`Workspace/SDD/` 只保存 CLI、模板和校验规则。
- `openspec/` 仅保留历史资产和显式兼容维护入口，不作为 SystemAgent 默认计划或执行入口。

## 修改规则

- 默认中文回答；命令、代码、错误信息保留原文。
- 改文件前先读相关文件；涉及文件修改时前后运行 `git status --short`。
- 大型功能、架构变更、跨模块重构、长期设计决策优先使用 SDD。
- 不要把 BrotatoLike 专属玩法、UI、资产路径上提为框架默认。
- 不随意加依赖、大重构。AI 可自动 commit（需先 `git status --short` + 明确 message）；push 需确认。
- 不覆盖、回滚、删除用户已有改动。

## Git 操作约束

- 执行任何 git 操作前，先 `cd` 到目标仓库目录，确认 `git status` 输出符合预期。
- 工作区根目录 `/home/slime/Code/SlimeAI` 已是 Git 仓库，禁止在此执行 `git init` 或误把嵌套仓库改动当成同一个提交范围。
- 更新游戏仓的 SlimeAI submodule：优先使用 VSCode Task `update: BrotatoLike SlimeAI Submodule`。
- 手动更新时：
  1. `cd Games/<Game>`（不是工作区根）
  2. `git submodule update --remote SlimeAI`
  3. 如报错"未跟踪文件将被覆盖"：先在 `SlimeAI/` 目录内处理（提交到框架仓或 `git clean -fd`）
  4. 最后在游戏仓 `git add SlimeAI && git commit`
- 禁止在游戏仓的 `SlimeAI/` 目录内直接做业务改动；框架改动必须切换到 `/home/slime/Code/SlimeAI/SlimeAI` 处理。
- `**/*.uid` 已在框架仓 `.gitignore` 中全局忽略；如发现未跟踪 `.uid` 文件，先确认 `.gitignore` 是否生效。

## 工作区视野约束

- 日常开发关注：`SlimeAI/`、`Games/BrotatoLike/`、`.ai-config/`、`SDD/`
- `Resources/Engine/` —— 引擎源码与框架分析报告，研究参考时查阅
- `Resources/Games/` —— 破解游戏逆向素材与分析文档，游戏机制参考时查阅
- `Resources/Agent/` —— 外部 AI 项目分析，agent 工作流参考时查阅
- `Resources/Else/` —— 旧框架代码，仅迁移对照，禁止作为事实源

## AI 配置统一源

本工作区同时维护 Claude、Codex、Windsurf 的 skill、rule、command、hook 和 subagent。**skill/rule/command 使用 `.ai-config` 统一源；hook/subagent 直接写工具项目配置。**

| 类型 | 维护位置 | 副本位置 | 同步方式 |
| ---- | ------ | -------- | -------- |
| Skill | `.ai-config/skills/<category>/<name>/SKILL.md` | `.codex/skills/`、`.claude/skills/`、`.windsurf/skills/`（打平） | `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` |
| Rule | `.ai-config/rules/rules.md` | `AGENTS.md`、`CLAUDE.md`、`.windsurf/rules/windsurfrules.md` | 同上（Windsurf 副本由脚本自动追加 frontmatter） |
| Command | `.ai-config/skills/<category>/<name>/SKILL.md` | `.claude/commands/opsx/*.md`（仅兼容命令需要时生成） | 同上（脚本自动转换格式） |
| Claude hook | `.claude/settings.json` | 无副本 | 直接维护 |
| Claude subagent | `.claude/agents/*.md` | 无副本 | 直接维护 |
| Codex hook | `.codex/hooks.json` | 无副本 | 直接维护 |
| Codex subagent | `.codex/agents/*.toml`、`.codex/config.toml` | 无副本 | 直接维护 |

**skill/rule/command 只改 `.ai-config/`，不改副本**。脚本通过遍历实现，不硬编码分类名；`.ai-config/skills/` 下一层目录作为分类，skill 目录在分类下，同步时自动打平到各工具顶层。

**禁止直接修改同步副本**：`.codex/skills/`、`.claude/skills/`、`.windsurf/skills/`、`.claude/commands/opsx/`、`AGENTS.md`、`CLAUDE.md`、`.windsurf/rules/windsurfrules.md`。
改完后**必须**运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`，否则副本会被下次同步覆盖。

**允许直接修改项目运行配置**：`.claude/settings.json`、`.claude/agents/`、`.codex/hooks.json`、`.codex/agents/`、`.codex/config.toml`。这些不是 `.ai-config` 同步副本。

## SDD 工作流

- SDD 是中大型任务的默认计划、执行记忆和恢复事实源；格式与 CLI 以 `Workspace/SDD/` 为准。
- 大型功能、架构变更、跨模块重构、长期设计决策、迁移账本和跨目录文档治理，优先进入 `SDD/active/<sdd>/`。
- 探索阶段可普通分析，只读代码和文档，不直接改实现；设计发现使用 `sdd-design-discovery`。
- 创建和管理任务使用 `sdd-workflow` / `sdd-management`，并维护 `README.md`、`design/`、`tasks.md`、`progress.md`、`bdd.md`。
- 执行中每完成一批任务，及时更新对应 `tasks.md` checkbox 和 `progress.md` Latest Resume，并同步必要的 `SlimeAI/Src/ECS/**` 旁文档、`SlimeAI/DocsNew/`、SDD design 或游戏侧状态文档。
- 完成后按影响面运行验证；文档类至少检查 `python3 Workspace/SDD/sdd.py validate <sdd-id>` 和目标文件清单，代码类按下方验证入口执行。
- 极小修复、拼写、链接、注释、临时排查和一次性脚本不强制使用 SDD；必要时仍更新相关状态文档。

## OpenSpec 兼容边界

- `openspec/` 与 `.ai-config/skills/openspec/` 仅作为历史兼容资产保留。
- 除非用户明确要求处理历史 OpenSpec change、归档旧资产或维护兼容命令，否则新任务不进入 OpenSpec。
- 不把 OpenSpec active/archived change 当作当前 SystemAgent 默认入口或长期事实源。

## 验证入口

框架验证：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
Tools/run-build.sh
Tools/run-tests.sh
```

Godot 场景验证：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
