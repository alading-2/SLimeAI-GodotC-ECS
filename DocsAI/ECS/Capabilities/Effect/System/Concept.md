# Effect 概念

> status: current
> sourcePaths: Src/ECS/Capabilities/Effect/
> relatedDocs: DocsAI/ECS/Capabilities/Effect/System/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

特效系统三层架构：EffectTool（入口/编排）→ EffectComponent（挂载/跟随/计时）→ EffectEntity（Node2D + IEntity + IPoolable）。

## 2. 核心概念

### 三层架构

```
EffectTool（入口/编排）
  ├─ 创建特效
  ├─ 管理特效生命周期
  └─ 自定义编排逻辑

EffectComponent（挂载/跟随/计时）
  ├─ 挂载到目标 Entity
  ├─ 跟随目标位置
  └─ 计时销毁

EffectEntity（Node2D + IEntity + IPoolable）
  ├─ 视觉表现
  ├─ 对象池复用
  └─ IPoolable 生命周期
```

### 关键边界

- `EffectTool` 是特效领域 facade；当前仍处理对象池和视觉注入，但生命周期只接入 `EntityManager.AttachLifecycleParent / LifecycleTree`，host/owner 业务引用只接入 `EffectOwnershipService`。
- 特效 Entity 可以进入对象池。
- 特效跟随目标 Entity 的位置。
- 不用 `EntityRelationshipManager` 表达 effect host、owner 或 damage attribution。

## 3. 职责边界

| Effect 做 | Effect 不做 |
| ---- | ---- |
| 特效创建与销毁 | 伤害计算 |
| 特效跟随与计时 | 碰撞检测 |
| 视觉表现 | 战斗逻辑 |

## 4. 依赖关系

- **EffectTool**：特效编排入口
- **EffectOwnershipService**：Effect host/owner projection 和 owner list 查询入口
- **EffectComponent**：特效组件
- **EffectEntity**：特效实体
- **ObjectPool**：特效对象池
- **TimerManager**：特效持续时间
