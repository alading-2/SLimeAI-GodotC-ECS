# Timer 概念

> status: current
> sourcePaths: Src/ECS/Tools/Timer/
> relatedDocs: DocsAI/ECS/Tools/Timer/Usage.md
> lastReviewed: 2026-06-03

## 1. 一句话定位

`TimerManager` 是 Godot 生命周期 adapter；实际调度由纯 C# `TimerScheduler` 负责。新 gameplay timer 必须通过 `TimerHandle + TimerOptions` 显式声明 owner、purpose、clock 和 cancel point，避免每帧扫描全部 active timer。

## 2. 核心概念

### TimerScheduler

- 纯 C# core，不依赖 `Godot.Node`、`GD.Print`、`SceneTree` 或 Godot 时间 API。
- 内部按 clock 维护 min-heap due queue；普通 no-due tick 只查看堆顶，不扫描全部 active timer。
- callback 先进入 dispatch queue，再由 `TimerManager` 在主线程调用 `DispatchDueCallbacks()`。
- `OnUpdate` 只加入显式 per-frame update 列表，不让普通 timer 进入每帧进度扫描。

### TimerManager

- 作为全局系统入口，接收 Godot `_Process(delta)` 和真实时间 delta。
- 同一帧依次 tick `TimerClock.Game`、`TimerClock.Real`，再主线程派发 callback。
- 与项目 `SimulationState.Suspended` 联动：暂停 `TimerClock.Game`，不影响 `TimerClock.Real`。
- 旧 `GameTimer` 链式 API 仅作为兼容 facade；新 gameplay 代码优先使用 handle/options API。

### TimerHandle

`TimerHandle` 包含 slot id 和 generation。取消、查询 remaining/progress 时必须传 handle；generation 用于拒绝复用 slot 后的旧 handle。

### TimerOptions

`TimerOptions` 是新 timer 的语义契约：

| 字段 | 说明 |
| ---- | ---- |
| `Owner` | 生命周期归属，例如 Component、Entity、System、Ability、Feature、Test。 |
| `Purpose` | 业务用途，例如 Cooldown、Charge、AttackWindup、DoT、SpawnWave、AIWait。 |
| `Clock` | `Game` 受项目暂停影响；`Real` 不受游戏暂停影响；`Fixed` 预留给固定步长。 |
| `Tag` | 辅助分类；不能替代 owner/purpose。 |
| `OnUpdate` | 显式 per-frame progress callback；只给真正需要每帧进度的 timer 使用。 |
| `Source` | 调试来源字符串，用于 dump 和 artifact。 |

### Owner / Purpose

- gameplay timer 必须能按 owner 批量取消，组件卸载、实体销毁、目标失效时要有明确 cancel point。
- 多个 timer 不能共用泛 purpose；例如攻击需要区分 `AttackWindup`、`AttackRecovery`、`AttackValidation`。
- 多目标 relation timer 的 owner id 应包含 source-target 或 relation id。

### Clock

- `TimerClock.Game`：战斗逻辑、冷却、攻击、DoT、AI、波次推进。
- `TimerClock.Real`：暂停菜单、overlay、调试 UI、真实时间提示。
- 不在 gameplay 中直接使用 Godot Timer、SceneTreeTimer、.NET timer 或 `Task.Delay` 触发业务 callback。

### Diagnostics

诊断快照只在显式调用时创建，不参与普通 tick。核心字段包括 active/paused/dispatch/per-frame 数量、heap count、owner type 分布、purpose 分布、clock 分布、lazy cancelled heap items、tick/dispatch cost、top owners、leak hints 和 bounded entries。

主要入口：

```csharp
var snapshot = TimerManager.Instance.GetTimerDiagnostics(
    new TimerDiagnosticsFilter(Purpose: TimerPurpose.DoT, MaxEntries: 20));

TimerManager.Instance.PrintTimerSummary(topN: 5);
TimerManager.Instance.ExportTimerDiagnosticsJson(
    ".ai-temp/timer-diagnostics.json",
    new TimerDiagnosticsFilter(MaxEntries: 100));
```

## 3. 职责边界

| TimerManager 做 | TimerManager 不做 |
| ---- | ---- |
| 接收 Godot 生命周期 tick | 在 core 中调用 Godot API |
| 驱动 `TimerScheduler` | 直接承载游戏逻辑 |
| 主线程派发 callback | 用后台线程执行 gameplay callback |
| 提供 handle/options facade 和 legacy facade | 让旧 tag 语义替代 owner/purpose |
| 输出显式 diagnostics / JSON dump | 每帧自动打印无上限 dump |

## 4. 验证入口

纯 C# scheduler：

```bash
dotnet run --project Workspace/Tools/TimerSchedulerTdd/TimerSchedulerTdd.csproj
```

框架 build：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

Godot stress scene：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/Timer/Tests/TimerStressValidation.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

`TimerStressValidation` 的标准答案文件是 `Src/ECS/Tools/Timer/Tests/README.md`，artifact 路径是 `.ai-temp/scene-tests/artifacts/timer-stress-validation.json`。
