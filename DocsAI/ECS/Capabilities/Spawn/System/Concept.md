# Spawn 概念

> status: current
> sourcePaths: Src/ECS/Capabilities/Spawn/System/
> relatedDocs: DocsAI/ECS/Capabilities/Spawn/System/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

SpawnSystem 是规则驱动的程序化敌人生成系统，采用 Pipeline 架构（What/Where/How），集成 DataOS snapshot 配置和 TimerManager。

## 2. 核心概念

### Pipeline 架构

```
What（生成什么）→ Where（在哪里生成）→ How（怎么生成）
```

- **What**：从 DataOS snapshot 配置中选择敌人类型
- **Where**：位置采样策略（随机、固定、围绕玩家）
- **How**：实例化方式（EntityManager.Spawn）

### DataOS snapshot 配置

生成参数通过 DataOS snapshot 配置，运行时由 RuntimeDataSnapshotLoader 加载。

### TimerManager 集成

波次计时和生成间隔通过 TimerManager 管理。

## 3. 职责边界

| Spawn 做 | Spawn 不做 |
| ---- | ---- |
| 程序化敌人生成 | 敌人 AI（归 AI System） |
| 波次管理 | 战斗逻辑 |
| 位置采样 | 碰撞检测 |
