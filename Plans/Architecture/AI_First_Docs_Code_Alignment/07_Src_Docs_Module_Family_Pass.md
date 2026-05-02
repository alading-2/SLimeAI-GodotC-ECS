# 07 Src Docs Module Family Pass

## 目标

继续按模块族处理 `Src/**/*.md`，优先修正 AI 会照抄后直接写错代码的源码旁文档。

## 本轮处理

- 修正 `Src/ECS/Test/SingleTest/ECS/ECSTest/README.md` 的旧 `Src/Test/ECS/ECSTest` 场景路径。
- 修正 `Src/ECS/Test/SingleTest/Tools/ObjectPool/README.md` 的旧 `Src/Test/SingleTest/Tools/ObjectPool` 场景路径。
- 修正 `Src/ECS/Base/Entity/Core/EntityManager.md`：
  - `EntitySpawnConfig.Config` 当前来源写为 `Data/DataNew` POCO 或字典配置。
  - `UnregisterEntity` 说明不再暗示业务层可绕过 `EntityManager` 调 `QueueFree`。
- 修正 `Src/ECS/Base/Component/Ability/CostComponent/README.md`：
  - 配置入口指向 `Data/DataNew/Ability/*AbilityData.cs` 或测试字典。
  - 去掉旧 `AbilityExecutor / IAbilityExecutor` 扩展示例。
  - 新增字段示例改为 `static readonly DataMeta + DataRegistry.Register`。
  - 相关文档入口改为 `DocsAI/Modules/*` 与当前源码路径。
- 修正 `Src/ECS/Base/Data/README.md`：
  - 文档分工加入 `Data/DataNew` 主入口，旧 `Data/Data` 标记为归档。
  - 延迟移除 Buff 示例从 `GetTree().CreateTimer` 改为 `TimerManager.Instance.Delay`。
  - 新增普通 DataKey / 计算 DataKey 示例改为 `DataMeta` 写法。
  - 对象池复用说明改为 Data 清理与 `Entity.Events` 清理由 `EntityManager.Destroy` 分工处理。
- 修正 `Src/ECS/Base/Component/Component规范.md`：
  - 普通业务字段不要新增 `const string`。
  - Spawn 时序按当前代码改为 `Data.LoadFromConfig` 先于 `RegisterComponents`。

## 不做

- 不删除 `Src/**/*.md`。
- 不迁移长设计说明到 `Docs/`，只修本轮已确认会误导 AI 的内容。
- 不处理 `.claude/skills`、`.opencode/skills`、`.windsurf/skills` 删除状态。
- 不修改运行时代码。

## 验证命令

```bash
rg -n "AbilityExecutor|IAbilityExecutor|Src/Test|GetTree\\(\\)\\.CreateTimer|public const string ManaRegen|public const string MaxMana|public const string EffectiveMana|通常来自 \\.tres|Data/Config/System/System/Resource/TestSystem\\.tres" Src/ECS -g "*.md"
dotnet build
```

`AbilityExecutor / IAbilityExecutor` 若只出现在“不要新增/不要恢复/不要通过旧入口”语境中，属于预期保留的禁止性说明。
