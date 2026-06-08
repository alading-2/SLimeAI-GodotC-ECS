# SystemAgent 目录完全重构设计

> 日期：2026-05-25
> 前提：SDD-0006 ~ SDD-0010 已完成逻辑分层整改，但物理目录仍然过散、过浅、含大量冗余。
> 目标：**完全按 SystemAgent 执行理念重构目录，零兼任，删除过时文件，合并同类项。**
> 命名方案：用户已将 wrapper skill 路径改为 Routes/Actors/Rules/Tools/Registry，本设计对齐此方案。

---

## 一、SystemAgent 的本质

SystemAgent 做一件事：**帮 AI 把用户意图变成可验证的执行结果**。

执行路径只有四步：

```
用户意图 → 选路由(Routes) → 用执行者(Actors)做 → 用工具(Tools)验
```

所有其他内容——规则、边界、索引、配置——都是这四步的**约束和支撑**，不是并列的独立概念。

当前 13 个一级目录把约束和支撑当成了与核心并列的东西，导致：
- 看目录结构分不清什么是"核心"什么是"约束"
- 约束被拆成 Protocol / Policy / Gate / Skill 四个概念，边界模糊
- 索引被拆成 Catalog / Config / INDEX.md 三处，维护负担大

---

## 二、逐文件生死判定

### 2.1 保留（核心 + 必要约束）

| 文件 | 判定 | 新位置 | 理由 |
|------|------|--------|------|
| `Workflows/NewFeature.md` | **保留** | `Routes/NewFeature.md` | 核心路由 |
| `Workflows/DebugFix.md` | **保留** | `Routes/DebugFix.md` | 核心路由 |
| `Workflows/WorkflowIteration.md` | **保留** | `Routes/WorkflowIteration.md` | 核心路由 |
| `Workflows/ConfigMaintenance.md` | **保留** | `Routes/ConfigMaintenance.md` | 核心路由 |
| `Workflows/ResearchAdoption.md` | **保留** | `Routes/ResearchAdoption.md` | 核心路由 |
| `Workflows/ValidationRelease.md` | **保留** | `Routes/ValidationRelease.md` | 核心路由 |
| `Roles/Planner.md` | **保留** | `Actors/Planner.md` | 核心执行者 |
| `Roles/Implementer.md` | **保留** | `Actors/Implementer.md` | 核心执行者 |
| `Roles/Debugger.md` | **保留** | `Actors/Debugger.md` | 核心执行者 |
| `Roles/TestDesigner.md` | **保留** | `Actors/TestDesigner.md` | 核心执行者 |
| `Roles/Reviewer.md` | **保留** | `Actors/Reviewer.md` | 核心执行者 |
| `Roles/Verifier.md` | **保留** | `Actors/Verifier.md` | 核心执行者 |
| `Roles/Retrospective.md` | **保留** | `Actors/Retrospective.md` | 核心执行者 |
| `Roles/ResearchAnalyst.md` | **保留** | `Actors/ResearchAnalyst.md` | 核心执行者 |
| `Roles/Documentarian.md` | **保留** | `Actors/Documentarian.md` | 核心执行者 |
| `Roles/DesignCritic.md` | **保留** | `Actors/DesignCritic.md` | 核心执行者 |
| `Roles/SeniorGameDeveloper.md` | **保留** | `Actors/SeniorGameDeveloper.md` | 条件执行者 |
| `Roles/SeniorProgrammer.md` | **保留** | `Actors/SeniorProgrammer.md` | 条件执行者 |
| `Capabilities/DesignDiscovery.md` | **保留** | `Actors/DesignDiscovery.md` | 本质是执行者能力 |
| `Tools/skill-test/*` | **保留** | `Tools/skill-test/` | 验证工具 |
| `Tools/systemagent-hooks/*` | **保留** | `Tools/systemagent-hooks/` | 验证工具 |
| `Gates/ReviewGates.md` | **保留** | `Rules/ReviewGates.md` | 核心规则 |
| `Gates/VerdictVocabulary.md` | **保留** | `Rules/VerdictVocabulary.md` | 核心规则 |
| `Policies/GitPolicy.md` | **保留** | `Rules/Git.md` | 核心规则 |
| `Policies/SubagentPolicy.md` | **保留** | `Rules/Subagent.md` | 核心规则 |
| `Policies/AIConfigBoundary.md` | **保留** | `Rules/AIConfig.md` | 核心规则 |
| `Protocols/FrameworkVsGameBoundary.md` | **保留** | `Rules/Boundary.md` | 核心规则 |
| `Protocols/TDDProtocol.md` | **保留** | `Rules/TDD.md` | 核心规则 |
| `Catalog/manifest.yaml` | **保留** | `Registry/manifest.yaml` | 机器索引 |
| `Catalog/workflow-catalog.yaml` | **保留** | `Registry/workflow-catalog.yaml` | 机器索引 |
| `Catalog/systemagent-catalog.yaml` | **保留** | `Registry/skills.yaml` | 机器索引（重命名） |
| `Config/review-mode.txt` | **保留** | `Registry/review-mode.txt` | 运行配置 |
| `Config/.last-sync` | **保留** | `Registry/.last-sync` | 运行配置 |
| `BDDSceneFormat.md` | **保留** | `Tools/BDDSceneFormat.md` | 验证格式 |

