---
name: test-system
description: 开发或扩展运行时测试系统时使用。适用于：新增测试模块、接入实体选择、实现属性测试/技能测试、通过 FeatureDebugService 调试能力生命周期、在测试场景中主动指定选中实体。触发关键词：TestSystem、FeatureDebugService、AttributeTestModule、AbilityTestModule、运行时测试、测试模块、临时加成、调试面板。
---

# TestSystem 使用规范

## 系统定位

`TestSystem` 是项目里的运行时调试宿主。

它负责：

- 调试 UI 入口
- 选中实体
- 模块注册与切换
- 刷新当前测试模块

它不负责：

- 正式玩家 UI
- 技能主动执行前台
- 复制一套独立 Feature / Ability 生命周期

涉及真正的能力生命周期时，必须继续复用：

- `EntityManager`
- `FeatureSystem`
- `AbilitySystem`

## 什么时候该用本 Skill

以下场景应优先参考本 Skill：

- 新增一个运行时测试模块
- 修改 `TestSystem` 面板结构、实体选择、模块切换逻辑
- 给属性测试模块添加新的调试控件
- 给技能测试模块增加新的管理动作
- 希望在 TestSystem 中调试 Feature / Ability，但又不能绕开正式运行时链路
- 需要在测试场景启动后自动选中某个实体

如果你要处理的是：

- 通用 Feature 生命周期设计 → 看 `@feature-system`
- 技能执行链路 → 看 `@ability-system`
- Data 运行时读写规则 → 看 `@ecs-data`
- Data 目录配置与映射 → 看 `@data-authoring`

## 当前结构

### 宿主层

- `Src/ECS/Base/System/TestSystem/TestSystem.cs`
- `Src/ECS/Base/System/TestSystem/TestSystem.tscn`
- `Src/ECS/Base/System/TestSystem/TestModuleBase.cs`
- `Src/ECS/Base/System/TestSystem/Core/ITestModule.cs`
- `Src/ECS/Base/System/TestSystem/Core/ITestModuleContext.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestModuleContext.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestModuleDefinition.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestModuleSceneDefinition.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestModuleSceneRegistry.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestModulePath.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestModuleGroupId.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestSelectionContext.cs`
- `Data/EventType/Global/GameEventType_Global_TestSystem.cs`

### 模块层

- `Src/ECS/Base/System/TestSystem/Attribute/AttributeTestModule.cs`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeTestModule.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeEditorRow.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeCheckEditor.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeOptionEditor.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeNumericEditor.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeTextEditor.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeModifierEditor.tscn`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityTestModule.cs`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityTestModule.tscn`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityGroupSection.tscn`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityCatalogItem.tscn`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityOwnedItem.tscn`
- `Src/ECS/Base/System/TestSystem/System/SystemInfoTestModule.cs`
- `Src/ECS/Base/System/TestSystem/System/SystemInfoTestModule.tscn`
- `Src/ECS/Base/System/TestSystem/Info/ObjectPoolInfoModule.cs`
- `Src/ECS/Base/System/TestSystem/Info/ObjectPoolInfoModule.tscn`
- `Src/ECS/Base/System/TestSystem/ResourceCatalog/ResourcePickerControl.cs`
- `Src/ECS/Base/System/TestSystem/ResourceCatalog/ResourcePickerControl.tscn`
- `Src/ECS/Base/System/TestSystem/ResourceCatalog/ResourceCatalogTestModule.cs`
- `Src/ECS/Base/System/TestSystem/ResourceCatalog/ResourceCatalogTestModule.tscn`
- `Src/ECS/Base/System/TestSystem/Spawn/SpawnTestModule.cs`
- `Src/ECS/Base/System/TestSystem/Spawn/SpawnTestModule.tscn`

### 服务 / 适配层

- `Src/ECS/Base/System/TestSystem/FeatureDebugService.cs`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityTestService.cs`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityTestViewModels.cs`
- `Src/ECS/Base/System/TestSystem/System/SystemInfoService.cs`
- `Src/ECS/Base/System/TestSystem/Info/ObjectPoolInfoService.cs`
- `Data/ResourceManagement/ResourceCatalog.cs`

### 文档

- `Src/ECS/Base/System/TestSystem/README.md`
- `Docs/框架/ECS/System/TestSystem/TestSystem.md`

### 已迁出 TestSystem 的独立测试

- `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.cs`
- `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn`
- `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewWorldController.cs`
- `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewCatalogService.cs`
- `Src/ECS/Base/Entity/Preview/VisualPreviewEntity.cs`

