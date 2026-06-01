# ECS 框架目录架构大重构

> 状态：current
> 更新：2026-06-01
> 对应 SDD：SDD-0025

## 一句话结论

SlimeAI 当前仓库继续保留 **AI-first ECS** 定位，但源码和 DocsAI 的物理目录从“ECS 技术层平铺”调整为：

```text
Runtime 内核 + Capabilities 功能 owner
```

目标结构：

```text
Src/ECS/
├── Runtime/
│   ├── Entity/
│   ├── Data/
│   ├── Event/
│   └── System/
├── Capabilities/
│   ├── Ability/
│   ├── Damage/
│   ├── Movement/
│   ├── Collision/
│   ├── Feature/
│   ├── Effect/
│   ├── Projectile/
│   ├── AI/
│   ├── Spawn/
│   └── Unit/
├── Tools/
└── UI/
```

DocsAI 对齐：

```text
DocsAI/ECS/
├── Runtime/
├── Capabilities/
├── Tools/
├── UI/
└── Foundations/
```

## 核心裁决

1. **不去 ECS 化**：Entity / Component / Data / Event / System 仍是框架语义、文档契约和实现红线。
2. **物理入口按 Capability 聚合**：AI 修改 Ability、Damage、Movement 等功能时，优先进入同一个 owner 目录，而不是跨 Component/System/Event/Test 多处搜索。
3. **Runtime 只放跨域基础设施**：Entity identity/lifecycle、Data runtime、EventBus、System lifecycle 进入 Runtime；业务能力不得塞回 Runtime。
4. **Capability 内部保留 ECS 子结构**：每个功能域允许 `Component/`、`System/`、`Events/`、`Tests/`、`DataKeys/`、`Docs` 索引，但这些只是 owner 内部结构，不再作为顶层路由。
5. **DocsOld 原文迁入 `DocsAI/ECS/Foundations/`**：原文直接复制，不重写，不作为当前执行入口；用于保存历史理念、设计根基和追溯上下文。

## 阅读顺序

1. `01-现状证据与AI-first裁决.md`
2. `02-目标目录架构与归属规则.md`
3. `03-迁移切片与验证门禁.md`
4. `../../ECS框架与AIFirst方向决策.md`
5. `../3.Entity系统优化/README.md`
6. `../../roadmap.md`

## 与既有设计的关系

- 承接 `DocsAI/ECS框架与AIFirst方向决策.md`：AI-first 是工程方式，不是放弃 ECS。
- 承接 AiFirst 参考项目的 `GameOS/Capabilities` 优点：功能 owner 自包含、依赖显式、测试和事件就近。
- 覆盖早期 `DocsAI/ECS` 按 Entity/Data/Event/Component/System 分类的文档组织方式，但不立即删除旧文档；具体迁移由 SDD-0025 分阶段执行。
- 不改变 DataOS descriptor-first / generated handle / runtime snapshot 当前事实源。
- 不改变 Entity hard cutover 的裁决；目录重构必须在 SDD-0024 已完成结果上执行，不恢复旧 Relationship。

## 非目标

- 不把当前仓迁到 `GameOS/`。
- 不复制 AiFirst 的“不是传统 ECS”定位。
- 不一次性重写系统逻辑。
- 不把 BrotatoLike 专属玩法、UI 或资产路径上提为框架默认。
- 不在本设计中定义每个文件的最终命名空间重构细节；命名空间和 Godot `.tscn` 路径修正属于 SDD-0025 执行任务。

