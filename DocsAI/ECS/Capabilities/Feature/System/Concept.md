# FeatureSystem 概念

> status: current
> sourcePaths: Src/ECS/Capabilities/Feature/System/
> relatedDocs: DocsAI/ECS/Capabilities/Feature/System/Usage.md, DocsAI/ECS/Capabilities/Ability/System/Concept.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

FeatureSystem 是通用能力层，表达"被授予时发生什么、何时触发、触发时执行什么、移除时如何回滚"。AbilitySystem 是施法阶段的特化。

## 2. 核心概念

### 三段生命周期

```
Grant（授予）→ Enable（启用）→ Disable（禁用）→ Remove（移除）
```

- **Grant**：创建 Feature 实例，注册 Handler
- **Enable**：激活 Feature，开始监听触发条件
- **Disable**：暂停 Feature
- **Remove**：回滚所有修改，清理资源

### 与 AbilitySystem 的关系

AbilitySystem 是 FeatureSystem 的激活阶段特化：
- FeatureSystem 管理通用能力（被动、光环、Buff）
- AbilitySystem 管理主动施法（输入、目标选择、消耗、冷却）

### Modifier 应用

Feature 通过 Modifier 修改 Entity.Data：
- `AddModifier`：添加修改器
- `RemoveModifier`：移除修改器
- 自动回滚：Feature Remove 时自动清理所有 Modifier

## 3. 职责边界

| FeatureSystem 做 | FeatureSystem 不做 |
| ---- | ---- |
| 能力生命周期管理 | 具体施法逻辑（归 AbilitySystem） |
| Modifier 应用与回滚 | 目标选择 |
| 触发条件监听 | 伤害计算 |

## 4. 依赖关系

- **Data**：Feature 配置和运行时状态
- **Entity.Events**：触发条件监听
- **IFeatureHandler**：Feature 行为定义
- **IFeatureAction**：Feature 动作定义
