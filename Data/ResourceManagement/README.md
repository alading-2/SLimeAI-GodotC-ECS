# ResourceLoading - 当前资源加载 facade

## 概述
`ResourceLoading` 是项目的统一资源加载入口。它通过 `ResourcePaths.cs`（由 `ResourceGenerator` 自动生成）获取当前项目资源 catalog，并提供 strict lookup、source/owner/usage diagnostics 和结构化 `ResourceLoadResult`。

AI-first 目标：让 AI 和运行时从同一个 owner 查资源，不在业务代码、测试 UI 或预览工具里散落裸 `GD.Load("res://...")`。

> [!NOTE]
> `ResourceManagement` 只是迁移期薄转发；current public 心智是极薄 `ResourceLoading`。它不是 Godot 资源系统替代品，也不是跨职责资源管理器。底层仍由 Godot `GD.Load` / `ResourceLoader` 加载资源。
> `res://` 本身不是问题，它是 Godot project root 路径。移动、重命名或删除资源后，必须通过 project directory / resource-path migration workflow 替换旧引用，重新运行 `Workspace/Tools/ResourceGenerator` 并检查生成结果；当前 manifest 仍保存项目相对资源路径，不能自动修复忘记迁移的 stale path。

## 核心原则
1. **禁止裸加载**: 业务代码不要直接 `GD.Load("res://...")` / `ResourceLoader.Load("res://...")`。`res://` 可以作为 DataOS resource ref、`.tscn/.tres` 引用或带 source 的 `LoadPath` 输入。
2. **统一分类**: 资源必须属于 `ResourceCategory` 中的某一分类；Preset 是独立分类，不等同于 Component。
3. **类型安全**: current 代码使用 `ResourceLoading.Load<T>`。
4. **目录选择**: UI / 编辑器需要“列出可选资源”时，优先使用 `ResourceCatalog`，不要在运行时全盘扫描 `res://`。
5. **严格加载**: 精确 key / category 查找；找不到资源会暴露结构化失败原因，不通过相近名称 fallback 静默加载其他资源。
6. **路径来源**: `LoadPath` 只用于 DataOS resource ref、debug/test 或明确来源的资源引用；业务 Capability 不应直接传裸 `res://` 绕过 manifest。
7. **路径迁移**: 移动、重命名、删除或检查目录后用 `project-filesystem` skill / script 替换旧路径，并用 `rg` 验证旧路径残留。

## 使用方法

### 1. 加载场景 (PackedScene)
推荐使用 `typeof(T).Name` 进行类型安全的加载，确保代码重构（如类更名）时能自动保持同步。
```csharp
// 自动匹配类型名作为资源名
var scene = ResourceLoading.Load<PackedScene>(typeof(PlayerEntity).Name, ResourceCategory.Entity);
```

### 2. 加载 UI
```csharp
var uiScene = ResourceLoading.Load<PackedScene>("HealthBarUI", ResourceCategory.UI);
```

### 3. 构建资源选择列表

`ResourceCatalog` 从 runtime snapshot records 生成数据目录条目，并从 `ResourcePaths.Resources` 生成场景、特效和单位视觉 Asset 条目：

- `unit.enemy` snapshot record => `Unit.Enemy`
- `unit.player` snapshot record => `Unit.Player`
- `ability` snapshot record + `AbilityFeatureGroup` => `Ability.*`
- `assets/Effect/Explosion/Explosion.tscn` => `Effect.Explosion`
- `assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn` => `AssetUnit.Enemy`
- `assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn` => `AssetUnit.Player`

Data 目录条目的加载对象是 snapshot record id，不是 `.tres` 配置资源。

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

目录服务只负责发现与展示分组；真正加载仍通过 `ResourceLoading.Load<T>(entry.ResourceKey, entry.Category)` 完成。独立视觉预览场景位于 `Src/ECS/Test/GlobalTest/VisualPreview/`，它直接基于 `ResourcePaths.Resources` 收集全部 `Asset*` 分类并批量生成可选中的预览 Entity。

## 最佳实践

### 文件命名与分类规范
项目采用以下资源分类约定：

- **Entity**: 游戏实体预制体 (如 `PlayerEntity.tscn`)
- **Component**: 组件预制体 (如 `HealthComponent.tscn`)
- **Preset**: 组件组合预设 (如 `UnitCorePreset.tscn`、`AbilityPreset.tscn`)
- **System**: 需要通过 `SystemRegistry` 加载或管理的系统场景
- **UI**: 界面预制体 (如 `HealthBarUI.tscn`)
- **Asset**: 纯视觉资源场景 (如 `豺狼人.tscn` - 仅包含动画/解构视图)
- **DataOS snapshot**: 单位、技能、系统配置等运行时数据来自 `Data/DataOS/Snapshots/runtime_snapshot.json`。

## 资源生成
本项目使用 `Workspace/Tools/ResourceGenerator` 自动扫描目录并生成 `ResourcePaths.cs`。

> [!IMPORTANT]
> **什么时候需要运行生成器？**
> 当你 **添加、重命名、移动或删除** 了任何 `.tscn` 或 `.tres` 文件时，必须运行生成器更新索引，否则当前 `ResourceLoading.Load` 将无法找到资源。
> 如果是移动/重命名，还应先运行路径迁移脚本替换旧引用：
>
> ```bash
> python3 .ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py --old "<old>" --new "<new>"
> ```
>
> 如果同一次目录移动同时存在 `res://`、项目相对路径和当前仓绝对路径残留，先 dry-run 变体替换：
>
> ```bash
> python3 .ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py --old "<old>" --new "<new>" --include-variants
> ```
>
> 如果当前目录是游戏仓，使用框架仓脚本绝对路径并限制 `--root .`：
>
> ```bash
> python3 /home/slime/Code/SlimeAI/SlimeAI/.ai-config/skills/core/project-filesystem/scripts/migrate_resource_path.py --root . --old "<old>" --new "<new>"
> ```
> 
> ```bash
> dotnet run --project Workspace/Tools/ResourceGenerator/ResourceGenerator.csproj
> ```

生成器当前扫描：

- `assets/**` 视觉资源
- `Src/ECS/Runtime/**` 共享 Runtime 资源
- `Src/ECS/Capabilities/**` 功能 owner 内部 `Entity / Component / System / Tests / Presets`
- `Src/ECS/Tools/**`
- `Src/ECS/UI/**`
- `Src/ECS/Test/**`

不要把 `Src/ECS/Capabilities` 整体理解成 `Component` 分类；分类由 owner 内部目录层决定。

## 后续裁决

Resource loading 方向已在 `DocsAI/ECS/Tools/ResourceManagement/README.md` 和 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md` 中校准：

- 保留统一资源加载工具；`ResourceManagement` 不作为长期“资源管理器”概念保留，只作为迁移期薄转发。
- 保留 `ResourceCatalog` / `ResourceGenerator` 的轻量 catalog 价值，但不把它们当跨游戏全局资源身份系统。
- `Load<T>` 的 contains fallback 已删除。
- `LoadPath` 必须携带 source policy。
- 已增加 `ResourceLoadResult` / `ResourceCatalogDiagnostics`。
- `CommonTool.LoadPackedScene` 已迁入 ResourceLoading，`CommonTool` current 入口已删除。
- 未来框架仓与游戏仓分离后，框架 generator 不默认拥有游戏资源；游戏仓应生成自己的 resource catalog。
