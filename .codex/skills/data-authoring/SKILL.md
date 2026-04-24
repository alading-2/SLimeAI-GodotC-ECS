---
name: data-authoring
description: 编写或修改 Data 目录下的数据配置、Config、DataKey、EventType、Resource 映射规则时使用。适用于：新增配置字段、设计 DataKey 分域、决定字段放 Data/Data 还是 Data/Config、编写事件协议。触发关键词：Data目录、Config配置、DataKey定义、EventType、数据配置、Resource映射。
---

# Data 目录数据配置规范

## 适用范围

当你修改以下目录时，优先遵循本 Skill：

- `Data/Config/`
- `Data/Data/`
- `Data/DataKey/`
- `Data/EventType/`

它关注的是 **数据内容与协议怎么组织**，而不是运行时 `Data` 容器本身如何工作。

## 与 `ecs-data` 的分工

- **`data-authoring`**：负责 `Data/` 目录下的配置结构、DataKey 定义、事件协议、字段映射
- **`ecs-data`**：负责 `Src/ECS/Base/Data/` 运行时容器、`DataMeta`、`DataRegistry`、读写规则

## 目录职责

### `Data/Config/`

放系统级配置：

- 波次规则
- Spawn 全局参数
- 系统开关
- `Data/Config/System/**/*.tres` 这类系统配置 / 系统预设 / 系统默认装载策略
- 阈值与限制

补充约定：

- `SystemConfig` 继续归 `Data/Config/System/System/`，不要并到 `Data/Data/`
- `SystemPreset` 继续归 `Data/Config/System/Preset/`，只负责按标签和显式 `SystemId` 选择启动装载集合
- `Data/DataNew/System/SystemConfigData.cs` 是系统配置的纯 C# 优先数据源；同名 `.tres` 只作为兼容回退
- `Data/DataNew/System/SystemPresetData.cs` 是系统预设的纯 C# 优先数据源；同名 `.tres` 只作为兼容回退
- 默认预设若只需要少量调试入口，优先写入 `EnabledSystemIds`（当前为 `TestSystem`、`MouseSelectionSystem`），不要为了方便把 `Debug / Test` 标签整体加入默认标签集合
- `SystemConfig` 的 `AllowedFlowStates / AllowedSimulationStates = None` 表示不限制，`BlockedOverlays = None` 表示不屏蔽，`RequiredOverlays = None` 表示不要求覆盖层
- `AllowedFlowStates` 使用 `GameFlowState` Flags，`AllowedSimulationStates` 使用 `SimulationState` Flags；不要再新增或引用单独的 Mask enum
- 系统运行条件优先使用 Phase 预设组合：局内主玩法用 `GameFlowState.Gameplay + OverlayFlags.Blocking + SimulationState.Running`，允许暂停/运行都响应用 `SimulationState.Any`
- 系统配置不是 `Data.LoadFromResource()` 注入到 Entity.Data 的业务数据

不要放：

- 某个 Entity 的初始属性输入
- 需要注入 Entity.Data 的字段

### `Data/Data/`

放会被 `Data.LoadFromResource()` 读取的配置类：

- `UnitConfig`
- `EnemyConfig`
- `PlayerConfig`
- 技能配置
- 特效配置

补充约定：

- `FeatureGroupId` 只表示技能展示分组，应放在 `Data/Data/Ability/AbilityConfig.cs`；测试面板按完整 `FeatureGroupId` 分组和显示；运行时执行器选择必须使用 `FeatureHandlerId`
- 不要再为技能额外维护 `AbilityCategory` 这类重复展示字段；只要运行时实体和系统要读，就属于 `Data/Data/`
- 若存在 `Data/DataNew/` 的纯 C# POCO 镜像配置，字段默认值也必须与旧 `Data/Data/` 保持同语义：优先直读 `DataKey.Xxx.DefaultValue`，若旧类对该字段有特化默认值（如 `UnitConfig.EntityType = EntityType.Unit`、`AbilityConfig.EntityType = EntityType.Ability`、`TargetingIndicatorConfig` 的布尔默认值），`DataNew` 也要同步保留该特化语义
- `DataNew` 静态实例迁移时，以旧 `.tres` 的显式赋值为准；不要因为 POCO 的裸类型默认值 `0/false/空串` 把旧资源中已有值静默丢掉
- `DataNew` 若提供 `All` 聚合，优先写成延迟求值属性（如 `public static XxxData[] All => [...]`），不要在静态实例声明之前写 `static readonly All`，否则 C# 静态初始化顺序会把后续实例收集成 `null`

