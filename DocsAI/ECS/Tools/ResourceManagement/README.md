# ResourceLoading

> 状态：current
> sourcePaths: `Src/ECS/Tools/ResourceLoading/`, `Data/ResourceManagement/`, `Tools/ResourceGenerator/`
> relatedDesign: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md`
> lastReviewed: 2026-06-07

## 一句话定位

`ResourceLoading` 是 SlimeAI 的极薄资源加载 facade：strict key/category lookup、`LoadPath` source/owner/usage diagnostics、结构化 `ResourceLoadResult`。`ResourceGenerator` 生成当前项目的 `ResourcePaths` catalog；`ResourceCatalog` 给 UI、TestSystem 和 AI 提供可检查资源目录。

它不是 Godot 资源系统替代品，也不是自动路径修复器。底层仍由 Godot `ResourceLoader` / `GD.Load` 加载 `res://` 或未来验证后的 `uid://` 资源引用。

`res://` 本身不是问题。它是 Godot project root 路径，`.tscn/.tres`、C# `ResourceLoader`、场景 runner、游戏仓 submodule 路径都会正常使用它。SlimeAI 禁止的是业务 Capability 绕过统一 owner 直接裸加载路径。

## 为什么保留加载工具

SlimeAI 需要一个 AI 可读、可检查的资源加载入口：

- 业务代码不应散落 `GD.Load("res://...")`。
- AI 不应全仓猜资源路径语义。
- DataOS snapshot 可以保存资源引用，但加载时必须通过统一 owner。
- UI / TestSystem / 视觉预览需要稳定目录，而不是运行时全盘扫描 `res://`。
- 资源路径移动后，应该有工具替换引用和 diagnostics 检查残留，而不是人工全仓搜索。

其他引擎通常用 GUID、Address、Soft Reference 或 Asset Manager 解决资源身份和加载问题；SlimeAI 当前采用轻量 facade + generated catalog + diagnostics，是合理折中。

但这不代表要保留 `ResourceManagement` 作为“资源管理器”概念。当前类代码量很小，真正需要的是统一加载工具：

```text
保留：ResourceLoading.Load / TryLoad / LoadPath(source, owner, usage)
保留：ResourceCatalog / ResourceGenerator 作为旁路 catalog 和 diagnostics 输入
不保留：ResourceManagement 作为目录移动、资源身份、catalog 合并和 runtime loader 的大管理器
```

## 当前边界

| 模块 | 职责 | 不做 |
| --- | --- | --- |
| `ResourceGenerator` | 扫描当前项目 `.tscn` / `.tres`，按规则生成 `ResourcePaths.cs` | 不自动迁移 DataOS resource ref；不默认拥有外部游戏仓资源 |
| `ResourcePaths` | 记录 category、key、path 的 generated catalog | 不代表真实资源生命周期；不作为跨游戏全局资源身份 |
| `ResourceLoading` | 统一加载入口、source/owner/usage diagnostics、错误报告 | 不让 Capability 直接绕过 facade 加载资源；不做目录移动或跨游戏资源管理 |
| `ResourceCatalog` | 为 UI/Test/AI 构建资源目录和分组 | 不进入 gameplay 热路径全量刷新 |
| project directory / `resource-path-migration` skill | 新增、删除、重命名、移动或检查目录后的路径替换和旧路径残留检查 | 不替代 Godot 资源系统；不默认跨 git boundary 改文件 |

## 设计裁决

当前统一加载方向正确，但目标不是“移动目录后永远不会报错”。因为 `ResourcePaths.cs` 仍保存 Godot 项目相对资源路径，移动资源后必须替换引用、重新运行生成器并检查 diagnostics。

后续 Resource Loading hard cutover 的默认方向：

- public current API 是 `ResourceLoading`；旧 `ResourceManagement` 只作为迁移期薄转发，不作为文档入口。
- `Load<T>(key, category)` strict lookup，精确 key 不存在就失败。
- `Contains` fallback 已删除，避免静默加载相近资源。
- `LoadPath<T>` 只给 DataOS resource ref、debug/test 或明确来源使用，并必须携带 source/owner/usage。
- `ResourceLoadResult` 失败会说明 category、key/path、source、owner 和 error code。
- `ResourceCatalogDiagnostics` 覆盖 duplicate key、missing path、stale generated source、DataOS selected refs loadable。
- `CommonTool.LoadPackedScene` 已迁入 ResourceLoading；`CommonTool` current 入口已删除。
- 目录 / 路径移动使用 project directory / `resource-path-migration` skill 做 dry-run、替换和 `rg` 残留检查。

