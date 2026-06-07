# SystemAgent 目录与 AI 配置迁入 SlimeAI 后的规则更新设计

> 日期：2026-05-30
> 状态：设计发现 / 已迁移后修订
> 范围：`SlimeAI/SDD`、`SlimeAI/Workspace`、`SlimeAI/.ai-config`、`SlimeAI/.claude`、`SlimeAI/.codex`、`SlimeAI/.windsurf`、`SlimeAI/AGENTS.md`、`SlimeAI/DocsNew`、`SlimeAI/Src/ECS/**`
> 结论：目录已迁入 `SlimeAI/`；下一步不是继续搬目录，而是把 rules / skill / SDD / Workspace 文档从“旧工作区规则”改成“框架仓内规则”。

## 1. 当前事实

用户已经完成关键目录迁移：

```text
/home/slime/Code/SlimeAI/SlimeAI/
  .ai-config/
  .claude/
  .codex/
  .windsurf/
  SDD/
  Workspace/
```

旧根 `/home/slime/Code/SlimeAI` 现在应视为外层资源与多仓容器，不再是 SlimeAI ECS 框架的 AI 主目录。框架仓内 `Workspace/Tools/ai-config-sync/sync-ai-config.sh` 仍使用：

```bash
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
```

因为脚本现在位于 `SlimeAI/Workspace/Tools/ai-config-sync/`，所以 `ROOT` 已经自然解析为 `/home/slime/Code/SlimeAI/SlimeAI`。也就是说，旧设计里“必须改 target root，否则会生成到旧根”的风险已经被目录迁移消除。

当前真正的问题是：被迁入的正文仍大量保留旧工作区语义。

## 2. 设计判断

我的判断是：这次迁移方向正确，但现在进入第二阶段，重点应从“目录移动”切到“语义收口”。

现在 `SlimeAI/` 内同时包含源码、SDD、Workspace、AI 配置和工具副本，这让 AI 打开框架仓时可以自包含地恢复上下文。但如果 rules 和 skill 仍写：

- “当前目录 `/home/slime/Code/SlimeAI` 是工作区根”
- “默认入口是 `AGENTS.md -> Workspace/SystemAgent/README.md -> SDD/INDEX.md -> SlimeAI/Src/ECS/**`”
- “框架文档事实源是 `SlimeAI/DocsNew` / `SlimeAI/Src/ECS`”

那么迁移后会产生双层路径与角色混乱：

```text
旧写法：SlimeAI/Src/ECS
新根内实际应写：Src/ECS

旧写法：SlimeAI/DocsNew
新根内实际应写：DocsNew

旧写法：/home/slime/Code/SlimeAI 是工作区根
新语义：/home/slime/Code/SlimeAI/SlimeAI 是框架仓与 AI 主目录
```

因此，本阶段目标不是继续扩大迁移，而是把 `SlimeAI/` 内所有“当前入口规则”改到一致。

## 3. 推荐入口链

迁移后 SlimeAI ECS 框架任务的默认入口应是：

```text
AGENTS.md
  -> DocsNew/README.md
  -> SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md
  -> SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md
  -> Src/ECS/** 旁文档
  -> owner skill
  -> Tools/run-build.sh / Tools/run-tests.sh / Godot scene test
```

SystemAgent 不应再作为 ECS 业务的第一入口。它应保留为流程工具：

- 大任务设计发现。
- SDD 管理。
- AI config / skill / rule 同步治理。
- 验证发布和复盘。
- subagent / hook / gate 规则。

也就是说：

```text
业务事实源：DocsNew + SDD/PRJ-0002 + Src/ECS/** 旁文档
流程事实源：Workspace/SystemAgent
配置事实源：.ai-config
生成副本：.claude / .codex / .windsurf / AGENTS.md / CLAUDE.md
```

## 4. 必须更新的目录语义

### 4.1 AI 配置统一源

当前维护源应是：

```text
SlimeAI/.ai-config/
```

同步目标应是：

