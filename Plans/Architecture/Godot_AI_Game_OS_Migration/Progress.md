# Godot AI Game OS Migration Progress

> 更新日期：2026-05-06

## 当前状态

Phase 03 的 GameOS Runtime 最小内核已完成到 Relationship / Schedule，Phase 04 的 BrotatoLike 最小接入已完成，Phase 05 GodotBridge 第一版已完成编译接入，M5-M17 的 Movement / Collision / Damage / Ability / Projectile / Effect / Feature / AI / Attack / DataOS 最小闭环已完成，M18 DataOS 扩大迁移与正式生成入口切片已完成，M19 DataOS 旧 AbilityData 通用字段补齐切片已完成，M20 BrotatoLike 统一 Godot 场景测试 runner 和 Observation / Debug / Trace 日志入口已完成，M21 DataOS 敌人生成规则 catalog 已完成，M22 游戏侧 SpawnSystem Tick 实例化入口已完成，M23 RuntimeSchedule 门禁驱动 Spawn Tick 已完成，M24 BrotatoLikeGameRuntime 主运行时入口已完成，M25 Main 场景正式挂载运行时已完成，M26 旧 Main 正式启动逻辑第一段已完成，M27 Ability handler-specific DataOS 参数第三段、`sine_wave_shot` 真实 handler 执行闭环、Boomerang / BezierCurve / CircularArc / ArcShot / Orbit Movement handler 执行接线、Dash 位移 handler 执行接线、ChainLightning 延迟弹跳伤害 handler 执行接线、Slam / CircleDamage 范围伤害 handler 执行接线，以及 TargetPoint / AuraShield 真实 handler 执行接线已完成，M28 Capability 标准模板体系建立已完成，M28.1 GodotPlayerInputComponent + Movement Acceleration 平滑移动 + BrotatoLike 玩家生成已完成。

当前仓库定位已经从长期主项目切换为迁移输入仓库。长期架构建设目标位置是 `/home/slime/Code/SkilmeAI/SkilmeAI`，第一个游戏目标位置是 `/home/slime/Code/SkilmeAI/Games/BrotatoLike`。

## 已完成