### 2.2 删除（过时 / 冗余 / 空壳）

| 文件 | 判定 | 理由 |
|------|------|------|
| `Protocols/AIFeatureDevelopmentProtocol.md` | **删除** | 与 NewFeature route + DesignDiscovery 大面积重复；独特内容提取到 Rules/Philosophy.md |
| `Protocols/AITaskCompletionContract.md` | **删除** | 与 RV-RETROSPECTIVE gate + 各 route completion criteria 重复；零独特内容 |
| `Protocols/LongRunningPlanProtocol.md` | **删除** | 与 SDD workflow + progress.md 维护规则重复；零独特内容 |
| `Protocols/CapabilityChangeProtocol.md` | **删除** | 与 Boundary.md + 各 owner skill 重复；零独特内容 |
| `Policies/DocumentationManagement.md` | **删除** | 与 README.md + AIConfig.md 大面积重复；零独特内容 |
| `Policies/ExternalResources.md` | **删除** | 33行，内容过于简单，合并为 Rules/Boundary.md 一段 |
| `Skills/README.md` | **删除** | 9行，只说"这里没有skill"，无信息量（用户已删） |
| `Skills/WrapperSkillPolicy.md` | **删除** | 29行，与 AIConfig.md 重复（用户已删） |
| `Workflows/INDEX.md` | **删除** | 15行，与 workflow-catalog.yaml 重复 |
| `Roles/INDEX.md` | **删除** | 23行，与 manifest.yaml roles 段重复 |
| `Capabilities/INDEX.md` | **删除** | 38行，只有1个capability的索引，无存在必要 |
| `Policies/INDEX.md` | **删除** | 23行，重构后 Rules/ 目录自解释 |
| `INDEX.md`（根） | **删除** | 路由信息合并入 README.md |

**删除统计**：13 个文件，约 550 行，~55KB

### 2.3 删除理由详解

#### AIFeatureDevelopmentProtocol.md（85行）

与 `Routes/NewFeature.md` 对比：

| AIFeatureDevelopmentProtocol 内容 | NewFeature route 是否覆盖 |
|---|---|
| 执行顺序 10 步 | ✅ NewFeature phases 已覆盖 |
| 设计检查 9 问 | ✅ DesignDiscovery 已覆盖 |
| 功能收尾闸门 | ✅ ReviewGates RV-* 已覆盖 |
| 文档同步 | ✅ AIConfig + DocumentationManagement 已覆盖 |
| 基本原则 4 条 | ⚠️ 部分独特，提取到 Rules/Philosophy.md |

独特内容提取：4 条基本原则（AI便利优先、不为旧框架保留新入口、小内核可选能力、纯Runtime优先C#标准库）→ `Rules/Philosophy.md`。

#### AITaskCompletionContract.md（101行）

| 契约内容 | 已由谁覆盖 |
|---|---|
| 汇报字段 | 各 route completion criteria |
| 禁止事项 | ReviewGates + Git.md |
| 验证命令 | 各 route + Tools |
| 多轮失败复盘 | SDD progress.md + Retrospective |

