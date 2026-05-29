# DataConfigEditor C# 表格插件方案

> 日期：2026-04-24  
> 范围：`Data/DataOS runtime table/` 纯 C# 配置、`addons/DataConfigEditor/` Godot C# 插件、独立工具 `DataConfigEditor_CSharp` 的路线取舍  
> 结论：短期继续完善 Godot C# 插件，长期抽取共享解析/写回核心给独立软件复用。

## 1. 背景

当前项目已经把主要配置迁移到 `Data/DataOS runtime table/`：

- 一张表对应一个 C# 配置类，例如 `AbilityData`、`EnemyData`。
- 一行数据对应一个 `public static readonly XxxData` 静态实例。
- 运行时通过 `DataTable.GetAll<T>()` / `DataTable.GetByName<T>()` 建立表级索引。
- Entity 生成时通过 `Data.LoadFromConfig(config)` 把 POCO 属性注入到运行时 `Data` 字典。

因此问题不是“数据最终能不能变成字典”，而是：

1. 编辑器能不能把 C# 强类型配置稳定表格化。
2. 表格编辑后能不能可靠写回 C# 源码。
3. 作者侧是否还保留强类型、注释、默认值和 DataKey 映射。

## 2. 核心决策

### 2.1 DataOS runtime table 继续作为数据真相源

保留当前形态：

```csharp
public static readonly EnemyData Yuren = new()
{
    Name = "鱼人",
    Team = Team.Enemy,
    VisualScenePath = "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn",
    BaseHp = 150f,
    MoveSpeed = 150f,
};
```

理由：

- C# 类型就是 schema，字段类型能直接驱动表格编辑器。
- `<summary>` 注释能作为表头说明和 tooltip。
- `DataKey` / `[DataKey]` 映射还能保持运行时 Data 规范。
- 重构时 IDE 能发现类型和属性名错误。
- 不需要维护 `.tres` 与 C# 常量两套真相源。

### 2.2 不把主数据改成手写静态大字典

不推荐把配置写成：

```csharp
public static readonly Dictionary<string, Dictionary<string, object>> Data = new()
{
    ["Yuren"] = new()
    {
        ["BaseHp"] = 150f,
        ["MoveSpeed"] = 150f,
    },
};
```

原因：

- 字段失去强类型，表格编辑器只能猜类型。
- `DataKey` 拼写错误更难发现。
- `<summary>`、默认值、分组、枚举候选项都要额外维护。
- 复杂字段会退化成 `object`，后续工具会更难做。

正确做法是：作者侧保持 POCO 静态实例，运行时或工具侧生成/缓存字典索引。

推荐补充：

```csharp
public static IReadOnlyDictionary<string, EnemyData> ByName => DataTable.GetByNameMap<EnemyData>();
```

如果需要更快查询，可以让工具生成 `ById` / `ByName` 索引代码，但它是生成产物，不是手写主数据。

### 2.3 短期继续做 Godot C# 插件

当前 `addons/DataConfigEditor/` 已经具备这些基础：

- 能扫描 `Data/DataOS runtime table` 类型。
- 能显示配置类和静态实例。
- 能读属性 `<summary>` 注释。
- 能显示 enum、Flags enum、bool、数值、路径字符串。
- 已有 `CsFileWriter` 写回器雏形。

短期继续做插件的收益最高：

- 已经嵌入 Godot 工程，不需要额外打开独立软件。
- 可以直接使用 Godot 的 `res://`、`ResourceLoader`、资源拖拽和缩略图。
- 与当前 DataOS runtime table 类型、DataKey、枚举、FeatureId 常量在同一编译上下文。
- 见效快，先解决“能编辑并写回 C#”这个关键阻塞。

### 2.4 独立 C# 软件作为长期方向

`/mnt/e/Code/C#/DataConfigEditor_CSharp` 的定位更适合长期产品化：

- WinForms / ReoGrid 更适合大表格、多选、冻结、筛选、排序、批量编辑。
- 独立软件可以做更强的设置面板、工作区管理和诊断。
- 不受 Godot 编辑器 UI 和 C# 插件编译节奏限制。

但当前独立软件仍有成本：

- 要自己解决 Godot 路径、资源预览、项目 DLL 类型语义。
- 要实现保存回写、撤销重做、类型化编辑。
- 当前主表格仍偏只读浏览，还不能直接替代插件。

因此不建议现在切主线。更稳的路线是先把插件写回语义做对，再抽取共享核心给独立软件。

## 3. 插件当前关键问题

### 3.1 编辑只改了内存实例

当前单元格编辑会通过反射修改静态实例对象的属性。这个修改只影响当前编辑器进程内存，不会自动改变 `.cs` 文件。

必须点击保存，并且写回器必须能把变更落到对应对象初始化器里。

### 3.2 写回器只适合替换已有属性

当前写回器的核心逻辑是匹配：

```csharp
PropName = oldValue
```

然后替换为新值。

如果某个属性没有在对象初始化器中显式出现，例如原来依赖默认值：