视觉预览场景的当前交互约定：

- 刷新后保留当前分类与动作选择，再把统一动作重新应用到重建后的预览实体
- 鼠标移到动作下拉框上滚轮切换动作，便于快速浏览同分类动画
- 鼠标选中实体详情时，名称与资源信息之间保留空行，避免信息挤在一起
- 预览场景直接扫描并控制 `VisualRoot` 下的唯一 `AnimatedSprite2D`，不依赖 `UnitAnimationComponent`
- 若资源中存在多个 `AnimatedSprite2D`，记录错误日志并只控制第一个

## 核心原则

### 1. TestSystem 只做宿主

`TestSystem` 应只负责：

- 绑定调试面板场景骨架
- 读取模块清单并在切换时动态实例化当前模块
- 维护 `SelectedEntity`
- 通过 `TestSystem.Events` 广播 TestSystem 局部事件
- 处理模块切换与刷新

不要把具体业务测试逻辑继续堆进 `TestSystem.cs`。

`TestSelectionContext` 只保存选中实体状态；TestSystem 专属事件统一放在 `TestSystem.Events`，不要新增 C# `event` 给宿主和模块做联动。

### 1.2 模块失败隔离

`TestSystem` 动态实例化模块时，单个模块初始化失败不能把整个宿主一起拖垮。

要求：

- 宿主层对 `module.Initialize(...)` 失败做 `try/catch`
- 日志里记录模块 `Id / ModulePath / error`
- 失败模块跳过注册，其他模块继续初始化

否则一个新模块节点路径写错，就会让整个测试面板都进不来，排查成本过高。

### 1.1 模块路径分组

测试模块通过 `TestModuleDefinition.ModulePath` 写完整点分路径：

- `信息.对象池`
- `属性.属性测试`
- `资源.资源目录`
- `系统.系统监控`
- `技能.技能测试`
- `技能.触发.冷却测试`

路径最后一段是模块名，前面的段是多级分组。宿主按 `ModulePath` 自动生成左侧 `Tree` 导航。

模块注册顺序使用 `TestModuleSceneRegistry` 中的清单顺序，不再使用 `SortOrder`，也不做字母排序。

### 2. UI 不直接承担系统生命周期

如果模块里出现以下需求：

- 添加 / 移除 Feature
- 启用 / 禁用 Feature
- 运行时构造临时 `FeatureDefinition`

不要把系统调用散落在 UI 控件回调里。

推荐做法：

- UI 模块收集输入
- Service / Adapter 封装业务
- 正式系统 API 执行生命周期
- 条目骨架放到独立 `tscn`，模块只做实例化与数据绑定

当前对应适配层就是：

- `FeatureDebugService`

对象池信息、资源目录、系统监控这类只读 / 资源型 / 管理型模块也应该先经过服务层整理数据，再交给 UI 模块渲染，不要把目录解析、容量拼装、SystemManager 操作等细节堆进 `TestSystem.cs`。

视觉预览不再是 `TestSystem` 模块。需要预览 `Asset*` 视觉资源时，单独运行 `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn`，该场景自己生成 `VisualPreviewEntity` 并消费 `MouseSelectionSystem` 的全局选择结果。

补充约束：

- 对象池信息模块的详情字段要用中文展示，避免把调试面板做成英文统计表
- 对象池信息模块在前台激活时应支持模块内定频刷新；当前约定为每秒自动刷新一次，并在刷新后保留当前对象池选择
- 系统监控模块必须通过 `SystemInfoService` 汇总 `SystemConfigService / SystemRegistry / SystemManager`；UI 按钮只调用服务层，不直接散落 `SystemManager.TryAddSystem / TryRemoveSystem / TrySetSystemEnabled`
- 系统监控模块中 `Required == true` 的系统禁止禁用和移除；被其他已加载系统依赖的系统禁止移除
- TestSystem 宿主应提供基本的面板尺寸调节能力；新增模块把内容挤压到不可读时，优先先扩展宿主尺寸，而不是继续压缩文案

### 3. 属性测试走双轨

当前属性测试不是单一路径，而是：

- **状态覆写**：直接 `entity.Data.Set(...)`
- **持续加成**：通过 `FeatureDebugService.ApplyTemporaryModifier(...)` 生成运行时 Feature

只有在以下条件下才应展示“临时加成”：

- `meta.IsNumeric == true`
- `meta.SupportModifiers == true`
- `meta.IsComputed == false`

### 4. 技能测试只做管理，不做执行

技能测试模块当前边界是：

