# 09 Component AI UI Docs Code Alignment

## 目标

继续按模块族校准源码旁文档，处理 Component / Attack / Collision / UI 中会误导 AI 的运行时语义。

## 本轮处理

- 修正 `Src/ECS/Base/Component/Component规范.md`：
  - 配置数据来源从旧 `Spawn Config (.tres)` 改为 `EntitySpawnConfig.Config`，通常来自 `Data/DataNew` POCO 或测试字典。
- 修正 `Src/ECS/Base/Component/Unit/Common/AttackComponent/AttackComponent.md`：
  - 攻击间隔语义按当前代码改为组件内部用 `AttackInterval - WindUp - Recovery` 追加冷却。
  - 即时模式说明改为 `WindUpTime=0` 仍走 Timer，频率仍由本组件锁住。
  - 目标无效说明去掉旧 `QueueFree` 表述，改为销毁或回收。
- 修正 `Src/ECS/Base/Component/Collision/PickupComponent/PickupComponent.md`：
  - 标明 `PickupComponent.cs` 当前整文件注释归档，不是可直接挂载的运行时组件。
  - 重新启用前必须恢复/重写实现，并按当前 Entity.Events / DataMeta / EntityManager.Destroy / ObjectPool 规则校准。
- 更新 `DocsAI/Modules/Component.md`：
  - 增加 PickupComponent 当前不可直接使用的契约说明。
- 修正 `Src/ECS/UI/README.md`：
  - UI 对象池资源加载示例从旧 `LoadScene<T>()` 改为 `ResourceManagement.Load<PackedScene>(..., ResourceCategory.UI)`。
  - `UIManager` 路径改为 `Src/ECS/UI/Core/UIManager.cs`。
  - `UIManager` 说明从旧 AutoLoad 单例改为 `SystemRegistry` 注册。
  - 自动绑定说明改为监听 `EntitySpawned / EntityDestroyed`。
- 更新 `DocsAI/Modules/UI.md`：
  - 增加 UI 场景加载和 UIManager 注册方式契约。
- 修正 `Src/ECS/Base/Event/README_EventBus.md`：
  - 事件定义路径改为当前 `Data/EventType/`。
  - 事件名格式说明改为 `domain:snake_case`。

## 不做

- 不恢复 `PickupComponent.cs` 运行时代码。
- 不迁移 Movement 长设计文档，只处理本轮确认不一致的入口和示例。
- 不改运行时代码。

## 验证命令

```bash
rg -n "LoadScene|Src/UI/Core|Src/ECS/Base/Event/Type|小写下划线|AutoLoad 单例|Unit\\.Created|Spawn Config \\(\\.tres\\)|外部系统（如 AI 的 CD 控制|所有频率控制 100%|WindowUp|被回收/QueueFree|PickupComponent\\` 添加|AbilityTargetTeamFilter|GetTree\\(\\)\\.CreateTimer|new Timer\\(|GD\\.Load|ResourceLoader\\.Load|GetNodesInGroup|Data\\.On\\(|public const string" Src -g "*.md"
dotnet build
```

`GetNodesInGroup` 若只出现在禁止性说明中、`public const string` 若出现在事件名或特殊引用键示例中，属于预期保留。
