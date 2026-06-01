# 01 - 现状证据与 AI-first 裁决

> 状态：current
> 更新：2026-06-01

## 1. 问题陈述

当前 `Src/ECS` 和 `DocsAI/ECS` 同时存在两套心智模型：

- 代码目录仍以 `Base/Component`、`Base/Data`、`Base/Event`、`Base/System` 等 ECS 技术层为主。
- AI 实际执行任务时，用户说的是 Ability、Movement、Damage、Collision、Feature 等功能 owner。

这导致 AI 每次修改一个功能，都需要跨多个目录搜集上下文：

```text
Ability 任务
  -> Src/ECS/Base/Component/Ability
  -> Src/ECS/Base/System/AbilitySystem
  -> Data/DataKey/Ability
  -> Data/EventType/Ability
  -> Src/ECS/Test/SingleTest/ECS/System/AbilitySystemTest
  -> DocsAI/ECS/Component/Ability
  -> DocsAI/ECS/System/AbilitySystem
```

这种结构不是 ECS 本身的问题，而是“物理路由”和“任务语言”不一致。

## 2. 当前事实

### 2.1 当前源码按技术层分散

现状目录包括：

```text
Src/ECS/Base/Component/
Src/ECS/Base/Data/
Src/ECS/Base/Event/
Src/ECS/Base/Entity/
Src/ECS/Base/System/
Src/ECS/Test/
Src/ECS/Tools/
Src/ECS/UI/
```

其中 Ability、Movement、Damage 等功能实际散在多个顶层技术层下。

### 2.2 当前 DocsAI 也保留技术层入口

`DocsAI/ECS/README.md` 当前按核心 owner、System、Tools、UI、Component 分层。这个结构对解释 ECS 概念有帮助，但对执行功能任务不够直接。

例如 Ability 文档被拆到：

```text
DocsAI/ECS/System/AbilitySystem/
DocsAI/ECS/Component/Ability/
```

AI 要改 Ability 时，需要额外判断“这个问题属于 System 还是 Component”，而用户实际意图通常是“改技能能力”。

### 2.3 AiFirst 的 Capability 结构解决的是路由问题

AiFirst 参考项目把功能 owner 聚合在：

```text
GameOS/Capabilities/Ability/
GameOS/Capabilities/Damage/
GameOS/Capabilities/Movement/
```

其中 Ability 目录包含 Service、DataKeys、Events、Tests、capability manifest 等 owner 信息。这个结构让 AI 很快知道：

- 这个功能有哪些数据。
- 这个功能发什么事件。
- 这个功能依赖哪些 Runtime 和其他 Capability。
- 这个功能跑哪些测试。

但 AiFirst 的长期定位曾经弱化 ECS 概念，这一点不能照搬。

## 3. 核心矛盾

目录重构不是“功能域 vs ECS”的二选一，而是两个层级的问题：

| 层级 | 应该回答的问题 | 推荐组织 |
| --- | --- | --- |
| AI 路由层 | 我要改哪个功能？ | `Capabilities/<Ability|Damage|Movement>` |
| 架构语义层 | 功能内部如何工作？ | Entity / Component / Data / Event / System |
| 共享基础设施层 | 所有功能依赖什么内核？ | `Runtime/<Entity|Data|Event|System>` |

如果继续只按 ECS 技术层分，AI 路由成本高。  
如果只讲 Capability、删除 ECS，AI 会失去数据/事件/系统边界。  
因此必须使用混合结构。

## 4. 裁决

### 4.1 顶层保留 `Src/ECS`

目录仍叫 `Src/ECS`，不改成 `GameOS`。原因：

- 当前仓事实源已经裁决为 AI-first ECS。
- ECS 是 AI 和人类共享的成熟心智模型。
- 当前代码、DataOS、DocsAI、Skill、验证脚本都围绕 ECS 主线建立。

### 4.2 `Base/` 退出长期目录语义

`Base/` 不是长期事实源。它曾经承载“底层”的含义，但现在混入了：

- Runtime 基础设施。
- 业务功能 System。
- Godot Component。
- Entity 具体类型。

后续迁移应让 `Base/` 清空或只作为临时兼容过渡，最终目标是不让 AI 从 `Base` 判断业务归属。

### 4.3 Runtime 与 Capabilities 分治

最终目录应表达：

```text
Runtime = 跨所有功能共享的 ECS 内核和基础协议
Capabilities = 业务功能 owner，可选能力包和 AI 修改入口
```

Runtime 不应该承载 Ability、Damage、Movement 这种业务逻辑；Capability 不应该重新实现 Entity、Data、EventBus 或 System lifecycle。

### 4.4 Capability 内部继续使用 ECS 词汇

每个 Capability 内部允许使用：

```text
Component/
System/
Events/
Tests/
DataKeys/
```

这不是倒退回技术层平铺，而是在 owner 内部保留 ECS 语义。AI 先找 owner，再理解 owner 内的 ECS 契约。

## 5. 不推荐方案

### 5.1 继续现状，只补索引

不推荐。索引能缓解路由，但无法改变源码和测试物理分散的问题。AI 仍会在修改中漏掉 DataKey、EventType、Test 或 DocsAI 关联项。

### 5.2 平铺功能目录到 `Src/ECS/Ability`

不推荐作为最终形态。它比技术层分散更好，但 `Runtime` 以外的功能域会和 `Tools/UI` 混在同一层，无法显式表达“这些都是可选能力包”。

### 5.3 照搬 AiFirst，迁到 `GameOS/Capabilities`

不推荐。它会再次引入“纯 GameOS 替代 ECS”的误解，违背当前 AI-first ECS 裁决。

### 5.4 在 `Src` 放 Markdown 文档

不推荐。当前 DocsAI 管理规则明确 `Src/ECS` 不承载框架 Markdown 文档。源码近场信息可以用简短中文注释表达，长期说明仍进 DocsAI。

## 6. 结论

最佳方向是：

```text
Src/ECS/Runtime + Src/ECS/Capabilities
DocsAI/ECS/Runtime + DocsAI/ECS/Capabilities
```

并把 DocsOld 原始理念文档移动到：

```text
DocsAI/ECS/Foundations/
```

这样 AI 的默认路径变成：

```text
AGENTS.md
-> DocsAI/README.md
-> DocsAI/ECS/README.md
-> DocsAI/ECS/Capabilities/<owner>/README.md
-> owner skill
-> Src/ECS/Capabilities/<owner>/
-> Tests / validation
```

