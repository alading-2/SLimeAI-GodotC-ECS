# Visual Preview Scene Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move Asset visual preview out of `TestSystem` into an independent `GlobalTest` scene that spawns selectable preview entities for every `ResourcePaths` `Asset*` category.

**Architecture:** Add a dedicated `VisualPreviewEntity` shell plus a standalone `VisualPreviewScene`. The scene scans `ResourcePaths.Resources`, spawns one lightweight entity per Asset resource, injects the visual scene through `EntityManager`, drives animations through `UnitAnimationComponent`, and consumes `MouseSelectionSystem` events for selection details.

**Tech Stack:** Godot 4.6 C#, .NET 8.0, project pseudo-ECS, `EntityManager`, `UnitAnimationComponent`, `MouseSelectionSystem`, `ResourceManagement`, `ResourcePaths`.

---

## File Structure

- Create: `Data/DataKey/Test/DataCategory_Test.cs`
- Create: `Data/DataKey/Test/DataKey_TestVisualPreview.cs`
- Create: `Data/Data/Test/VisualPreviewEntityConfig.cs`
- Create: `Src/ECS/Base/Entity/Preview/VisualPreviewEntity.cs`
- Create: `Src/ECS/Base/Entity/Preview/VisualPreviewEntity.tscn`
- Modify: `Src/ECS/Base/Component/Unit/Common/UnitAnimationComponent/UnitAnimationComponent.cs`
- Create: `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewEntry.cs`
- Create: `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewCatalogService.cs`
- Create: `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewWorldController.cs`
- Create: `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.cs`
- Create: `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn`
- Modify: `Data/ResourceManagement/ResourcePaths.cs` by running `Tools/ResourceGenerator`
- Modify: `Docs/框架/项目索引.md`
- Modify: `Docs/框架/ECS/System/TestSystem/TestSystem.md`
- Modify: `.codex/skills/test-system/SKILL.md`

---

### Task 1: Preview Data Contract And Animation Drive Switch

**Files:**
- Create: `Data/DataKey/Test/DataCategory_Test.cs`
- Create: `Data/DataKey/Test/DataKey_TestVisualPreview.cs`
- Create: `Data/Data/Test/VisualPreviewEntityConfig.cs`
- Modify: `Src/ECS/Base/Component/Unit/Common/UnitAnimationComponent/UnitAnimationComponent.cs`

- [ ] **Step 1: Add test data category**

Create `Data/DataKey/Test/DataCategory_Test.cs`:

```csharp
/// <summary>
/// 测试工具数据分类。
/// </summary>
public enum DataCategory_Test
{
    /// <summary>视觉预览。</summary>
    VisualPreview,
}
```

- [ ] **Step 2: Add preview DataKey definitions**

Create `Data/DataKey/Test/DataKey_TestVisualPreview.cs`:

```csharp
/// <summary>
/// 视觉预览测试数据键。
/// </summary>
public static partial class DataKey
{
    public static readonly DataMeta AnimationAutoDriveEnabled = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(AnimationAutoDriveEnabled),
            DisplayName = "动画自动驱动",
            Description = "是否允许 UnitAnimationComponent 根据实体运动状态自动切换 idle/run。",
            Category = DataCategory_Test.VisualPreview,
            Type = typeof(bool),
            DefaultValue = true
        });

    public static readonly DataMeta PreviewResourceKey = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewResourceKey), DisplayName = "预览资源键", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });

    public static readonly DataMeta PreviewResourcePath = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewResourcePath), DisplayName = "预览资源路径", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });

    public static readonly DataMeta PreviewResourceCategory = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewResourceCategory), DisplayName = "预览资源分类", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });

    public static readonly DataMeta PreviewCatalogPath = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewCatalogPath), DisplayName = "预览目录分类", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });

    public static readonly DataMeta PreviewDefaultAnimation = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewDefaultAnimation), DisplayName = "预览默认动作", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });

    public static readonly DataMeta PreviewCurrentAnimation = DataRegistry.Register(
        new DataMeta { Key = nameof(PreviewCurrentAnimation), DisplayName = "预览当前动作", Category = DataCategory_Test.VisualPreview, Type = typeof(string), DefaultValue = string.Empty });
}
```