零独特内容，完全删除。

#### LongRunningPlanProtocol.md（91行）

| 协议内容 | 已由谁覆盖 |
|---|---|
| 计划维护规则 | SDD tasks.md + progress.md |
| 禁止事项 | Git.md + ReviewGates |
| 验证命令 | SDD CLI + 各 route |
| 完成汇报 | AITaskCompletionContract（本身也是冗余） |

零独特内容，完全删除。

#### CapabilityChangeProtocol.md（113行）

| 协议内容 | 已由谁覆盖 |
|---|---|
| 修改规则 | Boundary.md + 各 owner skill |
| 禁止事项 | Boundary.md |
| 验证命令 | 各 route + Tools |
| 完成汇报 | AITaskCompletionContract（本身也是冗余） |

零独特内容，完全删除。

#### DocumentationManagement.md（51行）

| 策略内容 | 已由谁覆盖 |
|---|---|
| 事实源边界 | README.md |
| 允许/禁止/验证 | AIConfig.md |
| Memory 分层 | README.md |
| Schema migration | manifest.yaml |

零独特内容，完全删除。

#### ExternalResources.md（33行）

内容只有"不默认读 Resources、用时要记录范围和原因、不复制外部代码"。合并为 `Rules/Boundary.md` 一段，不需要独立文件。

---

## 三、新目录结构

```
Workspace/SystemAgent/
├── README.md                    # 唯一入口：身份 + 路由 + 目录地图 + 边界
├── Routes/                      # 路由编排（原 Workflows/）
│   ├── NewFeature.md
│   ├── DebugFix.md
│   ├── WorkflowIteration.md
│   ├── ConfigMaintenance.md
│   ├── ResearchAdoption.md
│   └── ValidationRelease.md
├── Actors/                      # 执行者（原 Roles/ + Capabilities/）
│   ├── Planner.md
│   ├── Implementer.md
│   ├── Debugger.md
│   ├── TestDesigner.md
│   ├── Reviewer.md
│   ├── Verifier.md
│   ├── Retrospective.md
│   ├── ResearchAnalyst.md
│   ├── Documentarian.md
│   ├── DesignCritic.md
│   ├── DesignDiscovery.md       ← 从 Capabilities/ 移入
│   ├── SeniorGameDeveloper.md
│   └── SeniorProgrammer.md
├── Rules/                       # 所有行为约束（合并 Protocols + Policies + Gates + Skills）
│   ├── ReviewGates.md           ← 原样移入
│   ├── VerdictVocabulary.md     ← 原样移入
│   ├── Git.md                   ← GitPolicy.md 去后缀
│   ├── Subagent.md              ← SubagentPolicy.md 去后缀
│   ├── AIConfig.md              ← AIConfigBoundary.md 精简
│   ├── Boundary.md              ← FrameworkVsGameBoundary.md + ExternalResources.md 精华合并
│   ├── TDD.md                   ← TDDProtocol.md 去后缀
│   ├── Philosophy.md            ← 从 AIFeatureDevelopmentProtocol 提取的独特原则
│   └── Documentation.md        ← DocumentationManagement.md 精华（旧路径分类规则）
├── Tools/                       # 工具（保留 + 吸收 BDDSceneFormat）
│   ├── skill-test/              ← 原样
│   ├── systemagent-hooks/       ← 原样
│   └── BDDSceneFormat.md        ← 从根目录移入
└── Registry/                    # 机器索引 + 运行配置（合并 Catalog + Config）
    ├── manifest.yaml
    ├── workflow-catalog.yaml
    ├── skills.yaml              ← systemagent-catalog.yaml 重命名
    ├── review-mode.txt
    └── .last-sync
```

### 对比

| 指标 | 旧结构 | 新结构 |
|------|--------|--------|
| 一级目录数 | 13 | 6 |
| 一级文件数 | 3 | 1（README.md） |
| 概念数 | 10（Workflow/Capability/Role/Protocol/Gate/Policy/Catalog/Config/Tool/Skill） | 4（Route/Actor/Rule/Tool）+ 1 机器索引（Registry） |
| INDEX.md 数量 | 5 | 0（目录自解释，机器索引在 Registry/） |
| 需人工同步处 | 6+ | 2（README + Registry） |

