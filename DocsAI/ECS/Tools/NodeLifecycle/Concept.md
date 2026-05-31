# NodeLifecycle 概念

> status: current
> sourcePaths: Src/ECS/Tools/NodeLifecycle/
> relatedDocs: DocsAI/ECS/Tools/NodeLifecycle/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

通用节点生命周期管理（注册/查询/注销），是 EntityManager 和 UIManager 的基础。单一职责，关系管理委托给其他模块。

## 2. 核心概念

### 核心 API

```csharp
NodeLifecycleManager.Register(node);
NodeLifecycleManager.Unregister(node);
NodeLifecycleManager.GetNode(id);
NodeLifecycleManager.GetNodesByType(type);
```

### 设计原则

- **单一职责**：只管理节点注册/查询/注销
- **关系管理委托**：Entity-Component 关系由 EntityRelationshipManager 管理
- **是 EntityManager 的基础**：EntityManager 在其之上构建 Entity 特化逻辑

## 3. 职责边界

| NodeLifecycle 做 | NodeLifecycle 不做 |
| ---- | ---- |
| 节点注册/查询/注销 | Entity 特化逻辑（归 EntityManager） |
| 类型索引 | 关系管理（归 EntityRelationshipManager） |
| 基础生命周期 | 对象池管理 |