- 建立 `Plans/Architecture/Godot_AI_Game_OS_Migration/` 计划入口。
- 建立资产盘点文件 `00_Inventory.md`。
- 建立新工作区骨架 `/home/slime/Code/SkilmeAI/`。
- 建立框架主仓库骨架 `SkilmeAI/`。
- 建立游戏仓库骨架 `Games/BrotatoLike/`。
- 建立 Godot 引擎位置说明 `/home/slime/Code/SkilmeAI/Engine/README.md`。
- 在框架仓库创建 `GameOS/SkilmeAI.GameOS.csproj`。
- 创建 `SkilmeAI.slnx` 并加入 GameOS 项目。
- 创建 `Tools/run-build.sh` 和 `Tools/run-pack.sh`。
- `Tools/run-build.sh` 已通过。
- `Tools/run-pack.sh` 已生成本地 NuGet 包。
- 在 `Games/BrotatoLike` 创建最小 Godot C# 项目。
- `BrotatoLike` 已通过 `ProjectReference` 引用 `SkilmeAI.GameOS`。
- `Games/BrotatoLike/Tools/run-build.sh` 已通过。
- 迁入 Runtime Data 最小内核，并从旧 `IEntity/GameEventType/Log/Godot Resource` 直接依赖中解耦。
- 迁入 Runtime Event 最小内核：`EventBus / EventContext / GlobalEventBus / GameEventType / EventDataChangeSink`。
- 将 `Data` 变更通知通过 `EventDataChangeSink` 接入 `RuntimeEntity.Events`。
- 迁入 Runtime Entity 最小内核：`IEntity / RuntimeEntity / EntitySpawnConfig / EntityManager`。
- 迁入 Runtime Relationship 最小内核：`RelationshipManager / RelationshipType / RelationshipLifecycle / ParentDestroyPolicy`。
- 迁入 Runtime Schedule 最小内核：`RuntimeSchedule / ProjectStateService / SystemRunCondition / IRuntimeSystem / IRuntimeCommandHandler`。
- 迁入 Runtime Resource 最小内核：`ResourceCatalog / ResourceManagement`。
- 迁入 Runtime Pool 最小内核：`ObjectPool<T> / ObjectPoolManager / IPoolable / PoolStats`。
- 迁入 Runtime Timer 最小内核：`TimerManager / GameTimer`，由外部 `Tick` 驱动。
- 建立 `Tests/SkilmeAI.GameOS.Tests` 和 `Tools/run-tests.sh`，覆盖 Event/Data/Entity/Relationship/Schedule/Pool/Timer/Resource 最小行为。
- 在 `Games/BrotatoLike` 建立 `Scenes/Main.tscn`、`Src/Game/Main.cs` 和 `GameBootstrap.RunFrameworkSmokeProbe()`。
- 在 `Games/BrotatoLike/Plans/README.md` 建立新游戏仓库整体迁移计划。
- 迁入 GodotBridge 第一版：`GodotEntity / IGodotComponent / GameOSGodotBridge / GodotNodeRegistry / GameOSTimerDriver`。
- `Games/BrotatoLike` 已建立 `SmokeGodotComponent`，`Main._Ready` 会创建 `GodotEntity + Component + GameOSTimerDriver` 探针。
- 迁入 GodotBridge Node 对象池和碰撞隔离第一段：`GodotNodePool<T> / GodotNodePoolConfig / GodotNodePoolManager / GodotCollisionIsolation`。
- `Games/BrotatoLike` `_Ready` 探针已追加 `GodotNodePool<Area2D>`，覆盖延迟激活、回池脱树、复用和 `ReturnToPool` 编译接入。
- `Games/BrotatoLike/Tools/run-godot-smoke.sh` 已建立，使用现成 Godot 4.6.2 mono CLI 运行 headless smoke，断言 `BrotatoLike GameOS smoke PASS`。
- `Games/BrotatoLike/Tools/run-godot-scene.sh` 已建立统一 Godot 场景测试 runner，支持 `list / run / run-main-smoke`、构建开关、超时和日志目录；`Tools/analyze-godot-scene-logs.sh` 可分析最新 `.ai-temp/scene-tests/runs` 日志，提取 PASS 标记和错误摘要；`DocsAI/GodotSceneTesting.md` 记录 Observation / Debug / Trace 环境变量和日志约定。
- 迁入 Movement Capability 纯 C# 垂直切片：`Vector2Value / MovementDataKeys / MoveMode / MovementParams / IMovementStrategy / MovementStrategyRegistry / ChargeMovementStrategy / OrbitMovementStrategy / SineWaveMovementStrategy / BezierCurveMovementStrategy / BoomerangMovementStrategy / AttachToHostMovementStrategy / PlayerInputMovementStrategy / AIControlledMovementStrategy / ParabolaMovementStrategy / CircularArcMovementStrategy / MovementSystem / MovementStopReason / MovementStopContext`。
- 迁入 Godot 2D 位移桥第一段：`GodotEntity2D / GodotMovementDriver`，由 `MovementSystem.Tick` 推进 Runtime Position，并同步到 `Node2D.Position`。
- 迁入 Collision Capability 第一批：`CollisionLayers / CollisionDataKeys / CollisionFilterPolicy / CollisionContact / CollisionSystem / GameEventType.Collision / GodotAreaEntity2D / GodotCollisionBridge / GodotCollisionComponent / GodotHurtboxComponent`，覆盖 layer/mask、同队过滤、Entered / Exited 运行时事件和 Godot `Area2D` / Hurtbox bridge。
- `Tests/SkilmeAI.GameOS.Tests` 已追加 Movement 测试，覆盖 Charge 按距离推进、到目标点停止、停止事件、`IsMoving` / `Velocity` 复位、Orbit 90 度环绕、SineWave 单周期采样、Bezier 中点/终点、Boomerang 返回完成、AttachToHost 跟随、PlayerInput / AIControlled Data 输入、Parabola 中点顶高和终点、CircularArc 半圈中点和终点。
- `Games/BrotatoLike` 的框架 smoke 已追加 Movement `MovementSystem + MoveMode.Charge` 到点停止断言；GodotBridge smoke 已覆盖 Charge / Orbit / SineWave / BezierCurve / Boomerang / AttachToHost / PlayerInput / AIControlled / Parabola / CircularArc 同步到真实 `Node2D.Position`。
- `Games/BrotatoLike` 的框架 smoke 已追加 Collision `CollisionSystem + CollisionLayers + GameEventType.Collision` 进入事件断言；GodotBridge smoke 已追加 `GodotAreaEntity2D / GodotCollisionComponent / GodotHurtboxComponent` 手动进入/离开事件断言。
- 迁入 AI 攻击请求事件最小 Runtime：`GameEventType.Attack / AttackCancelReason / AIDataKeys.IsAttackRequested / AIDataKeys.AttackRange / IsTargetInRangeCondition / RequestAttackAction`，覆盖范围检测、停步面向目标、请求 payload 和每 Tick 请求标记清理。
- 迁入 AI 巡逻和行为树预制块最小 Runtime：`PatrolAction / EnemyBehaviorBlocks / EnemyBehaviorTreeBuilder`，覆盖确定性左右巡逻、等待倒计时、近战预制树攻击优先和无目标巡逻回退。
- 迁入 Godot AIComponent bridge 第一段：`GodotAIBehaviorTreeKind / GodotAIComponent`，覆盖导出参数写入、手动 / `_Process` Tick、巡逻意图写入和 AIControlled 位移闭环；BrotatoLike headless smoke 已通过。
- 迁入 Attack Runtime 最小结算：`AttackDataKeys / AttackState / AttackService / AttackTriggerResult / AttackTriggerReport`，覆盖攻击事件消费、距离和冷却门禁、前摇 / 后摇 Timer，并通过 `DamageService` 造成 `DamageTags.Attack` 伤害。
- 迁入 GodotAttackComponent bridge 第一段：`GodotAttackComponent` 注册默认 `AttackService`，写入导出攻击参数，把 Godot 节点目标映射为 Runtime 攻击请求；BrotatoLike smoke 覆盖导出参数、节点目标解析和 HP 扣减。
- 迁入 Attack 动画事件桥第一段：`GameEventType.Unit / GodotUnitAnimationComponent`，`GodotAttackComponent` 可把 Attack Started / Cancelled 转为动画播放 / 停止请求；BrotatoLike smoke 覆盖 attack 动画播放和取消回 idle。
- 迁入旧 AttackComponent 场景兼容第一段：`AttackComponent` 继承 `GodotAttackComponent`，默认保留注册前已有 Attack Data；BrotatoLike smoke 覆盖旧类名包装、Data 保留和伤害结算。
- 迁入 Ability 点选目标语义和 Projectile / Effect Runtime 生成第一段：`AbilityCastContext.TargetPosition`、`ProjectileDataKeys / ProjectileTool / GameEventType.Projectile`、`EffectDataKeys / EffectTool / GameEventType.Effect`；Runtime tests 覆盖 Point / EntityOrPoint 目标、Projectile / Effect Data 写入、关系绑定和事件发布，BrotatoLike headless smoke 覆盖项目侧调用。
- 迁入 Projectile / Effect Godot 实例化第一段：`GodotProjectileEffectSpawner` 监听 Projectile / Effect 生成事件，按 `ScenePath` 加载 `PackedScene`，实例化视觉节点并按 Runtime EntityId 注册到 `GodotNodeRegistry`；BrotatoLike headless smoke 覆盖真实投射物 / 特效资产实例化和位置同步。
- 迁入 Projectile 命中生命周期第一段：`ProjectileMovementOptions / ProjectileTool.StartMovement / GameEventType.Projectile.Hit`，通过 MovementCollision 转 DamageService 伤害并支持命中停止 / 销毁；Runtime tests 和 BrotatoLike headless smoke 覆盖命中扣血、Projectile.Hit 和视觉节点清理。
- 迁入 Effect 动画播放第一段：`EffectDataKeys.AnimationName / EffectSpawnOptions.AnimationName / GodotProjectileEffectSpawner`，GodotBridge 实例化 Effect 后自动解析 `AnimatedSprite2D`、播放动画并按 `Duration` 调整 `SpeedScale`；Runtime tests 覆盖动画名写入，BrotatoLike headless smoke 覆盖真实 Effect 资产动画播放。
- 迁入 Ability 自动索敌第一段：`AbilityAutoTargetOptions / AbilityTargetingTool / AbilityDataKeys.AutoTarget* / PrepareAbilityAutoTargetContextsAction`，Runtime tests 覆盖同队 / 死亡 / 范围过滤、自动构造 `AbilityCastContext` 和 AI 自动施法上下文准备；BrotatoLike headless smoke 覆盖真实 Godot Entity 自动索敌命中。
- 迁入 Projectile 穿透 / 生命周期扩展：Movement 碰撞支持同帧多命中并显式跳过自身，`ProjectileDataKeys` 增加 `MaxHitCount / HitCount / MaxLifeTime`，`ProjectileTool.StartMovement` 默认读取最大命中数和生命周期并在停止后销毁；Runtime tests 和 BrotatoLike headless smoke 覆盖穿透两名目标、第三目标不受伤和 MaxLifeTime 到时销毁。
- 迁入 DataOS 正式适配最小闭环：框架仓库新增 SQLite core schema、migration、snapshot generator、validator、`RuntimeDataSnapshot` loader 和 DataOS 验证脚本；BrotatoLike 新增第一批 `DataOS/Authoring/BrotatoLike.seed.sql`、`DataOS/Snapshots/runtime_snapshot.json` 和 `Tools/run-dataos-snapshot.sh`，build 会先生成 snapshot，headless smoke 覆盖读取 `unit.enemy/yuren`、`ability/slam` 写入 Runtime Data 并注册资源映射。
- 扩大 DataOS 旧 DataNew / 系统配置迁移范围：框架新增 `ScheduleDataKeys`，扩展 Unit / Ability / Feature DataMeta；BrotatoLike seed 覆盖 TargetingIndicator、ChainAbility、Feature definition / modifier、System config / preset、Spawn config 和 ResourcePaths 第一批；新增 `BrotatoLikeDataOSBootstrap`，headless smoke 覆盖从 snapshot 生成 Runtime Entity。
- 补齐 DataOS 旧 AbilityData 通用字段：BrotatoLike seed / snapshot 现覆盖 Ability Level / MaxLevel / CostAmount / UsesCharges / MaxCharges / CurrentCharges / ChargeTime，以及缺失的 FeatureGroupId / Description / IconPath / CostType / CastRange / EffectRadius；headless smoke 追加 `ability/chain_lightning` 和 `ability/parabola_shot` 字段断言。
- 建立 BrotatoLike 统一 Godot 场景测试与日志分析入口：`Tools/run-godot-scene.sh` 接管当前 Main smoke，`Tools/run-godot-smoke.sh` 保持兼容委托；`--log-dir` 会生成 stdout / stderr / screenshots / artifacts 目录并注入 `GODOT_SCENE_TEST_*` 环境变量；`Tools/analyze-godot-scene-logs.sh` 已验证可读最新日志并输出 PASS / 错误摘要。
- 推进 DataOS 接真实 SpawnSystem 入口：`BrotatoLikeDataOSBootstrap` 新增 `BuildEnemySpawnCatalog()` 和 `SpawnEnemyFromRule()`，从 `unit.enemy` 与 `spawn.config/default` 构建波次可用敌人生成规则，包含视觉路径、位置策略、间隔、数量、权重和波次限制；BrotatoLike headless smoke 已覆盖第 1 波 yuren / chailangren 规则。
- 推进 Spawn catalog 接真实 Godot 敌人实例化入口：`BrotatoLikeEnemySpawnSystem` 可消费 `BrotatoLikeSpawnCatalog`，通过显式 `Tick()` 按规则实例化 `GodotEntity2D` 敌人包装节点，加载 `Unit.VisualScenePath` 视觉场景，写入 DataOS 敌人字段和 `Movement.Position`；BrotatoLike headless smoke 已覆盖第 1 波生成 2 个 `chailangren` 与 3 个 `yuren`。
- 推进 Spawn Tick 接 RuntimeSchedule 门禁：`BrotatoLikeDataOSBootstrap.BuildSystemScheduleConfig()` 已从 DataOS `system.config/SpawnSystem` 生成 `SystemConfig`，`BrotatoLikeScheduledEnemySpawnSystem` 实现 `IRuntimeSystem + IRuntimeCommandHandler`，由 `RuntimeSchedule.Execute` 驱动 Tick；BrotatoLike headless smoke 已覆盖 Boot 阻断、Gameplay 生成和 Pause 阻断。
- 提取 BrotatoLike 主运行时入口：`BrotatoLikeGameRuntime` 封装 DataOS 初始化、资源注册、Spawn catalog、RuntimeSchedule 注册、Gameplay / Pause 状态切换和 `_Process` 自动 Tick；当前 headless smoke 已通过该节点验证 SpawnSystem 调度链路。
- 接入 Main 正式运行路径：`Scenes/Main.tscn` 已挂载 `GameRuntime` 子节点，普通运行路径由 `Main.StartGameRuntime()` 初始化正式运行时；`--gameos-smoke-exit` 路径保持独立探针并退出。普通 `Scenes/Main.tscn` headless 短时运行和 main smoke 均已通过。
- 迁入旧 Main 正式启动逻辑第一段：`MigrationInput/Src/Main` 的 `GlobalEventBus.TriggerGameStart()` 已替换为游戏侧 `BrotatoLikeGameEventType.Game.Started`，普通入口保留初始化日志，`Scenes/Main.tscn` 补回 `Camera2D`，main smoke 已断言正式启动事件、smoke 入口分离和 Camera 挂载。
- 扩大 Ability handler-specific DataOS 参数第一段：`BrotatoLike.seed.sql` 已补齐新 GameOS 已有 DataKey 覆盖的链式技能默认参数、连线特效路径、自动索敌细节和持续伤害参数；`RunDataOSSnapshotProbe()` 已断言 `AbilityDataKeys.ChainCount / ChainRange / ChainDelay / LineEffectScenePath / AutoTargetMaxTargets / AutoTargetIgnoreSameTeam / AutoTargetRequiresDamageable / DamageInterval / DamageRepeatCount / ApplyImmediateDamage`。
- 扩大 Ability handler-specific DataOS 参数第二段：`BrotatoLike.seed.sql` 已补齐当前 GameOS 已有 `ProjectileDataKeys` / `EffectDataKeys` 可表达的旧 handler 参数，包括 `sine_wave_shot / parabola_shot / boomerang_throw / arc_shot / bezier_shot / orbit_skill` 的投射物速度、命中上限、生命周期和伤害，以及 `slam / parabola_shot / circle_damage / dash` 的特效名称、动画名和持续时间；`RunDataOSSnapshotProbe()` 已断言 `ProjectileDataKeys.Speed / MaxHitCount / MaxLifeTime / Damage` 和 `EffectDataKeys.Name / Duration`。
- 扩大 Ability handler-specific DataOS 参数第三段：框架新增 `MovementDataKeys` 的 SineWave / Orbit / Boomerang / Bezier / Parabola / CircularArc handler authoring 参数；`BrotatoLike.seed.sql` 已补齐旧 movement handler 的振幅、频率、最大距离、轨道数量 / 半径 / 角速度 / 时长、回旋镖弧高 / 停顿 / 回程倍率、Bezier 阶数 / 模式 / 多发数量、CircularArc 时长 / 半径推导 / BowWorldUp 等参数；`RunDataOSSnapshotProbe()` 已断言相关 `MovementDataKeys`。
- 接入 DataOS Movement handler 真实执行闭环：`BrotatoLikeAbilityHandlers` 已注册 `技能.投射物.正弦波射击 / 回旋镖投掷 / 贝塞尔射击 / 定点抛炸弹` 和 `技能.被动.环绕技能`，通过 `AbilityService -> FeatureHandler -> ProjectileTool -> MovementSystem` 从 DataOS Runtime Data 生成投射物并启动 SineWave / Boomerang / BezierCurve / CircularArc / Orbit Movement；headless smoke 已覆盖这些路径。
- 接入 `arc_shot` 真实 DataOS handler 执行闭环：`BrotatoLikeAbilityHandlers` 已注册 `技能.投射物.圆弧射击`，`BrotatoLike.seed.sql` 已补齐 `Projectile.Speed` 和 CircularArc Movement 参数，headless smoke 已覆盖从 DataOS Runtime Data 生成投射物并启动 CircularArc Movement。
- 接入 `dash` 真实 DataOS handler 执行闭环：`BrotatoLikeAbilityHandlers` 已注册 `技能.位移.冲刺`，`BrotatoLike.seed.sql` 已补齐 Charge Movement 速度、最大距离和最大时长参数，headless smoke 已覆盖从 DataOS Runtime Data 启动施法者冲刺移动并生成 Effect Runtime 事件。
- 接入 `chain_lightning` 真实 DataOS handler 执行闭环：`BrotatoLikeAbilityHandlers` 已注册 `技能.主动.连锁闪电`，通过 `AbilityService -> FeatureHandler -> DamageTool -> TimerManager` 从 DataOS Runtime Data 读取链式目标、伤害、弹跳次数、范围、延迟和衰减，headless smoke 已覆盖延迟弹跳命中 3 个敌方 Runtime Entity。
- 接入 `slam / circle_damage` 真实 DataOS handler 执行闭环：`BrotatoLikeAbilityHandlers` 已注册 `技能.主动.猛击` 和 `技能.被动.圆环伤害`，通过 `AbilityService -> FeatureHandler -> DamageTool + EffectTool` 从 DataOS Runtime Data 读取范围、伤害、周期伤害和 Effect 参数，headless smoke 已覆盖范围内敌人扣血、同队 / 范围外过滤和 Effect Runtime 事件。
- 接入 `target_point_skill / aura_shield` 真实 DataOS handler 执行闭环：`target_point_skill` 通过 `技能.主动.位置目标` 范围伤害 handler 读取点选目标、范围、伤害和 Effect 参数；`aura_shield` 通过 `技能.被动.光环护盾` 投射物 handler 读取 Projectile 参数并启动 AttachToHost Movement；headless smoke 已覆盖点选范围伤害、Effect Runtime 事件和 AttachToHost 投射物生成。
- 旧 `assets/` 已复制到 `Games/BrotatoLike/assets`；旧 `Data/` 和 `Src/Main/` 已复制到 `Games/BrotatoLike/MigrationInput/` 并排除编译。
- `Engine/Tools/build-linux-editor-mono.sh` 已建立；当前机器缺少 `SCons`，尚未从源码构建自定义 Godot CLI。现阶段使用 `/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64` 进行 headless 验证。
- 框架仓库和游戏仓库 build 均为 0 warning / 0 error。
- 框架 `MovementDataKeys.Acceleration` 新增，支持 Lerp 平滑移动（backward-compatible，无 Acceleration 时退化为直接速度）。
- 框架 `GodotPlayerInputComponent` 建立，实现 Godot Input Map 到 `MovementDataKeys.InputDirection` 桥接，支持 `CanMoveInput` 门控和 AI 共存。
- BrotatoLike `project.godot` 输入映射已定义（MoveLeft/Right/Up/Down：WASD + 方向键 + 手柄左摇杆）。
- BrotatoLike DataOS `unit.player/deluyi` 已追加 `Movement.Acceleration = 12`。
- BrotatoLike `BrotatoLikeGameRuntime.SpawnPlayer()` 已建立，从 DataOS 生成玩家 Entity，挂载 `GodotPlayerInputComponent` 和 `GodotMovementDriver`，加载 `deluyi` 视觉场景。
- BrotatoLike `Main.StartGameRuntime()` 已自动调用 `SpawnPlayer()`，玩家在游戏启动时生成。
- BrotatoLike smoke 新增 `GodotPlayerInputProbe`，覆盖组件注册、InputDirection Data 写入、Acceleration > 0 平滑加速（0.05s ~45px/s → 0.55s ~100px/s）和 Acceleration = 0 直接速度回退。

