# TestSystem 源码目录说明

## 1. 目录定位

`Src/ECS/Base/System/TestSystem/` 用来承载项目的运行时测试系统源码，属性测试模块位于 `Attribute/` 子目录，技能测试模块位于 `Ability/` 子目录，系统监控模块位于 `System/` 子目录，对象池信息模块位于 `Info/` 子目录，资源目录模块位于 `ResourceCatalog/` 子目录，敌人生成测试模块位于 `Spawn/` 子目录。视觉预览已迁出 TestSystem，独立入口位于 `Src/ECS/Test/GlobalTest/VisualPreview/`。

这套系统面向开发调试阶段，目标是：

- 提供统一的运行时调试入口
- 通过通用鼠标选择系统选择实体
- 支持属性调试与技能管理
- 支持后续扩展更多测试模块

如果你要理解概念与设计边界，请先看：

- `Docs/框架/ECS/System/TestSystem/TestSystem.md`

如果你要按规范扩展此目录，请看：

- `.codex/skills/test-system/SKILL.md`

## 2. 当前文件职责

| 文件 | 职责 |
|------|------|
| `../MouseSelection/README.md` | 通用鼠标选择系统说明，负责汇总当前 `MouseSelectionSystem` 主文件 + `Interaction / Picking / SelectionBoxUi` 3 个 partial 的阅读入口、职责边界与事件协议 |
| `TestSystem.cs` | 调试系统宿主，负责 SystemRegistry 注册、场景骨架绑定、读取模块清单、动态实例化当前模块与切换；单个模块初始化失败时记录错误并跳过，不中断整个宿主；支持放大 / 缩小测试面板 |
| `TestSystem.MouseSelection.cs` | 鼠标选择适配，负责在“选择实体”开关开启时消费全局选择完成事件 |
| `Core/ITestModule.cs` | 模块协议，定义宿主依赖的最小模块接口 |
| `Core/ITestModuleContext.cs` | 模块上下文协议，统一注入宿主和选择上下文 |
| `Core/TestModuleDefinition.cs` | 模块定义，统一描述模块稳定 Id 与点分 `ModulePath` |
| `Core/TestModuleSceneDefinition.cs` | 模块场景定义，统一描述导航元数据和对应的模块场景资源键 |
| `Core/TestModuleSceneRegistry.cs` | 模块场景清单，集中维护模块顺序、路径和对应场景 |
| `Core/TestModulePath.cs` | 模块路径工具，负责标准化点分路径和解析模块显示名 |
| `Core/TestModuleGroupId.cs` | 模块分组常量，统一属性、技能等测试模块分组根路径 |
| `Core/TestSelectionContext.cs` | 统一选中上下文，只收口当前选中实体状态 |
| `Core/TestModuleContext.cs` | 模块共享上下文，向模块注入宿主与选中实体上下文 |
| `../../../Data/EventType/Global/GameEventType_Global_TestSystem.cs` | TestSystem 事件协议定义，当前承载 `GameEventType.TestSystem.SelectionChanged` |
| `TestSystem.tscn` | 测试系统主面板骨架，承载顶部工具栏、信息栏、可隐藏模块树与动态模块挂载槽位 |
| `TestModuleBase.cs` | 所有测试模块的统一基类 |
| `FeatureDebugService.cs` | 调试适配层，负责把调试操作转发到正式 Feature / Ability 生命周期 |
| `Attribute/AttributeTestModule.cs` | 属性测试模块，负责 Data 编辑与临时加成 UI 绑定 |
| `Attribute/AttributeTestModule.tscn` | 属性测试固定布局骨架，提供分类列表与右侧滚动编辑区 |
| `Attribute/AttributeEditorRow.tscn` 等 | 属性词条复用场景，负责词条骨架与具体编辑器结构 |
| `Ability/AbilityTestService.cs` | 技能测试服务，负责目录缓存、按完整 `FeatureGroupId` 分组、视图模型与业务操作；内部通过生成的 typed handle 显式访问 Data 键名 |
| `Ability/AbilityTestViewModels.cs` | 技能测试共享视图模型，分组字段显式命名为 `FeatureGroupId` |
| `Ability/AbilityTestModule.cs` | 技能测试 UI，负责左右双列列表、卡片交互与事件刷新 |
| `Ability/AbilityTestModule.tscn` | 技能测试固定布局骨架，提供左右滚动区与卡片宿主节点 |
| `Ability/AbilityGroupSection.tscn / AbilityCatalogItem.tscn / AbilityOwnedItem.tscn` | 技能条目复用场景，负责分组区块与卡片结构 |
| `ResourceCatalog/ResourcePickerControl.cs` | 通用资源选择控件，基于 `ResourceCatalog` 提供分组、搜索和条目选择 |
| `ResourceCatalog/ResourcePickerControl.tscn` | 资源选择控件场景骨架 |
| `ResourceCatalog/ResourceCatalogTestModule.cs` | 资源目录测试模块，通过分类下拉框展示 `ResourceCatalog.GetGroups()` 返回的分类，选择分类后自动显示该分类资源 |
| `ResourceCatalog/ResourceCatalogTestModule.tscn` | 资源目录测试模块固定布局骨架，提供分类选择、分类资源列表和详情展示 |
| `System/SystemInfoService.cs` | 系统监控服务，合并 snapshot 系统配置、`SystemRegistry` 与 `SystemManager` 运行态，封装 Add / Remove / Enable / Disable 调试操作 |
| `System/SystemInfoTestModule.cs` | 系统监控模块，负责系统列表筛选、运行状态详情展示和按钮操作转发 |
| `System/SystemInfoTestModule.tscn` | 系统监控模块固定布局骨架，提供分组 / 标签 / 状态 / 搜索筛选、系统列表、详情和操作按钮 |
| `Info/ObjectPoolInfoService.cs` | 对象池信息服务，负责把 `ObjectPoolManager` 统计与容量元数据合并成测试面板快照 |
| `Info/ObjectPoolInfoModule.cs` | 对象池信息模块，负责用中文展示对象池名称列表、当前池概览和紧凑详情 |
| `Info/ObjectPoolInfoModule.tscn` | 对象池信息模块固定布局骨架，提供左侧名称列表和右侧概览 / 详情区 |
| `Spawn/SpawnTestModule.cs` | 敌人生成测试模块，选择 snapshot 敌人记录后通过正式 `SpawnSystem.SpawnBatch(...)` 生成敌人 |
| `Spawn/SpawnTestModule.tscn` | 敌人生成测试模块固定布局骨架 |

