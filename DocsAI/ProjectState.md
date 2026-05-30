# DocsAI ProjectState

本文记录 AI-First 文档体系当前状态。保持短，只写能帮助下一个 AI 会话继续工作的内容。

## 当前目标

当前默认目标已从“迁移到 Godot AI Game OS / SkilmeAI 多仓库”纠偏为“回到旧 Godot C# ECS 框架主线，先治理事实源、入口和验证，再小步优化 `Src/ECS`”。

当前正在推进：

- `SlimeAINew/DataOS/` SQLite-only authoring 真相源。
- Data runtime 已通过 SDD-0021 收口为 `runtime_snapshot.json` + snapshot query/projection + generated typed handle + catalog-bound `Data` 主链路。当前 Data 修改不要使用旧 `DataKey` 别名、未绑定 Data 路径、C# 静态表、DataMeta/DataRegistry 或 Resource `.tres` authoring 作为事实源。
- 旧 `.tres` 数据插件停用。

当前优先校准：

- `DocsAI/Modules/` 模块契约。
- `.claude/skills/*` / `.codex/skills/*` 短入口。
- `Docs/` 人类文档入口和项目索引。
- `Src/**/*.md` 已全部移除，源码入口统一由项目索引指向 .cs 文件。
- 历史 Godot AI Game OS 彻底迁移方向保留为 history / migration-pointer，可作为机制参考，不作为当前默认路线。
- 历史 SkilmeAI 多仓库彻底迁移方向保留为 history / migration-pointer；当前仓库不再默认降级为迁移输入。

## 当前纠偏入口

- 工作区入口：`/home/slime/Code/SlimeAI/Workspace/DocsAI/SlimeAINewReorientation/00-README.md`
- OpenSpec change：`/home/slime/Code/SlimeAI/openspec/changes/reorient-slimeainew-ecs-framework/`
- 本仓入口：`AGENTS.md`、`DocsAI/INDEX.md`
- 本 change 不修改 `Src/ECS` 核心代码。旧 ECS 核心审计和实现优化必须放到后续 `audit-slimeainew-ecs-core` 或其它明确 OpenSpec change。

## 当前阶段

`AI_First_Docs_Code_Alignment`（二轮收敛）已完成。历史计划仍可作为审计材料。后续当前切片：
- 恢复 `SlimeAINew` DocsAI 路由和历史材料分类。
- 初始化或设计 `Games/BrotatoLike/` ECS-return 新游戏目录。
- 审计 `SlimeAINew/Src/ECS` 核心模块，先验证再改代码。

历史已完成 / 历史迁移指针：
- `Plans/Architecture/AI_First_Test_Infra_Deep_Docs/`（5 阶段完成）：GodotSkill 测试基础设施 + Movement/AI/Collision 文档深层审计
- `Plans/Architecture/AI_First_Src_Docs_Deep_Audit/`（完成）：剩余 Src .md 全量删除，唯一真相源为 DocsAI + Docs/ + 项目索引
- `Plans/Architecture/框架整体迁移/迁移.md`：history / migration-pointer，旧不兼容迁移到 Godot AI Game OS 的总计划。
- `Plans/Architecture/SkilmeAI_多仓库彻底迁移/README.md`：history / migration-pointer，旧多仓库迁移计划。
- `Plans/Architecture/Godot_AI_Game_OS_Migration/README.md`：history / migration-pointer，旧 Godot AI Game OS 迁移控制面计划。

## 已完成