## 正在做

- 继续迁剩余 Feature actions 和具体 Ability 逻辑。
- 从旧代码中迁入真实 UI / 输入 / 场景内容到新仓库 `Src/Game/`。
- 建立 Capability 回归测试场景并接入 `Tools/run-godot-scene.sh`。
- 玩家技能输入（LB/RB 切换、X 释放、Point 目标点选）接入 `GodotPlayerInputComponent`。

## 下一步

1. 扩大 DataOS：迁移更多剩余 Feature actions 和具体 Ability 逻辑，继续减少 smoke 中的临时硬编码。
2. 从 `MigrationInput/` 适配 BrotatoLike 游戏特定 Data、真实 UI / 输入和正式场景内容。
3. 继续整理 Main 的测试入口与正式入口边界，避免 smoke 探针污染普通运行路径。
4. 把后续 UI / SpawnSystem / 真实主场景 smoke 接入 `Tools/run-godot-scene.sh`，并把 trace 写到 `.ai-temp/scene-tests/runs/<date>/<time>/artifacts`。
5. Godot 引擎底层修改统一进入 `/home/slime/Code/SkilmeAI/Engine/godot-4.6.2-stable`。

## 当前阻塞

无代码阻塞。GodotBridge 对象池碰撞隔离、Movement、Collision、Damage / ContactDamage / Attack / Ability / Projectile / Effect / DataOS bootstrap / `BrotatoLikeGameRuntime` / Main 正式运行路径 / RuntimeSchedule 门禁驱动的 SpawnSystem Tick 实例化已通过 BrotatoLike headless smoke；本机缺少 `SCons`，自定义 Godot CLI 尚未从源码构建；旧 DataNew 全量迁移和真实主场景入口逻辑尚未完成。