- [ ] **Step 3: Add runtime config resource used by EntityManager.Spawn**

Create `Data/Data/Test/VisualPreviewEntityConfig.cs`:

```csharp
using Godot;

namespace Slime.Config.Test
{
    /// <summary>
    /// 视觉预览实体运行时配置。
    /// <para>由预览场景代码临时创建，不需要落成 .tres。</para>
    /// </summary>
    public partial class VisualPreviewEntityConfig : Resource
    {
        [DataKey(nameof(DataKey.Name))]
        [Export] public string Name { get; set; } = string.Empty;

        [DataKey(nameof(DataKey.Team))]
        [Export] public Team Team { get; set; } = Team.Neutral;

        [DataKey(nameof(DataKey.EntityType))]
        [Export] public EntityType EntityType { get; set; } = EntityType.Unit;

        [DataKey(nameof(DataKey.AnimationAutoDriveEnabled))]
        [Export] public bool AnimationAutoDriveEnabled { get; set; } = false;

        [DataKey(nameof(DataKey.PreviewDefaultAnimation))]
        [Export] public string PreviewDefaultAnimation { get; set; } = string.Empty;

        [DataKey(nameof(DataKey.VisualScenePath))]
        [Export] public PackedScene? VisualScenePath { get; set; }
    }
}
```

- [ ] **Step 4: Modify UnitAnimationComponent to honor AnimationAutoDriveEnabled**

In `UnitAnimationComponent._Process`, add the auto-drive check after null checks and before idle/run switching:

```csharp
public override void _Process(double delta)
{
    // Dead/Reviving 期间锁定动画，Alive 允许正常切换
    if (IsDeadOrReviving) return;
    if (_body == null || _sprite == null) return;

    if (!_data.Get<bool>(DataKey.AnimationAutoDriveEnabled))
    {
        return;
    }

    // 一次性动画（攻击/受击等）播放期间不打断，由 OnAnimationFinished 信号负责回退
    if (_playMode == AnimPlayMode.OneShot) return;

    bool isMoving = _body.Velocity.LengthSquared() > 1f;
    Play(isMoving ? Anim.Run : Anim.Idle);
}
```

Also guard `FindSprite` from null `VisualRoot`, because preview Projectile resources may not expose `AnimatedSprite2D`:

```csharp
var visualRoot = entity.GetNodeOrNull("VisualRoot");
if (visualRoot == null)
{
    _log.Warn($"[{entity.Name}] 未找到 VisualRoot，动画组件无效");
    return null;
}
```

- [ ] **Step 5: Run build and fix compile errors**

Run:

```bash
dotnet build
```

Expected: build succeeds, or only reports errors from exact new symbols if a namespace/import is missing. Fix missing `using Godot;` or namespace issues before moving on.

---

### Task 2: VisualPreviewEntity Shell

**Files:**
- Create: `Src/ECS/Base/Entity/Preview/VisualPreviewEntity.cs`
- Create: `Src/ECS/Base/Entity/Preview/VisualPreviewEntity.tscn`
- Modify: `Data/ResourceManagement/ResourcePaths.cs` by running generator

- [ ] **Step 1: Create entity script**

Create `Src/ECS/Base/Entity/Preview/VisualPreviewEntity.cs`:

```csharp
using Godot;

/// <summary>
/// 视觉预览实体。
/// <para>只作为可被鼠标选择的 Entity 壳，具体视觉由 VisualRoot 注入。</para>
/// </summary>
public partial class VisualPreviewEntity : Node2D, IEntity
{
    public Data Data { get; private set; }
    public EventBus Events { get; } = new EventBus();

    public VisualPreviewEntity()
    {
        Data = new Data(this);
    }
}
```