```text
SlimeAI/AGENTS.md
SlimeAI/CLAUDE.md
SlimeAI/.claude/skills/
SlimeAI/.claude/commands/opsx/
SlimeAI/.codex/skills/
SlimeAI/.windsurf/skills/
SlimeAI/.windsurf/rules/windsurfrules.md
```

规则文档必须明确：

- 只改 `.ai-config/skills/**`、`.ai-config/rules/rules.md`。
- 不直接改 `.claude/skills`、`.codex/skills`、`.windsurf/skills`。
- 改完后从框架仓运行：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
```

### 4.2 SDD

当前 SDD 根应是：

```text
SlimeAI/SDD/
```

常用命令应从框架仓运行：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
python3 Workspace/SDD/sdd.py validate --all
python3 Workspace/SDD/sdd.py list
python3 Workspace/SDD/sdd.py show SDD-xxxx
```

`Workspace/SDD/Src/config.py` 当前通过 `parents[3]` 推导 `REPO_ROOT`。在新位置下，这会正确指向 `/home/slime/Code/SlimeAI/SlimeAI`。但 `Workspace/SDD/Src/templates.py` 模板里仍有：

```text
Git Boundary: /home/slime/Code/SlimeAI
```

应更新为：

```text
Git Boundary: /home/slime/Code/SlimeAI/SlimeAI
```

### 4.3 Workspace

当前 Workspace 根应是：

```text
SlimeAI/Workspace/
```

`Workspace/SystemAgent/` 保留，但语义要改：

- 不再是旧根工作区的长期事实源。
- 是框架仓内的 AI 执行流程事实源。
- 其中仍引用 `SlimeAI/DocsAI/GameOS/...` 的文档需要标历史或改向 `DocsNew` / `SDD` / `Src/ECS`。

### 4.4 DocsNew

当前 DocsNew 根应是：

```text
SlimeAI/DocsNew/
```

迁移后相对引用不应再使用 `../../SDD` 或 `../../Workspace` 这种旧根假设。因为 `DocsNew/` 位于框架仓内，指向 SDD 和 Workspace 时应是：

```text
../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/
../Workspace/SystemAgent/
```

`DocsNew/README.md` 目前仍有入口漂移：表格写 `01-*`、`02-*`，实际文件是：

```text
DocsNew/ECS框架与AIFirst方向决策.md
DocsNew/ECS/Data/Data系统说明.md
```

这应列入第一批文档修正。

## 5. rules.md 重写设计

`SlimeAI/.ai-config/rules/rules.md` 当前仍是旧工作区规则。应重写为框架仓规则，建议结构如下。

### 5.1 标题

```text
# SlimeAI ECS 框架仓规则
```

### 5.2 定位

规则应明确：

- 当前目录 `/home/slime/Code/SlimeAI/SlimeAI` 是 SlimeAI ECS 框架仓，也是 AI 主目录。
- 当前框架是 Godot 4.6 C# AI-first ECS 框架主线。
- 保留 Entity / Component / Data / Event / System / Relationship / Test / Docs 等 ECS 心智模型。
- AI-first 的含义是入口、事实源、调试、验证和工作流更可执行，不是把旧 ECS 替换成纯 GameOS。

应删除旧说法：

- “这是 AI-first GameOS 框架工作区”。
- “当前目录 `/home/slime/Code/SlimeAI` 是工作区根”。
- “框架改动必须切换到 `/home/slime/Code/SlimeAI/SlimeAI` 处理”。

迁移后已经在框架仓内，规则应写：

```text
框架改动默认在当前仓处理；游戏仓或外层资源目录只在任务明确涉及时访问。
```

### 5.3 必读入口

建议写成：

```text
- 方向入口：DocsNew/README.md
- 当前项目设计：SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md
- 设计索引：SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md
- 模块事实源：Src/ECS/** 旁文档
- Data 当前说明：DocsNew/ECS/Data/Data系统说明.md
- 流程工具：Workspace/SystemAgent/README.md
```