## 2.1 第一次看这个系统，推荐按这个顺序读

如果你是第一次接手 `TestSystem`，不要一上来就从某个模块细节往里钻，建议按“**宿主 -> 协议 -> 模块 -> 服务 -> 正式链路**”这个顺序看。

### 第一步：先看 `TestSystem.cs`

先看这个文件，因为它回答的是“**系统怎么启动、主界面怎么绑定、模块怎么接进来**”。

你重点看这几类内容：

- `ModuleInitializer + SystemRegistry.Register(...)`：系统为什么会自动出现
- `_Ready()`：当前会扫描哪些模块，默认打开哪个模块
- `TestSystem.tscn + CacheUiNodes()`：主面板骨架在哪里、代码如何拿到关键节点
- `SetSelectedEntity(...)`：选中实体后，状态如何通过 `TestSystem.Events` 广播给各模块
- `SwitchModule(...)`：模块切换时，生命周期是怎么流转的
- `RebuildModuleTree(...)`：模块路径如何自动生成左侧分组树
- `OnMouseSelectionCompleted(...)`：宿主如何监听全局鼠标选择结果并切换测试实体

如果你只想先建立全局认知，**先把这个文件看明白，其他文件就不会散**。

### 第二步：再看 `TestModuleBase.cs`

这个文件回答的是“**所有测试模块必须遵守什么协议**”。