- 添加技能
- 移除技能
- 启用 / 禁用技能

不要在 TestSystem 中新增：

- 主动触发技能按钮
- 技能执行验证器
- 测试版 AbilityExecutor / Registry

## 属性测试模块接入要点

### 分类来源

使用 `DataRegistry.GetCachedMetaByCategory(...)` 收集元数据。

### 编辑项筛选

只保留：

- `bool`
- 数值
- 枚举
- `string`
- `HasOptions`

排除：

- `IsComputed == true`
- 不适合在调试面板直接编辑的复杂类型

### 临时加成

当 `DataMeta` 满足：

- `IsNumeric == true`
- `SupportModifiers == true`
- `IsComputed == false`

应通过：

- `GetTemporaryModifierValue(...)`
- `ApplyTemporaryModifier(...)`
- `ClearTemporaryModifier(...)`

来复用正式 Modifier 链路。

### UI 场景化约定

属性测试条目不要继续在代码中 `new Label / SpinBox / Button / LineEdit` 拼布局。

推荐拆分为：

- 词条容器场景
- 布尔编辑器场景
- 下拉编辑器场景
- 数值编辑器场景
- 文本编辑器场景
- 临时加成编辑器场景

## 技能测试模块接入要点

### 数据来源

候选技能来自：

- `AbilityData.All`

### 分组规则

优先使用：

- `AbilityData.FeatureGroupId`

显示规则：

- UI 分类标题和 Tooltip 显示完整 `FeatureGroupId`
- 不要把 `技能.被动` / `技能.位移` 这类分组裁剪成最后一段
- 视图模型字段应显式命名为 `FeatureGroupId`，避免回退到旧的 `GroupPath` 语义

兜底规则：

- `AbilityType` 推导出的默认分组

### 事件刷新约定

监听：

- `GameEventType.Ability.Added`
- `GameEventType.Ability.Removed`
- `GameEventType.Feature.Enabled`
- `GameEventType.Feature.Disabled`

刷新由模块自己直接执行，不再接入宿主级 `TestRefreshScheduler`。模块仍应区分结构变化与局部变化：

- 技能增删：允许重建分组结构
- 启停变化：优先 patch 对应条目
- 面板隐藏或模块失活：不要继续响应实体事件

## 资源目录 / 选择器接入要点

### 数据来源

通用资源选择器必须通过：

- `ResourceCatalog.GetEntries(...)`
- `ResourcePaths.Resources`
- `ResourceManagement.Load<T>(...)`

不要在运行时测试面板里全盘扫描 `res://` 作为主数据源。`ResourceCatalog` 的业务数据条目来自 DataNew，场景/特效等资产条目来自 `ResourcePaths`。

### 目录分类来源

`ResourceCatalog` 不维护固定 enum 分类，目录分类由路径自动推导：

- `EnemyData.All` => `Unit.Enemy`
- `PlayerData.All` => `Unit.Player`
- `AbilityData.All` => `Ability.*`
- `assets/Effect/Explosion/x.tscn` => `Effect.Explosion`

路径中的 `Resource` 目录只表示资源存放位置，不参与分类名。

具体测试模块必须只消费自己允许的目录前缀，不要把通用选择器的全部资源直接暴露给业务操作。

### 资源目录测试

`ResourceCatalogTestModule` 用于验证通用目录服务当前能拿到哪些分类和资源：

- 使用 `ResourceCatalog.GetGroups()` 展示完整分类树
- 顶部显示分类总数与资源总数
- 不一次性展开全部资源
- 选择分类后自动展示当前分类下的资源
- 选中资源时展示 `ResourceKey / CatalogPath / Category / ResourceType / Path`

该模块只验证资源目录索引，不应加载并实例化资源，也不应扫描 `res://`。

## 系统监控模块接入要点

### 数据来源

`SystemInfoTestModule` 只通过 `SystemInfoService` 获取展示快照。

服务层允许消费：

- `SystemConfigService.GetAllConfigs()`
- `SystemRegistry.GetDescriptor(...)`
- `SystemManager.GetAllSystems()`
- `SystemManager.GetSystemRuntimeInfo(...)`

不要在模块 UI 中直接扫描节点树或 `res://` 来推导系统清单。

### 操作边界

运行时系统管理只允许走：

- `SystemManager.TryAddSystem(...)`
- `SystemManager.TryRemoveSystem(...)`
- `SystemManager.TrySetSystemEnabled(...)`

约束：

- `Required == true` 的系统禁止禁用和移除
- 已加载系统依赖的目标系统禁止移除
- 未注册或缺少配置的系统不能添加
- 操作失败必须给中文原因，并展示到模块状态栏

