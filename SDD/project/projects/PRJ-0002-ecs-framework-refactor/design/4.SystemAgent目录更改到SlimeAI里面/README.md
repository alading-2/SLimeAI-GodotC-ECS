# SystemAgent 目录与 AI 配置主入口迁移到 SlimeAI 的设计分析

> 日期：2026-05-30  
> 状态：设计发现 / 迁移前分析  
> 范围：`/home/slime/Code/SlimeAI` 根 AI 配置、`SlimeAI/` 框架仓 AI 入口、同步脚本、规则生成目标、SDD / Workspace 路径引用  
> 结论：先做 `.ai-config` 源迁移与同步脚本改造，不建议首轮整体搬 `Workspace/`、`SDD/`、`.claude/`、`.codex/`、`.windsurf/`

## 1. 结论

我认可把 AI 主入口从大目录 `/home/slime/Code/SlimeAI` 收回到框架仓 `/home/slime/Code/SlimeAI/SlimeAI`，原因是当前日常开发对象已经是 `SlimeAI/` 旧 Godot C# AI-first ECS 框架主线；继续让 AI 从大目录进入，会把 `Resources/`、历史 `Workspace/DocsAI/`、游戏仓、旧 OpenSpec、`SlimeAI-AiFirst/` 等历史材料混成默认上下文。

但首轮不应整包搬 `Workspace/`、`SDD/`、`.claude/`、`.codex/`、`.windsurf/`。更稳的做法是：

1. 把 `.ai-config/` 作为唯一可维护源迁入 `SlimeAI/.ai-config/`。
2. 改造同步脚本，让它以 `SlimeAI/` 为生成根，生成 `SlimeAI/AGENTS.md`、`SlimeAI/CLAUDE.md`、`SlimeAI/.codex/`、`SlimeAI/.claude/`、`SlimeAI/.windsurf/`。
3. 根目录 `/home/slime/Code/SlimeAI` 只保留工作区说明和资源索引，不再是 ECS 框架 AI 主入口。
4. `Workspace/` 和 `SDD/` 暂时作为工具与项目设计资产留在旧根，通过绝对路径或显式引用访问；后续文档目录重构时再决定是否迁入框架仓。

这个方案符合用户补充的“实际上只迁移 ai-config 就行，脚本可以生成新的”，也避免了跨 Git 边界一次性大搬迁。

## 2. 已读取事实源

### 2.1 当前规则与入口

- 根 `.ai-config/rules/rules.md`、`AGENTS.md`、`CLAUDE.md` 当前同义，定位是“工作区 / SystemAgent 根规则”，不是 `SlimeAI/` 框架仓规则。
- `SlimeAI/AGENTS.md` 是框架仓入口，但仍保留旧纠偏入口、OpenSpec change、`DocsAI/INDEX.md` 等过期引用。
- `SlimeAI/DocsNew/README.md` 明确 `SlimeAI/DocsAI/` 已删除，当前模块事实源临时回到 `Src/ECS/**` 旁文档和 SDD design。
- `SlimeAI/DocsNew/ECS框架与AIFirst方向决策.md` 与 `PRJ-0002 design/main.md` 明确当前方向是 AI-first ECS 框架主线，不是纯 GameOS 替代旧 ECS。

### 2.2 当前同步机制

- `Workspace/Tools/ai-config-sync/sync-ai-config.sh` 通过 `ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"` 推导旧根。
- 当前脚本会把 `.ai-config/rules/rules.md` 复制到旧根 `AGENTS.md`、旧根 `CLAUDE.md` 和旧根 `.windsurf/rules/windsurfrules.md`。
- 当前脚本会把 `.ai-config/skills/**/SKILL.md` 打平同步到旧根 `.codex/skills`、`.claude/skills`、`.windsurf/skills`。
- `generate-claude-commands.py` 同样以旧根推导 `.ai-config/skills/openspec` 和 `.claude/commands/opsx`。
- `Workspace/SystemAgent/Tools/skill-test/lint.py`、R003、R004、R005、R006 默认从旧根 `.ai-config/skills` 和旧根同步副本取数据。