### 5.4 事实源边界

应明确：

- `DocsNew/`：方向决策、临时总览、少量系统说明。
- `SDD/`：中大型任务设计、进度、执行记忆。
- `Src/ECS/**` 旁文档：当前模块契约与实现说明。
- `.ai-config/`：skill / rule / command 维护源。
- `.claude/.codex/.windsurf`：生成副本或工具运行配置。
- `Workspace/SystemAgent/`：流程、角色、gate、hook、skill-test 工具。
- `DocsAI/`：已删除旧入口，不恢复，不作为当前事实源。

### 5.5 ECS 架构红线

可从现有 `SlimeAI/AGENTS.md` 保留并改写：

- Entity 生命周期通过 `EntityManager` / 当前 Entity runtime 入口处理。
- 业务状态进 `Data`，不放组件私有字段作为长期事实源。
- 数据键使用 DataOS descriptor 生成的 typed handle。
- 核心通信走 `EventBus` / Entity events，不用 Godot Signal 承载核心逻辑。
- 资源加载走 `ResourceManagement`。
- 计时、目标选择、伤害、对象池走对应系统或服务。
- `_Process` 中禁止高频 `new` 和 LINQ。

Data 规则必须跟 SDD-0022 后状态一致：

- 新增 DataKey 先写 DataOS descriptor。
- runtime snapshot 是运行时字段定义与 records 的来源。
- generated handle 是业务访问入口。
- 不恢复旧 `DataMeta` / `DataRegistry` / `DataKey_Compatibility` 作为事实源。

### 5.6 Git 边界

迁移后规则应简化为：

- 当前仓：`/home/slime/Code/SlimeAI/SlimeAI`。
- 外层资源：`/home/slime/Code/SlimeAI/Resources`，只作显式研究输入。
- 游戏仓：`/home/slime/Code/SlimeAI/Games/*`，需要时单独进入对应仓运行 `git status --short`。
- 游戏 submodule：`Games/*/SlimeAI/` 仍是只读镜像，禁止直接做框架业务改动。

不应再把外层 `/home/slime/Code/SlimeAI` 描述成当前 AI 配置仓。

### 5.7 验证入口

框架验证：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
Tools/run-build.sh
Tools/run-tests.sh
```

SDD / AI 配置验证：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
python3 Workspace/SDD/sdd.py validate --all
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

Godot 场景验证仍需要进入承载游戏仓：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

## 6. 路径更新清单

### 6.1 立即更新

| 文件 | 当前问题 | 应改为 |
| --- | --- | --- |
| `.ai-config/rules/rules.md` | 仍是旧工作区 / GameOS 规则 | ECS 框架仓规则 |
| `AGENTS.md` | 旧 OpenSpec / DocsAI / 纠偏入口 | 由新 rules 生成 |
| `CLAUDE.md` | 当前缺失，sync 后应生成 | 由新 rules 生成 |
| `.windsurf/rules/windsurfrules.md` | 应跟 rules 同义 | 由 sync 生成 |
| `DocsNew/README.md` | 指向不存在的 `01-*` / `02-*` | 指向真实文件名 |
| `Workspace/SDD/Src/templates.py` | Git Boundary 是旧根 | 改为框架仓路径 |
| `.ai-config/skills/godot/godot-scene-test/SKILL.md` | 旧 `.ai-config` 绝对路径、旧 DocsAI | 改为新根与当前事实源 |
| `.ai-config/skills/godot/godot-scene-test/scripts/analyze-logs.sh` | 指向旧 `SlimeAI/DocsAI` | 改为实际存在的 catalog 或 artifact 规则 |

### 6.2 第二批更新

| 区域 | 原因 |
| --- | --- |
| `.ai-config/skills/**` | 多处仍引用 `DocsAI/GameOS`、`SlimeAI/DocsAI`、旧外层路径 |
| `Workspace/SystemAgent/Rules/**` | 多处仍把 GameOS / DocsAI 作为默认语义 |
| `Workspace/Docs/**` | 多数是历史说明，可标 historical 或从入口移除 |
| `SDD/project/**/execution-prompt.md` | 已完成历史记录不批量改；只改 current / future 模板 |

## 7. 执行顺序

推荐按以下顺序推进：

1. 更新本设计文档，冻结“目录已迁入 SlimeAI”的新事实。
2. 更新 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md`，删除旧 `2.1` 记录，登记 `4.SystemAgent...`。
3. 重写 `.ai-config/rules/rules.md` 为 ECS 框架仓规则。
4. 运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 生成 `AGENTS.md`、`CLAUDE.md` 和三工具副本。
5. 修 `DocsNew/README.md` 的入口漂移。
6. 修 `Workspace/SDD/Src/templates.py` 的 Git Boundary 模板。
7. 修高风险 skill：`godot-scene-test`、`ai-config-management`、`project-index`、`ecs-data`。
8. 运行 `python3 Workspace/SDD/sdd.py validate --all`、`skill-test`、`git diff --check`。
9. 再决定是否清理 `.history/.ai-config`、`.history/SDD`、`.history/Workspace`。

