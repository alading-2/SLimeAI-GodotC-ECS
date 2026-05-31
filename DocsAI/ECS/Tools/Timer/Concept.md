# Timer 概念

> status: current
> sourcePaths: Src/ECS/Tools/Timer/
> relatedDocs: DocsAI/ECS/Tools/Timer/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

高性能定时器系统，集成 ObjectPool 实现零 GC，支持链式 API、Tag 批量操作、TimeScale 和项目级暂停联动。

## 2. 核心概念

### 链式 API

```csharp
TimerManager.Create(2.0f)
    .OnComplete(() => DoSomething())
    .WithTag("cooldown")
    .WithTimeScale(TimeScale.Game);
```

### Tag 批量操作

```csharp
TimerManager.PauseByTag("cooldown");
TimerManager.ResumeByTag("cooldown");
TimerManager.CancelByTag("cooldown");
```

### TimeScale

- **Game**：受游戏暂停/加速影响
- **Real**：不受游戏状态影响

### 项目级暂停联动

与 SimulationState 联动，暂停时自动暂停所有 Game 类型定时器。

## 3. 职责边界

| TimerManager 做 | TimerManager 不做 |
| ---- | ---- |
| 高性能定时器管理 | 游戏逻辑 |
| 零 GC 对象池集成 | 帧级精确计时 |
| Tag 批量操作 | 物理计时 |