你要重点理解这几个生命周期：

- `TestModuleDefinition`
- `Initialize(ITestModuleContext context)`
- `OnSelectedEntityChanged(IEntity? entity)`
- `OnActivated()`
- `OnDeactivated()`
- `Refresh()`

看懂它之后，你就知道所有模块都应该把逻辑挂在哪个阶段，不会在读具体模块时迷路。

### 第三步：按你的目标选模块读

#### 如果你要看属性测试

推荐顺序：

1. `Attribute/AttributeTestModule.tscn`
2. `Attribute/AttributeEditorRow.tscn` 与各类 `Attribute*Editor.tscn`
3. `Attribute/AttributeTestModule.cs`
4. `FeatureDebugService.cs`
5. 相关 `DataKey / descriptor / DataCategory`

这样看的原因是：

- `Attribute/AttributeTestModule.tscn` 先让你看懂固定布局
- `Attribute/AttributeEditorRow.tscn` 等复用场景先让你看懂每个词条的固定结构
- `Attribute/AttributeTestModule.cs` 再看右侧属性编辑行如何根据元数据实例化这些场景
- 然后看 `FeatureDebugService.cs`，理解“临时加成”为什么不直接硬改 Data
- 最后再回到 descriptor / generated handle，确认哪些字段允许编辑、哪些字段支持 Modifier

你读属性测试时，重点抓住两条线：

- **直接覆写线**：`entity.Data.Set(...)`
- **临时加成线**：`FeatureDebugService.ApplyTemporaryModifier(...)`

#### 如果你要看技能测试

推荐顺序：

1. `Ability/AbilityTestModule.tscn`
2. `Ability/AbilityGroupSection.tscn / AbilityCatalogItem.tscn / AbilityOwnedItem.tscn`
3. `Ability/AbilityTestModule.cs`
4. `Ability/AbilityTestService.cs`
5. `FeatureDebugService.cs`
6. `Ability/AbilityTestViewModels.cs`

这样看的原因是：

- `Ability/AbilityTestModule.tscn` 先让你看懂左右双栏固定布局
- `Ability/AbilityGroupSection.tscn / AbilityCatalogItem.tscn / AbilityOwnedItem.tscn` 先让你看懂分组区块与卡片结构
- `Ability/AbilityTestModule.cs` 再回答列表如何重建、交互怎么转发
- `Ability/AbilityTestService.cs` 再回答“数据从哪来、操作怎么做”
- `FeatureDebugService.cs` 再告诉你操作最后是如何转发到正式链路
- `Ability/AbilityTestViewModels.cs` 最后补齐展示数据结构

你读技能测试时，重点抓住一条主链：

- **UI 输入** -> `AbilityTestModule`
- **业务编排** -> `AbilityTestService`
- **正式系统适配** -> `FeatureDebugService`
- **运行时能力生命周期** -> `EntityManager / FeatureSystem`

### 第四步：最后再看 `FeatureDebugService.cs`

很多人第一次看会直接钻这个文件，但更好的方式是**先知道谁在调用它，再看它怎么转发**。

这个文件的核心定位不是“再做一套测试逻辑”，而是：

- 给 `TestSystem` 提供统一调试适配层
- 把调试动作转发到正式运行时 API
- 避免 UI 模块直接操作底层系统

你重点看：

- `GrantAbility(...)`
- `RemoveAbility(...)`
- `SetFeatureEnabled(...)`
- `ApplyTemporaryModifier(...)`
- `ClearTemporaryModifier(...)`

### 第五步：带着代码回头看正式说明

当你把上面几个文件看过一遍后，再去看：

- `Docs/框架/ECS/System/TestSystem/TestSystem.md`

这时你会更容易把“代码实现”和“设计边界”对上。

