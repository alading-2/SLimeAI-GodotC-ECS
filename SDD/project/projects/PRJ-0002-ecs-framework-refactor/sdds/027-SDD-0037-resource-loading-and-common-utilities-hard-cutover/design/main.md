# Resource Loading And Common Utilities Hard Cutover

## Goal

完成资源加载和通用工具边界 hard cutover：

- `ResourceManagement` public 心智收敛为极薄 `ResourceLoading` facade。
- `Load<T>(key, category)` strict lookup，删除 contains fallback。
- `LoadPath` 必须携带 `ResourceLoadSource` / owner / usage。
- 加载失败返回 structured result 或 preflight diagnostics，不只靠 log。
- 新增 `ResourceCatalogDiagnostics`：duplicate key、missing path、stale generated source、DataOS selected refs loadable。
- `CommonTool.LoadPackedScene` 迁入 ResourceLoading；当前 `CommonTool` 删除或 internal 化。
- 建立 `Src/ECS/Tools/CommonUtilities/` + `DocsAI/ECS/Tools/CommonUtilities/` 的受约束 owner。

非目标：

- 不把 `res://` 当作错误格式。
- 不把 ResourceLoading 做成 Unity Addressables / Unreal Asset Manager。
- 不修改 DataOS resource ref schema，除非另有 SDD。
- 不让框架仓 ResourceGenerator 默认拥有游戏仓资源。

## Context

必须先读：

- `design/Tool/其他Tool/README.md`
- `design/Tool/其他Tool/01-现状证据与AI-first裁决.md`
- `design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md`
- `design/Tool/其他Tool/06-实施路线与验证门禁.md`
- `DocsAI/ECS/Tools/ResourceManagement/README.md`
- `design/resource-path-migration-boundary.md`
- `Data/ResourceManagement/**`
- `Tools/ResourceGenerator/**`
- `Src/ECS/Tools/CommonTool.cs`

用户已确认：

- Common 工具放在 `Src/ECS/Tools`，但应是 `CommonUtilities/` 受约束目录，不是根目录杂物箱。
- `ResourceManagement` 可以大量简化，但仍需要统一资源加载工具。
- 资源加载改 strict fail-fast。
- 路径移动由 workflow + generator + diagnostics 处理。

## Design

目标目录和类型：

```text
Data/ResourceManagement/
  ResourceLoading.cs
  ResourceLoadRequest.cs
  ResourceLoadResult.cs
  ResourceLoadSource.cs
  ResourceCatalogDiagnostics.cs

Src/ECS/Tools/CommonUtilities/
  README / manifest-backed helpers only
```

目标 API 形态：

```text
ResourceLoading.Load<T>(ResourceKey key, ResourceCategory category, ResourceLoadSource source)
ResourceLoading.LoadPath<T>(string path, ResourceLoadSource source)
ResourceLoading.TryLoad<T>(ResourceLoadRequest request, out ResourceLoadResult<T> result)
ResourceCatalogDiagnostics.Run()
```

hard cutover 口径：

- 删除 `Load<T>` contains fallback。
- `LegacyContainsFallback` 不允许作为最终行为。
- `CommonTool.LoadPackedScene` 不作为 current API。
- 能归入明确 owner 的 helper 不允许进入 Common Utilities。
- 资源移动必须走 `resource-path-migration` 或等价脚本 + ResourceGenerator + `rg` 残留分类。

## Verification

基础验证：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
python3 Workspace/SDD/sdd.py validate SDD-0037
```

grep gate：

```bash
rg -n "CommonTool\\.|GD\\.Load|ResourceLoader\\.Load" Src/ECS Data DocsAI/ECS
rg -n "LegacyContainsFallback|Contains\\(name, StringComparison\\.OrdinalIgnoreCase\\)" Data/ResourceManagement Src/ECS
```

resource path workflow smoke：

```bash
python3 .ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py --old "<old>" --new "<new>"
```