- [ ] **Step 2: Create entity scene**

Create `Src/ECS/Base/Entity/Preview/VisualPreviewEntity.tscn` with this structure:

```ini
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" path="res://Src/ECS/Base/Entity/Preview/VisualPreviewEntity.cs" id="1_preview_entity"]
[ext_resource type="PackedScene" path="res://Src/ECS/Base/Component/Unit/Common/UnitAnimationComponent/UnitAnimationComponent.tscn" id="2_animation"]

[node name="VisualPreviewEntity" type="Node2D"]
script = ExtResource("1_preview_entity")

[node name="Component" type="Node2D" parent="."]

[node name="UnitAnimationComponent" parent="Component" instance=ExtResource("2_animation")]
```

- [ ] **Step 3: Regenerate resource paths**

Run:

```bash
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
```

Expected:

```text
ResourcePaths.cs includes Entity_VisualPreviewEntity
```

- [ ] **Step 4: Build after ResourcePaths regeneration**

Run:

```bash
dotnet build
```

Expected: build succeeds.

---

### Task 3: Asset Catalog Service

**Files:**
- Create: `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewEntry.cs`
- Create: `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewCatalogService.cs`

- [ ] **Step 1: Create preview entry model**

Create `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewEntry.cs`:

```csharp
using System;

/// <summary>
/// 视觉预览资源条目。
/// </summary>
public readonly record struct VisualPreviewEntry(
    string ResourceKey, // ResourcePaths 中的资源键
    ResourceCategory Category, // ResourceManagement 加载分类
    string ResourcePath, // res:// 资源路径
    string SceneName, // 资源路径最后场景名
    string CatalogPath, // UI 分类路径
    string DefaultAnimation // 该资源的默认回退动作
)
{
    public bool SupportsAnimationHint =>
        Category != ResourceCategory.AssetProjectile;
}
```

- [ ] **Step 2: Create catalog service**

Create `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewCatalogService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 视觉预览资源目录服务。
/// <para>只以 ResourcePaths.Resources 为数据源，不扫描 res://。</para>
/// </summary>
internal sealed class VisualPreviewCatalogService
{
    public IReadOnlyList<VisualPreviewEntry> GetEntries()
    {
        var result = new List<VisualPreviewEntry>();
        foreach (var (category, resources) in ResourcePaths.Resources)
        {
            if (!IsAssetCategory(category))
            {
                continue;
            }

            foreach (var (resourceKey, data) in resources)
            {
                var sceneName = ResolveSceneName(data.Path);
                result.Add(new VisualPreviewEntry(
                    resourceKey, // ResourcePaths 资源键
                    data.Category, // 实际资源分类
                    data.Path, // res:// 路径
                    sceneName, // 场景名
                    ResolveCatalogPath(data.Category), // 左侧分类
                    ResolveDefaultAnimation(data.Category) // 默认动作
                ));
            }
        }

        return result
            .OrderBy(entry => entry.CatalogPath, StringComparer.Ordinal)
            .ThenBy(entry => entry.SceneName, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<string> GetCatalogPaths(IReadOnlyList<VisualPreviewEntry> entries)
    {
        return entries
            .Select(entry => entry.CatalogPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsAssetCategory(ResourceCategory category)
    {
        return category.ToString().StartsWith("Asset", StringComparison.Ordinal);
    }

    private static string ResolveSceneName(string resourcePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(resourcePath);
        return string.IsNullOrWhiteSpace(fileName) ? resourcePath : fileName;
    }

    private static string ResolveCatalogPath(ResourceCategory category)
    {
        return category.ToString();
    }

    private static string ResolveDefaultAnimation(ResourceCategory category)
    {
        var categoryName = category.ToString();
        if (categoryName.StartsWith("AssetUnit", StringComparison.Ordinal))
        {
            return Anim.Idle;
        }

        if (category == ResourceCategory.AssetEffect)
        {
            return Anim.Effect;
        }

        return string.Empty;
    }
}
```