## 最新验证

```bash
cd /home/slime/Code/SkilmeAI/SkilmeAI
Tools/run-build.sh
Tools/run-tests.sh
Tools/run-pack.sh
cd /home/slime/Code/SkilmeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-smoke.sh
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 3 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

结果：

- build：0 warning / 0 error。
- tests：Event/Data/DataOS snapshot/Entity/Relationship/Schedule/Pool/Timer/Resource/Collision/Movement 全部 PASS，Movement 覆盖 Charge / Orbit / SineWave / BezierCurve / Boomerang / AttachToHost / PlayerInput / AIControlled / Parabola / CircularArc，Collision 覆盖 layer/mask、同队过滤和 Entered / Exited 事件。
- pack：生成 `Packages/LocalNuGet/SkilmeAI.GameOS.0.1.0-alpha.0.nupkg`。
- BrotatoLike build：0 warning / 0 error；`Tools/run-build.sh` 会先生成 DataOS runtime snapshot；普通 `Scenes/Main.tscn` headless 短时运行通过并输出 `BrotatoLike main scene initialized`；`Tools/run-godot-smoke.sh` 和 `Tools/run-godot-scene.sh run-main-smoke` 输出 `BrotatoLike GameOS smoke PASS`，覆盖 Runtime / DataOS bootstrap / Ability handler-specific DataOS 参数第三段 / SineWave / Boomerang / BezierCurve / CircularArc / ArcShot / Orbit / Dash / ChainLightning / Slam / TargetPoint / CircleDamage / AuraShield 真实 handler 执行闭环 / **PlayerInput + Acceleration 平滑移动 / GodotPlayerInputComponent** / `BrotatoLikeGameRuntime` / Main 正式启动事件 / RuntimeSchedule 门禁驱动的 SpawnSystem Tick 实例化 / Collision / Movement / Attack / Godot AI bridge / Ability / Projectile / Effect / GodotBridge / NodePool；`Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/2026-05-06/13-15-57` 输出 PASS marker found 且 Error markers none。
