# SDD 重构与 CLI 详细执行计划

> 本文档是 SDD 独立化重构的执行计划草案，用于用户评审。当前阶段只规划，不直接修改 `.ai-config/skills`、`Workspace/SDD`、`SDD` 或 SystemAgent 正文。

## 1. 结论摘要

SDD 应作为 SlimeAI 工作区内独立的中大型任务管理系统，而不是 SystemAgent 的子目录、OpenSpec 的子命令，或某个 AI 工具的私有格式。

推荐最终边界如下：

| 层级 | 路径 | 定位 | 是否长期事实源 |
| --- | --- | --- | --- |
| SDD 系统源码 | `/home/slime/Code/SlimeAI/Workspace/SDD/` | SDD CLI、模板、校验器、文档和测试 | 是 |
| SDD 任务实例 | `/home/slime/Code/SlimeAI/SDD/` | 每个中大型任务的设计、计划、执行、恢复、归档单元 | 是 |
| SDD skill 源 | `/home/slime/Code/SlimeAI/.ai-config/skills/sdd/` | AI 使用 SDD 流程的技能入口和能力说明 | 是 |
| 当前计划评审区 | `/home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0001-systemagent-optimization/design/` | 本轮 SDD 重构计划、草案、用户评审材料 | 否，后续迁移或归档 |

推荐 CLI 技术选型：**Python 标准库优先，Shell wrapper 辅助，不引入第三方依赖**。

原因：

- 工作区已有 Python 工具链，例如 `Workspace/SystemAgent/Tools/skill-test/lint.py` 和 `Workspace/SystemAgent/Tools/systemagent-hooks/systemagent_hook.py`。
- SDD CLI 的核心任务是文件系统操作、JSON/Markdown 读写、校验、索引和状态流转，不需要复杂依赖。
- Python 标准库足够覆盖 `argparse`、`json`、`pathlib`、`dataclasses`、`datetime`、`subprocess` 等能力。
- 与 superpowers zero-dependency brainstorm server 的思想一致：能不用依赖就不用依赖，降低安装成本和维护风险。
- 与 SlimeAI 现有规则一致：不随意加依赖、大重构。

推荐命令入口：

```text
Workspace/SDD/sdd.py
Workspace/SDD/sdd.sh
```

其中：

- `sdd.py` 是权威 CLI 实现。
- `sdd.sh` 是薄 wrapper，负责定位工作区根并调用 `python3 Workspace/SDD/sdd.py`。
- 后续可选择在 `Workspace/Tools/` 增加 `run-sdd.sh`，但 MVP 不必增加额外入口。

## 2. 目标与非目标

### 2.1 目标

1. 建立 SDD 的独立源码根：`Workspace/SDD/`。
2. 建立 SDD 的独立任务根：`SDD/`。
3. 在 `.ai-config/skills/sdd/` 下新增 SDD 相关 skill，引用该 skill 即代表进入 SDD 流程。
4. 设计一个轻量、可恢复、可校验的 SDD CLI。
5. 让 SystemAgent 可以调用 SDD，但不把 SDD 吞并进 SystemAgent。
6. 让中大型任务具备固定上下文胶囊，减少跨会话丢上下文。
7. 用 SDD 逐步承接 OpenSpec 的任务管理职责，但不立即删除 OpenSpec。

### 2.2 非目标

1. 本计划不直接实现 CLI。
2. 本计划不立即创建正式 `SDD/active/*` 实例，除非用户确认进入实施阶段。
3. 本计划不删除 OpenSpec。
4. 本计划不移动大量旧文档。
5. 本计划不修改 `.codex/skills`、`.claude/skills`、`.windsurf/skills` 同步副本。
6. 本计划不引入 npm、pip 第三方依赖。
7. 本计划不把 SDD 设计成必须依赖某个 AI 工具的格式。

## 3. 为什么 SDD 必须独立