### 命名语义

| 目录 | 语义 | 为什么不用旧名 |
|------|------|----------------|
| `Routes/` | 路由 = 把用户意图导向具体执行路径 | "Workflow"暗示工作流引擎，实际只是路由选择 |
| `Actors/` | 执行者 = 带视角和能力的人/角色 | "Role"只暗示视角，"Capability"被拆成独立目录；合并后统一为"执行者" |
| `Rules/` | 规则 = 所有行为约束 | "Protocol/Policy/Gate"三概念边界模糊，统一为"规则" |
| `Tools/` | 工具 = 验证和运维工具 | 原名OK，保留 |
| `Registry/` | 注册表 = 机器可读索引 + 运行配置 | "Catalog/Config"拆成两个目录但内容都很少，合并 |

---

## 四、Rules/ 各文件内容设计

### ReviewGates.md（~220行）

原样移入。内容不变。

### VerdictVocabulary.md（~55行）

原样移入。内容不变。

### Git.md（~50行）

原样移入 `GitPolicy.md`。去掉"Policy"后缀——在 Rules/ 下一切都是规则，不需要再标"Policy"。

### Subagent.md（~69行）

原样移入 `SubagentPolicy.md`。去掉"Policy"后缀。

### AIConfig.md（~40行）

合并精华：

- **来自 AIConfigBoundary.md**：.ai-config 源边界、允许/禁止动作、skill-test 验证
- **来自 WrapperSkillPolicy.md**：wrapper 不复制正文、不反向生成（精简为 3 行）

删除冗余的"Source-of-truth boundary"重复声明。

### Boundary.md（~150行）

合并精华：

- **来自 FrameworkVsGameBoundary.md**：所有权判断、事件归属决策树、禁止事项
- **来自 ExternalResources.md**：外部资源边界（精简段）

删除重复的验证命令段、完成汇报段、多轮失败复盘段（这些由 route + gate 覆盖）。

### TDD.md（~80行）

精简 `TDDProtocol.md`（133行→~80行）：

- 保留：铁律、微循环步骤、RED/GREEN 证据标准、与 Godot 配合
- 删除：与 ReviewGates 重复的 RV-TEST-COVERAGE 段、与 TestDesigner role 重复的配合段

### Philosophy.md（~30行）

从 `AIFeatureDevelopmentProtocol.md` 提取 4 条独特原则：

1. AI 便利优先：入口少、事实源少、验证命令固定、artifact 可检查
2. 不为旧框架兼容保留新入口
3. 小内核，可选能力
4. 纯 Runtime 优先 C# 标准库，GodotBridge 只做引擎适配

加上术语原则：SlimeAI 是 AI-first GameOS / Capability Composition Runtime，不是传统 ECS 框架。

### Documentation.md（~20行）

从 `DocumentationManagement.md` 提取独特内容：

- 旧路径命中分类规则（historical / migration pointer / archive / violation）
- 事实源层级（Working / Persistent Review / Semantic Baseline / Event Log）

其余内容已被 README.md 和 AIConfig.md 覆盖。

---

## 五、README.md 重写设计

当前 README.md（74行）承载了太多"旧结构说明"。

新 README.md 只做四件事：

### 5.1 身份（5行）

```markdown
# SystemAgent

Workspace/SystemAgent/ 是 SlimeAI 工作区 AI 执行的唯一正文事实源。
```

### 5.2 路由（15行）

```markdown
## 入口

README → 选 Route → 按 Route phase 读 Actors / Rules → 用 Tools 验

| 用户意图 | Route |
| --- | --- |
| 新功能、重构、迁移、SDD 实施 | NewFeature |
| bug、测试失败、异常定位 | DebugFix |
| 改进 SystemAgent 流程/角色/策略/gate | WorkflowIteration |
| 修改 skill/rule/hook/subagent/sync | ConfigMaintenance |
| 研究外部资料、参考框架 | ResearchAdoption |
| 大改后验证、归档前检查 | ValidationRelease |
```

