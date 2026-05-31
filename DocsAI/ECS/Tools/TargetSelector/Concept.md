# TargetSelector 概念

> status: current
> sourcePaths: Src/ECS/Tools/TargetSelector/
> relatedDocs: DocsAI/ECS/Tools/TargetSelector/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

目标查询三层架构：Geometry2D（底层几何）→ GeometryCalculator（计算器）→ EntityTargetSelector / PositionTargetSelector（业务查询）。

## 2. 核心概念

### 三层架构

```
Geometry2D（底层几何计算）
  └─ 距离、角度、相交、范围判断

GeometryCalculator（计算器层）
  └─ 扇形、圆形、矩形范围计算

EntityTargetSelector / PositionTargetSelector（业务查询层）
  ├─ EntityTargetSelector：查询范围内 Entity
  └─ PositionTargetSelector：随机位置采样
```

## 3. 职责边界

| TargetSelector 做 | TargetSelector 不做 |
| ---- | ---- |
| 范围内 Entity 查询 | 目标选择 UI（归 TargetingSystem） |
| 随机位置采样 | AI 决策（归 AI System） |
| 几何计算 | 物理碰撞检测 |
