# DocsAI 集中式 ECS 文档目录方案

> 更新：2026-05-30
> 状态：superseded / historical-input。
> 最新裁决：`SlimeAI/DocsAI/` 已删除，本文不再作为当前文档布局方案；当前框架文档事实源临时回到 `SlimeAI/Src/ECS/**` 旁文档、`SlimeAI/DocsNew/` 和 PRJ-0002 SDD design。
> 范围：`SlimeAI/DocsAI`、`SlimeAI/Docs`、`SlimeAI/Src/ECS/**/*.md` 的长期事实源归属和目录组织。
> 结论：文档主事实源集中到 `DocsAI`，按 ECS / `Src/ECS` 的目录心智分组；默认一个功能 owner 一个目录，目录内保留 `Concept.md`、`Usage.md`，有测试再加 `Tests.md`；`Docs` 只保留面向人类的简化入口；源码旁长文档收敛为短指针或迁移到 `DocsAI`。

## 0. 废弃原因

2026-05-30 用户裁决 `SlimeAI/DocsAI/` 也是旧入口，并已删除该目录。本文保留用于解释曾经的集中化尝试为什么存在，但不得再指导后续文档更新。

当前执行规则：

- 不恢复 `SlimeAI/DocsAI/`。
- 不继续向 `DocsAI/Modules`、`DocsAI/Tests` 或 `DocsAI/ProjectState.md` 写同步内容。
- 当前 Data / Entity / Event 等模块文档优先更新 `SlimeAI/Src/ECS/**` 旁文档、`SlimeAI/DocsNew/` 和对应 SDD design。
- 后续如果重新统一文档体系，需要新建 SDD 重新裁决，不复用本文作为 current 方案。

## 1. 背景

当前仓库同时存在三类文档入口：

- `DocsAI/`：AI 执行入口，包含模块契约、测试矩阵、协议和当前状态。
- `Docs/框架/ECS/`：早期开发功能时写下的概念、方案、使用说明和优化记录。
- `Src/ECS/**/*.md`：源码旁说明，部分是使用文档，部分是设计解释，部分是测试或调试提示。

这三类文档都曾有价值，但长期并存会让 AI 路由变慢：

```text
想改 Ability
  -> DocsAI/Modules/AbilitySystem.md
  -> Docs/框架/ECS/Ability/*
  -> Src/ECS/Base/System/AbilitySystem/README.md
  -> Src/ECS/Test/SingleTest/ECS/System/AbilitySystemTest/*
  -> DocsAI/Tests/*
```

AI-first 的目标不是让文档贴近某个目录，而是让 AI 能用最短路径进入稳定事实源，并且不会同时读到多个互相冲突的版本。

## 2. 设计结论

长期采用以下分层：

```text
DocsAI/
  INDEX.md
  ProjectState.md
  ECS/
    README.md
    Runtime/
    Base/
    System/
    Tools/
    UI/
  Tests/
  Protocols/
  Experience/

Docs/
  README.md
  QuickStart.md
  ArchitectureOverview.md
  HumanGuides/

Src/ECS/
  ...源码、事件定义、测试代码、测试场景...
  README.md / *.md 只保留短指针
```

核心规则：

1. `DocsAI` 是 AI 修改框架前的主事实源。
2. `DocsAI/ECS/` 按 ECS 与 `Src/ECS` 的心智分组，不按零散文档类型分组。
3. 功能相关文档默认聚合到同一个 owner 目录中：概念一篇、使用一篇，有测试再加测试一篇。
4. `Docs/` 不再保存长期事实源，只保存人类快速理解和使用框架的简化版文档。
5. `Src/ECS` 不保存长篇概念文档；源码旁可以保留短 README，指向对应 `DocsAI/ECS/...` owner。
6. 不默认拆 `Contract.md`、`Events.md`、`Debug.md`、`Migration.md`；这些内容并入 `Concept.md`、`Usage.md` 或 `Tests.md`。

## 3. DocsAI 目标目录

`DocsAI/ECS/` 按当前 `Src/ECS` 心智组织，而不是照搬 `SlimeAI-AiFirst/GameOS` 的 GameOS 术语。

推荐目录：

```text
DocsAI/ECS/
  README.md
  Runtime/
    Entity/
    Component/
    Data/
    Event/
    Relationship/
    SystemCore/
  Base/
    Component/
    Entity/
    Data/
    Event/
  System/
    AbilitySystem/
    FeatureSystem/
    DamageSystem/
    Movement/
    AI/
    Collision/
    ProjectileSystem/
    EffectSystem/
    Spawn/
    TargetingSystem/
    TestSystem/
  Tools/
    Input/
    Logger/
    Math/
    ObjectPool/
    TargetSelector/
    Timer/
  UI/
```

说明：

