# Math 用法

> status: current
> sourcePaths: Src/ECS/Tools/Math/
> relatedDocs: DocsAI/ECS/Tools/Math/Concept.md
> lastReviewed: 2026-06-07

## 概率

```csharp
var rng = DeterministicRandom.Create(1234);
bool triggered = ProbabilityTool.RollPercent(37.5f, rng);
```

概率值单位为百分比 0-100。越界值由 `ProbabilityTool` clamp：负数按 0%，超过 100 按 100%。

## 几何

```csharp
bool inCone = Geometry2D.IsPointInCone(targetPos, casterPos, facing, 300f, 60f);
Vector2 random = Geometry2D.GetRandomPointInCircle(casterPos, 150f, DeterministicRandom.Create(42));
```

- 纯算法请优先调 `Geometry2D`。
- `GeometryType`、`TargetSelectorQuery` 属于 `TargetSelector` 领域层，不下沉到 Math。
- `GeometryCalculator` 旧门面已删除。

## 曲线

```csharp
var curve = Parabola2D.Create(start, end, apexHeight);
Vector2 position = curve.Evaluate(t);
Vector2 tangent = curve.EvaluateTangent(t);
```

- `BezierCurve` 是纯数学核心。
- `BezierTemplateBuilder` 带 Movement / Ability 曲线弹模板语义；新增技能模板时要确认是否应放在 capability owner，而不是继续扩大 Math。

## 公式 owner

- 护甲 / 伤害倍率公式：`DamageFormula`
- 冷却 / 充能公式：`AbilityFormula`
- Spawn 随机位置：`SpawnPositionCalculator` 调用 `Geometry2D`，可传 `RandomNumberGenerator`
- TargetSelector 随机位置：`TargetQueryEngine.QueryPositions`，通过 `RandomSeed` 或 `RandomSource` 控制复现