## 多游戏仓边界

未来游戏仓和框架仓分离后：

```text
游戏仓根 = project.godot 所在 = res://
框架 submodule = res://SlimeAI/
游戏资源 = res://assets/、res://Scenes/、res://Src/Game/ 等游戏仓目录
```

框架仓只能拥有框架资源 catalog；游戏资源 catalog 应由游戏仓生成和验证。后续 `ResourceGenerator` 应支持 `--project-root` / `--scan-root` / `--output` 一类参数，在游戏仓根生成游戏自己的 catalog，或生成可被框架 `ResourceLoading` 消费的 game-local catalog。

不要把 BrotatoLike 或其他游戏专属资源写入框架仓 `Data/ResourceManagement/ResourcePaths.cs` 作为长期事实源。

## `uid://` 口径

Godot 的 `ResourceUID` / `uid://` 和 `.cs.uid` 文件同属 UID 体系，但不是同一个使用层。

- `.cs.uid` 是 Godot 生成的脚本资源 UID metadata sidecar，本仓 `.gitignore` 已忽略。
- `uid://` 是 Resource UID 的文本引用形式，可帮助资源移动或重命名后保持引用关系。

`uid://` 对资源移动有意义，但当前不作为 DataOS 或 `ResourcePaths` 主存储。后续需要单独验证 C# 加载、snapshot JSON、导出包、Godot 版本差异和 submodule 游戏仓链路。

## 使用规则

推荐：

```csharp
var scene = ResourceLoading.Load<PackedScene>(
    ResourcePaths.Entity_PlayerEntity,
    ResourceCategory.Entity);
```

允许但必须受 source policy 约束：

```csharp
// DataOS snapshot 中保存资源路径时，后续应通过带 source 的 LoadPath 入口加载。
var scene = ResourceLoading.LoadPath<PackedScene>(
    path,
    ResourceLoadSource.DataOS("unit.player/player", "VisualScenePath"));
```

禁止：

```csharp
GD.Load<PackedScene>("res://Src/ECS/Capabilities/Unit/Entity/Player/PlayerEntity.tscn");
ResourceLoader.Load<PackedScene>("res://...");
```

例外：

- `Tools/ResourceGenerator` 生成 catalog 时可以构造 `res://`。
- `.tscn` / `.tres` 内部 `ext_resource path="res://..."` 是 Godot 场景格式事实，不按 C# 业务加载入口处理。
- 验证 artifact 写入时使用 `ProjectSettings.GlobalizePath("res://")` 不属于资源加载。

## 验证入口

资源变更后运行：

```bash
python3 .ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py --old "<old>" --new "<new>"
python3 .ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py --old "<old>" --new "<new>" --include-variants
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

如果当前目录是游戏仓，使用框架仓脚本绝对路径并限制 `--root .`：

```bash
python3 /home/slime/Code/SlimeAI/SlimeAI/.ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py --root . --old "<old>" --new "<new>"
```

设计阶段检查：

```bash
rg -n "CommonTool\\.|GD\\.Load|ResourceLoader\\.Load|res://" Src/ECS Data DocsAI/ECS
python3 Workspace/SDD/sdd.py validate --all
```

后续 hard cutover gate 示例：

```bash
rg -n "LegacyContainsFallback|Contains\\(name, StringComparison\\.OrdinalIgnoreCase\\)" Data/ResourceManagement Src/ECS
rg -n "CommonTool\\." Src/ECS Data DocsAI/ECS
```

## 待确认

- DataOS resource ref 是否从 `res://` 迁到 `ResourceKey + Category` 或 `uid://`。
- Godot `uid://` 是否进入下一阶段验证。
- `ResourceCatalogDiagnostics` 优先做 CLI artifact，还是同时接入 TestSystem UI。
- 游戏仓分离后 `GameResourceCatalog` 的输出路径和合并方式。