### 5.3 目录地图（15行）

```markdown
## 目录

| 目录 | 职责 |
| --- | --- |
| Routes/ | 6 个执行路由 |
| Actors/ | 13 个执行者 + DesignDiscovery 能力 |
| Rules/ | 行为约束：ReviewGates、VerdictVocabulary、Git、Subagent、AIConfig、Boundary、TDD、Philosophy |
| Tools/ | skill-test lint、hook smoke、BDD 场景格式 |
| Registry/ | 机器索引（manifest、catalog）+ 运行配置 |
```

### 5.4 边界（10行）

```markdown
## 边界

- 正文在 Workspace/SystemAgent/；.ai-config/ 只保存 wrapper skill 源
- Skill/rule/command 只改 .ai-config/，改后运行 sync 脚本
- Hook/subagent 配置直接维护 .claude/.codex，不走 .ai-config 同步
- SDD 任务实例在 SDD/；Workspace/SDD/ 只保存 CLI 和模板
```

总计约 45 行，比当前 74 行精简 40%。

---

## 六、迁移影响分析

### 6.1 已完成的改动（用户已做）

用户已修改 8 个 wrapper skill 的路径引用：

| 旧路径 | 新路径 | 状态 |
|--------|--------|------|
| `Workspace/SystemAgent/Workflows/` | `Workspace/SystemAgent/Routes/` | ✅ wrapper 已改 |
| `Workspace/SystemAgent/Roles/` | `Workspace/SystemAgent/Actors/` | ✅ wrapper 已改 |
| `Workspace/SystemAgent/Capabilities/DesignDiscovery.md` | `Workspace/SystemAgent/Actors/DesignDiscovery.md` | ✅ wrapper 已改 |
| `Workspace/SystemAgent/Policies/AIConfigBoundary.md` | `Workspace/SystemAgent/Rules/AIConfig.md` | ✅ wrapper 已改 |
| `Workspace/SystemAgent/Policies/ExternalResources.md` | `Workspace/SystemAgent/Rules/ExternalResources.md` | ⚠️ 设计改为合并入 Boundary.md |
| `Workspace/SystemAgent/Policies/SubagentPolicy.md` | `Workspace/SystemAgent/Rules/Subagent.md` | ✅ wrapper 未改但路径对齐 |
| `Workspace/SystemAgent/Policies/DocumentationManagement.md` | `Workspace/SystemAgent/Rules/Documentation.md` | ✅ wrapper 已改 |
| `Workspace/SystemAgent/Gates/ReviewGates.md` | `Workspace/SystemAgent/Rules/ReviewGates.md` | ✅ wrapper 已改 |
| `Workspace/SystemAgent/Gates/VerdictVocabulary.md` | `Workspace/SystemAgent/Rules/VerdictVocabulary.md` | ✅ wrapper 已改 |
| `Workspace/SystemAgent/Catalog/systemagent-catalog.yaml` | `Workspace/SystemAgent/Registry/skills.yaml` | ✅ wrapper 已改 |
| `Workspace/SystemAgent/Skills/WrapperSkillPolicy.md` | `Workspace/SystemAgent/Rules/WrapperPolicy.md` | ⚠️ 设计改为合并入 AIConfig.md |

用户已删除 `Skills/README.md` 和 `Skills/WrapperSkillPolicy.md`。

### 6.2 需要更新的位置