- [ ] **Step 3: Build service**

Run:

```bash
dotnet build
```

Expected: build succeeds.

---

### Task 4: World Controller And Animation Fallback

**Files:**
- Create: `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewWorldController.cs`

- [ ] **Step 1: Create controller class**

Create `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewWorldController.cs`:

```csharp
using Godot;
using Slime.Config.Test;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 独立视觉预览世界控制器。
/// </summary>
internal sealed class VisualPreviewWorldController
{
    private const int ColumnCount = 5;
    private const float ColumnSpacing = 220f;
    private const float RowSpacing = 220f;

    private readonly List<VisualPreviewEntity> _entities = new();
    private readonly Dictionary<VisualPreviewEntity, VisualPreviewEntry> _entriesByEntity = new();

    public IReadOnlyList<VisualPreviewEntity> Entities => _entities;

    public void Rebuild(IReadOnlyList<VisualPreviewEntry> entries)
    {
        Clear();
        for (var i = 0; i < entries.Count; i++)
        {
            var entity = SpawnPreviewEntity(entries[i], ResolveGridPosition(i));
            if (entity == null)
            {
                continue;
            }

            _entities.Add(entity);
            _entriesByEntity[entity] = entries[i];
        }
    }

    public void Clear()
    {
        foreach (var entity in _entities)
        {
            if (GodotObject.IsInstanceValid(entity))
            {
                EntityManager.Destroy(entity);
            }
        }

        _entities.Clear();
        _entriesByEntity.Clear();
    }

    public bool TryGetEntry(IEntity? entity, out VisualPreviewEntry entry)
    {
        if (entity is VisualPreviewEntity previewEntity && _entriesByEntity.TryGetValue(previewEntity, out entry))
        {
            return true;
        }

        entry = default;
        return false;
    }

    public void ShowCatalog(string catalogPath)
    {
        foreach (var entity in _entities)
        {
            if (!_entriesByEntity.TryGetValue(entity, out var entry))
            {
                continue;
            }

            entity.Visible = string.Equals(entry.CatalogPath, catalogPath, StringComparison.Ordinal);
        }
    }

    public IReadOnlyList<string> GetAnimationUnion(string catalogPath)
    {
        var result = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entity in _entities)
        {
            if (!_entriesByEntity.TryGetValue(entity, out var entry)
                || !string.Equals(entry.CatalogPath, catalogPath, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var animationName in entity.Data.Get<List<string>>(DataKey.AvailableAnimations))
            {
                result.Add(animationName);
            }
        }

        return result.ToArray();
    }

    public void ApplyAnimation(string catalogPath, string animationName)
    {
        foreach (var entity in _entities)
        {
            if (!_entriesByEntity.TryGetValue(entity, out var entry)
                || !string.Equals(entry.CatalogPath, catalogPath, StringComparison.Ordinal))
            {
                continue;
            }

            var resolvedAnimation = ResolveAnimation(entity, entry, animationName);
            entity.Data.Set(DataKey.PreviewCurrentAnimation, resolvedAnimation);
            if (string.IsNullOrWhiteSpace(resolvedAnimation))
            {
                entity.Events.Emit(GameEventType.Unit.StopAnimationRequested, new GameEventType.Unit.StopAnimationRequestedEventData());
                continue;
            }

            entity.Events.Emit(
                GameEventType.Unit.PlayAnimationRequested,
                new GameEventType.Unit.PlayAnimationRequestedEventData(
                    resolvedAnimation, // 动作名
                    true, // 强制重播
                    -1f // 不限制播放时长
                )
            );
        }
    }

    private static VisualPreviewEntity? SpawnPreviewEntity(VisualPreviewEntry entry, Vector2 position)
    {
        var visualScene = ResourceManagement.Load<PackedScene>(
            entry.ResourceKey, // ResourcePaths 资源键
            entry.Category // 资源分类
        );
        if (visualScene == null)
        {
            return null;
        }

        var config = new VisualPreviewEntityConfig
        {
            Name = entry.SceneName,
            VisualScenePath = visualScene,
            PreviewDefaultAnimation = entry.DefaultAnimation,
            AnimationAutoDriveEnabled = false
        };

        var entity = EntityManager.Spawn<VisualPreviewEntity>(new EntitySpawnConfig
        {
            Config = config,
            UsingObjectPool = false,
            Position = position
        });
        if (entity == null)
        {
            return null;
        }

        entity.Data.Set(DataKey.PreviewResourceKey, entry.ResourceKey);
        entity.Data.Set(DataKey.PreviewResourcePath, entry.ResourcePath);
        entity.Data.Set(DataKey.PreviewResourceCategory, entry.Category.ToString());
        entity.Data.Set(DataKey.PreviewCatalogPath, entry.CatalogPath);
        entity.Data.Set(DataKey.PreviewDefaultAnimation, entry.DefaultAnimation);
        return entity;
    }

    private static string ResolveAnimation(VisualPreviewEntity entity, VisualPreviewEntry entry, string requestedAnimation)
    {
        var available = entity.Data.Get<List<string>>(DataKey.AvailableAnimations);
        if (!string.IsNullOrWhiteSpace(requestedAnimation) && available.Contains(requestedAnimation))
        {
            return requestedAnimation;
        }

        if (!string.IsNullOrWhiteSpace(entry.DefaultAnimation) && available.Contains(entry.DefaultAnimation))
        {
            return entry.DefaultAnimation;
        }

        return available.Count > 0 ? available[0] : string.Empty;
    }

    private static Vector2 ResolveGridPosition(int index)
    {
        var column = index % ColumnCount;
        var row = index / ColumnCount;
        var totalWidth = (ColumnCount - 1) * ColumnSpacing;
        return new Vector2(column * ColumnSpacing - totalWidth * 0.5f, row * RowSpacing);
    }
}
```