## 敌人生成测试模块接入要点

### 边界

`SpawnTestModule` 只做手动批量生成敌人：

- 按敌人 `Name` 获取 `EnemyData`
- 调用 `SpawnSystem.Instance.SpawnBatch(...)`
- 调用 `SpawnSystem.Instance.KillAllEnemies()`
- 展示 `ObjectPoolNames.EnemyPool` 统计

不要在该模块中：

- 生成玩家、目标指示器、技能或特效
- 复制 `SpawnSystem` 波次计时逻辑
- 直接 `new EnemyEntity()` 或直接 `QueueFree()`
- 绕开 `EnemyPool`

## FeatureDebugService 使用要点

当前服务负责：

- `GrantAbility(...)`
- `GrantFeature(...)`
- `RemoveAbility(...)`
- `SetFeatureEnabled(...)`
- `GetTemporaryModifierValue(...)`
- `ApplyTemporaryModifier(...)`
- `ClearTemporaryModifier(...)`

临时 Feature 命名统一使用：

- `TestSystem.Modifier.{dataKey}`

底层必须继续复用：

- `EntityManager.AddAbility(owner, AbilityData)`
- `EntityManager.AddAbility(owner, FeatureDefinition)`
- `EntityManager.RemoveAbility(owner, ability)`
- `FeatureSystem.EnableFeature(feature, owner)`
- `FeatureSystem.DisableFeature(feature, owner)`

## 新增模块的标准步骤

### 第一步

新建：

- `public partial class MyTestModule : TestModuleBase`
- `MyTestModule.tscn`

### 第二步

实现：

- `Definition`，必须包含稳定 `Id` 和完整 `ModulePath`
- `Initialize(...)`
- `Refresh()`
- 必要时实现 `OnSelectedEntityChanged / OnActivated / OnDeactivated`

并把固定 UI 骨架放进 `MyTestModule.tscn`。

### 第三步

在 `TestModuleSceneRegistry` 中登记 `MyTestModule.tscn` 对应的资源键、稳定 `Id` 与 `ModulePath`，由 `TestSystem` 在切换时动态实例化模块。清单顺序就是默认模块顺序。

### 第四步

如果模块要调用正式系统能力：

- 优先新增 Service / Adapter
- 不要把系统调用直接散落进按钮回调

## DataKey 访问规范

在 TestSystem 内部访问 `Data.Get/Set` 时，**必须使用 `DataKey.XXX.Key` 显式取键名**：

```csharp
// ✅ 正确
ability.Data.Get<string>(DataKey.Name.Key);
entity.Data.Set(DataKey.CurrentHp.Key, value);

// ❌ 错误：依赖 DataMeta 的 implicit operator string
ability.Data.Get<string>(DataKey.Name);
```

原因：规避不同工程上下文下 `DataMeta` 到 `string` 的编译兼容差异。早期代码曾使用反射 `ResolveDataKey` 方法绕过此问题，现已统一改为直接 `.Key` 访问，移除了 `using System.Reflection` 和相关反射方法。

## 日志级别规范

TestSystem UI 控件统一使用以下日志级别：

| 级别    | 用途                                 |
| ------- | ------------------------------------ |
| `Info`  | 用户操作确认（点击添加/移除/切换等） |
| `Warn`  | 节点回退查找、操作前置条件不满足     |
| `Error` | 场景实例化失败、分组渲染异常         |

**不要使用 `LogLevel.Debug`**。运行时测试系统的日志面向开发者调试，Debug 级别在测试面板中属于冗余输出。

## 禁止事项

- ❌ 不要把 TestSystem 做成正式玩家 UI
- ❌ 不要在 TestSystem 内新增技能主动触发前台
- ❌ 不要绕开 `FeatureSystem` / `EntityManager` 复制能力生命周期
- ❌ 不要让 TestSystem 模块实现 `IFeatureHandler` 或伪造 FeatureEntity 来接入 UI 生命周期
- ❌ 不要恢复宿主级 `TestRefreshScheduler`；模块刷新应保持简单直接
- ❌ 不要直接编辑计算属性
- ❌ 不要让后台模块持续保留事件订阅
- ❌ 不要把复杂业务逻辑继续堆进 `TestSystem.cs`
- ❌ 不要在 `Data.Get/Set` 中直接传 `DataKey` 对象，必须用 `DataKey.XXX.Key`
- ❌ 不要使用 `LogLevel.Debug` 或 `_log.Debug`，仅用 `Info / Warn / Error`