### 2.3 当前路径风险

- `Workspace/Tools/run-full-validation.sh` 硬编码框架仓和游戏仓绝对路径。
- `Workspace/SDD/Src/templates.py` 的模板默认 `Git Boundary: /home/slime/Code/SlimeAI`。
- `.ai-config/skills/godot/godot-scene-test/scripts/analyze-logs.sh` 仍可能返回旧 `../../SlimeAI/DocsAI/Tests/ValidationCatalog.md`。
- 多个 `.ai-config/skills/**` 仍引用 `DocsAI/GameOS/...`、`SlimeAI/DocsAI/...` 或旧 GameOS 事实源。
- `SlimeAI/DocsNew/README.md` 的表格存在入口漂移：表格写 `01-*`、`02-*`，实际文件是 `ECS框架与AIFirst方向决策.md` 和 `ECS/Data/Data系统说明.md`。

### 2.4 Git 边界现状

- 旧根 `/home/slime/Code/SlimeAI` 是一个 Git 仓库，包含工作区配置、Workspace、SDD 和历史资产。
- `SlimeAI/` 是独立框架仓，当前已有用户或既有改动：`AGENTS.md` 修改、`CLAUDE.md` 删除、若干 `.uid` 删除、AI / Enemy 相关 C# 文件修改。
- 本轮分析只读扫描未修改这些既有改动。

## 3. 目标与非目标

### 3.1 目标

- AI 打开 `/home/slime/Code/SlimeAI/SlimeAI` 时，默认读到的是 ECS 框架仓规则，而不是工作区 / SystemAgent 根规则。
- `.ai-config/rules/rules.md` 改回 ECS 框架仓规则后，只生成 `SlimeAI/` 内副本，不误覆盖旧根 `AGENTS.md`。
- `.ai-config/skills/**` 中的 owner skill、Godot runner、Data / ECS 路由，以 `SlimeAI/` 框架仓为默认工作根。
- `SlimeAI/DocsNew`、`PRJ-0002 design`、`SlimeAI/Src/ECS/**` 旁文档成为 ECS 框架任务的事实源链路。
- `Resources/` 继续可通过显式路径索引，不要求放在同一个 AI 工作目录里。

### 3.2 非目标

- 不首轮迁移全部 `Workspace/`、`SDD/` 历史。
- 不首轮清理所有历史 `Resources/**`、`Plans/**`、`ChatHistory/**` 旧路径。
- 不恢复 `SlimeAI/DocsAI/`。
- 不把 BrotatoLike 游戏规则上提为框架默认。
- 不把 SystemAgent route / actor / gate 正文复制进 ECS 框架规则；只保留必要流程入口。

## 4. 可选方案

### 4.1 方案 A：只迁移 `.ai-config`，脚本生成 `SlimeAI/` 内副本

做法：

- 新源：`SlimeAI/.ai-config/`。
- 新生成目标：`SlimeAI/AGENTS.md`、`SlimeAI/CLAUDE.md`、`SlimeAI/.codex/skills`、`SlimeAI/.claude/skills`、`SlimeAI/.windsurf/skills`、`SlimeAI/.claude/commands/opsx`。
- 同步脚本可继续放旧根 `Workspace/Tools/ai-config-sync/`，但必须支持 `SOURCE_ROOT` / `TARGET_ROOT` 或显式把目标根改为 `/home/slime/Code/SlimeAI/SlimeAI`。
- skill-test 必须支持 `--root /home/slime/Code/SlimeAI/SlimeAI`，或新增 wrapper 从框架根扫描。

优点：

- 改动范围最小，符合“脚本可以生成新的”。
- 不把 SDD / Workspace 工具链和框架源码仓强行混在一个 Git 边界。
- 失败可回滚：旧根 `.ai-config` 暂留一段时间，验证新同步链路稳定后再删除或标历史。

缺点：

- 首轮存在两个 `.ai-config` 的过渡期，需要明确冻结旧根源。
- `Workspace/SystemAgent` 仍在旧根，skill 中引用它时要用绝对路径或明确 `../Workspace`。

结论：推荐。