- [ ] **Step 2: Build controller**

Run:

```bash
dotnet build
```

Expected: build succeeds. If `Data.Get<List<string>>` fails because `AvailableAnimations` is not present for non-animated resources, add a helper that returns an empty list when the key is absent.

---

### Task 5: Standalone VisualPreviewScene UI And Selection

**Files:**
- Create: `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.cs`
- Create: `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn`

- [ ] **Step 1: Create scene script**

Create `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.cs`:

```csharp
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 独立视觉资源预览测试场景。
/// </summary>
public partial class VisualPreviewScene : Node2D
{
    private readonly VisualPreviewCatalogService _catalogService = new();
    private readonly VisualPreviewWorldController _world = new();
    private IReadOnlyList<VisualPreviewEntry> _entries = Array.Empty<VisualPreviewEntry>();

    private PanelContainer _panel = null!;
    private Button _togglePanelButton = null!;
    private OptionButton _catalogOption = null!;
    private OptionButton _animationOption = null!;
    private Button _refreshButton = null!;
    private Label _summaryLabel = null!;
    private Label _selectionLabel = null!;

    public override void _Ready()
    {
        CacheUi();
        BindUi();
        BindSelectionEvents();
        Reload();
    }

    public override void _ExitTree()
    {
        GlobalEventBus.Global.Off<GameEventType.Global.MouseSelectionCompletedEventData>(
            GameEventType.Global.MouseSelectionCompleted,
            OnMouseSelectionCompleted
        );
        GlobalEventBus.Global.Off<GameEventType.Global.MouseSelectionMissedEventData>(
            GameEventType.Global.MouseSelectionMissed,
            OnMouseSelectionMissed
        );
        _world.Clear();
    }

    private void CacheUi()
    {
        _panel = GetNode<PanelContainer>("UILayer/Panel");
        _togglePanelButton = GetNode<Button>("UILayer/TogglePanelButton");
        _catalogOption = GetNode<OptionButton>("UILayer/Panel/Margin/Layout/CatalogOption");
        _animationOption = GetNode<OptionButton>("UILayer/Panel/Margin/Layout/AnimationOption");
        _refreshButton = GetNode<Button>("UILayer/Panel/Margin/Layout/RefreshButton");
        _summaryLabel = GetNode<Label>("UILayer/Panel/Margin/Layout/SummaryLabel");
        _selectionLabel = GetNode<Label>("UILayer/Panel/Margin/Layout/SelectionLabel");
    }

    private void BindUi()
    {
        _togglePanelButton.Pressed += () => _panel.Visible = !_panel.Visible;
        _refreshButton.Pressed += Reload;
        _catalogOption.ItemSelected += _ => ApplyCatalogSelection();
        _animationOption.ItemSelected += _ => ApplyAnimationSelection();
    }

    private void BindSelectionEvents()
    {
        GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionCompletedEventData>(
            GameEventType.Global.MouseSelectionCompleted,
            OnMouseSelectionCompleted
        );
        GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionMissedEventData>(
            GameEventType.Global.MouseSelectionMissed,
            OnMouseSelectionMissed
        );
    }

    private void Reload()
    {
        _entries = _catalogService.GetEntries();
        _world.Rebuild(_entries);
        RebuildCatalogOptions();
        ApplyCatalogSelection();
    }

    private void RebuildCatalogOptions()
    {
        _catalogOption.Clear();
        foreach (var catalogPath in _catalogService.GetCatalogPaths(_entries))
        {
            var index = _catalogOption.ItemCount;
            _catalogOption.AddItem(catalogPath);
            _catalogOption.SetItemMetadata(index, catalogPath); // 保存分类路径
        }

        if (_catalogOption.ItemCount > 0)
        {
            _catalogOption.Select(0);
        }
    }

    private void RebuildAnimationOptions(string catalogPath)
    {
        _animationOption.Clear();
        foreach (var animationName in _world.GetAnimationUnion(catalogPath))
        {
            var index = _animationOption.ItemCount;
            _animationOption.AddItem(animationName);
            _animationOption.SetItemMetadata(index, animationName); // 保存动作名
        }

        if (_animationOption.ItemCount > 0)
        {
            _animationOption.Select(0);
        }
    }

    private void ApplyCatalogSelection()
    {
        var catalogPath = GetSelectedCatalogPath();
        _world.ShowCatalog(catalogPath);
        RebuildAnimationOptions(catalogPath);
        ApplyAnimationSelection();
        _summaryLabel.Text = $"分类：{catalogPath}\n资源数：{_entries.Count(entry => entry.CatalogPath == catalogPath)}";
    }

    private void ApplyAnimationSelection()
    {
        _world.ApplyAnimation(GetSelectedCatalogPath(), GetSelectedAnimationName());
    }

    private string GetSelectedCatalogPath()
    {
        var selected = _catalogOption.Selected;
        return selected < 0 ? string.Empty : _catalogOption.GetItemMetadata(selected).AsString();
    }

    private string GetSelectedAnimationName()
    {
        var selected = _animationOption.Selected;
        return selected < 0 ? string.Empty : _animationOption.GetItemMetadata(selected).AsString();
    }

    private void OnMouseSelectionCompleted(GameEventType.Global.MouseSelectionCompletedEventData evt)
    {
        if (!_world.TryGetEntry(evt.PrimaryEntity, out var entry))
        {
            return;
        }

        var entity = (VisualPreviewEntity)evt.PrimaryEntity!;
        _selectionLabel.Text =
            $"名称：{entity.Data.Get<string>(DataKey.Name)}\n" +
            $"资源键：{entry.ResourceKey}\n" +
            $"路径：{entry.ResourcePath}\n" +
            $"分类：{entry.Category}\n" +
            $"默认动作：{entity.Data.Get<string>(DataKey.PreviewDefaultAnimation)}\n" +
            $"当前动作：{entity.Data.Get<string>(DataKey.PreviewCurrentAnimation)}";
    }

    private void OnMouseSelectionMissed(GameEventType.Global.MouseSelectionMissedEventData evt)
    {
        _selectionLabel.Text = "未选择";
    }
}
```

