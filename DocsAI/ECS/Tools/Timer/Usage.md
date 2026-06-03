# Timer 使用说明

> sourcePaths: Src/ECS/Tools/Timer/
> relatedDocs: DocsAI/ECS/Tools/Timer/Concept.md
> lastReviewed: 2026-06-03

## 新 gameplay timer

新业务代码优先使用 `TimerOptions` 和 `TimerHandle`。owner、purpose、clock 和 cancel point 必须在创建 timer 时一起设计。

```csharp
private TimerHandle _cooldownTimer;

private void StartCooldown(string abilityId, float seconds)
{
    var owner = new TimerOwner(TimerOwnerType.Ability, abilityId);
    var options = new TimerOptions(
        owner,
        TimerPurpose.Cooldown,
        TimerClock.Game,
        Source: nameof(StartCooldown));

    _cooldownTimer = TimerManager.Instance.Delay(seconds, options, () =>
    {
        _cooldownTimer = default;
        MarkCooldownReady();
    });
}

private void CancelCooldown(string abilityId)
{
    var owner = new TimerOwner(TimerOwnerType.Ability, abilityId);
    TimerManager.Instance.CancelByOwnerAndPurpose(
        owner,
        TimerPurpose.Cooldown,
        TimerCancelReason.AbilityCancelled);
}
```

## Delay / Loop / Repeat / Countdown

```csharp
var owner = new TimerOwner(TimerOwnerType.Component, $"{Entity.Id}:regen");
var options = new TimerOptions(owner, TimerPurpose.Recovery, TimerClock.Game);

TimerHandle delay = TimerManager.Instance.Delay(1.0f, options, OnDelayComplete);

TimerHandle loop = TimerManager.Instance.Loop(1.0f, options, RecoverOnce);

TimerHandle repeat = TimerManager.Instance.Repeat(
    0.2f,
    count: 3,
    options,
    remaining => EmitBurst(remaining),
    onComplete: FinishBurst);

TimerHandle countdown = TimerManager.Instance.Countdown(
    duration: 3.0f,
    interval: 0.1f,
    options with { OnUpdate = progress => progressBar.Value = progress },
    onTick: (elapsed, progress) => UpdateCastBar(elapsed, progress),
    onComplete: CompleteCast);
```

循环 timer 必须有 cancel point：

```csharp
public override void _ExitTree()
{
    TimerManager.Instance.Cancel(_recoveryTimer, TimerCancelReason.ComponentUnregistered);
    _recoveryTimer = default;
}
```

## Clock 选择

```csharp
// 战斗逻辑：受项目暂停影响。
var gameplay = new TimerOptions(owner, TimerPurpose.AttackRecovery, TimerClock.Game);

// UI overlay：项目暂停时仍继续。
var overlay = new TimerOptions(owner, TimerPurpose.Debug, TimerClock.Real);
```

`TimerClock.Game` 适合 cooldown、charge、attack、contact damage、DoT、spawn、AI wait、recovery。`TimerClock.Real` 只给暂停菜单、overlay、debug UI 等真实时间逻辑使用。

## 取消和查询

```csharp
TimerManager.Instance.Cancel(handle, TimerCancelReason.Manual);
TimerManager.Instance.CancelByOwner(owner, TimerCancelReason.OwnerDestroyed);
TimerManager.Instance.CancelByOwnerAndPurpose(owner, TimerPurpose.DoT, TimerCancelReason.TargetInvalid);

if (TimerManager.Instance.TryGetRemaining(handle, out var remaining))
{
    label.Text = $"{remaining:F1}s";
}

if (TimerManager.Instance.TryGetProgress(handle, out var progress))
{
    progressBar.Value = progress;
}
```

不要把 tag 当作生命周期语义。tag 只能作为兼容或调试分类；业务 cleanup 以 owner/purpose/handle 为准。

## Diagnostics

```csharp
var snapshot = TimerManager.Instance.GetTimerDiagnostics(
    new TimerDiagnosticsFilter(Owner: owner, MaxEntries: 20));

GD.Print(TimerManager.Instance.FormatTimerSummary(snapshot, topN: 5));
GD.Print(TimerManager.Instance.FormatTimerDump(snapshot));

TimerManager.Instance.ExportTimerDiagnosticsJson(
    ".ai-temp/timer-diagnostics.json",
    new TimerDiagnosticsFilter(Purpose: TimerPurpose.DoT, MaxEntries: 100));
```

诊断 dump 是显式工具，不允许每帧无上限打印。

## Legacy facade

旧 `GameTimer` 链式调用仍保留，用于旧测试、demo 或迁移过渡：

```csharp
GameTimer legacy = TimerManager.Instance.Delay(0.5f)
    .OnComplete(OnLegacyComplete);
```

新增 gameplay 不使用 legacy facade 作为默认入口。需要迁移旧代码时，按 owner/purpose/clock 设计 `TimerOptions`，并保留清晰 cancel point。

## 禁止项

- 不在 gameplay 中直接使用 `GetTree().CreateTimer(...)` 或 Godot `Timer` 节点。
- 不用 `.NET` timer、`PeriodicTimer` 或 `Task.Delay` 执行业务 callback。
- 不在 Timer core 内调用 Godot API。
- 不通过后台线程调用 `EntityManager.Spawn/Destroy`、`Data.Set`、`EventBus.Emit`、`Node.AddChild` 或 `QueueFree`。
- 不恢复每帧扫描全部 active timer 的主循环。

## 验证

```bash
dotnet run --project Tools/TimerSchedulerTdd/TimerSchedulerTdd.csproj
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

Godot 场景验证需要承载游戏提供 runner：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/Timer/Tests/TimerStressValidation.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

不能只用 stdout 或 exit code 声明场景通过；必须同时有 README 五字段、runner `index.json`、per-scene `result.json` 和 PASS artifact。
