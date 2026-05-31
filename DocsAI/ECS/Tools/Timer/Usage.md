<!-- migrated-from: Src/ECS/Tools/Timer/TimerManager.md -->

> 迁移来源：`Src/ECS/Tools/Timer/TimerManager.md`
> 迁移说明：本文主体从原 `Src/ECS` 文档迁入 `DocsAI` 统一管理；原 `Src/ECS` Markdown 文件已删除。

# TimerManager - 高性能定时器系统

## 概述

`TimerManager` 是轻量级、高性能的定时器管理系统，专为 Godot 4.x + C# 设计。

## 核心优势

- **对象池集成**: 完全集成项目的 `ObjectPool<T>` 系统，零 GC 压力
- **链式 API**: 流畅的 `.OnComplete()`, `.OnLoop()`, `.WithTag()` 调用
- **批量管理**: Tag 系统支持批量操作
- **TimeScale 支持**: 游戏时间 / 真实时间双模式
- **项目级暂停联动**: `SimulationState = Suspended` 时，自动暂停 `useUnscaledTime = false` 的计时器

## 快速开始

### 延迟执行（单次）

```csharp
TimerManager.Instance.Delay(2.0f)
    .OnComplete(() => GD.Print("2秒后执行"));

// 带进度追踪
TimerManager.Instance.Delay(10.0f)
    .OnUpdate(p => progressBar.Value = p)
    .OnComplete(() => GD.Print("完成"));
```

### 无限循环

```csharp
var timer = TimerManager.Instance.Loop(1.0f)
    .WithTag("Buff")
    .OnLoop(() => GD.Print("每秒执行"));
```

### 重复 N 次

```csharp
TimerManager.Instance.Repeat(0.5f, 5)
    .OnRepeat(n => GD.Print($"剩余 {n} 次"))
    .OnComplete(() => GD.Print("全部完成"));

// 立即执行模式：创建后立刻触发第一次回调，两种写法：
TimerManager.Instance.Repeat(0.5f, 5, true)
    .OnRepeat(n => GD.Print($"剩余 {n} 次"));
TimerManager.Instance.Repeat(0.5f, 5)
    .Immediate()
    .OnRepeat(n => GD.Print($"剩余 {n} 次"));
```

### 倒计时

```csharp
TimerManager.Instance.Countdown(10.0f, 0.5f)
    .OnCountdown((elapsed, progress) => {
        label.Text = $"剩余 {10 - elapsed:F0}s";
        progressBar.Value = progress;
    })
    .OnComplete(() => GD.Print("时间到"));

// 立即执行模式：创建后立刻触发第一次回调
TimerManager.Instance.Countdown(10.0f, 0.5f, immediate: true)
    .OnCountdown((elapsed, progress) => {
        label.Text = $"剩余 {10 - elapsed:F0}s";
        progressBar.Value = progress;
    });
```

### 真实时间（不受暂停影响）

```csharp
// UI 动画使用真实时间
TimerManager.Instance.Delay(0.5f, useUnscaledTime: true)
    .OnComplete(() => panel.Hide());
```

### 项目级暂停语义

```csharp
// 默认：受项目暂停影响，暂停菜单打开后会自动停住
TimerManager.Instance.Loop(1.0f)
    .OnLoop(SpawnEnemy);

// 覆盖层/UI：不受项目暂停影响，暂停菜单打开后仍会继续
TimerManager.Instance.Delay(0.25f, useUnscaledTime: true)
    .OnComplete(() => tooltip.Show());
```

- `useUnscaledTime = false`：适合战斗逻辑、波次推进、DoT、恢复、AI 等 gameplay 计时
- `useUnscaledTime = true`：适合暂停菜单、过场 UI、提示动画等 overlay 计时
- 手动 `Pause()/Resume()` 与项目级自动暂停会叠加；项目恢复时不会误恢复手动暂停的 timer

## 标签管理

```csharp
// 创建时设置标签
var timer = TimerManager.Instance.Loop(1.0f)
    .WithTag("Buff");

// 批量操作
TimerManager.Instance.CancelByTag("Buff");
TimerManager.Instance.SetAllTimerPausedByTag("Buff", true);
```

## 生命周期管理（重要）

定时器是池化对象，必须在 `_ExitTree` 中主动取消：

```csharp
public partial class Enemy : CharacterBody2D
{
    private GameTimer _regenTimer;

    public override void _Ready()
    {
        _regenTimer = TimerManager.Instance.Loop(1.0f)
            .WithTag("Buff")
            .OnLoop(OnRegen);
    }

    public override void _ExitTree()
    {
        _regenTimer?.Cancel();
        _regenTimer = null;
    }
}
```

## API 参考

### 工厂方法

| 方法 | 说明 |
|------|------|
| `Delay(float duration)` | 延迟执行（单次） |
| `Loop(float interval)` | 无限循环 |
| `Repeat(float interval, int count)` | 重复 N 次 |
| `Countdown(float duration, float interval, bool immediate = false)` | 倒计时，可选创建后立即首跳 |

### 链式配置

| 方法 | 说明 |
|------|------|
| `.OnComplete(Action)` | 完成回调 |
| `.OnLoop(Action)` | 循环回调 |
| `.OnRepeat(Action<int>)` | 重复回调（参数：次数） |
| `.OnTick(Action<float,float>)` | 倒计时回调（参数：elapsed, progress），也可使用 `.Countdown()` |
| `.OnCountdown(Action<float,float>)` | 同上（链式调用更自然） |
| `.OnUpdate(Action<float>)` | 进度更新（参数：0-1） |
| `.WithTag(string)` | 设置标签 |

### 管理方法

| 方法 | 说明 |
|------|------|
| `Cancel(string id)` | 按 ID 取消 |
| `CancelByTag(string tag)` | 按标签取消 |
| `SetAllTimerPaused(bool)` | 暂停/恢复全部 |
| `SetAllTimerPausedByTag(string, bool)` | 按标签暂停 |