- [ ] **Step 2: Create scene tree**

Create `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn` with this minimum structure:

```ini
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.cs" id="1_scene"]

[node name="VisualPreviewScene" type="Node2D"]
script = ExtResource("1_scene")

[node name="Camera2D" type="Camera2D" parent="."]
enabled = true
position = Vector2(0, 240)
zoom = Vector2(0.75, 0.75)

[node name="UILayer" type="CanvasLayer" parent="."]

[node name="TogglePanelButton" type="Button" parent="UILayer"]
offset_left = 16.0
offset_top = 16.0
offset_right = 120.0
offset_bottom = 48.0
text = "预览面板"

[node name="Panel" type="PanelContainer" parent="UILayer"]
offset_left = 16.0
offset_top = 56.0
offset_right = 420.0
offset_bottom = 520.0

[node name="Margin" type="MarginContainer" parent="UILayer/Panel"]

[node name="Layout" type="VBoxContainer" parent="UILayer/Panel/Margin"]

[node name="CatalogOption" type="OptionButton" parent="UILayer/Panel/Margin/Layout"]

[node name="AnimationOption" type="OptionButton" parent="UILayer/Panel/Margin/Layout"]

[node name="RefreshButton" type="Button" parent="UILayer/Panel/Margin/Layout"]
text = "刷新"

[node name="SummaryLabel" type="Label" parent="UILayer/Panel/Margin/Layout"]
text = "未加载"

[node name="SelectionLabel" type="Label" parent="UILayer/Panel/Margin/Layout"]
text = "未选择"
autowrap_mode = 3
```