```csharp
public static readonly EnemyData Yuren = new()
{
    Name = "鱼人",
};
```

用户在表格里编辑了 `BaseHp`，写回器必须新增：

```csharp
BaseHp = 150f,
```

否则保存后 C# 文件不会变化。

这是当前插件从“能看”走向“能编辑”的第一优先级。

### 3.3 复杂表达式不能伪装成可编辑

这些值第一阶段应显示为只读或原始表达式：

- `new List<FeatureModifierEntryData> { ... }`
- 嵌套对象初始化器
- `SomeConst.Path`
- `nameof(...)`
- 方法调用或计算表达式

插件不能把复杂表达式当普通字符串随便覆盖。否则很容易破坏源码。

## 4. 推荐架构

```text
Data/DataOS runtime table/*.cs
  ↓
ConfigSourceParser
  ↓
TableDocument
  ↓
Godot DataConfigEditor UI
  ↓
EditOperation / DirtyState
  ↓
CsTableWriter
  ↓
写回对象初始化器
  ↓
Godot 重新编译 C# 项目
```

### 4.1 真相源

- 真相源是 `Data/DataOS runtime table/*.cs`。
- `.tres` 仅作为兼容回退。
- 不再把 `.tres` 作为新数据的主要编辑入口。

### 4.2 表格模型

内部应增加中间模型，避免 UI 直接绑定反射对象：

```csharp
public sealed class TableDocument
{
    public string SourceFilePath { get; init; }
    public Type ConfigType { get; init; }
    public IReadOnlyList<TableColumn> Columns { get; init; }
    public IReadOnlyList<TableRow> Rows { get; init; }
}
```

```csharp
public sealed class TableColumn
{
    public string PropertyName { get; init; }
    public Type PropertyType { get; init; }
    public string Summary { get; init; }
    public string DataKeyName { get; init; }
    public bool IsEditable { get; init; }
}
```

```csharp
public sealed class TableRow
{
    public string FieldName { get; init; }
    public object Instance { get; init; }
    public Dictionary<string, TableCell> Cells { get; init; }
}
```

### 4.3 写回边界

只写回满足以下条件的单元格：

- 对应 `public static readonly` 静态字段。
- 字段初始化器是 `new()` 或 `new Type()` 对象初始化器。
- 目标属性是简单类型：string、bool、int、float、double、enum、Flags enum。
- 能定位到初始化器范围。

不能写回时，单元格显示为只读，并给出诊断原因。

## 5. 分阶段计划

### 5.0 已落地记录

2026-04-24 已完成首轮插件落地：

- `CsFileWriter` 改为定位 `public static readonly XxxData Field = new() { ... }` 的对象初始化器范围，支持替换顶层简单属性。
- 缺失属性只有在当前值不同于默认实例值时才补写，避免保存时把所有默认字段刷进源码。
- 字符串按合法 C# 字面量转义；`float` / `double` 使用 `InvariantCulture`；普通 enum 输出 `EnumType.Member`；Flags enum 输出 `EnumType.A | EnumType.B`。
- 已有源码值若是复杂表达式，保存器跳过该属性，避免覆盖 `SomeConst.Path`、`nameof(...)`、方法调用等手写表达式。
- `PropertyMetadata` 增加可编辑边界，集合、数组、嵌套对象等复杂字段在 UI 中只读，并通过 tooltip / 批量编辑占位提示原因。
- 单元格编辑时先直接反射写入当前静态实例，再登记 Godot UndoRedo；随后立即调用单元格写回入口，只定位当前实例的当前属性，不再扫描整张表。
- `CsFileWriter.WriteSingleChangeWithDiagnostics` 只替换或补写一个对象初始化器里的一个属性；`保存C#` 只用于重试失败的脏单元格，不再执行全表保存。
- 单元格写回输出结构化诊断：源码文件、初始化器命中、写入字段、复杂表达式跳过和不支持类型跳过。Godot Output 中每条 `[DataConfigEditor] 单元格写回诊断` 都能说明该字段为什么写入或跳过。

### Phase 1：修通保存写回

目标：表格编辑后 C# 文件真的改变。

任务：

1. 给 `CsFileWriter` 增加测试用例或手工验证样例。
2. 支持替换已有属性值。
3. 支持给初始化器新增缺失属性。
4. 支持字符串转义，例如引号、反斜杠、换行。
5. 支持 `float` 使用 InvariantCulture 输出，例如 `1.5f`。
6. 支持 enum 输出 `EnumType.Member`。
7. 支持 Flags enum 输出 `EnumType.A | EnumType.B`。
8. 保存后刷新表格并提示需要重新编译 C#。

验收：

- 修改 `EnemyData.Yuren.BaseHp` 后，`EnemyData.cs` 出现新的 `BaseHp = ...` 或已有值被替换。
- 修改路径字符串后，文件中保持合法 C# 字符串。
- 修改 enum 后，文件中保存为枚举成员名，不保存为中文说明或整数。

### Phase 2：明确可编辑/只读诊断

