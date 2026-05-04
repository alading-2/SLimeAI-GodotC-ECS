# SkilmeAI 多仓库迁移 Progress

> 更新日期：2026-05-04

## 当前状态

M1 / M2 / M3 / M4 已进入可验证状态。M3 Runtime 最小内核已覆盖 Relationship / Schedule；M4 BrotatoLike 最小接入已覆盖新 Runtime smoke probe。

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

## 下一步

- 迁移 GodotBridge：Node Entity、Component 生命周期、Godot `_Process` Timer bridge。
- 从当前仓库迁移 BrotatoLike 资产、游戏场景和游戏特定 Data。
- 分批迁移 Capabilities：Movement / Collision / Damage / Ability / Feature / AI。
- 建立 DataOS SQLite schema、生成器和 runtime snapshot。