### 4.2 方案 B：迁移 `.ai-config`、`.claude`、`.codex`、`.windsurf`，但不迁移 `Workspace/SDD`

做法：

- 直接搬运行配置和生成副本到 `SlimeAI/`。
- `.claude/settings.json`、`.codex/hooks.json`、subagent 配置也改为框架仓运行配置。

优点：

- AI 工具打开 `SlimeAI/` 时可直接加载本仓配置。

缺点：

- 如果先手工搬副本，容易违反“副本由脚本生成”的纪律。
- hook/subagent 与 `Workspace/SystemAgent` 的相对路径会断，需要同步改一批脚本。

结论：可以作为方案 A 验证通过后的第二步；不建议和 `.ai-config` 源迁移同批完成。

### 4.3 方案 C：整体迁移 `Workspace/`、`SDD/`、`.ai-config`、工具配置到 `SlimeAI/`

做法：

- 把整个 SystemAgent / SDD / AI 配置体系并入框架仓。

优点：

- 所有 AI 长期资产都在框架仓内，自包含。

缺点：

- 风险最大，会改变 SDD 历史、项目容器路径、Workspace 文档、skill-test、hook、subagent、OpenSpec 兼容路径和旧根 Git 边界。
- `Resources/` 与游戏仓仍在旧根，整体迁移也不能真正消除跨路径访问。
- 历史 SDD 中大量绝对路径会变成噪音，容易把“历史记录”误改成“当前事实”。

结论：不推荐首轮做。

## 5. 推荐迁移设计

### 5.1 目录目标形态

```text
/home/slime/Code/SlimeAI/
  Workspace/                 # 暂留：SystemAgent / SDD CLI / sync 脚本维护源
  SDD/                       # 暂留：PRJ-0002 等设计事实源
  Resources/                 # 暂留：显式路径索引
  Games/                     # 暂留：游戏仓
  SlimeAI/                   # 新 AI 主目录 / 框架仓
    AGENTS.md                # 由 SlimeAI/.ai-config/rules/rules.md 生成
    CLAUDE.md                # 由 SlimeAI/.ai-config/rules/rules.md 生成
    .ai-config/              # 新唯一维护源
    .codex/                  # 生成副本 + 直接运行配置
    .claude/                 # 生成副本 + 直接运行配置
    .windsurf/               # 生成副本
    DocsNew/
    Src/ECS/
```

旧根 `.ai-config/` 不应再作为维护源。过渡期可以保留但要在旧根 `AGENTS.md` 或 README 标明 frozen / historical，防止继续从旧根 sync。

### 5.2 规则生成目标

当前最大冲突是 `.ai-config/rules/rules.md` 生成旧根 `AGENTS.md`。迁移后必须改成：

```text
SlimeAI/.ai-config/rules/rules.md
  -> SlimeAI/AGENTS.md
  -> SlimeAI/CLAUDE.md
  -> SlimeAI/.windsurf/rules/windsurfrules.md
```

旧根 `AGENTS.md` 不再由框架 `.ai-config` 生成。它可以成为工作区打开说明，内容只保留：

- 资源大目录用途。
- 框架主入口是 `SlimeAI/AGENTS.md`。
- 游戏仓入口是 `Games/<Game>/AGENTS.md`。
- `Workspace/` / `SDD/` 是工具和历史设计资产。

### 5.3 `rules.md` 应整合的 ECS 框架规则

建议新 `SlimeAI/.ai-config/rules/rules.md` 结构：

1. **定位**：`SlimeAI/` 是 Godot C# AI-first ECS 框架主线；保留 Entity / Component / Data / Event / System / Relationship / Test / Docs。
2. **默认入口**：
   ```text
   AGENTS.md -> DocsNew/README.md -> /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/ -> Src/ECS/** 旁文档 -> owner skill -> Tools/run-build.sh / Tools/run-tests.sh / Godot scene test
   ```
