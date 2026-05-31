# TargetingSystem 概念

> status: current
> sourcePaths: Src/ECS/Base/System/TargetingSystem/
> relatedDocs: DocsAI/ECS/System/TargetingSystem/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

TargetingManager 提供异步目标选择会话基础设施，管理 TargetingIndicatorEntity，支持确认/取消流程，回填 CastContext.TargetPosition。

## 2. 核心概念

### 异步目标选择会话

```
发起选择请求 → 创建 TargetingIndicatorEntity → 玩家操作（确认/取消）→ 回填目标位置 → 继续施法
```

### TargetingIndicatorEntity

目标选择指示器实体，显示选择范围和方向。

### CastContext

施法上下文，TargetPosition 在目标选择完成后回填。

## 3. 职责边界

| TargetingSystem 做 | TargetingSystem 不做 |
| ---- | ---- |
| 异步目标选择会话管理 | 具体施法逻辑（归 AbilitySystem） |
| 指示器创建与销毁 | 目标查询（归 TargetSelector） |
| 确认/取消流程 | 伤害计算 |
