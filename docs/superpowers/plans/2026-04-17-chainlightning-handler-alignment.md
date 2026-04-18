# ChainLightning Handler Alignment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `ChainLightning` 从半迁移状态收回到当前项目稳定的 `AbilityFeatureHandler` 模式，并同步对齐链式 DataKey、配置和项目索引。

**Architecture:** 保持 `AbilitySystem` 现有职责边界不变，让 `ChainLightning` 在 `ExecuteAbility` 内完成首目标选择，再复用现有链式弹跳逻辑处理后续命中。链式专属配置全部走 `DataMeta` + `[DataKey]` 声明，避免继续混用旧字符串键。

**Tech Stack:** Godot 4.6, C# / .NET 8, 项目自定义 ECS / AbilitySystem / DataRegistry

---

### Task 1: 对齐 ChainLightning 处理器

**Files:**
- Modify: `Data/Data/Ability/Ability/ChainLightning/ChainLightning.cs`
- Check: `Src/ECS/Base/System/AbilitySystem/AbilityFeatureHandler.cs`

- [ ] **Step 1: 写出目标状态草案**

```csharp
internal class ChainLightningExecutor : AbilityFeatureHandler
{
    public override string FeatureId => global::FeatureId.Ability.Active.ChainLightning;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var firstTarget = FindInitialTarget(caster, ability);
        if (firstTarget == null)
        {
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        context.Targets = new List<IEntity> { firstTarget };
        return new AbilityExecutedResult { TargetsHit = 1 };
    }
}
```

- [ ] **Step 2: 恢复首目标查询方法**

```csharp
private static IEntity? FindInitialTarget(IEntity caster, AbilityEntity ability)
{
    if (caster is not Node2D casterNode) return null;

    float castRange = ability.Data.Get<float>(DataKey.AbilityCastRange); //索敌半径
    if (castRange <= 0f) return null;

    var targets = EntityTargetSelector.Query(new TargetSelectorQuery
    {
        Geometry = GeometryType.Circle, //查询形状
        Origin = casterNode.GlobalPosition, //查询中心
        Range = castRange, //查询半径
        CenterEntity = caster, //中心实体
        TeamFilter = ability.Data.Get<AbilityTargetTeamFilter>(DataKey.AbilityTargetTeamFilter), //阵营过滤
        Sorting = ability.Data.Get<TargetSorting>(DataKey.TargetSorting), //排序方式
        MaxTargets = 1 //最大目标数
    });

    return targets.Count > 0 ? targets[0] : null;
}
```

- [ ] **Step 3: 清理半迁移残留**

```csharp
// 删除 FeatureGroup 覆写
// 删除对外部预填 context.Targets[0] 的强依赖
// 保留 ExecuteBounce / FindNextChainTarget 的现有链式逻辑
```

- [ ] **Step 4: 运行定向检查**

Run: `dotnet build`
Expected: BUILD SUCCEEDED，`ChainLightning.cs` 无基类或 DataKey 编译错误

### Task 2: 对齐链式 DataKey 与配置

**Files:**
- Modify: `Data/Data/Ability/Ability/ChainLightning/Data/DataKey_Chain.cs`
- Modify: `Data/Data/Ability/Ability/ChainLightning/Data/ChainAbilityConfig.cs`

- [ ] **Step 1: 将 LineEffect 键升级为 DataMeta**

```csharp
public static readonly DataMeta AbilityChainLineEffect = DataRegistry.Register(
    new DataMeta
    {
        Key = nameof(AbilityChainLineEffect),
        DisplayName = "链式连线特效",
        Description = "链式技能命中之间播放的连线特效场景",
        Category = DataCategory_Ability.Visual,
        Type = typeof(PackedScene),
        DefaultValue = null
    });
```

- [ ] **Step 2: 保持 ChainAbilityConfig 全部走统一声明方式**

```csharp
[DataKey(nameof(DataKey.AbilityChainLineEffect))]
[Export] public PackedScene? LineEffectScene { get; set; } = (PackedScene?)DataKey.AbilityChainLineEffect.DefaultValue;
```

- [ ] **Step 3: 复查其它链式字段默认值**

```csharp
[Export] public int ChainCount { get; set; } = (int)DataKey.AbilityChainCount.DefaultValue!;
[Export] public float ChainRange { get; set; } = (float)DataKey.AbilityChainRange.DefaultValue!;
[Export] public float ChainDelay { get; set; } = (float)DataKey.AbilityChainDelay.DefaultValue!;
[Export] public float ChainDamageDecay { get; set; } = (float)DataKey.AbilityChainDamageDecay.DefaultValue!;
```

- [ ] **Step 4: 运行编译验证**

Run: `dotnet build`
Expected: BUILD SUCCEEDED，`PackedScene` 类型 DataMeta 与导出配置编译通过

### Task 3: 更新索引文档

**Files:**
- Modify: `Docs/框架/项目索引.md`

- [ ] **Step 1: 补充 AbilitySystem / ChainLightning 的现行说明**

```markdown
- `ChainLightning`：主动链式技能示例，沿用 `AbilityFeatureHandler` 模式，在 `ExecuteAbility` 内完成第一跳索敌，后续通过延迟弹跳继续结算。
- `ChainAbilityConfig` / `DataKey_Chain`：链式技能模板配置，现已统一接入 `[DataKey(nameof(DataKey.*))]` + `DataMeta.DefaultValue` 规范。
```

- [ ] **Step 2: 确认文档表述与代码一致**

Run: `rg -n "ChainLightning|ChainAbilityConfig|DataKey_Chain" Docs/框架/项目索引.md`
Expected: 能找到新增索引项，且描述与最终实现一致

### Task 4: 最终验证

**Files:**
- Check: `Data/Data/Ability/Ability/ChainLightning/ChainLightning.cs`
- Check: `Data/Data/Ability/Ability/ChainLightning/Data/DataKey_Chain.cs`
- Check: `Data/Data/Ability/Ability/ChainLightning/Data/ChainAbilityConfig.cs`
- Check: `Docs/框架/项目索引.md`

- [ ] **Step 1: 执行整体验证**

Run: `dotnet build`
Expected: BUILD SUCCEEDED

- [ ] **Step 2: 检查最终差异**

Run: `git diff -- Data/Data/Ability/Ability/ChainLightning/ChainLightning.cs Data/Data/Ability/Ability/ChainLightning/Data/DataKey_Chain.cs Data/Data/Ability/Ability/ChainLightning/Data/ChainAbilityConfig.cs Docs/框架/项目索引.md`
Expected: 只包含本次对齐修改，没有额外回退无关代码
