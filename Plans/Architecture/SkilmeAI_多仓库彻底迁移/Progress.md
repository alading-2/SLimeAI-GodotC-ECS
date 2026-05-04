# SkilmeAI 多仓库迁移 Progress

> 更新日期：2026-05-04

## 当前状态

M1 迁移蓝图已进入执行；M2 新工作区骨架已创建。

## 已完成

- 创建 `/home/slime/Code/SkilmeAI/` 顶层工作区。
- 创建框架主仓库骨架：`/home/slime/Code/SkilmeAI/SkilmeAI`。
- 创建第一个游戏仓库骨架：`/home/slime/Code/SkilmeAI/Games/BrotatoLike`。
- 创建 Godot 引擎位置说明：`/home/slime/Code/SkilmeAI/GodotEngine/README.md`。
- 创建工作区说明：`/home/slime/Code/SkilmeAI/Workspace/README.md`。
- 补充资产分流清单：`AssetRouting.md`。
- 补充工作区边界说明：`WorkspaceLayout.md`。
- 创建 `SkilmeAI.GameOS` 最小可构建项目。
- `Tools/run-build.sh` 验证通过。
- `Tools/run-pack.sh` 生成本地 NuGet 包。
- 创建 `Games/BrotatoLike` 最小 Godot C# 项目。
- `BrotatoLike` 已通过本地项目引用接入 `SkilmeAI.GameOS`。
- `Games/BrotatoLike/Tools/run-build.sh` 验证通过。
- Runtime Data 最小内核已迁入 `SkilmeAI.GameOS`。

## 下一步

- 从当前仓库迁移 Runtime Event / Entity 最小内核。
- 从当前仓库迁移 BrotatoLike 最小启动场景、入口代码和资产。
- 将当前仓库的 Runtime 和游戏资产按 `AssetRouting.md` 分批迁移。