建议你重点确认三件事：

- `TestSystem` 只是测试宿主，不是正式玩法 UI
- 技能测试只负责管理，不负责执行
- Feature / Ability 生命周期必须继续复用正式系统，而不是在这里另起炉灶

### 一个最实用的阅读口诀

如果你只记一句话，就记这个：

- **先看宿主 `TestSystem.cs + TestSystem.tscn`，再看协议 `TestModuleBase`，然后按模块读 `*.tscn` 骨架、`*.cs` 动态逻辑和服务层，最后看 `FeatureDebugService` 怎么接正式链路。**

## 3. 使用方式

### 3.1 运行时打开面板

`TestSystem` 通过 `ModuleInitializer + SystemRegistry.Register(...)` 注册，挂载位置和默认装载策略由 `system.config/TestSystem` snapshot record 决定。

正常启动游戏后：

1. 点击左上角“测试”按钮
2. 打开或隐藏测试面板
3. 通过左侧模块树切换“属性测试”、“技能测试”、“系统监控”、“资源目录”、“对象池”与“敌人生成”模块；模块树可用顶部按钮隐藏
4. 通过顶部“缩小 / 放大”按钮调整测试面板尺寸

### 3.2 选择实体

有两种常用方式：

#### 方式 A：鼠标点选 / 框选

- 打开“选择实体”开关
- 鼠标点击场景中的目标实体，或拖拽框选多个实体
- 通用选择系统会完成拾取，并通过全局事件广播结果集合
- `TestSystem` 只在面板可见且“选择实体”开关开启时消费结果；选中变化会通过 `TestSystem.Events.Emit(GameEventType.TestSystem.SelectionChanged, ...)` 广播给宿主与模块；正式玩法系统应监听鼠标选择系统的全局结果后自行按 `EntityType / Team` 收窄候选

#### 方式 B：代码主动指定

如果你在测试场景中创建了一个固定玩家 / 敌人，推荐在生成后直接调用：

```csharp
TestSystem.Instance?.SetSelectedEntity(entity);
```

适合：

- 单元测试场景
- 技能测试场景
- 属性回归测试场景

## 4. 属性测试怎么用

属性测试模块当前采用双轨模式：

### 4.1 直接改 Data

适合：

- 临时改当前值
- 快速验证状态字段
- 回归检查 `DataDefinition` 上下限与类型约束

底层行为：

- 直接 `selectedEntity.Data.Set(...)`

### 4.2 临时 Feature 加成

当 `DataDefinition` 满足：

- `IsNumeric == true`
- `SupportModifiers == true`
- `IsComputed == false`

面板会显示“临时加成”行。

底层行为：

- 通过 `FeatureDebugService` 构造运行时 `RuntimeDataRecordDto`
- 用 `EntityManager.AddRuntimeFeature(owner, record)` 挂到目标实体
- 用 `FeatureModifierEntryData` 验证正式 Modifier 链路

## 5. 技能测试怎么用

技能测试模块只负责**管理技能**，不负责执行技能。

### 当前支持

- 添加技能
- 移除技能
- 启用技能
- 禁用技能

### 数据来源

左侧技能库来自：

- `runtime_snapshot.json` 的 `ability` records

分组优先看：

- `AbilityFeatureGroup` projection 字段

显示规则：

- 面板分类标题和 Tooltip 显示完整 `FeatureGroupId`
- 不再把 `技能.被动` 裁剪成 `被动`
- 缺少 `FeatureGroupId` 时使用 `AbilityType` 兜底

### 交互方式

- 左侧卡片：点击“添加”按钮
- 右侧卡片：点击“启用 / 禁用 / 移除”按钮

底层不会绕开正式系统，而是通过：

- `AbilityTestService`
- `FeatureDebugService`
- `EntityManager` / `FeatureSystem`

完成转发。

## 6. 敌人生成测试怎么用

敌人生成测试模块只负责**手动批量生成敌人**，不负责完整波次演示。

