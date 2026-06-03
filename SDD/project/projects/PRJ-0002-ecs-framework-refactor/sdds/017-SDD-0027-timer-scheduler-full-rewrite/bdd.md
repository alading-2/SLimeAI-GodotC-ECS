# BDD

## Applicability

- **Required**: true
- **Reason**: Timer 是 gameplay/runtime 热路径工具，本 SDD 修改公共 API、调度语义、生命周期 cleanup、diagnostics 和 Godot 场景验证方式。

## Scenarios

### Scenario: Long delay timers do not cost linear work per frame

Given 10k Game clock long-delay timers are scheduled and none are due
When TimerManager ticks several frames
Then TimerScheduler only checks due queue state, does not scan all active timers, and ordinary tick records no per-frame allocation.

### Scenario: Gameplay callbacks dispatch on main thread

Given timers are scheduled from gameplay systems
When they become due
Then callbacks execute from the main-thread dispatch phase, not from ThreadPool or a background .NET timer callback.

### Scenario: Game pause does not stop Real clock timers

Given one Game clock timer and one Real clock timer are active
When SimulationState enters Suspended
Then the Game timer remaining time does not progress and the Real timer continues according to unscaled time.

### Scenario: Owner cleanup cancels only owned timers

Given multiple owners have cooldown, attack, spawn and DoT timers
When one owner is destroyed or unregistered
Then CancelByOwner cancels only that owner's timers and records cancel reason without scanning all active timers.

### Scenario: Stale handles cannot affect new timers

Given a TimerHandle points to an old timer generation
When the entry slot is reused for a new timer
Then cancel/query with the old handle fails and cannot cancel or observe the new timer.

### Scenario: Per-frame progress timers are isolated

Given 1k timers require progress updates and 10k regular timers do not
When TimerManager ticks a frame
Then only the explicit per-frame list is traversed for progress, while regular timers stay in due queues.

### Scenario: Diagnostics explain Timer health

Given many active, paused, cancelled and loop timers exist
When GetTimerDiagnostics, PrintTimerSummary, PrintTimerDump and ExportTimerDiagnosticsJson are called explicitly
Then the output includes owner/purpose/clock/state counts, heap counts, dispatch queue length, leak hints and bounded entries.

### Scenario: TimerStressValidation produces machine-readable PASS

Given TimerStressValidation.tscn runs headless through the game runner
When all checks pass
Then it writes a PASS JSON artifact containing expectedInputs, expectedObservations, passCriteria, failCriteria, artifactPath, checks, timerDiagnostics and performance data.

### Scenario: DocsAI does not teach nonexistent Timer API

Given an AI reads DocsAI Timer documentation after the refactor
When it searches for TimerManager.Create, WithTimeScale, PauseByTag or ResumeByTag
Then current docs do not present those APIs as usable examples.