- [ ] **Step 3: Regenerate ResourcePaths for scene**

Run:

```bash
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
```

Expected:

```text
ResourcePaths.cs includes Test_VisualPreviewScene and Entity_VisualPreviewEntity
```

- [ ] **Step 4: Build scene code**

Run:

```bash
dotnet build
```

Expected: build succeeds.

---

### Task 6: Remove TestSystem VisualPreview Registration And Update Docs

**Files:**
- Modify: `Src/ECS/Base/System/TestSystem/Core/TestModuleSceneRegistry.cs`
- Modify: `Data/ResourceManagement/ResourcePaths.cs` by running generator
- Modify: `Docs/框架/项目索引.md`
- Modify: `Docs/框架/ECS/System/TestSystem/TestSystem.md`
- Modify: `.codex/skills/test-system/SKILL.md`
- Optional delete after references are removed: `Src/ECS/Base/System/TestSystem/VisualPreview/AssetVisualPreviewModule.cs`
- Optional delete after references are removed: `Src/ECS/Base/System/TestSystem/VisualPreview/AssetVisualPreviewController.cs`
- Optional delete after references are removed: `Src/ECS/Base/System/TestSystem/VisualPreview/AssetVisualPreviewService.cs`
- Optional delete after references are removed: `Src/ECS/Base/System/TestSystem/VisualPreview/AssetVisualPreviewModule.tscn`

- [ ] **Step 1: Find VisualPreview TestSystem registration**

Run:

```bash
rg -n "AssetVisualPreview|视觉预览|VisualPreview" Src/ECS/Base/System/TestSystem Data/ResourceManagement Docs .codex/skills
```

Expected: find the old module registration and docs references.

- [ ] **Step 2: Remove old module from TestSystem registry**

In `TestModuleSceneRegistry`, remove the entry that loads `ResourcePaths.System_AssetVisualPreviewModule`.

Expected behavior: `TestSystem` no longer shows `资源.视觉预览`.

