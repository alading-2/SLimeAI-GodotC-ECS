# ChainLightning Handler 对齐设计

## 背景

`ChainLightning` 当前存在一次未完成的迁移：

- 工作区版本把 [`ChainLightning.cs`](/mnt/e/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Data/Data/Ability/Ability/ChainLightning/ChainLightning.cs) 从 `AbilityFeatureHandler` 改成了 `AbilityFeatureHandlerBase`
- 但现有 `AbilitySystem` 与大多数技能处理器仍采用“每个 Handler 在 `ExecuteAbility` 内自行决策目标”的模式
- `ChainLightning` 同时还保留了旧式首目标逻辑残留、链式专属 DataKey 的旧定义，以及未同步的索引文档

这使得该技能处于“回滚后半更新”状态：它既没有完整保留旧实现，也没有完整接上新的统一目标注入链路。

## 目标

将 `ChainLightning` 对齐到当前项目稳定范式：

- 继续使用 `AbilityFeatureHandler`
- 在 `ExecuteAbility` 内自行选择第一跳目标
- 保留链式弹跳、延迟、伤害衰减和特效表现
- 将链式配置与 DataKey 对齐到当前 Data/Config 规范
- 更新项目索引，明确 `ChainLightning` 的职责和接入方式

## 非目标

- 不引入新的统一首目标注入基类
- 不修改 `AbilitySystem` 的整体流水线语义
- 不把链式弹跳重构为 `AbilityImpactTool` 的通用能力
- 不扩展新的链式技能家族（如冰链、毒链）

## 方案

### 1. 保持 Handler 自主选首目标

`ChainLightning` 恢复为 `AbilityFeatureHandler`，并在 `ExecuteAbility` 内：

- 读取 `AbilityCastRange`
- 以施法者为中心查询最近的敌方实体
- 选出第一跳目标后写入 `context.Targets`
- 若找不到目标则直接返回 `TargetsHit = 0`

这样可以与当前 `Slam`、`CircleDamage`、`ArcShot` 等技能保持一致，也避免 `AbilitySystem` 额外承担统一前置选目标职责。

### 2. 保留链式弹跳实现，但清理迁移残留

保留 `ExecuteBounce` 和延迟弹跳的整体结构，因为它已经覆盖：

- 命中去重
- 按范围与排序查询下一跳
- 伤害衰减
- 线段特效播放

需要清理的内容：

- 去掉对 `AbilityFeatureHandlerBase` 的依赖
- 恢复本技能内部的第一跳查询
- 保证 `TeamFilter` 与 `Sorting` 来自技能 Data，而不是硬编码或半迁移状态

### 3. 对齐链式 DataKey 与配置声明

`DataKey_Chain.cs` 目前混用了 `DataMeta` 与 `const string`，其中 `AbilityChainLineEffect` 仍是旧式字符串键。

本次改为：

- 保留 `AbilityChainCount / Range / Delay / DamageDecay` 的 `DataMeta`
- 将 `AbilityChainLineEffect` 也升级为 `static readonly DataMeta`
- 让 [`ChainAbilityConfig.cs`](/mnt/e/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Data/Data/Ability/Ability/ChainLightning/Data/ChainAbilityConfig.cs) 全部走 `[DataKey(nameof(DataKey.*))]` + `DefaultValue` 直读模式

这样能避免该技能继续作为 Data 体系中的特例存在。

### 4. 文档同步

更新 [`Docs/框架/项目索引.md`](/mnt/e/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Docs/框架/项目索引.md)，补充：

- `ChainLightning` 仍采用 Handler 内部首目标选择
- `ChainAbilityConfig` 与 `DataKey_Chain` 已按新 Data 规范对齐
- 本技能是链式延迟弹跳技能的参考实现

## 风险与验证

主要风险：

- 首目标恢复为技能内查询后，若上游曾依赖 `context.Targets` 预填，会出现行为变化
- `AbilityChainLineEffect` 改为 `DataMeta` 后，需要确认 `PackedScene` 的读取链路兼容

验证重点：

- 编译通过
- `ChainLightning` 找不到目标时安全返回
- 有目标时第一跳与后续弹跳正常执行
- 线段特效和伤害衰减仍按配置生效