| 位置 | 影响范围 | 需要的改动 |
|------|----------|-----------|
| `.claude/agents/systemagent-*.md` | 4 个 subagent launcher | Roles/ → Actors/ |
| `.codex/agents/systemagent-*.toml` | 4 个 subagent launcher | Roles/ → Actors/ |
| `.claude/settings.json` | hook 脚本路径 | Tools/ 路径不变 |
| `.codex/hooks.json` | hook 脚本路径 | Tools/ 路径不变 |
| `Workspace/SystemAgent/Tools/systemagent-hooks/systemagent_hook.py` | hook 内路径引用 | Policies/ → Rules/, Gates/ → Rules/ |
| `Workspace/SystemAgent/Tools/skill-test/lint.py` | lint 路径模式 | Catalog/ → Registry/, Policies/ → Rules/, Gates/ → Rules/ |
| `Workspace/SystemAgent/Tools/skill-test/rules/*.py` | 各 lint 规则的路径检查 | 同上 |
| `AGENTS.md` / `CLAUDE.md` / `.windsurf/rules/windsurfrules.md` | 同步副本中的路径 | Workflows/ → Routes/, Roles/ → Actors/ 等 |
| `Workspace/SystemAgent/Catalog/manifest.yaml` | 源路径声明 | 全部更新 |
| `Workspace/SystemAgent/Catalog/workflow-catalog.yaml` | 路径引用 | 全部更新 |
| `Workspace/SystemAgent/Catalog/systemagent-catalog.yaml` | 路径引用 | 全部更新 |
| `SDD/` 下的历史 SDD | 旧路径引用 | 标记为 historical |
| `openspec/` 下的历史规格 | 旧路径引用 | 标记为 historical |

### 6.3 不需要更新的位置

- `SlimeAI/` 框架仓（跨 git 边界，不引用 SystemAgent 路径）
- `Games/BrotatoLike/` 游戏仓（同上）
- `Workspace/SDD/` CLI 源码（不硬编码 SystemAgent 路径）

### 6.4 迁移执行顺序

1. 创建新目录 Routes/、Actors/、Rules/、Registry/
2. 移动保留文件到新位置（git mv）
3. 创建新文件：Rules/Philosophy.md、Rules/Boundary.md（合并精华）
4. 删除旧目录和废弃文件
5. 重写 README.md
6. 更新 Registry/manifest.yaml、workflow-catalog.yaml、skills.yaml 的路径引用
7. 更新 wrapper skill 中与设计不一致的路径（ExternalResources → Boundary, WrapperPolicy → AIConfig）
8. 更新 subagent launcher（.claude/agents/、.codex/agents/）
9. 更新 hook 脚本内路径引用
10. 更新 skill-test lint 路径模式
11. 运行 sync 脚本
12. 运行验证（skill lint、SDD validate、hook smoke、git diff --check）

---

## 七、删除的旧目录

| 旧目录 | 处理 |
|--------|------|
| `Workflows/` | 删除（文件移入 Routes/） |
| `Roles/` | 删除（文件移入 Actors/） |
| `Capabilities/` | 删除（DesignDiscovery.md 移入 Actors/，INDEX.md 删除） |
| `Catalog/` | 删除（yaml 移入 Registry/） |
| `Config/` | 删除（文件移入 Registry/） |
| `Gates/` | 删除（文件移入 Rules/） |
| `Policies/` | 删除（保留文件移入 Rules/，其余删除） |
| `Protocols/` | 删除（保留文件移入 Rules/，其余删除） |
| `Skills/` | 删除（用户已删内容，目录待清） |
| `INDEX.md`（根） | 删除（路由信息合并入 README.md） |

---

## 八、与旧分析的差异

本设计与 `10.5-SystemAgent目录结构深度分析.md` 的关键差异：

| 方面 | 旧分析（10.5） | 本设计 |
|------|---------------|--------|
| 根目录文件 | 保留 README + INDEX + BDDSceneFormat | 只保留 README；INDEX 合并入 README；BDDSceneFormat 移入 Tools/ |
| Capabilities/ | 保留（等后续充实） | 删除；DesignDiscovery 移入 Actors/ |
| Skills/ | 保留 | 删除；内容合并入 Rules/AIConfig.md |
| Config/ | 保留或合并 | 删除；文件移入 Registry/ |
| Protocols | 保留全部 | 删除 4 个冗余文件，保留 2 个移入 Rules/ |
| Policies | 保留全部 | 删除 3 个冗余文件，保留 3 个移入 Rules/（去掉 Policy 后缀） |
| INDEX.md | 保留 5 个 | 删除全部 5 个（目录自解释 + Registry 机器索引） |
| 命名方案 | 保留 Workflows/Roles | 改为 Routes/Actors（对齐用户已改的 wrapper skill） |
| 总目录数 | 13→10（保守） | 13→6（彻底） |