当前 SystemAgent 优化文档已经形成了大量设计、分层、hook、gate、skill、subagent、OpenSpec 退场相关材料。若继续把这些材料留在 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/`，会出现几个问题：

1. **任务事实源不清晰**：设计、执行状态、验证结果和临时想法混在同一目录。
2. **恢复成本高**：跨会话恢复时，AI 需要读大量过渡文档，才能判断当前任务进度。
3. **OpenSpec 过重**：OpenSpec 适合规格变更，但对于“设计-实施-验证-复盘”的连续工程任务，格式和命令成本偏高。
4. **SystemAgent 被污染**：如果把任务实例放入 `Workspace/SystemAgent/`，SystemAgent 会同时承担“流程框架”和“任务记录”两种职责。
5. **AI 配置边界不清**：skill 是流程入口，SDD 是任务事实载体，二者需要通过明确接口连接，而不是互相嵌套。

因此，推荐模型是：

```text
SystemAgent = AI 执行治理框架
SDD = 中大型任务上下文胶囊
.ai-config/skills/sdd = AI 进入 SDD 流程的技能入口
Workspace/SDD = SDD 系统实现
SDD/ = SDD 任务实例库
```

## 4. 参考 superpowers brainstorming 的取舍

### 4.1 可借鉴点

superpowers brainstorming 的核心价值不是具体目录，而是流程控制：

1. **先理解上下文，再设计**：做方案前先读项目文件、现有文档和约束。
2. **实现前硬 gate**：在设计获得用户确认前，不写代码、不脚手架、不实施。
3. **多方案对比**：给出 2-3 个方案、权衡和推荐。
4. **设计文档落盘**：把讨论结果固化为 spec/design 文档。
5. **自检**：检查 TBD、TODO、矛盾、范围过大、歧义。
6. **设计转计划**：设计批准后，再生成 implementation plan。
7. **计划可交接**：计划假设执行者没有上下文，必须写清文件、任务、测试和预期结果。

这些点应吸收到 SDD 流程中。

### 4.2 不应照搬点

以下内容不适合 SlimeAI 直接照搬：

1. **强制所有创造性任务都 brainstorming**：SlimeAI 已有 SystemAgent workflow 和 owner skill，SDD 应只覆盖中大型任务。
2. **逐问逐答**：用户已明确偏好“深度思考、详细分析”，SlimeAI 更适合输出确认包，而不是强制一次一个问题。
3. **固定 docs/superpowers 路径**：SlimeAI 需要 `Workspace/SDD` 与 `SDD` 双根结构。
4. **自动 commit 设计文档**：SlimeAI 允许自动 commit，但仍需先确认 Git 边界和范围；计划阶段不默认 commit。
5. **visual companion**：SDD CLI MVP 不需要浏览器伴侣。
6. **Node-first**：SlimeAI 现有工作区工具更偏 Python 与 Bash，SDD CLI 不应为了参考 superpowers 而引入 Node 运行时设计。

### 4.3 转化为 SDD 规则

SDD 应吸收为以下流程规则：

```text
需求/问题 -> 上下文探索 -> 设计确认 -> SDD 创建/绑定 -> 计划拆解 -> 执行记录 -> 验证证据 -> 归档
```

对应 CLI 支持：

```text
sdd new       创建 SDD 胶囊
sdd validate 运行结构和内容校验
sdd status   展示状态、任务进度、阻塞项
sdd note     追加上下文和决策
sdd task     管理任务 checkbox
sdd block    标记阻塞
sdd done     完成并进入归档候选
```

## 5. 总体架构

### 5.1 三层结构

```text
/home/slime/Code/SlimeAI/
├── Workspace/
│   └── SDD/
│       ├── README.md
│       ├── sdd.py
│       ├── sdd.sh
│       ├── docs/
│       │   ├── SDDFormat.md
│       │   ├── CLI.md
│       │   └── ValidationRules.md
│       ├── templates/
│       │   ├── instance/
│       │   │   ├── README.md
│       │   │   ├── sdd.json
│       │   │   ├── tasks.md
│       │   │   ├── progress.md
│       │   │   ├── bdd.md
│       │   │   ├── notes.md
│       │   │   └── design/INDEX.md
│       │   └── root/
│       │       └── README.md
│       └── tests/
│           ├── fixtures/
│           └── test_sdd_cli.py
├── SDD/
│   ├── README.md
│   ├── index.json
│   ├── active/
│   │   └── SDD-0001-example/
│   ├── paused/
│   └── archived/
└── .ai-config/
    └── skills/
        └── sdd/
            ├── sdd-workflow/
            │   └── SKILL.md
            ├── sdd-management/
            │   └── SKILL.md
            └── sdd-design-discovery/
                └── SKILL.md