3. **事实源边界**：`DocsNew` 负责方向，`PRJ-0002 design` 负责系统设计，`Src/ECS/**` 旁文档负责当前模块契约；`DocsAI/` 已删除，不恢复。
4. **Git 边界**：框架改动只在 `SlimeAI/` 仓；游戏仓通过 submodule 指针更新；禁止在 `Games/*/SlimeAI/` 直接做框架业务改动。
5. **ECS 架构红线**：从 `SlimeAI/AGENTS.md` 保留 Entity 生命周期、Data、EventBus、ResourceManagement、TimerManager、TargetSelector、DamageService、对象池等规则。
6. **Data 当前规则**：以 DataOS descriptor、runtime snapshot、generated handle、catalog-bound Data 为主；不要恢复旧 `DataMeta` / `DataRegistry` 事实源。
7. **任务流程**：小改直接做；Data / Entity / Event / Relationship 等中大型改动走 SDD；SystemAgent 只作为流程参考。
8. **AI 配置规则**：只改 `SlimeAI/.ai-config` 源，运行同步脚本；不要直接改生成副本。
9. **验证入口**：框架 build/test、DataOS validate/generate、必要 Godot scene、`git diff --check`。

应删除或降级：

- “当前 OpenSpec change”。
- “大型 ECS 改造前先读工作区纠偏文档和 OpenSpec change”。
- `DocsAI/INDEX.md` 作为当前入口。
- “AI-first GameOS 框架工作区”作为主定位。

### 5.4 `SlimeAI/AGENTS.md` 与 `SlimeAI/.ai-config/rules/rules.md`

迁移后 `SlimeAI/AGENTS.md` 应成为生成副本，不再手工维护。当前 `SlimeAI/AGENTS.md` 中仍合理的内容应回收进新 rules 源，包括：

- 中文回复。
- 禁止 PowerShell。
- 新增/修改代码需要适当中文注释。
- 性能红线：`_Process` 中禁止 `new` 对象和 LINQ。
- 数值无限制语义统一用 `-1`。
- 角度输入语义统一用度。
- ECS 架构红线。

但 Data 相关规则需要按当前 Data 重构结果改写，不能照搬旧描述。

### 5.5 skill 源迁移

迁移路径：

```text
/home/slime/Code/SlimeAI/.ai-config/skills/*
  -> /home/slime/Code/SlimeAI/SlimeAI/.ai-config/skills/*
```

需要批量审查的高风险引用：

| 类别 | 当前问题 | 迁移后要求 |
| --- | --- | --- |
| `DocsAI/GameOS/...` | 多个 GameOS skill 仍把已删除或历史 GameOS 文档当事实源 | 框架 ECS 任务改到 `DocsNew`、`Src/ECS/**`、PRJ-0002；历史 GameOS 参考必须标 historical |
| `/home/slime/Code/SlimeAI/.ai-config/...` | Godot scene skill 写死旧源路径 | 改为 `/home/slime/Code/SlimeAI/SlimeAI/.ai-config/...` 或相对新根 |
| `SlimeAI/DocsAI` | 已删除入口 | 禁止恢复；改为 `DocsNew` / `Src/ECS` / SDD design |
| `SlimeAI/Src/ECS` | 在旧根相对可用，在新根会变成双层错误 | 新根内改为 `Src/ECS` |
| `Workspace/SystemAgent` | 暂不迁移 | 使用绝对路径 `/home/slime/Code/SlimeAI/Workspace/SystemAgent/...` 或明确外部工具引用 |
| `SDD/project/...` | 暂不迁移 | 使用绝对路径 `/home/slime/Code/SlimeAI/SDD/project/...` |

## 6. 必改文件清单

### 6.1 第一批：迁移脚本与生成目标

- `Workspace/Tools/ai-config-sync/sync-ai-config.sh`
  - 增加 `SOURCE_ROOT` / `TARGET_ROOT` 或明确 `TARGET_ROOT=/home/slime/Code/SlimeAI/SlimeAI`。
  - source 改为 `SlimeAI/.ai-config`。
  - target 改为 `SlimeAI/.codex`、`SlimeAI/.claude`、`SlimeAI/.windsurf`、`SlimeAI/AGENTS.md`、`SlimeAI/CLAUDE.md`。