目标：不安全的字段不允许误编辑。

任务：

1. `PropertyMetadata` 增加 `IsEditable`、`ReadOnlyReason`。
2. 简单类型开放编辑。
3. `List<T>`、数组、字典、嵌套对象初始化器先只读。
4. 源码表达式不是简单字面量时只读。
5. UI 用 tooltip 显示只读原因。

验收：

- `FeatureDefinitionData.Modifiers` 不会被普通文本框误改。
- 复杂表达式不会被保存器覆盖。
- 用户能看懂为什么某一列不能编辑。

### Phase 3：增强路径字段

目标：路径字段成为插件相对独立软件的优势。

任务：

1. `*Path`、`*ScenePath`、`EffectScenePath`、`ProjectileScenePath` 识别为资源路径。
2. 支持从 FileSystem 面板拖拽资源到单元格。
3. 用 `ResourceLoader.Exists()` 校验路径。
4. 场景、贴图字段显示缩略图或有效/无效状态。
5. 保存时统一规范为 `res://`。

验收：

- 拖拽 `.tscn` 到路径单元格后，源码保存为 `res://...`。
- 不存在的路径有红色提示，但不会阻止用户临时保存。

### Phase 4：抽取共享核心

目标：为独立 C# 软件复用做准备。

建议新建或整理为：

```text
addons/DataConfigEditor/Core/
├── TableDocument.cs
├── TableColumn.cs
├── TableRow.cs
├── TableCell.cs
├── CsTableParser.cs
├── CsTableWriter.cs
└── CellValueFormatter.cs
```

Godot 插件只负责：

- Godot UI。
- Godot 路径校验。
- 资源拖拽。
- 预览图加载。

共享核心负责：

- C# 配置文件解析。
- 注释解析。
- 对象初始化器定位。
- 单元格值格式化。
- 源码写回。

验收：

- 插件和独立软件能使用同一套写回规则。
- 新增一个保存规则时，不需要在两个项目重复实现。

### Phase 5：独立软件继续产品化

目标：当插件已经满足日常编辑后，再投入独立软件。

独立软件优先做插件不擅长的能力：

- 大表格性能。
- 多选和批量编辑。
- 筛选排序。
- 列冻结。
- 分组折叠。
- 设置面板。
- 工作区级诊断。

不重复优先做：

- Godot 资源拖拽。
- Godot 内部预览。
- 依赖 Godot EditorInterface 的操作。

## 6. 数据规则

### 6.1 DataOS runtime table 表写法

必须遵循：

- 行数据用 `public static readonly XxxData`。
- `Name` 在同表内唯一。
- 默认值优先读 `DataKey.Xxx.DefaultValue`。
- 属性名与目标 DataKey 不一致时使用 `[DataKey(nameof(DataKey.Xxx))]`。
- 不新增 `const string` DataKey，除非是特殊引用键。

### 6.2 系统配置

系统级配置继续走 `Data/DataOS runtime table/System/`：

- `SystemData` 是系统配置纯 C# 优先数据源。
- `SystemPresetData` 是系统预设纯 C# 优先数据源。
- 同名 `.tres` 只作为兼容回退。

### 6.3 不限制语义

数值型“不限制”继续使用 `-1`，不要用 `0`。

例如：

- `SpawnMaxWave = -1`
- `SpawnMaxCountPerWave = -1`
- `MoveMaxDuration = -1`
- `MoveMaxDistance = -1`

### 6.4 概率语义

概率值统一 `0-100`，计算时 `/100`。

表格 UI 可以显示为百分比，但保存回 C# 时仍保存 `0-100` 数值。

## 7. 风险与处理

| 风险 | 处理 |
|------|------|
| 正则写回破坏源码 | 写回前定位对象初始化器范围，只处理简单属性；复杂表达式只读 |
| 保存后 Godot C# 重新编译导致插件状态丢失 | 保存后提示刷新，必要时延迟刷新 |
| 编辑内存实例但源码未变 | DirtyState 必须以源码写回成功为准 |
| 大表格 Godot UI 性能差 | 插件优先服务当前规模；大规模编辑交给独立软件 |
| DataKey 映射失效 | 属性元数据必须显示 DataKeyName；保存不改 DataKey 定义 |
| 静态初始化顺序问题 | `All` 使用延迟属性，不在静态实例前构建数组 |

## 8. 最终推荐

当前主线：

```text
继续完善 addons/DataConfigEditor
  → 先修保存写回
  → 再补只读诊断
  → 再增强路径拖拽和资源预览
```

长期主线：

```text
抽取 C# 表格解析/写回核心
  → 插件复用
  → 独立 C# 软件复用
  → 独立软件承担复杂表格体验
```

不要做：

```text
手写静态 Dictionary<string, object> 作为主数据源
```

可以做：

```text
从 DataOS runtime table POCO 自动生成或缓存查询字典
```

这样可以同时保留 C# 强类型配置的维护优势、运行时字典读取的便利性，以及插件快速落地的效率。
