# ResourceManagement - 资源管理系统

## 概述
`ResourceManagement` 是项目的统一资源加载入口。它通过 `ResourcePaths.cs`（由 `ResourceGenerator` 自动生成）获取资源路径，并提供类型安全的加载方法。

## 核心原则
1. **禁止硬编码**: 严禁在业务代码中使用 `res://` 开头的字符串路径。统一管理有利于路径变更时的重构。
2. **统一分类**: 资源必须属于 `ResourceCategory` 中的某一分类。
3. **类型安全**: 优先使用 `ResourceManagement.Load<T>` 进行加载。
4. **目录选择**: UI / 编辑器需要“列出可选资源”时，优先使用 `ResourceCatalog`，不要在运行时全盘扫描 `res://`。
5. **数据目录**: Data 业务条目从 `DataOS` runtime snapshot 投影；`ResourceManagement` 不再承担 `.tres` authoring 配置读取职责。

## 使用方法

### 1. 加载场景 (PackedScene)
推荐使用 `typeof(T).Name` 进行类型安全的加载，确保代码重构（如类更名）时能自动保持同步。
```csharp
// 自动匹配类型名作为资源名
var scene = ResourceManagement.Load<PackedScene>(typeof(PlayerEntity).Name, ResourceCategory.Entity);
```

### 2. 加载业务数据引用
```csharp
// 业务数据 DTO 里只保存 res:// 字符串，最终消费点再按需加载
var visual = ResourceManagement.LoadPath<PackedScene>("res://assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn");
```

### 3. 加载 UI
```csharp
var uiScene = ResourceManagement.Load<PackedScene>("HealthBarUI", ResourceCategory.UI);
```

### 4. 构建资源选择列表

`ResourceCatalog` 基于两类输入生成选择器条目：

- `ResourcePaths.Resources`：场景、特效、视觉资产。
- `DataOS` runtime snapshot：业务数据 DTO，源路径显示为 `DataOS/Authoring/slimeainew.authoring.db`。

资产分类仍由资源路径自动推导，例如：

- `assets/Effect/Explosion/Explosion.tscn` => `Effect.Explosion`
- `assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn` => `AssetUnit.Enemy`
- `assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn` => `AssetUnit.Player`

示例：

```csharp
var enemyEntries = ResourceCatalog.GetEntries("Unit.Enemy");
var allGroups = ResourceCatalog.GetGroups(
    "Unit", // 全部单位
    "Ability", // 全部技能配置
    "Effect", // 全部特效资源
    "AssetUnit" // 全部单位视觉 Asset
);
```

目录服务只负责发现与展示分组；真正加载仍通过 `ResourceManagement.Load<T>(entry.ResourceKey, entry.Category)` 完成。独立视觉预览场景位于 `Src/ECS/Test/GlobalTest/VisualPreview/`，它直接基于 `ResourcePaths.Resources` 收集全部 `Asset*` 分类并批量生成可选中的预览 Entity。

## 🛠️ 最佳实践

### 文件命名与分类规范
为避免资源与业务数据概念混淆，项目采用以下约定：

- **Entity**: 游戏实体预制体 (如 `PlayerEntity.tscn`)
- **Component**: 组件预制体 (如 `HealthComponent.tscn`)
- **UI**: 界面预制体 (如 `HealthBarUI.tscn`)
- **Asset**: 纯视觉资源场景 (如 `豺狼人.tscn` - 仅包含动画/解构视图)
- **Data**: 业务配置不再通过 `.tres` 主路径编辑，统一由 `DataOS` snapshot 驱动。

## 资源生成
本项目使用 `Tools/ResourceGenerator` 自动扫描目录并生成 `ResourcePaths.cs`。

> [!IMPORTANT]
> **什么时候需要运行生成器？**
> 当你 **添加、重命名、移动或删除** 了任何被 `ResourcePaths.cs` 索引的资源文件时，必须运行生成器更新索引，否则 `ResourceManagement.Load` 将无法找到资源。
>
> 业务数据更新时不跑 ResourceGenerator，而是重建 DataOS authoring DB 并重新生成 runtime snapshot。
> 
> ```bash
> dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
> ```