- `Workspace/Tools/ai-config-sync/generate-claude-commands.py`
  - 读取 `SlimeAI/.ai-config/skills/openspec`。
  - 输出 `SlimeAI/.claude/commands/opsx`。
  - 若 OpenSpec 不再是框架默认入口，可考虑生成关闭或只给 Codex 保留历史兼容。
- `.vscode/tasks.json`
  - `sync: AI Config` 的命令需要指向新脚本参数或新 wrapper。

### 6.2 第二批：lint 与 registry

- `Workspace/SystemAgent/Tools/skill-test/lint.py`
  - 确认 `--root /home/slime/Code/SlimeAI/SlimeAI` 可完整扫描。
- `Workspace/SystemAgent/Tools/skill-test/rules/R004-sync-targets-up-to-date.py`
  - target 副本应从新 root `.codex/.claude/.windsurf` 对比。
- `Workspace/SystemAgent/Registry/skills.yaml`
  - source path 如继续相对 root，需要决定 root 是旧根还是 `SlimeAI/`。
  - 推荐首轮给 skill-test 新增框架 root 模式，不急着改 registry 全部路径。

### 6.3 第三批：框架规则源与生成副本

- 新建或迁移 `SlimeAI/.ai-config/rules/rules.md`。
- 由同步脚本生成：
  - `SlimeAI/AGENTS.md`
  - `SlimeAI/CLAUDE.md`
  - `SlimeAI/.windsurf/rules/windsurfrules.md`
- 不再手工修 `SlimeAI/AGENTS.md`，除非明确处在迁移前的临时补丁。

### 6.4 第四批：高风险 skill 源

- `SlimeAI/.ai-config/skills/core/ai-config-management/SKILL.md`
- `SlimeAI/.ai-config/skills/core/project-index/SKILL.md`
- `SlimeAI/.ai-config/skills/ecs/ecs-data/SKILL.md`
- `SlimeAI/.ai-config/skills/godot/godot-scene-test/SKILL.md`
- `SlimeAI/.ai-config/skills/godot/godot-scene-test/scripts/analyze-logs.sh`
- `SlimeAI/.ai-config/skills/godot/scene-gate/SKILL.md`
- `SlimeAI/.ai-config/skills/systemagent/systemagent-new-feature/SKILL.md`
- `SlimeAI/.ai-config/skills/systemagent/systemagent-validation-release/SKILL.md`

## 7. 建议执行顺序

1. **冻结旧根源**：旧根 `.ai-config/` 暂不删，先标记不再维护；新改动只进 `SlimeAI/.ai-config/`。
2. **复制源到框架仓**：把旧根 `.ai-config/` 复制到 `SlimeAI/.ai-config/`，不复制旧副本目录。
3. **改 sync 脚本**：让脚本从 `SlimeAI/.ai-config` 生成到 `SlimeAI/` 内。
4. **写新 rules**：把工作区规则改成 ECS 框架仓规则，整合 `SlimeAI/AGENTS.md` 中仍有效的硬约束。
5. **先跑 sync**：生成 `SlimeAI/AGENTS.md`、`CLAUDE.md`、`.codex/.claude/.windsurf`。
6. **跑 lint**：用新 root 跑 skill-test，确认副本与源一致。
7. **修高风险 skill 路径**：优先修 `DocsAI`、`.ai-config` 绝对路径、`SlimeAI/Src` 双层路径风险。
8. **验证框架任务入口**：从 `/home/slime/Code/SlimeAI/SlimeAI` 打开，确认 AI 读取新 `AGENTS.md` 后不会路由到旧根规则。
9. **清理旧根入口**：旧根 `AGENTS.md` 改成工作区说明，或保留但明确框架开发入口在 `SlimeAI/AGENTS.md`。

## 8. 验证命令

迁移执行后至少需要：

```bash
cd /home/slime/Code/SlimeAI
git status --short
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only --root /home/slime/Code/SlimeAI/SlimeAI
git diff --check
```

如果 `lint.sh` 当前不支持 `--root` 位置参数，需要先扩展 wrapper，再执行等价命令：

```bash
python3 Workspace/SystemAgent/Tools/skill-test/lint.py --root /home/slime/Code/SlimeAI/SlimeAI --scope all --summary-only
```

