# NodeLifecycle 历史说明

> status: historical
> current: DocsAI/ECS/Runtime/NodeLifecycle/README.md
> sourcePaths: Src/ECS/Runtime/NodeLifecycle/
> lastReviewed: 2026-06-07

## 1. 一句话定位

旧 Tools NodeLifecycle 文档仅作迁移追溯。当前 NodeLifecycle 是 Runtime 底层 registry，用于 owner/source metadata、snapshot diagnostics 和 invalid cleanup；业务查询入口不走 NodeLifecycle。

## 2. 核心概念

### 核心 API

current 示例见 `DocsAI/ECS/Runtime/NodeLifecycle/README.md`。

### 设计原则

- **单一职责**：只管理节点注册/查询/注销
- **关系管理委托**：Entity 生命周期父子关系由 `LifecycleTree` 管理，Component owner 反查由 `ComponentRegistrar` 管理，业务 owner 由各 capability service 管理
- **是 EntityManager 的基础**：EntityManager 在其之上构建 Entity 特化逻辑

## 3. 职责边界

| Runtime NodeLifecycle 做 | Runtime NodeLifecycle 不做 |
| ---- | ---- |
| 节点注册/注销和 diagnostics | Entity 特化查询（归 EntityManager / TargetQueryEngine） |
| owner/source metadata | 关系管理（归 `LifecycleTree` / `ComponentRegistrar` / capability service） |
| 基础生命周期 | 对象池管理 |
