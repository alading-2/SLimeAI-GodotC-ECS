# ResourceGenerator 资源路径生成器

**ResourceGenerator** 是一个 C# 控制台工具，用于自动扫描当前 Godot 项目中的资源文件（`.tscn` 和 `.tres`），并生成可检查的资源 catalog 类 `ResourcePaths`。

它的 AI-first 价值不是“自动替 AI 决定架构”，而是把分散在当前项目 `Runtime / Capabilities / Tools / UI / Test / assets` 中的 Godot 资源收敛成一个可检查 catalog。这样 AI 和运行时都不需要到处猜资源路径语义，也不会因为目录重构后把整个 `Capabilities` 误分类成 `Component`。

注意：`ResourceGenerator` 不是自动路径修复器。`ResourcePaths.cs` 中仍保存 Godot 项目相对资源路径；移动、重命名或删除资源后，必须先通过 project directory / resource-path migration workflow 迁移旧引用，再重新运行生成器并检查输出。后续 ResourceLoading hard cutover 会补 `ResourceCatalogDiagnostics`，把 duplicate key、missing path 和 stale generated source 作为可检查 artifact。

`res://` 本身不是问题；它是 Godot project root 路径。问题是旧路径移动后没有自动替换和残留检查。

`ResourceGenerator` 输出供当前 `ResourceManagement` 读取；执行型 hard cutover 后目标读取方是 `ResourceLoading`。不要因此把 `ResourceGenerator`、`ResourceCatalog` 和 runtime loader 合并成一个大 `ResourceManagement`。

## 📖 核心功能

- **自动化**: 遍历指定目录，自动提取资源名称与路径。
- **分类管理**: 根据扫描根、最长前缀规则和 Capability 内部分层自动分类（Entity, Component, System, Preset, UI, Tools, Asset 等）。
- **去重与优先级**:

  - 自动处理同名资源冲突。
  - **优先级策略**: 遇到同名资源时，优先保留 `.tscn` (场景/表现)，忽略 `.tres` (数据/配置)。
- **可检查 manifest**: 生成一个包含嵌套字典的 C# 类，供当前 `ResourceManagement` / 目标 `ResourceLoading`、`ResourceCatalog`、TestSystem 和视觉预览读取。

## 🛠️ 使用方法

### 运行生成器

在添加、删除或重命名资源文件后，运行以下命令。

如果是移动/重命名，先 dry-run 路径替换：

```bash
python3 .ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py \
  --old "res://assets/Old" \
  --new "res://assets/New"
```

如果当前目录是游戏仓，使用框架仓脚本绝对路径并限制 `--root .`：

```bash
python3 /home/slime/Code/SlimeAI/SlimeAI/.ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py \
  --root . \
  --old "res://assets/Old" \
  --new "res://assets/New"
```

确认后加 `--apply`，再运行生成器：

```bash
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
```

### 查看生成文件

生成的文件位于 `Data/ResourceManagement/ResourcePaths.cs`。

当前输出是框架仓 catalog。未来游戏仓分离后，游戏仓资源应在游戏仓根生成自己的 catalog，或由 generator 支持 `--project-root` / `--scan-root` / `--output` 参数输出到游戏仓。

如果同一次目录移动同时留下 `res://`、项目相对路径和当前仓绝对路径引用，可先 dry-run：

```bash
python3 .ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py \
  --old "res://assets/Old" \
  --new "res://assets/New" \
  --include-variants
```

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
- **运行时加载**: 业务代码不要直接 `GD.Load("res://...")`，当前统一走 `ResourceManagement.Load<T>`，目标统一走 `ResourceLoading.Load<T>`。
- **何时运行**: 添加、移动、删除或重命名 `.tscn` / `.tres` 后运行生成器，并检查重复资源警告。
- **路径迁移**: 移动/重命名目录后先用 project directory / `resource-path-migration` skill 替换旧引用，再运行生成器；不要靠人工全仓搜索。
- **严格加载方向**: 后续 `ResourceLoading.Load<T>` 会删除相近名称 fallback；如果精确 key 不存在，应修正资源 key / manifest，而不是让加载器猜测。
- **DataOS 路径来源**: DataOS snapshot 中保存资源路径时，加载入口必须携带 source/owner/usage；不要把裸 `res://` 扩散到 Capability 业务代码。

## 多游戏仓注意事项

未来框架作为 submodule 放进游戏仓时：

```text
游戏仓根 = res://
框架 submodule = res://SlimeAI/
游戏资源 = res://assets/、res://Scenes/ 等游戏仓目录
```

框架仓的 `ResourcePaths.cs` 只应表达框架拥有的资源。游戏美术、玩法场景、游戏 DataOS resource refs 应由游戏仓自己的 catalog 或 game-local 输出负责。不要为了方便把游戏专属资源扫描结果写回框架仓。