- [ ] **Step 3: Delete or quarantine old TestSystem VisualPreview files**

Prefer delete if no references remain:

```bash
rg -n "AssetVisualPreview" Src Data Docs .codex/skills
```

Expected before deletion: only files under `Src/ECS/Base/System/TestSystem/VisualPreview` remain.

Delete old files only after the search proves no active reference exists. Do not delete unrelated `ObjectPoolInfo` or `ResourceCatalog` modules.

- [ ] **Step 4: Update project docs**

Update `Docs/框架/项目索引.md` with a new section under tests/global tests:

```markdown
### 独立视觉预览场景（2026-04）

- 💻 [VisualPreviewScene.cs](../../Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.cs) - 独立运行的 Asset 视觉预览入口，按 `ResourcePaths.Resources` 中全部 `Asset*` 分类生成预览实体。
- 💻 [VisualPreviewEntity.cs](../../Src/ECS/Base/Entity/Preview/VisualPreviewEntity.cs) - 专用预览 Entity 壳，承载 `VisualRoot` 与 `UnitAnimationComponent`。
- 📝 默认动作规则：`AssetUnit* -> idle`，`AssetEffect -> Effect`，其他 AnimatedSprite2D 资源取第一个动作，Projectile 仅展示和选择。
```

Update `Docs/框架/ECS/System/TestSystem/TestSystem.md` and `.codex/skills/test-system/SKILL.md` to say visual preview has moved out of `TestSystem` into `Src/ECS/Test/GlobalTest/VisualPreview`.

- [ ] **Step 5: Regenerate ResourcePaths after deletion**

Run:

```bash
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
```

Expected:

```text
ResourcePaths.cs no longer includes System_AssetVisualPreviewModule if the old scene was deleted.
ResourcePaths.cs includes Entity_VisualPreviewEntity and Test_VisualPreviewScene.
```

- [ ] **Step 6: Build**

Run:

```bash
dotnet build
```

Expected: build succeeds.

---

### Task 7: Verification

**Files:**
- Verify: `Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn`
- Verify: `Data/ResourceManagement/ResourcePaths.cs`
- Verify: docs and skill updates

- [ ] **Step 1: Compile verification**

Run:

```bash
dotnet build
```

Expected:

```text
Build succeeded.
0 Error(s)
```

- [ ] **Step 2: Static reference verification**

Run:

```bash
rg -n "AssetVisualPreview|System_AssetVisualPreviewModule" Src Data Docs .codex/skills
```

Expected:

```text
No active TestSystem registration remains.
Only historical design docs may mention the old name.
```

- [ ] **Step 3: ResourcePaths verification**

Run:

```bash
rg -n "VisualPreviewEntity|VisualPreviewScene|AssetVisualPreviewModule" Data/ResourceManagement/ResourcePaths.cs
```

Expected:

```text
Entity_VisualPreviewEntity is present.
Test_VisualPreviewScene is present.
System_AssetVisualPreviewModule is absent if old module scene was deleted.
```

- [ ] **Step 4: Runtime manual verification in Godot**

Open and run:

```text
res://Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn
```

Expected:

```text
The left panel lists all ResourceCategory values whose name starts with Asset.
Selecting a category hides non-selected preview entities.
Unit assets default to idle.
Effect assets default to Effect.
Projectile assets are visible and selectable, but do not require animation controls.
Clicking a preview entity updates the details label with DataKey.Name, resource key, path, category, default animation, and current animation.
```

- [ ] **Step 5: Git diff review**

Run:

```bash
git diff --stat
git diff -- Src/ECS/Test/GlobalTest/VisualPreview Src/ECS/Base/Entity/Preview Src/ECS/Base/Component/Unit/Common/UnitAnimationComponent Data/DataKey Data/Data/Test Docs/框架 .codex/skills/test-system/SKILL.md
```

Expected: changes are limited to the new standalone preview flow, `UnitAnimationComponent` preview-mode support, generated resource paths, and required docs/skill updates.