- `Runtime/` 记录 ECS 基础设施概念和跨系统契约。
- `Base/` 用于映射当前 `Src/ECS/Base/*` 的长期源码结构，承接旧目录事实。
- `System/` 按当前 `Src/ECS/Base/System/*` owner 聚合具体功能文档。
- `Tools/` 和 `UI/` 仍作为 ECS 运行时外围能力，避免混进 System owner。
- 如果后续源码目录真正重构，可以保持 `DocsAI/ECS` 目录不变，先更新 source path 映射。
- 每个 owner 目录默认只放 `Concept.md`、`Usage.md`，有测试时再放 `Tests.md`。

## 4. 单个 owner 的最小文档组

每个 owner 默认是一个小目录，而不是很多文档。目录内最多三类核心文档：

```text
DocsAI/ECS/System/AbilitySystem/
  Concept.md
  Usage.md
  Tests.md   # 只有存在测试或测试场景时才创建
```

### Concept.md

回答“这是什么，为什么这样做，边界在哪里”。

```text
# AbilitySystem

> status: current | history | rejected
> owner: AbilitySystem
> sourcePaths:
> testPaths:
> relatedDocs:
> lastReviewed:

## 1. 一句话定位
## 2. 核心概念
## 3. 职责边界
## 4. 依赖关系
## 5. 历史 / 迁移备注
```

`Contract` 类内容放进“职责边界”。历史设计文档和早期概念文档提炼后优先进入这里。

### Usage.md

回答“怎么用，改哪里，不能怎么改”。

```text
# AbilitySystem 使用说明

## 1. 源码入口
## 2. 常见调用流程
## 3. 数据和事件
## 4. 修改边界
## 5. Debug 入口
```

`Events` 类内容放进“数据和事件”。`Debug` 类内容先放进“Debug 入口”，只有非常长时才考虑拆独立 Debug 文档。

### Tests.md

只有当功能有测试代码、测试场景或验证命令时才创建。

```text
# AbilitySystem 测试说明

## 1. 测试代码 / 场景路径
## 2. 运行命令
## 3. PASS 标准
## 4. 常见失败和排查
```

测试文档不复制全局 runner 说明；全局测试工具仍归 `DocsAI/Tests/`。

各文件可以很短。没有内容就不写，不需要为了模板凑齐段落。

AI 读取规则：

1. 不懂概念或边界时读 `Concept.md`。
2. 要改代码或接入功能时读 `Usage.md`。
3. 要验证或 Debug 时读 `Tests.md`；如果没有 `Tests.md`，看 `Usage.md` 的 Debug 入口和全局 `DocsAI/Tests/`。
4. `history` / `rejected` 文档不得作为实现依据。

额外拆分条件：

| 条件 | 处理 |
| --- | --- |
| `Concept.md` 或 `Usage.md` 超过约 400 行且仍在增长 | 先压缩；压缩后仍复杂再拆 |
| Debug 日志和排查流程独立成体系 | 可拆 `Debug.md` |
| 历史迁移说明很长但仍需保留 | 可拆 `Migration.md` 或移入 `DocsAI/Archive/` |

额外文件是例外，不是默认模板。默认 owner 目录不要超过：

```text
DocsAI/ECS/System/AbilitySystem/
  Concept.md
  Usage.md
  Tests.md
```

## 5. Src/ECS 与测试位置

测试代码和测试场景应尽量靠近相关功能，但测试说明文档主事实源在 `DocsAI/ECS/<owner>/Tests.md`。

推荐长期形态：

```text
Src/ECS/Base/System/AbilitySystem/
  *.cs
  README.md      # 短指针：主文档见 DocsAI/ECS/System/AbilitySystem/

Src/ECS/Test/SingleTest/ECS/System/AbilitySystemTest/
  *.cs
  *.tscn
```

如果后续进行源码目录重构，可以考虑把测试场景迁到功能 owner 下：

```text
Src/ECS/Base/System/AbilitySystem/
  Tests/
    AbilitySystemPipelineTest.cs
    AbilitySystemPipelineTest.tscn
```

但这是代码/场景路径重构，必须单独走 SDD，并检查 Godot `.tscn` 脚本路径。当前文档收敛阶段只先确定事实源，不要求立即移动测试文件。

## 6. Docs 目录处理

`Docs/` 不应直接清空后丢弃历史内容。正确顺序是：

1. 扫描 `Docs/框架/ECS/**`。
2. 给每篇文档判定状态：`current`、`history`、`rejected`、`duplicate`。
3. `current` 概念内容提炼后并入 `DocsAI/ECS/<owner>/Concept.md`。
4. `current` 使用、事件、Debug 内容提炼后并入 `DocsAI/ECS/<owner>/Usage.md`。
5. 测试说明提炼后并入 `DocsAI/ECS/<owner>/Tests.md`。
6. `history` / `rejected` 内容如仍需保留，放入 `Concept.md` 的“历史 / 迁移备注”节；太长时再拆 `Migration.md` 或放入 `DocsAI/Archive/`，并从默认索引移除。
7. 重新生成 `Docs/` 的人类版文档，只保留概览、快速开始、使用指南和链接。

