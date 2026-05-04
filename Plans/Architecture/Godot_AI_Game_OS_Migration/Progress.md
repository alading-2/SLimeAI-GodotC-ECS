# Godot AI Game OS Migration Progress

> 更新日期：2026-05-04

## 当前状态

Phase 03 的 GameOS Runtime 最小内核已完成到 Relationship / Schedule，Phase 04 的 BrotatoLike 最小接入已完成。

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
- 框架仓库和游戏仓库 build 均为 0 warning / 0 error。

## 正在做

- 准备下一批：GodotBridge、Capabilities、DataOS 和真实游戏资产迁移。

## 下一步

1. 迁移 GodotBridge：Node Entity、Component 生命周期、Godot `_Process` Timer bridge。
2. 迁移 Capabilities：Movement / Collision / Damage / Ability / Feature / AI。
3. 从旧仓库迁移 BrotatoLike 资产、游戏场景和游戏特定 Data。
4. 建立 DataOS：SQLite schema、生成器、runtime snapshot。
5. Godot 引擎底层修改统一进入 `/home/slime/Code/SkilmeAI/Engine/godot-4.6.2-stable`。

## 当前阻塞

无代码阻塞。当前剩余风险是 Godot Node 运行时桥、Capability、DataOS 和真实游戏资产尚未迁入。

## 最新验证

```bash
cd /home/slime/Code/SkilmeAI/SkilmeAI
Tools/run-build.sh
Tools/run-tests.sh
Tools/run-pack.sh
cd /home/slime/Code/SkilmeAI/Games/BrotatoLike
Tools/run-build.sh
```

结果：

- build：0 warning / 0 error。
- tests：Event/Data/Entity/Relationship/Schedule/Pool/Timer/Resource 全部 PASS。
- pack：生成 `Packages/LocalNuGet/SkilmeAI.GameOS.0.1.0-alpha.0.nupkg`。
- BrotatoLike build：0 warning / 0 error。