### 当前支持

- 从 `ResourceCatalog.GetEntries("Unit.Enemy")` 选择 snapshot 敌人条目
- 选择 `SpawnPositionStrategy`
- 设置生成数量
- 调用 `SpawnSystem.SpawnBatch(...)`
- 调用 `SpawnSystem.KillAllEnemies()` 清空敌人
- 显示 `EnemyPool` 活跃与池内数量

### 数据来源

敌人列表来自：

- `ResourceCatalog.GetEntries("Unit.Enemy")`
- `runtime_snapshot.json` 的 `unit.enemy` records

模块按目录前缀 `Unit.Enemy` 过滤，不展示玩家、目标指示器、技能或特效资源，避免把非敌人配置传入 `SpawnSystem`。

### 边界

- 只生成 `EnemyEntity`
- 只使用 `ObjectPoolNames.EnemyPool`
- 不直接 `new Entity()`，不直接 `QueueFree()`
- 不复制波次计时逻辑，波次测试仍应使用 `SpawnSystem.StartWave(...)` 或正式事件

## 7. 资源目录测试怎么用

资源目录测试模块用于验证 `ResourceCatalog` 当前能拿到的**全部分类**，并按分类查看资源明细。

### 当前支持

- 调用 `ResourceCatalog.GetGroups()` 展示所有分类
- 不一次性展开全部资源
- 选择分类后自动显示该分类下的资源
- 顶部显示分类总数与资源总数
- 选中分类时显示分类路径和资源数
- 选中资源时显示 `ResourceKey / CatalogPath / Category / ResourceType / Path`

### 边界

- 不扫描 `res://`
- 不加载或实例化资源内容，只验证目录索引和分类结果
- 如果新增、移动、重命名资源后这里看不到，优先检查是否运行过 `Tools/ResourceGenerator`

## 8. 系统监控怎么用

系统监控模块用于查看当前 System Core 的配置与运行态，并对非必需系统执行受保护的调试操作。

### 当前支持

- 按分组、标签、状态和搜索文本筛选系统
- 左侧列表区分“未加载 / 已加载 / 已禁用 / 状态阻塞 / 运行中”
- 右侧展示 `SystemId / MountGroup / Tags / Required / AutoLoad / StartEnabled / Priority / Dependencies / Description`
- 展示 `SystemRuntimeInfo` 中的运行态、阻塞原因和自定义统计
- 对已注册但未加载系统执行添加
- 对已加载且非必需系统执行启用 / 禁用
- 对未被其他已加载系统依赖的非必需系统执行移除

### 边界

- 不扫描 `res://`，只消费 `SystemConfigService`、`SystemRegistry` 和 `SystemManager`
- `Required == true` 的系统禁止禁用和移除
- 被其他已加载系统依赖的系统禁止移除
- UI 只调用 `SystemInfoService`，不把 SystemManager 操作散落到控件回调中

## 9. 对象池信息怎么用

对象池信息模块用于查看当前已注册对象池的**运行时统计**和**容量配置**。

### 当前支持

- 左侧只展示对象池名称列表，避免被长统计文案挤占宽度
- 模块激活时每秒自动刷新一次，便于持续观察运行时池统计
- 自动刷新后保留当前对象池选择，不会每次跳回第一项
- 选中单个对象池后右侧用中文查看“风险提示 / 闲置数量 / 使用中数量 / 累计创建 / 复用率”
- 详情区继续显示“累计获取 / 累计池内复用 / 累计扩容新建 / 累计归还 / 累计丢弃 / 初始容量 / 最大容量”
- 显示 `InitialSize / MaxSize`
- 给出“缺少容量元数据 / 发生过容量丢弃 / 容量接近上限”等提示

### 边界

- 只读观测，不执行 `CleanupAll / DestroyAll`
- 容量信息来自对象池注册时记录的元数据，不从源码反查