## 8. 风险与取舍

### 8.1 最大风险：sync 会覆盖手写规则

迁移后 `sync-ai-config.sh` 已会生成当前仓副本，所以不能再手改 `AGENTS.md` 作为长期源。正确做法是：

```text
改 .ai-config/rules/rules.md -> sync -> 检查 AGENTS.md / CLAUDE.md / Windsurf rules
```

### 8.2 历史路径不能全局替换

不能简单把 `/home/slime/Code/SlimeAI` 全部替换成 `/home/slime/Code/SlimeAI/SlimeAI`。原因：

- 游戏仓仍在 `/home/slime/Code/SlimeAI/Games/*`。
- Resources 仍在 `/home/slime/Code/SlimeAI/Resources`。
- 历史 SDD / ChatHistory 中的旧路径是证据，不一定要改。
- Godot submodule 路径里的 `SlimeAI/Src` 可能是游戏仓内框架镜像，不等同于框架仓根。

### 8.3 SystemAgent 不能喧宾夺主

迁入 `Workspace/` 后，SystemAgent 很容易再次成为默认入口。规则必须压住这一点：

- ECS 业务任务先读 DocsNew / SDD / Src 旁文档。
- SystemAgent 是流程和 gate，不是 ECS 业务事实源。

### 8.4 DocsAI 残留要分级处理

`DocsAI/` 已删除，不应恢复。但历史文档、聊天记录、旧计划里会大量出现 `DocsAI`。处理策略：

- 当前规则 / skill / README：必须改。
- 当前验证脚本：必须改。
- 历史 SDD / ChatHistory：不批量改，可保留证据。
- Workspace 旧说明：标 historical 或从默认入口移除。

## 9. Must Confirm

继续实施前建议确认：

1. `SlimeAI/.ai-config/rules/rules.md` 是否现在就重写为框架仓规则，并允许 sync 生成 `SlimeAI/AGENTS.md` / `SlimeAI/CLAUDE.md`？
2. `.history/.ai-config`、`.history/SDD`、`.history/Workspace` 是否只保留历史，不纳入后续验证？
3. 旧根 `/home/slime/Code/SlimeAI/AGENTS.md` 是否还需要单独维护为“外层资源工作区说明”？

## 10. 默认执行假设

如果不额外确认，我建议默认：

- 现在就把 `SlimeAI/.ai-config/rules/rules.md` 改为 ECS 框架仓规则。
- 运行 sync 生成 `AGENTS.md` / `CLAUDE.md` / Windsurf rules。
- `.history/**` 只作为历史，不进入 active grep gate。
- 旧根 `AGENTS.md` 暂不改，避免跨 Git 边界扩大范围。
- 本轮只处理 `SlimeAI/` 仓内规则、设计、模板和高风险 skill。
