# ResourceGenerator 资源路径生成器

**ResourceGenerator** 是一个 C# 控制台工具，用于自动扫描项目中的资源文件（`.tscn` 和 `.tres`），并生成可检查的路径索引类 `ResourcePaths`。

它的 AI-first 价值不是“自动替 AI 决定架构”，而是把分散在 `Runtime / Capabilities / Tools / UI / Test / assets` 中的 Godot 资源收敛成一个稳定 manifest。这样 AI 和运行时都不需要到处猜 `res://` 路径，也不会因为目录重构后把整个 `Capabilities` 误分类成 `Component`。

## 📖 核心功能

- **自动化**: 遍历指定目录，自动提取资源名称与路径。
- **分类管理**: 根据扫描根、最长前缀规则和 Capability 内部分层自动分类（Entity, Component, System, Preset, UI, Tools, Asset 等）。
- **去重与优先级**:

  - 自动处理同名资源冲突。
  - **优先级策略**: 遇到同名资源时，优先保留 `.tscn` (场景/表现)，忽略 `.tres` (数据/配置)。
- **可检查 manifest**: 生成一个包含嵌套字典的 C# 类，供 `ResourceManagement`、`ResourceCatalog`、TestSystem 和视觉预览读取。

## 🛠️ 使用方法

### 运行生成器

在添加、删除或重命名资源文件后，运行以下命令：

```bash
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
```

### 查看生成文件

生成的文件位于 `Data/ResourceManagement/ResourcePaths.cs`。

## ⚙️ 配置说明

在 `ResourceGenerator.cs` 中可以配置以下参数：

### 扫描根 (`ScanRoots`)

扫描根只表达“去哪里找资源”，不直接表达分类：

- `assets/Effect`
- `assets/Unit`
- `assets/Unit/Player`
- `assets/Unit/Enemy`
- `assets/Projectile`
- `Src/ECS/Runtime`
- `Src/ECS/Capabilities`
- `Src/ECS/Tools`
- `Src/ECS/UI`
- `Src/ECS/Test`

### 排除路径 (`ExcludePaths`)

定义生成器会跳过目录：

- `addons`
- `.godot`

## 📂 自动分类逻辑

1. **显式最长前缀匹配**:
   - `res://assets/Effect/` -> `AssetEffect`
   - `res://assets/Unit/Player/` -> `AssetUnitPlayer`
   - `res://assets/Unit/Enemy/` -> `AssetUnitEnemy`
   - `res://assets/Projectile/` -> `AssetProjectile`
   - `res://Src/ECS/Runtime/Entity/` -> `Entity`
   - `res://Src/ECS/Runtime/Component/` -> `Component`
   - `res://Src/ECS/Runtime/System/` -> `System`
   - `res://Src/ECS/Tools/` -> `Tools`
   - `res://Src/ECS/UI/` -> `UI`
   - `res://Src/ECS/Test/` -> `Test`
2. **Capability 内部分层**:
   - `res://Src/ECS/Capabilities/<Owner>/Entity/` -> `Entity`
   - `res://Src/ECS/Capabilities/<Owner>/Component/` -> `Component`
   - `res://Src/ECS/Capabilities/<Owner>/System/` -> `System`
   - `res://Src/ECS/Capabilities/<Owner>/Tests/` -> `Test`
   - `res://Src/ECS/Capabilities/<Owner>/Presets/` -> `Preset`
3. **兜底**: 其他 unrecognized 路径归类为 `Other`。

`Preset` 是独立分类。`AbilityPreset / EnemyPreset / PlayerPreset / UnitCorePreset` 这类资源用于组合默认节点结构，不应再混入 `Component`。

## ⚠️ 开发规范

- **命名规范**: 请保持同分类资源文件名称唯一。DataOS 配置数据不进入 `ResourcePaths`，通过 runtime snapshot 查询。
- **手动更改**: **不要手动修改 `ResourcePaths.cs`**，因为每次运行生成器都会覆盖该文件。
- **运行时加载**: 业务代码不要直接 `GD.Load("res://...")`，统一走 `ResourceManagement.Load<T>`。
- **何时运行**: 添加、移动、删除或重命名 `.tscn` / `.tres` 后运行生成器，并检查重复资源警告。
