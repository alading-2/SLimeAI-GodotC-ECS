# DamageSystem 概念

> status: current
> sourcePaths: Src/ECS/Base/System/DamageSystem/
> relatedDocs: DocsAI/ECS/System/DamageSystem/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

DamageSystem 采用 Pipeline + Middleware 模式，实现 10 阶段伤害处理管线。

## 2. 核心概念

### 10 阶段管线

```
BaseDamage → Dodge → Crit → Shield → Defense → Amplification
  → FlatReduction → Lifesteal → HealthExecution → Statistics
```

每个处理器有优先级和核心职责，按顺序执行。

### DamageInfo

单次伤害上下文，携带：
- 基础伤害值
- 来源 Entity
- 目标 Entity
- 伤害类型
- 各阶段处理结果

### IDamageProcessor

中间件接口，每个处理器可以：
- 修改 DamageInfo
- 阻止后续处理（如闪避成功）
- 添加副作用（如吸血）

### 使用入口

```csharp
DamageService.Instance.Process(damageInfo);
```

## 3. 职责边界

| DamageSystem 做 | DamageSystem 不做 |
| ---- | ---- |
| 伤害管线编排 | 具体战斗数值（归 Data/配置） |
| 中间件注册与执行 | 碰撞检测（归 CollisionSystem） |
| 伤害追溯 | UI 显示（归 UI 层） |

## 4. 依赖关系

- **DamageService**：伤害处理入口
- **DamageInfo**：伤害上下文
- **IDamageProcessor**：中间件接口
- **HealService**：治疗服务
- **Data**：HP、Defense、Crit 等数值
