# System Core 概念

> status: current
> sourcePaths: Src/ECS/Base/System/Core/
> relatedDocs: DocsAI/ECS/System/Core/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

System Core 是 ECS 系统层的注册与生命周期管理基础设施，采用数据驱动的系统注册模型（SystemId + Factory + DataOS snapshot）。

## 2. 核心概念

### 四层分层

```
SystemManager（系统管理层）
  ├─ ProjectState（项目级状态：游戏暂停、菜单、战斗）
  ├─ Entity Status Effect（实体状态效果：无敌、眩晕、减速）
  └─ AI Behavior Tree（行为树：巡逻、追击、攻击）
```

### 数据驱动系统注册

系统通过 DataOS snapshot 配置，运行时由 SystemRegistry 统一管理。每个系统有：
- **SystemId**：唯一标识
- **Factory**：系统实例工厂
- **生命周期**：Enable/Disable + Start/Stop + SystemProfile

### 运行条件

系统可以声明运行条件（如"游戏未暂停"、"玩家存活"），SystemManager 在每帧执行前检查。

## 3. 职责边界

| SystemCore 做 | SystemCore 不做 |
| ---- | ---- |
| 系统注册与发现 | 具体业务逻辑（归各 System） |
| 生命周期管理 | 系统间直接调用 |
| 运行条件检查 | 手动 new System() |

## 4. 依赖关系

- **SystemManager**：系统管理层
- **SystemRegistry**：系统注册表
- **SystemProfile**：系统配置
- **DataOS snapshot**：系统配置来源
