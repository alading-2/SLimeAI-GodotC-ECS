# TargetSelector 概念

> status: current
> sourcePaths: Src/ECS/Tools/TargetSelector/
> relatedDocs: DocsAI/ECS/Tools/TargetSelector/README.md, DocsAI/ECS/Tools/TargetSelector/Usage.md
> lastReviewed: 2026-06-07

## 1. 一句话定位

TargetSelector 是目标查询领域入口：`TargetQueryEngine` 负责 candidate source、过滤、排序、截断和 diagnostics；纯几何算法直接使用 Math owner 的 `Geometry2D`。

## 2. 核心概念

### 当前架构

```
Geometry2D（底层几何计算）
  └─ 距离、角度、相交、范围判断

TargetQueryEngine（业务查询层）
  ├─ QueryEntities：查询范围内 Entity，返回 TargetQueryResult<IEntity>
  └─ QueryPositions：随机位置采样，返回 TargetQueryResult<Vector2>
```

## 3. 职责边界

| TargetSelector 做 | TargetSelector 不做 |
| ---- | ---- |
| 范围内 Entity 查询、排序、截断与 diagnostics | 目标选择 UI（归 TargetingSystem） |
| 随机位置采样与结果 ownership | AI 决策（归 AI System） |
| 调用 `Geometry2D` 执行纯几何判定/采样 | 拥有底层几何算法 |
| Candidate source 封装 | 业务直接扫描 NodeLifecycle |
