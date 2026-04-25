---
name: data-authoring
description: 编写或修改 Data 目录下的数据配置、Config、DataKey、EventType、Resource 映射规则时使用。适用于：新增配置字段、设计 DataKey 分域、决定字段放 Data/Data 还是 Data/Config、编写事件协议。触发关键词：Data目录、Config配置、DataKey定义、EventType、数据配置、Resource映射。
---

# Data 目录数据配置规范

## 适用范围

当你修改以下目录时，优先遵循本 Skill：

- `Data/Config/`
- `Data/Data/`
- `Data/DataNew/`
- `Data/DataKey/`
- `Data/EventType/`

它关注的是 **数据内容与协议怎么组织**，而不是运行时 `Data` 容器本身如何工作。

## 与 `ecs-data` 的分工

- **`data-authoring`**：负责 `Data/` 目录下的配置结构、DataNew 表数据、DataKey 定义、事件协议、字段映射
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

- `Data/DataNew/System/SystemData.cs` 是系统配置的唯一运行时数据源
- `Data/DataNew/System/SystemPresetData.cs` 是系统预设的唯一运行时数据源
- `Data/Config/System/System/` 与 `Data/Config/System/Preset/` 的旧 `.tres` 资源可保留归档，但运行时不导入
- 默认预设若只需要少量调试入口，优先写入 `EnabledSystemIds`（当前为 `TestSystem`、`MouseSelectionSystem`），不要为了方便把 `Debug / Test` 标签整体加入默认标签集合
- `SystemData` 的 `AllowedFlowStates / AllowedSimulationStates = None` 表示不限制，`BlockedOverlays = None` 表示不屏蔽，`RequiredOverlays = None` 表示不要求覆盖层
- `AllowedFlowStates` 使用 `GameFlowState` Flags，`AllowedSimulationStates` 使用 `SimulationState` Flags；不要再新增或引用单独的 Mask enum
- 系统运行条件优先使用 Phase 预设组合：局内主玩法用 `GameFlowState.Gameplay + OverlayFlags.Blocking + SimulationState.Running`，允许暂停/运行都响应用 `SimulationState.Any`
- 系统外部命令和系统自身 `_Process` / 生命周期共享同一套 `SystemData` 运行条件；如果同一系统内不同命令需要不同暂停/阶段语义，应拆成不同系统，而不是给命令单独加运行策略
- 系统配置不是 `Data.LoadFromConfig()` 注入到 Entity.Data 的业务数据

不要放：

- 某个 Entity 的初始属性输入
- 需要注入 Entity.Data 的字段

### `Data/DataNew/`

当前推荐数据源。放纯 C# 表数据：

- `AbilityData` / `ChainAbilityData`
- `UnitData` / `PlayerData` / `EnemyData` / `TargetingIndicatorData`
- `SystemData` / `SystemPresetData`

规则：

- 一张表对应一个 `XxxData` 类；一行数据对应一个 `public static readonly XxxData` 静态实例
- 默认用 `Name` 作为查询键，业务侧写 `EnemyData.Get("鱼人") ?? EnemyData.Yuren`
- `DataTable` 是静态工具类，不让数据类继承
- `GetByName` 找不到只写 `Log.Error` 并返回 `null`，调用方用 `??` 明确兜底
- 不做 `ResourcePaths` 式索引；同一张表的数据就在同一个 C# 文件里
- 场景/贴图引用必须保存 `res://` 路径字符串，如 `VisualScenePath` / `EffectScenePath` / `ProjectileScenePath`；注入到 `Data` 后仍保持字符串，使用点再加载资源
- DataNew 技能必须显式填写 `AbilityType` / `AbilityTriggerMode` / `AbilityTargetSelection` 等运行时枚举；手动技能写 `AbilityType.Active + AbilityTriggerMode.Manual`，否则技能栏会按 Passive 过滤掉

推荐写法：

```csharp
public class EnemyData : UnitData
{
    public static IReadOnlyList<EnemyData> All => DataTable.GetAll<EnemyData>();
    public static EnemyData? Get(string name) => DataTable.GetByName<EnemyData>(name);

    public static readonly EnemyData Yuren = new()
    {
        Name = "鱼人",
        Team = Team.Enemy,
        VisualScenePath = "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn",
        BaseHp = 150f
    };
}
```

### `Data/Data/`

旧 `.tres` 结构。保留历史配置类和资源文件，但运行时数据导入不再读取这里：

- `UnitConfig`
- `EnemyConfig`
- `PlayerConfig`
- 技能配置
- 特效配置

补充约定：

- 新运行时字段只加到 `Data/DataNew/` 和 `DataKey`，不要继续扩展旧 `.tres` 配置作为主流程
- `FeatureGroupId` 只表示技能展示分组，应放在 `AbilityData`；测试面板按完整 `FeatureGroupId` 分组和显示；运行时执行器选择必须使用 `FeatureHandlerId`
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

`ResourceCatalog` 的数据条目来自 `DataNew` 静态表，场景、特效和单位 Asset 条目来自 `ResourcePaths.Resources`；不要把运行时全盘扫描 `res://` 当成主数据源。

## 决策规则

### 什么时候字段要进 `Data/DataNew/`

满足任一项通常就该进入：

- 要成为 Entity 的初始 Data
- 要被 `Data.LoadFromConfig()` 自动注入
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
4. 在 `Data/DataNew/` 配置类中添加 `[DataKey(nameof(DataKey.Xxx))]`
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