- 旧 AI-First 迁移计划已建立 Docs / DocsAI / Skill / 测试 / 门禁基础。
- 本轮已新建：
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/README.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/01_Code_And_Docs_Audit.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/02_DocsAI_Module_Contracts_Update.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/03_Skill_Short_Entry_Refactor.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/04_Src_Docs_Consolidation.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/05_Final_Verification_And_Handoff.md`
- 本轮已补齐 `DocsAI/Modules/FeatureSystem.md` 与 `DocsAI/Modules/DataAuthoring.md`。
- 第二批已修正 Ability 参考示例、Data 测试 README、TestSystem README 和旧迁移计划状态分叉。
- 第三批已修正 CostComponent、Data README、Component 规范、EntityManager 文档和两个测试 README。
- 第四批已修正 Tools 族 ObjectPool、TargetSelector、TimerManager 源码旁文档。
- 第五批已修正 Component / Attack / Collision / UI 源码旁文档和 DocsAI 契约。
- 第 11 批已修正 Movement / AI / Collision 源码旁入口，新增三个 DocsAI 契约和三个短 Skill。
- Skill 当前为 16 个 `SKILL.md`，总计 906 行。
- 新对话交接见 `Plans/Architecture/AI_First_Docs_Code_Alignment/10_Handoff_For_New_Conversation.md`。

## 已完成（本轮）

- 压缩长 Skill 为短入口。
- 清理旧机器绝对路径、旧 Windsurf Skill 入口和过期计划指向。
- 更新本轮计划状态文件和验证结果。
- 新增 `DocsAI/Protocols/AI原生数据层协议.md`，明确 SQLite DataOS + 生成快照为目标。
- 新增 `DocsAI/Protocols/外部资料与源码研究协议.md` 和 `.codex/skills/research-reference-framework/SKILL.md`。
- 新增 `DocsAI/Protocols/AI表现复盘协议.md` 与 `DocsAI/Experience/` 经验库入口。
- 新增 Godot 物理与对象池碰撞经验文档，沉淀 PhysicsServer2D 时序和底层 trace 方向。
- 新增 `Plans/Architecture/SlimeAI_多仓库彻底迁移/README.md`，固化 SlimeAI 顶层工作区、多仓库、项目引用 / 本地 NuGet / DLL 三阶段和里程碑。
- 新增 `DocsAI/Protocols/SlimeAI多仓库AI工作流协议.md`，规定 AI CLI 在框架仓库、游戏仓库和 Godot 引擎仓库之间如何读取上下文与处理跨仓库修改。
- 启动 Godot AI Game OS 迁移控制面：新增 `Plans/Architecture/Godot_AI_Game_OS_Migration/`。
- 创建 `/home/slime/Code/SlimeAI/` 工作区骨架、`SlimeAI` 框架仓库骨架和 `Games/BrotatoLike` 游戏仓库骨架。
- 在 `/home/slime/Code/SlimeAI/SlimeAI` 创建 `SlimeAI.GameOS` 最小可构建包。
- `Tools/run-build.sh` 通过，`Tools/run-pack.sh` 已生成本地 NuGet 包。
- 在 `/home/slime/Code/SlimeAI/Games/BrotatoLike` 创建最小 Godot C# 项目。
- `BrotatoLike` 已通过本地项目引用接入 `SlimeAI.GameOS`，游戏仓库 build 通过。
- Runtime Data 最小内核已迁入 `SlimeAI.GameOS`，并同步 Contracts / ApiIndex。
- Runtime Event / Entity / Relationship / Schedule / Resource / Pool / Timer 最小内核已迁入 `SlimeAI.GameOS`。
- GodotBridge 第一版已迁入 `SlimeAI.GameOS`：`GodotEntity / IGodotComponent / GameOSGodotBridge / GodotNodeRegistry / GameOSTimerDriver`。
- Movement Capability 第一段纯 C# Charge / Orbit / SineWave / Parabola / CircularArc 垂直切片已迁入 `SlimeAI.GameOS`：`MovementSystem / MovementDataKeys / MoveMode.Charge / Orbit / SineWave / Parabola / CircularArc / MovementParams / ChargeMovementStrategy / OrbitMovementStrategy / SineWaveMovementStrategy / ParabolaMovementStrategy / CircularArcMovementStrategy`。
- Godot 2D 位移桥第一段已迁入 `SlimeAI.GameOS`：`GodotEntity2D / GodotMovementDriver`。
- Damage / ContactDamage / Damage 处理器管线 / HealService / DamageTool 第一批已迁入 `SlimeAI.GameOS`：`DamageService / HealService / DamageTool / IDamageProcessor / Base / Dodge / Critical / Shield / Armor / DamageTakenAmplification / HealthExecution / Lifesteal / Statistics / GodotContactDamageComponent`，Runtime tests 覆盖扣血、击杀、闪避、暴击、护盾、护甲、真实伤害、吸血、治疗、多目标和周期伤害、统计。
- Ability Runtime 最小切片已迁入 `SlimeAI.GameOS`：`AbilityDataKeys / AbilityService / AbilityTargetingTool / AbilityCastContext / AbilityExecutedResult / GameEventType.Ability`，Runtime tests 覆盖显式目标触发、点选目标触发、自动索敌、冷却、充能、缺目标失败、周期伤害、`AbilityTriggerMode.Periodic` 自动触发 Tick 和可选 Feature handler 调用。
- Projectile / Effect Runtime 生成第一段、Godot 实例化第一段、Projectile 命中生命周期、Projectile 穿透 / 生命周期扩展和 Effect 动画播放第一段已迁入 `SlimeAI.GameOS`：`ProjectileDataKeys / ProjectileTool / ProjectileMovementOptions / GameEventType.Projectile`、`EffectDataKeys / EffectTool / GameEventType.Effect` 和 `GodotProjectileEffectSpawner`，Runtime tests 覆盖 Data 写入、关系绑定、事件发布、Effect 动画名写入、投射物命中伤害和销毁、穿透多目标、MaxLifeTime 销毁；BrotatoLike headless smoke 已覆盖项目侧调用、真实资产实例化、位置同步、Effect `AnimatedSprite2D` 播放、锁定目标命中、HP 扣减、穿透和视觉节点清理。
- Feature Runtime 最小生命周期已迁入 `SlimeAI.GameOS`：`FeatureDataKeys / FeatureDefinition / FeatureModifierEntry / FeatureContext / FeatureEndReason / IFeatureHandler / FeatureHandlerRegistry / FeatureService / GameEventType.Feature`，Runtime tests 覆盖 Modifier 授予回滚和 handler 生命周期。
- BrotatoLike DataOS Spawn catalog 已接入游戏侧 `BrotatoLikeEnemySpawnSystem`，可通过显式 `Tick()` 实例化真实 `GodotEntity2D` 敌人包装节点、加载敌人视觉场景并写入 DataOS 字段；headless smoke 覆盖第 1 波 `chailangren` / `yuren` 生成。
- BrotatoLike Spawn Tick 已接入 RuntimeSchedule 门禁，`BrotatoLikeDataOSBootstrap.BuildSystemScheduleConfig()` 可从 DataOS `system.config/SpawnSystem` 生成 `SystemConfig`，`BrotatoLikeScheduledEnemySpawnSystem` 通过 `RuntimeSchedule.Execute` 验证 Boot / Gameplay / Pause 状态门禁并驱动敌人生成。
- BrotatoLike 已新增 `BrotatoLikeGameRuntime` 主运行时节点，封装 DataOS 初始化、资源注册、Spawn catalog、RuntimeSchedule 注册、Gameplay / Pause 状态切换和 `_Process` 自动 Tick；headless smoke 已通过该节点验证 SpawnSystem 调度链路。
- BrotatoLike `Scenes/Main.tscn` 已挂载 `GameRuntime` 子节点，普通运行路径由 `Main.StartGameRuntime()` 初始化正式运行时；`--gameos-smoke-exit` 路径保持独立 smoke 探针并退出。
- `Data` 变更通知已通过 `EventDataChangeSink` 接入 `RuntimeEntity.Events`。
- 新增 `Tests/SlimeAI.GameOS.Tests` 和 `Tools/run-tests.sh`，Runtime 行为测试覆盖 Event/Data/Entity/Relationship/Schedule/Pool/Timer/Resource。
- `Games/BrotatoLike` 已建立 `Scenes/Main.tscn`、`Src/Game/Main.cs`、`GameBootstrap.RunFrameworkSmokeProbe()` 和 GodotBridge 探针，用于最小框架接入验证，并新增 `Plans/README.md` 作为游戏仓库整体迁移计划。
- 旧 `assets/` 已复制到 `Games/BrotatoLike/assets`；旧 `Data/` 和 `Src/Main/` 已复制到 `Games/BrotatoLike/MigrationInput/` 并排除编译，等待按模块适配。
- Godot 引擎源码权威入口已更新为 `/home/slime/Code/SlimeAI/Engine/godot-4.6.2-stable`。
- `Engine/Tools/build-linux-editor-mono.sh` 已建立，但当前机器缺少 `SCons`，尚未构建出 Godot CLI。

## 未完成 / 风险

- `Src/**/*.md` 历史长文档尚未系统迁移，目前只按模块族清理已确认会误导 AI 的内容。
- Movement、AI、Collision 已完成第一轮深层模块族对齐；后续重点转向其它剩余模块和 `Docs/` 长设计归档。
- 旧 `MainTest` 失败不属于本轮文档收敛，需独立 Debug。
- 已有用户工作区改动集中在 Godot 场景测试 Skill、测试文档、Docs README 和项目索引，继续修改时必须合并而不是覆盖。
- DataOS 已在 `/home/slime/Code/SlimeAI/SlimeAI` 建立 SQLite schema / migration / generator / validator / runtime snapshot loader；authoring seed 已覆盖 TargetingIndicator、ChainAbility、Ability 通用字段、Feature definition / modifier、System config / preset、Spawn config 和 ResourcePaths 第一批，并新增正式 snapshot bootstrap。当前旧仓库仍只作为迁移输入，不再扩展旧手写数据表；snapshot record type、generated handle type 和文档事实源已由 SDD-0021 收口。
- Godot 底层 trace 目前是方案，尚未修改引擎 fork 和 GodotSkill。
- SlimeAI 新工作区和多仓库骨架已创建，Runtime 最小内核已迁移；当前仓库继续作为迁移计划和旧资产输入。
- Godot Node Entity / Component 生命周期已迁入第一版；旧资产已复制；对象池碰撞隔离、Movement、Collision、Damage / ContactDamage / Damage 处理器管线 / HealService / DamageTool 第一批、Ability Runtime 最小切片、Ability 点选目标语义、Ability 自动索敌第一段、Projectile / Effect Runtime 生成第一段、Projectile 命中生命周期、Projectile 穿透 / 生命周期扩展、Effect 动画播放第一段和 Godot 实例化第一段、Feature Runtime 最小生命周期、AI Runtime 最小行为树 + 最近目标查询 + 攻击请求事件 + 巡逻 + 行为树预制块 + Ability 自动索敌上下文准备 + Godot AI bridge、Attack Runtime 最小结算、GodotAttackComponent bridge 第一段、旧 AttackComponent 场景适配、Attack 动画事件桥第一段、DataOS 正式适配最小闭环、M18 DataOS 扩大迁移切片、M19 旧 AbilityData 通用字段补齐切片、M20 BrotatoLike 统一 Godot 场景测试 runner / Observation 日志入口、M21 DataOS 敌人生成规则 catalog、M27 Ability handler-specific DataOS 参数第三段，以及 SineWave / Boomerang / BezierCurve / CircularArc / Orbit / ChainLightning 真实 handler 执行闭环已迁入；Feature actions、真实主场景和其它具体 Ability 逻辑的迁移尚未完成。

## 推荐入口

- AI 索引：`DocsAI/INDEX.md`
- 当前纠偏计划：`/home/slime/Code/SlimeAI/Workspace/DocsAI/SlimeAINewReorientation/00-README.md`
- 当前 OpenSpec：`/home/slime/Code/SlimeAI/openspec/changes/reorient-slimeainew-ecs-framework/`
- 彻底迁移计划（history / migration-pointer）：`Plans/Architecture/框架整体迁移/迁移.md`
- SkilmeAI 多仓库迁移（history / migration-pointer）：`Plans/Architecture/SkilmeAI_多仓库彻底迁移/README.md`
- Godot AI Game OS 执行计划（history / migration-pointer）：`Plans/Architecture/Godot_AI_Game_OS_Migration/README.md`
- 多仓库 AI 工作流（history / migration-pointer）：`DocsAI/Protocols/SkilmeAI多仓库AI工作流协议.md`
- Skill 映射：`DocsAI/Skills/Skill到DocsAI映射.md`
- 测试矩阵：`DocsAI/Tests/测试矩阵.md`

## 验证方式

```bash
git status --short
find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l
find DocsAI -maxdepth 3 -type f | sort
rg -n "/mnt/[e]|file://[/]|复刻土豆兄[弟]|[.]windsurf" DocsAI .codex/skills Docs/框架/项目索引.md Src -g "*.md"
rg -n "DocsAI/Modules|DocsAI/Tests|DocsAI/Workflows" .codex/skills DocsAI
dotnet build
```