推荐写法：

```csharp
[DataKey(nameof(DataKey.BaseHp))]
[Export] public float BaseHp { get; set; } = (float)DataKey.BaseHp.DefaultValue!;
```

### `Data/DataKey/`

放所有 DataKey 与 DataMeta 定义。

主流写法：

```csharp
public static readonly DataMeta BaseHp = DataRegistry.Register(
    new DataMeta {
        Key = nameof(BaseHp),
        Type = typeof(float),
        DefaultValue = 0f
    });
```

特殊引用键才允许保留 `const string`。

### `Data/EventType/`

放模块间通信协议：

- 事件名
- 事件数据结构
- Base / Unit / Ability / Data 等分域事件定义

### `Data/ResourceManagement/`

放资源索引与资源目录辅助：

- `ResourcePaths.cs` 是 `Tools/ResourceGenerator` 自动生成的资源路径索引，禁止手改
- `ResourceManagement.cs` 是统一加载入口
- `ResourceCatalog.cs` 是运行时/测试面板/编辑器选择器使用的资源目录服务

`ResourceCatalog` 只基于 `ResourcePaths.Resources` 整理单位配置、技能配置、特效场景和单位 Asset 场景条目，分类从资源路径推导，`Resource` 目录会被跳过；不要把运行时全盘扫描 `res://` 当成主数据源。

## 决策规则

### 什么时候字段要进 `Data/Data/`

满足任一项通常就该进入：

- 要成为 Entity 的初始 Data
- 要被 `Data.LoadFromResource()` 自动注入
- 要映射到 `DataKey`
- 要被多个运行时组件/系统读取

### 什么时候字段只放 `Data/Config/`

满足以下特征通常只放系统配置：

- 不属于具体 Entity
- 不是运行时 Data 状态
- 不需要映射到 `DataKey`
- 更像全局规则或系统参数

## 新增配置字段标准流程

1. 先判断它属于 `Config` 还是 `Data`
2. 若属于运行时 Data，先在 `Data/DataKey/` 定义 `DataKey`
3. 补齐 `DataMeta`（类型、默认值、分类、约束）
4. 在 `Data/Data/` 配置类中添加 `[DataKey(nameof(DataKey.Xxx))]`
5. 默认值优先直接读取 `DataKey.Xxx.DefaultValue`
6. 如涉及通信，再补 `Data/EventType/` 契约

## 新增资源文件标准流程

当新增、移动、重命名或删除 `.tres` / `.tscn`：

1. 保持资源路径落在 `Tools/ResourceGenerator/ResourceGenerator.cs` 的扫描范围内
2. 运行 `dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj`
3. 检查 `Data/ResourceManagement/ResourcePaths.cs` 是否出现预期条目
4. 如果资源需要出现在通用选择器中，检查路径是否能推导出正确 `CatalogPath`；`Data/Data` 下按目录名分类，`Resource` 目录会被跳过
5. 更新 `Docs/框架/项目索引.md` 和相关系统文档

## 禁止事项

- ❌ 在 `Data/Data/` 里直接发明字符串键
- ❌ 手动修改 `Data/ResourceManagement/ResourcePaths.cs`
- ❌ 运行时全盘扫描目录替代 `ResourcePaths.Resources`
- ❌ 新增 `const string` DataKey（特殊引用键除外）
- ❌ 把系统配置误塞到 `Data/Data/`
- ❌ 把某个 Entity 的初始字段误塞到 `Data/Config/`
- ❌ 在 `Data/` 目录写运行时业务逻辑

## 推荐联动检查

修改 `Data/` 目录后，通常需要顺手检查：

- `Src/ECS/Base/Data/README.md` 是否仍和现状一致
- `Docs/框架/项目索引.md` 是否需要补导航
- `Docs/框架/ECS/Data/` 主文档是否需要同步
- `.codex/skills/ecs-data/SKILL.md` 是否需要联动更新