## 10. 视觉预览怎么用

视觉预览已迁出 TestSystem，不再作为运行时测试面板模块存在。请单独运行 `res://Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn`。

### 当前支持

- 通过 `ResourcePaths.Resources` 收集全部 `Asset*` 分类
- 为每个资源生成 `VisualPreviewEntity`，并把视觉场景注入为 `VisualRoot`
- 由预览场景直接扫描并控制 `VisualRoot` 下的唯一 `AnimatedSprite2D`；非循环动作播完后会持续回放当前预览动作
- 鼠标移到动作下拉框上滚轮切换动作
- 刷新时保留当前分类和动作选择
- 鼠标选择后显示 `DataKey.Name`、资源键、资源路径、分类、默认动作和当前动作
- `AssetUnit*` 默认动作为 `idle`，`AssetEffect` 默认动作为 `Effect`，其他 AnimatedSprite2D 资源默认取第一个动作
- `AssetProjectile` 只要求展示和选择，不强制参与 AnimatedSprite2D 动作控制

### 边界

- 第一版只支持 `AnimatedSprite2D + SpriteFrames`
- 默认每个预览资源只应有一个 `AnimatedSprite2D`；若发现多个则报错并只控制第一个
- 第一版不支持拖拽摆位和运行时调节网格参数
- 不走对象池，属于低频调试实例化

## 11. 新增测试模块的推荐步骤

### 第一步：创建模块类

新增一个继承 `TestModuleBase` 的模块：

```csharp
public partial class MyTestModule : TestModuleBase
{
    internal override TestModuleDefinition Definition => new(
        "my-module",
        "属性.我的测试"
    );

    internal override void Initialize(ITestModuleContext context)
    {
        base.Initialize(context);
    }

    internal override void Refresh()
    {
    }
}
```

### 第二步：注册模块

在 `Core/TestModuleSceneRegistry.cs` 里登记 `MyTestModule` 的稳定 `Id`、`ModulePath` 和对应场景资源键，宿主会在切换模块时动态实例化 `MyTestModule.tscn`。

模块必须保证：

- `Definition.Id` 稳定且不重复
- `Definition.ModulePath` 使用点分路径，最后一段是模块名，前面的段会成为左侧树分组
- 模块路径根分组优先复用 `TestModuleGroupId`

### 第三步：处理订阅生命周期

如果模块订阅了实体事件或全局事件：

- 在 `OnSelectedEntityChanged(...)` 切换旧订阅
- 在 `OnActivated()` 恢复订阅
- 在 `OnDeactivated()` 解除订阅
- 只有激活模块允许保留高频订阅
- 面板隐藏后模块必须停止高频刷新
- 不要仅依赖 `Visible` 判断后台模块是否还能继续工作

### 第四步：不要把系统调用直接塞进 UI

如果模块会操作：

- Feature 生命周期
- Ability 生命周期
- 其它正式子系统

推荐先写一层 Service / Adapter，再由 UI 转发调用。

## 12. 开发约束

维护此目录时请遵守以下边界：

- `TestSystem` 只做宿主与模块切换
- `TestSelectionContext` 只保存选中实体；TestSystem 专属事件统一走 `TestSystem.Events`
- `TestModuleBase` 只做统一生命周期协议
- UI 模块只做展示和输入转发
- Feature / Ability 生命周期优先复用正式链路
- TestSystem 模块是调试 UI，不要用 `IFeatureHandler` 或伪造 FeatureEntity 管理模块生命周期
- 不要在这里新增一套测试版技能执行系统
- 不要直接编辑计算属性

### 11.1 DataKey 访问规范

在 TestSystem 内部访问 `Data.Get/Set` 时，**必须使用生成的 typed handle**，不要依赖旧的隐式字符串转换：

```csharp
// ✅ 正确：显式 typed handle
ability.Data.Get<string>(GeneratedDataKey.Name);

// ❌ 错误：依赖旧字符串取键方式
ability.Data.Get<string>("Name");
```

