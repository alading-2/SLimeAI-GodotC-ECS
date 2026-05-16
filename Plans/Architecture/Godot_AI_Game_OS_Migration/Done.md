# Godot AI Game OS Migration Done

## 2026-05-04

- 冻结当前仓库定位：`brotato-my` 后续作为迁移输入仓库。
- 建立新迁移计划目录：`Plans/Architecture/Godot_AI_Game_OS_Migration/`。
- 建立初版资产盘点：`00_Inventory.md`。
- 建立 `/home/slime/Code/SlimeAI/` 多仓库工作区骨架。
- 创建 `SlimeAI.GameOS` 最小可构建项目。
- 创建 `SlimeAI.slnx`、`Tools/run-build.sh`、`Tools/run-pack.sh`。
- 验证框架仓库 build 和 pack 通过。
- 创建 `BrotatoLike` 最小 Godot C# 项目。
- 建立 `BrotatoLike -> SlimeAI.GameOS` 本地项目引用。
- 验证游戏仓库 build 通过。
- 迁入 Runtime Data 最小内核。
- 更新 `SlimeAI.GameOS.Contracts.md` 与 `ApiIndex.md`。

## 2026-05-06

- Runtime 最小内核已扩大到 Data / Event / Entity / Relationship / Schedule / Resource / Pool / Timer。
- Movement / Collision / Damage / Ability / Feature / AI / Attack / Projectile / Effect 第一批已迁入 `SlimeAI.GameOS`。
- DataOS SQLite schema / migration / snapshot generator / validator / Runtime snapshot loader 已建立。
- BrotatoLike 已接入 DataOS seed / snapshot、Spawn catalog、RuntimeSchedule 驱动 Spawn Tick、`BrotatoLikeGameRuntime` 和 Main 正式运行路径。
- BrotatoLike 统一 Godot 场景测试 runner 已建立：`Tools/run-godot-scene.sh` 和 `Tools/analyze-godot-scene-logs.sh`。
- 框架仓库已批量建立入口型 `.codex/skills`，覆盖 Runtime、DataOS、Capability、GodotBridge、Validation 和 Research，不再逐个迁 Skill 后停顿。
- 游戏仓库 `.codex/skills` 已更新为当前 DataOS / GameOS / Godot scene runner 状态。
- 旧 `GodotSkill` bundled scripts 和 `ability-system` 参数参考已迁入框架仓库 `.codex/skills`。
- 框架 active skills 已同步到 `Agent/SkillsSource`，新仓库具备可触发入口和可维护源头。
- `target_point_skill` 已接入 `技能.主动.位置目标` 范围伤害 handler，从 DataOS 读取点选目标、范围、伤害和 Effect 参数，主 smoke 覆盖扣血和 Effect 事件。
- `aura_shield` 已接入 `技能.被动.光环护盾` 投射物 handler，从 DataOS 读取 Projectile 和 AttachToHost Movement 参数，主 smoke 覆盖跟随投射物生成。

## 2026-05-06（本轮）

- 框架仓库 10 个 Capability（Movement / Collision / Damage / Effect / Unit / Attack / Projectile / Feature / Ability / AI）已建立标准模板体系：`capability.json` + `Contract.md` + `Debug.md` + `Tests/` 目录。
- 游戏仓库 `MigrationInput/` 已清理：所有旧 C# 源码（DataNew / DataKey / EventType / ResourceManagement / Config）、.tres Resource 和 .tscn 场景文件已删除；保留根 `README.md` 作为迁移审计痕迹。
- 框架仓库和游戏仓库 build 验证通过：0 warning / 0 error。

## 2026-05-06（追加）

- 框架 `MovementDataKeys.Acceleration` 新增，支持 Lerp 平滑移动（backward-compatible）。
- 框架 `GodotPlayerInputComponent` 建立，实现 Godot Input Map 到 `MovementDataKeys.InputDirection` 的桥接。
- BrotatoLike `project.godot` 输入映射已定义（WASD + 方向键 + 手柄左摇杆）。
- BrotatoLike DataOS `unit.player/deluyi` 已追加 `Movement.Acceleration = 12`。
- BrotatoLike `BrotatoLikeGameRuntime.SpawnPlayer()` 已建立，从 DataOS 生成玩家并挂载输入组件和 MovementDriver。
- BrotatoLike `Main.StartGameRuntime()` 已自动创建玩家。
- BrotatoLike smoke 新增 `GodotPlayerInputProbe`，覆盖组件注册、InputDirection 写入、Acceleration 平滑加速和直接速度回退。
- BrotatoLike smoke 新增 `GodotActiveSkillInputProbe`，覆盖技能切换（Previous/Next 改变 CurrentAbilityIndex）和技能释放（ability:activated 事件触发）。
- 框架和游戏仓库 build + smoke 验证通过：0 warning / 0 error。

## 2026-05-06（Phase 10：清理旧结构）

- 旧仓库 `brotato-my` 建立 `ARCHIVE.md` 归档清单，详细记录所有已迁移内容的去向和验证状态。
- 旧仓库 `Src/ECS/`（289 个 C# 源文件，4.2MB）已删除，全部内容已迁移到 `SlimeAI/GameOS/Runtime` 和 `Capabilities`。
- 旧仓库 `Data/`（227 个数据文件，1.4MB）已删除，全部内容已迁移到 `SlimeAI/DataOS` 和 `Games/BrotatoLike/DataOS/Authoring/`。
- 旧仓库保留内容：Plans/、Docs/、DocsAI/、assets/、.claude/、.codex/、AGENTS.md、CLAUDE.md 和 ARCHIVE.md 作为迁移审计痕迹。
- 游戏仓库 `Games/BrotatoLike/MigrationInput/` 已在此前清理，仅剩 README.md 作为审计痕迹。