`Docs/` 的目标读者是人，不是 AI 执行任务。AI 执行必须以 `DocsAI` 为主。

## 7. 与 SlimeAI-AiFirst 的关系

本方案吸收 `SlimeAI-AiFirst` 的三个思想：

- 少入口：固定从 `DocsAI/INDEX.md` 进入。
- owner 聚合：一个功能 owner 聚合概念、使用、契约、事件、测试、Debug 和验证命令。
- 长期事实源集中：同一事实不在 `Docs`、`DocsAI`、`Src` 三处重复维护。

本方案不照搬其 GameOS 术语和目录：

- 当前项目仍保留旧 Godot C# ECS 主线。
- 目录按 `ECS / Base / System / Tools / UI` 组织，便于映射当前 `Src/ECS`。
- Capability 只作为 owner 思想参考，不替代 ECS 的 Entity / Component / Data / Event / System 心智模型。
- 不照搬 `Contract.md`、`Debug.md` 等更多文件模板；当前旧 ECS 默认只保留 Concept / Usage / Tests 三类。

## 8. 迁移阶段建议

### 阶段 1：建立目录和索引

- 新建 `DocsAI/ECS/README.md`。
- 按需新建少量 owner 目录，每个目录默认只放 `Concept.md`、`Usage.md`，有测试再放 `Tests.md`。
- 更新 `DocsAI/INDEX.md`，让 AI 优先进入 `DocsAI/ECS/`。

### 阶段 2：选择一个 owner 试点

推荐先选 `AbilitySystem` 或 `Movement`：

- 这两个模块都有源码旁 README、Docs 概念文档、测试场景和 DocsAI 契约。
- 适合验证“Concept / Usage / Tests 三件套”是否真能降低 AI 搜索成本。

### 阶段 3：迁移 Docs/框架/ECS

按 owner 批量迁移，不按文档类型迁移。

例如 Ability：

```text
Docs/框架/ECS/Ability/*
Docs/框架/ECS/System/FeatureSystem/*
Src/ECS/Base/System/AbilitySystem/README.md
DocsAI/Modules/AbilitySystem.md
  -> DocsAI/ECS/System/AbilitySystem/
```

### 阶段 4：收缩旧入口

- `DocsAI/Modules/*.md` 收缩为兼容路由页或删除。
- `Src/ECS/**/*.md` 收缩为短指针。
- `Docs/框架/ECS/**` 清空后重建人类版概览。

### 阶段 5：验证

文档迁移至少验证：

```bash
git diff --check
rg -n "Docs/框架/ECS|Src/ECS/.+\\.md|DocsAI/Modules" DocsAI Docs Src -g "*.md"
```

如果移动测试或场景路径，必须追加 Godot 场景路径验证和对应测试命令。

## 9. 风险和护栏

| 风险 | 护栏 |
| --- | --- |
| `DocsAI` 变成大杂烩 | 必须按 ECS owner 聚合，并维护 `DocsAI/ECS/README.md` |
| 目录拆得太碎，AI 又要跨文件搜索 | 默认每个 owner 只放 Concept / Usage / Tests |
| 同一事实重复维护 | `Docs` 和 `Src` 只能保留指针或人类简化说明 |
| 旧概念文档误导 AI | 每篇迁移文档必须标 `status`，默认索引只指 current |
| 测试位置迁移破坏 Godot 场景 | 文档迁移不移动 `.tscn`；移动测试路径必须独立 SDD |
| Capability 术语再次替代 ECS | Capability 只作为 owner 思想，不替换 ECS 目录和心智模型 |

## 10. 决策表

| 问题 | 决策 |
| --- | --- |
| 长期文档主事实源 | `DocsAI` |
| `DocsAI` 怎么组织 | 按 ECS / `Src/ECS` 心智和 owner 聚合，默认 owner 小目录 |
| 概念文档放哪里 | `DocsAI/ECS/<owner>/Concept.md` |
| 使用文档放哪里 | `DocsAI/ECS/<owner>/Usage.md` |
| 测试说明放哪里 | 有测试时放 `DocsAI/ECS/<owner>/Tests.md` |
| 测试代码/场景放哪里 | 当前保持现状；长期可按 owner 迁移，但需独立 SDD |
| `Src/**/*.md` 如何处理 | 只保留短指针，不做长事实源 |
| `Docs/` 如何处理 | 清理后重建为人类简化版，不作为 AI 执行依据 |
| 是否照搬 SlimeAI-AiFirst | 吸收集中事实源和 owner 聚合，不照搬 GameOS 术语 |