原因：规避旧字符串键名和旧隐式转换写法回流。

### 11.2 日志级别规范

TestSystem UI 控件统一使用以下日志级别：

| 级别 | 用途 |
|------|------|
| `Info` | 用户操作确认（点击添加/移除/切换等） |
| `Warn` | 操作前置条件不满足 |
| `Error` | 节点最终缺失、场景实例化失败、分组渲染异常 |

节点查找允许在 `unique-name` 与普通路径之间安静回退，不要为回退成功路径输出 `Warn`，避免测试面板初始化时刷屏。

**不要使用 `LogLevel.Debug`**。运行时测试系统的日志面向开发者调试，Debug 级别在测试面板中属于冗余输出。

## 13. 你通常会改哪些地方

### 新增调试模块

通常要改：

- 新模块源码文件
- `Core/TestModuleSceneRegistry.cs` 中登记新模块场景与路径
- `Docs/框架/ECS/System/TestSystem/TestSystem.md`
- `.codex/skills/test-system/SKILL.md`
- `Docs/框架/项目索引.md`

### 扩展属性测试

通常要改：

- `Attribute/AttributeTestModule.cs`
- 必要时 `FeatureDebugService.cs`
- 相关 `descriptor / DataKey / DataCategory`
- 正式说明文档与 skill

属性模块当前推荐策略：

- 分类切换/实体切换时重建
- 普通属性变化只 patch 单行
- 普通属性变化只 patch 对应行；结构变化才重建

### 扩展技能管理

通常要改：

- `Ability/AbilityTestModule.cs`
- `Ability/AbilityTestService.cs`
- 必要时 `FeatureDebugService.cs`
- 正式说明文档与 skill

### 扩展资源选择 / 生成测试

通常要改：

- `Data/ResourceManagement/ResourceCatalog.cs`
- `ResourceCatalog/ResourcePickerControl.cs`
- `Info/ObjectPoolInfoModule.cs`
- 独立视觉预览场景：`Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.cs`
- `Spawn/SpawnTestModule.cs`
- `TestSystem.tscn`
- `Data/ResourceManagement/README.md`
- 正式说明文档与 skill

资源选择扩展规则：

- 可选资源来自 `ResourcePaths.Resources`
- 选择器分类来自 runtime snapshot record 或资源路径，snapshot 敌人记录会归到 `Unit.Enemy`
- 单位视觉 Asset 会按 `ResourceCategory.AssetUnitEnemy / AssetUnitPlayer` 推导到 `AssetUnit.Enemy / AssetUnit.Player`
- 独立视觉预览场景不走 `ResourceCatalog` 前缀过滤，而是直接收集 `ResourcePaths.Resources` 中全部 `Asset*` 分类
- 路径中的 `Resource` 目录只表示存放位置，不参与分类
- 新增资产或场景资源后必须运行 `Tools/ResourceGenerator`
- 不要让运行时测试面板全盘扫描目录作为主数据源
- 具体模块只消费自己允许的目录前缀，例如敌人生成只消费 `Unit.Enemy`

## 14. 快速自检清单

提交前建议检查：

- 模块切换后是否正确解除旧订阅
- 未选中实体时是否有明确提示
- 是否绕开了正式 `EntityManager / FeatureSystem`
- 是否错误地把技能测试做成了执行入口
- 敌人生成测试是否只允许选择 `Unit.Enemy`
- 资源目录测试是否切换分类后自动显示该分类资源
- 独立视觉预览场景是否覆盖全部 `Asset*` 分类，且退出场景时会通过 `EntityManager.Destroy` 清空预览实体
- 对象池信息是否仍保持只读观测
- Data.Get/Set 调用是否使用生成的 typed handle 显式访问
- 日志是否仅使用 `Info / Warn / Error`，无 `Debug` 级别
- 文档、项目索引、skill 是否同步更新
