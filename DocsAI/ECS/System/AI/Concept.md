# AI 概念

> status: current
> sourcePaths: Src/ECS/AI/
> relatedDocs: DocsAI/ECS/System/AI/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

AI 系统基于行为树框架，由 BehaviorTreeRunner 每帧 Tick 驱动决策，支持 Sequence/Selector/Decorator 组合节点。

## 2. 核心概念

### 行为树架构

```
BehaviorTreeRunner（每帧 Tick）
  ├─ CompositeNode
  │   ├─ SequenceNode（顺序执行，全成功才成功）
  │   └─ SelectorNode（选择执行，有一个成功就成功）
  ├─ DecoratorNode（装饰器：条件检查、循环、取反）
  └─ LeafNode
      ├─ Condition（条件节点）
      │   ├─ HasValidTarget
      │   ├─ IsInRange
      │   ├─ IsAttackReady
      │   ├─ IsAbilityReady
      │   └─ IsLowHp
      └─ Action（动作节点）
          ├─ Movement/（移动相关）
          └─ Combat/（战斗相关）
```

### AIContext

行为树执行上下文，携带当前 AI 的状态信息（目标、位置、组件引用等）。

### ECS 集成

- **AIComponent**：AI 数据组件
- **EntityMovementComponent**：移动执行
- **AttackComponent**：攻击执行
- **AIDataKeys**：AI 相关数据键

## 3. 职责边界

| AI 做 | AI 不做 |
| ---- | ---- |
| 行为树 Tick 驱动决策 | 具体移动执行（归 Movement） |
| 条件判断与动作选择 | 具体攻击执行（归 AttackComponent） |
| AI 状态管理 | 目标查询（归 TargetSelector） |

## 4. 依赖关系

- **BehaviorTreeRunner**：行为树执行器
- **AIContext**：AI 上下文
- **TargetSelector**：目标查询
- **EntityMovementComponent**：移动执行
- **AttackComponent**：攻击执行
