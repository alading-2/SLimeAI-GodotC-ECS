# SkilmeAI 多仓库迁移 Progress

> 更新日期：2026-05-06

## 当前状态

M1 / M2 / M3 / M4 已进入可验证状态。M5 GodotBridge 第一版已迁入框架并被 BrotatoLike 编译接入，M5.1 Node 对象池 / 碰撞隔离扩展第一段已完成。M3 Runtime 最小内核已覆盖 Relationship / Schedule；M4 BrotatoLike 最小接入已覆盖新 Runtime smoke probe。当前 GameOS Capability 已推进到 M16.5，BrotatoLike 游戏仓库已推进到 M27.2：DataOS Spawn catalog 可通过挂载在 `Scenes/Main.tscn/GameRuntime` 的 `BrotatoLikeGameRuntime` 实例化真实 Godot 敌人包装节点，Ability handler-specific DataOS 参数已覆盖当前 GameOS 已有 Ability / Projectile / Effect DataKey，并已通过普通场景短时运行和 headless smoke。

## 已完成

- 创建 `/home/slime/Code/SkilmeAI/` 顶层工作区。
- 创建框架主仓库骨架：`/home/slime/Code/SkilmeAI/SkilmeAI`。
- 创建第一个游戏仓库骨架：`/home/slime/Code/SkilmeAI/Games/BrotatoLike`。
- 创建 Godot 引擎位置说明：`/home/slime/Code/SkilmeAI/Engine/README.md`。
- 创建工作区说明：`/home/slime/Code/SkilmeAI/Workspace/README.md`。
- 补充资产分流清单：`AssetRouting.md`。
- 补充工作区边界说明：`WorkspaceLayout.md`。
- 创建 `SkilmeAI.GameOS` 最小可构建项目。
- `Tools/run-build.sh` 验证通过。
- `Tools/run-pack.sh` 生成本地 NuGet 包。
- 创建 `Games/BrotatoLike` 最小 Godot C# 项目。
- `BrotatoLike` 已通过本地项目引用接入 `SkilmeAI.GameOS`。
- `Games/BrotatoLike/Tools/run-build.sh` 验证通过。
- Runtime Data / Event / Entity / Relationship / Schedule / Resource / Pool / Timer 最小内核已迁入 `SkilmeAI.GameOS`。
- `Tests/SkilmeAI.GameOS.Tests` 已覆盖 Event / Data / Entity / Relationship / Schedule / Pool / Timer / Resource。
- `Games/BrotatoLike/Plans/README.md` 已建立游戏仓库整体迁移计划。
- GodotBridge 第一版已迁入：Node Entity、Component 生命周期、Godot `_Process` Timer bridge。
- `Games/BrotatoLike` 已接入 GodotBridge 最小探针。
- GodotBridge 扩展第一段已迁入：Node 对象池、延迟激活、回池泊车、`CollisionObject2D` 脱树、碰撞隔离基础工具。
- `Games/BrotatoLike` 已接入 `GodotNodePool<Area2D>` 编译探针。
- `Games/BrotatoLike/Tools/run-godot-smoke.sh` 已建立并通过，使用现成 Godot 4.6.2 mono CLI 输出 `BrotatoLike GameOS smoke PASS`。
- Movement / Collision / Damage / ContactDamage / Ability / Projectile / Effect / Feature / AI / Attack Runtime 多批已迁入框架；`GodotAttackComponent`、`GodotUnitAnimationComponent`、`GodotProjectileEffectSpawner`、Ability 点选目标、Ability 自动索敌、Projectile / Effect Runtime 生成与 Godot 实例化、Effect 动画播放、Projectile 命中生命周期和 Projectile 穿透 / 生命周期扩展已接入 BrotatoLike smoke，覆盖导出攻击参数、节点目标解析、HP 扣减、attack 动画播放和取消回 idle、Point 目标触发、自动索敌命中、投射物 / 特效 Data 写入、关系绑定、真实资产节点实例化、Effect `AnimatedSprite2D` 播放和投射物命中扣血 / 销毁 / 穿透 / MaxLifeTime 销毁。
- BrotatoLike DataOS seed / runtime snapshot 已覆盖 Spawn config 与敌人生成规则 catalog；`BrotatoLikeGameRuntime` 已封装 DataOS 初始化、资源注册、RuntimeSchedule 注册和 Spawn Tick，并已挂到 `Scenes/Main.tscn/GameRuntime`，实例化 `GodotEntity2D` 敌人包装节点、加载敌人视觉场景并写入 DataOS 字段。
- 旧 `assets/` 已复制到 `Games/BrotatoLike/assets`，保留 `res://assets/...` 路径。
- 旧 `Data/` 和 `Src/Main/` 已复制到 `Games/BrotatoLike/MigrationInput/`，当前排除编译。
- `Engine/Tools/build-linux-editor-mono.sh` 已建立；当前机器缺少 `SCons`，尚未打出自定义 Godot CLI。现阶段 headless 验证使用 `/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64`。

## 下一步

- 从 `MigrationInput/` 适配 BrotatoLike 游戏特定 Data、旧主场景入口和后续正式场景。
- 迁入 `MigrationInput/Src/Main` 中属于正式游戏入口的逻辑，继续整理 Main 的测试入口与正式入口边界。
- 继续扩大 Feature actions / 更细 Movement handler DataOS 参数。