框架规则生成后，再跑框架验证：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
Tools/run-build.sh
Tools/run-tests.sh
```

涉及 DataOS 或 Godot 场景时，再按对应 SDD / owner skill 追加 DataOS validate / generate 和 scene test。

## 9. 主要风险

| 风险 | 说明 | 缓解 |
| --- | --- | --- |
| 误覆盖旧根 `AGENTS.md` | 当前 sync 会从 `.ai-config/rules/rules.md` 覆盖旧根规则 | 先改 sync target，再写新 rules |
| 双层路径 | 新根内继续引用 `SlimeAI/Src/ECS` 会变成错误路径 | skill / rules 中框架内路径统一写 `Src/ECS`、`DocsNew` |
| 旧 DocsAI 残留 | 多处仍引用 `SlimeAI/DocsAI` 或 `DocsAI/GameOS` | 当前事实源改为 `DocsNew`、`Src/ECS`、PRJ-0002；历史引用标 historical |
| skill-test root 语义 | lint 默认旧根 `.ai-config` | 增加或验证 `--root SlimeAI` 模式 |
| hook/subagent 相对路径 | `.codex/.claude` 搬到框架仓后，`Workspace/SystemAgent` 不在当前根 | 运行配置中使用绝对路径或 `../Workspace`，不要假设同根 |
| Git 边界混淆 | 旧根和 `SlimeAI/` 是不同 Git 边界 | 每次提交前分别在目标仓 `git status --short` |
| 历史 SDD 路径噪音 | 已完成 SDD 中大量旧绝对路径 | 历史记录不批量改；只改 active / current / 新模板 |

## 10. DesignCritic

### Assumptions

- 用户现在希望日常 AI 打开 `SlimeAI/` 框架仓，而不是旧根。
- `Resources/` 仍可通过路径索引，不需要在 AI 主目录下。
- `Workspace/` / `SDD/` 暂时不迁移也能接受，只要新 rules 能显式引用旧根路径。

### Missing Context

- 是否希望旧根 `/home/slime/Code/SlimeAI/AGENTS.md` 保留为工作区说明，还是逐步删除。
- `.claude/settings.json`、`.codex/hooks.json` 是否需要立即复制到 `SlimeAI/`，还是等 `.ai-config` 同步链路稳定后再做。
- 旧根 `.ai-config` 是否要删除、改名还是保留 historical。

### Design Defects To Avoid

- 直接改旧根 `.ai-config/rules/rules.md`：会把 ECS 框架规则同步到旧根 `AGENTS.md`，改变整个工作区行为。
- 直接手工复制 `.codex/.claude/.windsurf/skills`：会破坏源 / 副本边界。
- 把所有 `SlimeAI/` 前缀机械删掉：游戏仓、旧根 SDD、跨仓绝对路径里有些 `SlimeAI/` 是真实仓名，不应全局替换。

### Better Option

最小安全方案是先做“新 root 参数化 sync”，再迁源，再同步生成。也就是先让脚本能明确 `SOURCE_ROOT` 和 `TARGET_ROOT`，再移动 `.ai-config`。这样即使中途失败，也不会产生半套副本。

## 11. Must Confirm

1. 旧根 `AGENTS.md` 最终要保留为“工作区说明”还是不再维护？
2. `SlimeAI/.claude`、`SlimeAI/.codex` 中的 hook / subagent 运行配置是否首轮迁移，还是只生成 skills/rules？
3. 旧根 `.ai-config` 迁移后是删除、重命名为 `.ai-config.archive`，还是保留但写明 frozen？

## 12. Defaults I Will Use

如果用户不补充，我建议默认：

- 旧根 `AGENTS.md` 保留为工作区说明。
- 首轮只迁移 `SlimeAI/.ai-config` 源和生成副本；hook / subagent 配置第二批处理。
- 旧根 `.ai-config` 先保留并标 frozen，等新同步链路验证两轮后再删除。
- 历史 SDD / ChatHistory / Plans 里的旧路径不批量改，只在新模板和 current 入口中改。