```

### 5.2 分层职责

| 层 | 负责 | 不负责 |
| --- | --- | --- |
| `Workspace/SDD` | CLI、模板、校验规则、格式文档、测试 | 保存某个任务的执行历史 |
| `SDD` | 任务实例、状态、设计、计划、验证、归档 | 实现 CLI 或存放 skill 定义 |
| `.ai-config/skills/sdd` | 告诉 AI 何时进入 SDD、如何调用 CLI、如何读写 SDD artifact | 保存任务内容或工具实现 |
| `Workspace/SystemAgent` | 总体 AI workflow/gate/policy，必要时路由到 SDD | 变成 SDD 的任务实例目录 |
| `openspec` | 规格基线和已有变更兼容 | 新任务的默认任务管理入口 |

## 6. SDD 任务实例格式

### 6.1 推荐目录

每个 SDD 是一个独立目录：

```text
SDD/active/SDD-0001-systemagent-optimization/
├── README.md
├── sdd.json
├── design/
│   ├── INDEX.md
│   ├── 01-context.md
│   ├── 02-options.md
│   └── 03-final-design.md
├── tasks.md
├── progress.md
├── bdd.md
├── notes.md
└── artifacts/
```

### 6.2 文件职责

| 文件 | 职责 |
| --- | --- |
| `README.md` | 人和 AI 的入口卡片，展示目标、当前状态、下一步、关键风险、验证入口 |
| `sdd.json` | 机器可读元数据，例如 id、title、status、owner、created、updated、paths、tags |
| `design/INDEX.md` | 设计文档索引，说明哪些设计是 canonical，哪些只是参考 |
| `design/*.md` | 需求、上下文、方案、最终设计、取舍记录 |
| `tasks.md` | 可执行任务清单，使用 checkbox 管理进度 |
| `progress.md` | 时间线式执行日志，记录本轮完成、验证、阻塞和下一步 |
| `bdd.md` | 行为场景和验收标准，供实现和验证引用 |
| `notes.md` | 临时笔记、用户确认点、跨会话提醒 |
| `artifacts/` | 验证产物、日志、报告、截图、命令输出摘要 |

### 6.3 状态模型

SDD 实例的状态建议限定为：

| 状态 | 含义 | 所在目录 |
| --- | --- | --- |
| `draft` | 正在设计，未批准实施 | `SDD/active/` |
| `ready` | 设计已确认，可开始实施 | `SDD/active/` |
| `active` | 正在实施 | `SDD/active/` |
| `blocked` | 阻塞，需要用户或外部条件 | `SDD/active/` 或 `SDD/paused/` |
| `paused` | 暂停，保留上下文但不推进 | `SDD/paused/` |
| `done` | 已完成，等待归档或已归档 | `SDD/archived/` |
| `archived` | 完成并进入历史 | `SDD/archived/` |

MVP 可以先不自动移动目录，只更新 `sdd.json.status` 并在 `README.md` 中展示状态。目录移动作为后续增强。

## 7. CLI 设计

### 7.1 技术选型

推荐：Python 标准库 + Bash wrapper。

备选方案对比：

| 方案 | 优点 | 缺点 | 结论 |
| --- | --- | --- | --- |
| Python 标准库 | 与现有工具一致；跨平台足够；JSON/Markdown/文件操作简单；无需依赖 | Markdown AST 能力较弱，需要约定简单格式 | 推荐 |
| Bash | 启动快；适合 wrapper | 复杂 JSON、任务状态、跨平台处理差 | 只做 wrapper |
| Node.js | superpowers 有参考；前端生态强 | SlimeAI 当前工具链不以 Node 为主；容易引入依赖 | 不推荐 MVP |
| dotnet tool | 与 Godot C# 技术栈一致；类型强 | 开发和分发成本高；对简单文件工具过重 | 后续可评估 |
| Rust/Go | 单文件二进制体验好 | 新增语言栈和构建成本 | 不推荐 MVP |

### 7.2 CLI 入口

MVP 推荐命令：

```bash
bash Workspace/SDD/sdd.sh <command> [args]
```

也支持直接：

```bash
python3 Workspace/SDD/sdd.py <command> [args]
```

不推荐一开始要求全局安装 `sdd` 命令，避免引入 PATH、打包和环境差异问题。

### 7.3 CLI 命令分期

#### Phase 1：只读与校验 MVP

| 命令 | 作用 |
| --- | --- |
| `sdd list` | 列出 `SDD/active`、`SDD/paused`、`SDD/archived` 下的实例 |
| `sdd show <id>` | 展示指定 SDD 的入口摘要 |
| `sdd validate [id\|--all]` | 校验结构、metadata、任务格式和必备字段 |
| `sdd index` | 重建 `SDD/index.json` |
| `sdd doctor` | 检查根目录、模板、索引和常见错误 |

这一阶段不做状态修改，风险低，可先落地。

#### Phase 2：创建与记录

| 命令 | 作用 |
| --- | --- |
| `sdd init-root` | 创建 `SDD/` 根目录、README、index 和状态目录 |
| `sdd new <title>` | 根据模板创建新 SDD 实例 |
| `sdd note <id> --text ...` | 追加 `notes.md` 记录 |
| `sdd progress <id> --text ...` | 追加 `progress.md` 记录 |
| `sdd set-status <id> <status>` | 更新 `sdd.json.status` 和入口摘要 |

#### Phase 3：任务与阻塞管理

| 命令 | 作用 |
| --- | --- |
| `sdd task list <id>` | 列出 `tasks.md` checkbox |
| `sdd task add <id> <text>` | 添加任务 |
| `sdd task check <id> <task-number>` | 勾选任务 |
| `sdd task uncheck <id> <task-number>` | 取消勾选 |
| `sdd block <id> --reason ...` | 标记阻塞并写入 progress |
| `sdd unblock <id> --reason ...` | 解除阻塞 |

#### Phase 4：归档与迁移

| 命令 | 作用 |
| --- | --- |
| `sdd done <id>` | 完成状态检查，确认任务、验证和文档齐全 |
| `sdd archive <id>` | 移动到 `SDD/archived/` 并刷新索引 |
| `sdd migrate-openspec <change>` | 将 OpenSpec change 信息转换为 SDD 草案 |
| `sdd import-design <id> <paths...>` | 把外部设计文档复制或登记到 `design/` |

### 7.4 命令输出原则

CLI 输出应适合人和 AI 共同使用：

1. 默认输出简洁文本表格。
2. 支持 `--json` 输出机器可读结果。
3. 错误信息必须明确指出文件路径、规则 ID 和修复建议。
4. 写操作默认显示将修改的文件。
5. 高风险操作，例如 `archive`、批量迁移、覆盖文件，应要求 `--yes`。
6. 不自动 commit。

示例输出形态：

```text
SDD validate: SDD-0001-systemagent-optimization
Status: active
Checks: 12 PASS, 1 FAIL, 2 WARN

FAIL SDD001 missing-required-file: bdd.md
  path: SDD/active/SDD-0001-systemagent-optimization/bdd.md
  fix: create from Workspace/SDD/templates/instance/bdd.md
```

### 7.5 校验规则 MVP

| 规则 ID | 严重度 | 检查内容 |
| --- | --- | --- |
| `SDD001` | error | 实例必须包含 `README.md`、`sdd.json`、`tasks.md`、`progress.md`、`bdd.md`、`notes.md`、`design/INDEX.md` |
| `SDD002` | error | `sdd.json` 必须是合法 JSON |
| `SDD003` | error | `sdd.json.id` 必须与目录名前缀一致 |
| `SDD004` | error | `sdd.json.status` 必须属于允许状态 |
| `SDD005` | warn | `README.md` 必须包含目标、当前状态、下一步、验证入口 |
| `SDD006` | warn | `tasks.md` 必须至少包含一个 checkbox |
| `SDD007` | warn | `design/INDEX.md` 必须标注 canonical 设计文档 |
| `SDD008` | warn | `progress.md` 最近记录应包含验证或阻塞说明 |
| `SDD009` | error | `SDD/index.json` 中不能登记不存在的实例路径 |
| `SDD010` | warn | active 状态实例数量过多时提醒暂停或归档 |

## 8. Skill 设计

### 8.1 新增 skill 分类目录

推荐在统一源下新增：

```text
.ai-config/skills/sdd/
├── sdd-workflow/
│   └── SKILL.md
├── sdd-management/
│   └── SKILL.md
└── sdd-design-discovery/
    └── SKILL.md
```

同步后会自动打平到：

```text
.codex/skills/sdd-workflow/
.claude/skills/sdd-workflow/
.windsurf/skills/sdd-workflow/
```

### 8.2 `sdd-workflow`：流程入口 skill

定位：用户说“用 SDD 流程做这个任务”、“创建 SDD”、“继续某个 SDD”、“中大型任务先做 SDD”时触发。

职责：

1. 判断是否需要 SDD。
2. 读取 `Workspace/SDD/README.md` 和 `Workspace/SDD/docs/SDDFormat.md`。
3. 查找或创建任务实例。
4. 要求先完成设计确认，再执行代码修改。
5. 调用 `sdd-management` 做状态读取、校验、任务更新。
6. 必要时调用 SystemAgent workflow、owner capability skill 或 OpenSpec 兼容入口。

不负责：

1. 不保存具体 SDD 内容。
2. 不直接实现 CLI。
3. 不绕过用户确认直接实施大改。

### 8.3 `sdd-management`：能力 skill

定位：可被 workflow 调用，也可被用户单独请求，用于管理 SDD artifact。

职责：

1. 使用 CLI 创建、列出、校验、恢复 SDD。
2. 更新 `tasks.md`、`progress.md`、`notes.md`。
3. 维护 `SDD/index.json`。
4. 输出当前状态、阻塞项和下一步。
5. 在结束前提醒运行 `sdd validate`。

不负责：

1. 不决定业务设计。
2. 不替代 owner skill。
3. 不直接修改同步副本。

### 8.4 `sdd-design-discovery`：设计发现 skill

定位：把 superpowers brainstorming 中有价值的设计探索步骤转化为 SlimeAI 风格。

职责：

1. 探索上下文。
2. 输出目标、约束、成功标准、风险、未知项。
3. 给出 2-3 个方案和推荐。
4. 输出用户确认包。
5. 将确认后的设计写入 SDD `design/`。

特点：

- 不强制一次一个问题。
- 不强制所有小任务都使用。
- 不独立成为顶层 workflow，而是 SDD workflow 和其他 SystemAgent workflow 可调用的 capability skill。

### 8.5 与现有 SystemAgent skill 的关系

| 现有入口 | 与 SDD 的关系 |
| --- | --- |
| `systemagent-new-feature` | 中大型 SystemAgent 新功能可先进入 `sdd-workflow`，再调用该入口处理 SystemAgent 事实源 |
| `openspec-apply-change` | 已存在 OpenSpec change 继续用 OpenSpec；新任务默认考虑 SDD |
| `ai-config-management` | 创建 SDD skill 时必须遵守统一源和同步规则 |
| `skill-test` / `systemagent-skill-test` | 新增 skill 后必须跑 static lint |
| owner capability skills | SDD 只管理任务上下文，具体代码仍路由到 owner skill，例如 movement、damage、ui-bind 等 |

## 9. 与 OpenSpec 的关系

SDD 不应一次性替换 OpenSpec。推荐渐进策略：

1. **现有 active OpenSpec change 不迁移**：避免中途换事实源造成混乱。
2. **新中大型任务优先 SDD**：尤其是设计、执行、验证需要跨会话恢复的任务。
3. **规格基线仍保留 OpenSpec**：如果最终需要长期规格声明，可在 SDD 完成后提炼到 `openspec/specs/` 或相关 DocsAI。
4. **迁移工具后置**：`sdd migrate-openspec` 作为 Phase 4，不进入 MVP。
5. **归档规则明确**：SDD 完成后应归档自身，不把 SDD 目录变成新的长期入口污染源。

推荐长期模型：

```text
SDD = 任务执行与恢复事实源
DocsAI = 长期知识和架构说明
OpenSpec specs = 仍需规格化约束的基线
OpenSpec changes = 兼容已有流程，逐步减少新建
```

## 10. SDD 与 Git 边界

SDD 源码、任务实例和 `.ai-config` 都位于工作区根仓库：

```text
/home/slime/Code/SlimeAI
```

因此，实施时可在工作区根仓库提交：

1. `Workspace/SDD/**`
2. `SDD/**`
3. `.ai-config/skills/sdd/**`
4. 同步生成的 `.codex/skills/**`、`.claude/skills/**`、`.windsurf/skills/**`
5. 必要的 `Workspace/SystemAgent/**` 文档引用更新

但必须避免混入：

1. `SlimeAI/` 框架仓改动。
2. `Games/BrotatoLike/` 游戏仓改动。
3. `Games/BrotatoLike/SlimeAI/` submodule 改动。
4. 用户已有未跟踪资源目录。

每次实施前后必须运行：

```bash
git status --short
```

如果修改了 skill：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static changed
```

## 11. 实施分期

### Phase 0：计划确认

目标：确认本文档中的边界和技术选型。

产出：

- 用户确认是否采用 Python 标准库 CLI。
- 用户确认 skill 目录采用 `.ai-config/skills/sdd/`。
- 用户确认是否把 SystemAgent 优化作为第一个正式 SDD。

不做：

- 不写 CLI。
- 不创建正式 SDD 实例。
- 不改 `.ai-config`。

### Phase 1：创建 SDD 系统源码骨架

目标：建立 `Workspace/SDD/`，但先只包含文档、模板和只读 CLI。

建议文件：

```text
Workspace/SDD/README.md
Workspace/SDD/sdd.py
Workspace/SDD/sdd.sh
Workspace/SDD/docs/SDDFormat.md
Workspace/SDD/docs/CLI.md
Workspace/SDD/docs/ValidationRules.md
Workspace/SDD/templates/instance/*
Workspace/SDD/tests/test_sdd_cli.py
```

任务：

1. 创建 README，说明 SDD 边界、入口命令和目录职责。
2. 创建格式文档，固化实例结构。
3. 创建 CLI 文档，列出 MVP 命令和输出格式。
4. 创建模板文件。
5. 创建 `sdd.py`，先实现 `list`、`show`、`validate`、`doctor`。
6. 创建测试，使用临时目录或 fixture 验证只读命令。

验证：

```bash
python3 Workspace/SDD/sdd.py doctor
python3 Workspace/SDD/sdd.py validate --root SDD --all
python3 -m unittest discover Workspace/SDD/tests
```

### Phase 2：创建 SDD 任务根

目标：创建正式任务实例根 `SDD/`。

建议文件：

```text
SDD/README.md
SDD/index.json
SDD/active/.gitkeep
SDD/paused/.gitkeep
SDD/archived/.gitkeep
```

任务：

1. 创建 SDD 根 README，说明 active/paused/archived 用法。
2. 创建空索引 `index.json`。
3. 创建状态目录。
4. 运行 `sdd doctor` 和 `sdd index`。

验证：

```bash
python3 Workspace/SDD/sdd.py doctor
python3 Workspace/SDD/sdd.py index
python3 Workspace/SDD/sdd.py list
```

### Phase 3：新增 SDD skill

目标：在 `.ai-config/skills/sdd/` 下新增 SDD 入口和能力 skill。

建议文件：

```text
.ai-config/skills/sdd/sdd-workflow/SKILL.md
.ai-config/skills/sdd/sdd-management/SKILL.md
.ai-config/skills/sdd/sdd-design-discovery/SKILL.md
```

任务：

1. 写 `sdd-workflow`，定义触发条件、流程和 gate。
2. 写 `sdd-management`，定义 CLI 使用和 artifact 管理规则。
3. 写 `sdd-design-discovery`，定义设计探索与确认包。
4. 运行同步脚本。
5. 运行 skill lint。

验证：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static changed
```

注意：只改 `.ai-config/skills/sdd/**`，同步副本由脚本生成。

### Phase 4：创建第一个真实 SDD

候选：SystemAgent 优化实施本身。

建议实例：

```text
SDD/active/SDD-0001-systemagent-optimization/
```

任务：

1. 使用 `sdd new "SystemAgent optimization"` 创建实例。
2. 将当前过渡分析区中的核心文档登记到 `design/INDEX.md`。
3. 先采用“引用登记”而不是立刻移动大量文档。
4. 在 `README.md` 中标注当前下一步：实现 SDD CLI 或进入 SystemAgent 优化实施。
5. 在 `tasks.md` 中拆解实施任务。

建议先不移动：

```text
SDD/project/projects/PRJ-0001-systemagent-optimization/design/*.md
```

等 SDD 流程稳定后，再决定复制或移动 canonical 设计文档。

验证：

```bash
python3 Workspace/SDD/sdd.py validate SDD-0001-systemagent-optimization
python3 Workspace/SDD/sdd.py status SDD-0001-systemagent-optimization
```

### Phase 5：写操作 CLI 增强

目标：补齐 `new`、`note`、`progress`、`task`、`set-status`。

任务：

1. 实现 `init-root`。
2. 实现 `new`。
3. 实现 `note` 和 `progress`。
4. 实现 `set-status`。
5. 实现简单任务 checkbox 管理。
6. 为每个写操作补充测试。

验证：

```bash
python3 -m unittest discover Workspace/SDD/tests
python3 Workspace/SDD/sdd.py new "Example" --dry-run
python3 Workspace/SDD/sdd.py validate --all
```

### Phase 6：SystemAgent 接入文档

目标：让 SystemAgent 明确何时路由到 SDD。

可能修改：

```text
Workspace/SystemAgent/README.md
Workspace/SystemAgent/INDEX.md
Workspace/SystemAgent/Workflows/NewFeature.md
Workspace/SystemAgent/Workflows/WorkflowIteration.md
Workspace/SystemAgent/Policies/*
Workspace/SystemAgent/Catalog/*
```

原则：

1. SystemAgent 只引用 SDD，不复制 SDD 正文。
2. `Workspace/SystemAgent/` 不保存任务实例。
3. SDD workflow skill 是入口，SystemAgent workflow 是治理框架。
4. 大型任务先判断是否需要 SDD，再进入 owner skill。

验证：

```bash
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all
```

如修改 OpenSpec baseline 或 SystemAgent catalog，需补充对应检查。

### Phase 7：归档和迁移策略

目标：处理旧过渡文档和 OpenSpec 兼容。

任务：

1. 确认哪些 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/*.md` 是 canonical。
2. 将 canonical 文档复制或移动到对应 SDD `design/`。
3. 将过渡目录 README 改为迁移指针。
4. 不删除历史，除非用户确认。
5. 评估是否需要 `sdd migrate-openspec`。

验证：

```bash
python3 Workspace/SDD/sdd.py validate --all
find SDD -maxdepth 4 -type f | sort
git status --short
```

## 12. MVP 推荐最小闭环

如果希望尽快获得可用价值，推荐 MVP 只做以下内容：

1. `Workspace/SDD/README.md`
2. `Workspace/SDD/docs/SDDFormat.md`
3. `Workspace/SDD/docs/CLI.md`
4. `Workspace/SDD/templates/instance/*`
5. `Workspace/SDD/sdd.py`：实现 `doctor`、`list`、`show`、`validate`、`index`
6. `Workspace/SDD/sdd.sh`
7. `SDD/README.md`
8. `SDD/index.json`
9. `SDD/active`、`SDD/paused`、`SDD/archived`
10. `.ai-config/skills/sdd/sdd-workflow/SKILL.md`
11. `.ai-config/skills/sdd/sdd-management/SKILL.md`

暂缓：

1. `sdd-design-discovery` 可先写简版，或并入 `sdd-workflow` 后续拆出。
2. `task add/check` 写操作可 Phase 5 再做。
3. OpenSpec 迁移工具可 Phase 7 再做。
4. 自动归档和目录移动可后置。

## 13. 风险与缓解

| 风险 | 表现 | 缓解 |
| --- | --- | --- |
| SDD 变成另一个 OpenSpec | 规则过多、命令复杂、创建成本高 | MVP 只做文件胶囊、索引、校验和状态 |
| SDD 与 SystemAgent 职责重叠 | 两边都定义 workflow/gate | SDD 管任务，SystemAgent 管 AI 执行治理 |
| skill 入口过多 | AI 不知道用哪个 skill | `sdd-workflow` 做唯一流程入口，其他为能力 skill |
| CLI 写操作误改文件 | 自动覆盖、移动、归档 | 写操作默认打印目标；高风险操作要求 `--yes`；先做只读 MVP |
| 旧文档迁移混乱 | canonical 不清 | 先引用登记，不移动；等用户确认后迁移 |
| 同步副本漂移 | 直接改 `.claude/.codex/.windsurf` | 只改 `.ai-config`，同步后跑 lint |
| active SDD 堆积 | 任务根变混乱 | `sdd validate` 和 `doctor` 对 active 数量给 warn |

## 14. 建议的第一个正式 SDD

推荐第一个正式实例为：

```text
SDD/active/SDD-0001-sdd-system-bootstrap/
```

而不是直接：

```text
SDD/active/SDD-0001-systemagent-optimization/
```

原因：

1. 当前首要任务是把 SDD 系统本身建立起来。
2. SDD 系统建立后，再用它承接 SystemAgent 优化更自然。
3. 可以避免“用尚不存在的 SDD 管理 SDD 之外的大任务”的递归混乱。

随后第二个实例可以是：

```text
SDD/active/SDD-0002-systemagent-optimization/
```

如果用户希望更快推进 SystemAgent 优化，也可以反过来：先创建 `SDD-0001-systemagent-optimization`，其第一个任务就是 bootstrap SDD。两种方案对比：

| 方案 | 优点 | 缺点 | 推荐度 |
| --- | --- | --- | --- |
| 先 `SDD-0001-sdd-system-bootstrap` | 边界清楚，先造工具再用工具 | 多一个 SDD 实例 | 推荐 |
| 先 `SDD-0001-systemagent-optimization` | 直接承接现有上下文 | SDD 系统与 SystemAgent 优化混在同一任务 | 可接受但不优先 |

## 15. 后续实施时的建议顺序

若用户确认本文档，建议下一步执行：

1. 创建 `Workspace/SDD/` 文档和模板。
2. 实现只读 CLI：`doctor`、`list`、`show`、`validate`、`index`。
3. 创建 `SDD/` 根目录。
4. 新增 `.ai-config/skills/sdd/sdd-workflow` 和 `sdd-management`。
5. 同步 AI 配置并运行 skill-test。
6. 创建 `SDD-0001-sdd-system-bootstrap`。
7. 再创建或迁移 `SDD-0002-systemagent-optimization`。

## 16. 用户确认点

进入实施前建议确认以下问题：

1. CLI 是否采用 **Python 标准库 + Bash wrapper**。
2. SDD skill 是否采用 `.ai-config/skills/sdd/sdd-workflow`、`sdd-management`、`sdd-design-discovery` 三个目录。
3. 第一个正式 SDD 是否命名为 `SDD-0001-sdd-system-bootstrap`。
4. 旧的 SystemAgent 优化文档先采用“引用登记”，暂不移动，是否可以接受。
5. MVP 是否只实现只读 CLI + 根目录 + skill 骨架，写操作后置。

## 17. 本计划自检

- 未要求删除 OpenSpec。
- 未要求立即移动大量文件。
- 未要求新增第三方依赖。
- 明确区分了 `Workspace/SDD`、`SDD`、`.ai-config/skills/sdd` 和 `Workspace/SystemAgent`。
- 明确了 superpowers brainstorming 的借鉴点和不照搬点。
- 明确了 CLI 技术选型、命令分期、校验规则和验证入口。
- 明确了实施前需要用户确认的问题。
