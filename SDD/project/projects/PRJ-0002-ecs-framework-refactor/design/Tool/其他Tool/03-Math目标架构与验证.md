# Math 目标架构与验证

> 更新：2026-06-03
> 状态：current design input
> 裁决：Math 工具必要且整体方向正确；后续重点不是重写，而是拆清 owner 边界、随机源、公式归属和 deterministic validation。

## 1. 当前证据

源码入口：

```text
Src/ECS/Tools/Math/MyMath.cs
Src/ECS/Tools/Math/WaveMath.cs
Src/ECS/Tools/Math/Bezier/
Src/ECS/Tools/Math/Curves/
Src/ECS/Tools/Math/Geometry/Geometry2D.cs
Src/ECS/Tools/Math/Tests/MyMathTest.cs
```

DocsAI 入口：

```text
DocsAI/ECS/Tools/Math/Concept.md
DocsAI/ECS/Tools/Math/Usage.md
DocsAI/ECS/Tools/Math/Concepts/通用数学工具重构执行方案.md
```

主要调用点：

- Damage：护甲、暴击、闪避、吸血概率。
- Ability：触发概率、冷却/充能缩减、抛物线/贝塞尔技能。
- Movement：Bezier、Parabola、CircularArc、Boomerang、SineWave 策略。
- Spawn：`Geometry2D` 随机采样。
- TargetSelector：`GeometryCalculator` 兼容门面转调 `Geometry2D`。

## 2. 是否必要

必要。

AI-first ECS 仍需要一个可复用数学层，原因是：

- 能避免 Movement、Ability、Spawn、TargetSelector 反复手写几何和曲线公式。
- 曲线、切线、弧长、范围判定是纯算法，适合集中测试。
- AI 修改 gameplay 时需要稳定公式事实源，而不是在每个 handler 里搜索散落数学。

但 Math 不是所有“算点东西”的 owner。它只负责可复用公式，不负责目标选择、AI 决策、伤害流程、Data 初始化或物理碰撞。

## 3. 当前优点

- `WaveMath` 已把正弦波采样、导数、速度和切线封装为语义 API。
- `BezierCurve` 有 Evaluate、Derivative、Tangent、Curvature、Split、ApproximateLength、FindClosestT 等通用能力。
- `Curves` 已有 `EllipseArc2D`、`Parabola2D`、`CircularArc2D`，Movement 策略可以直接采样。
- `Geometry2D` 已把 Circle/Ring/Box/Capsule/Cone 判定与随机采样下沉到纯几何层。
- `MyMathTest` 已覆盖概率、曲线端点、方向、无效半径、Bezier 模板侧偏上限等关键边界。

## 4. 当前缺陷

| 缺陷 | 证据 | 风险 |
| --- | --- | --- |
| `MyMath` 混合公式类型 | 属性倍率、冷却缩减、护甲减伤、概率都在同一类。 | Damage/Ability/Random owner 不清。 |
| 概率使用 `GD.Randf()` | `CheckProbability(float chance)` 无 RNG 参数。 | 测试和 replay 难复现。 |
| 护甲公式依赖 `GlobalConfig` | `CalculateArmorDamageMultiplier` 内部读取全局配置。 | Math 不再纯粹，公式变更来源不清。 |
| 负护甲注释显示旧逻辑残留 | 注释保留“原逻辑”和备选方案。 | AI 可能复制错误公式或误判裁决。 |
| `Geometry2D` ownership 文档有重叠 | Math 文档和 TargetSelector 文档都提 Geometry2D。 | AI 可能不知道几何算法归 Math，查询语义归 TargetSelector。 |
| `BezierTemplateBuilder` 有玩法语义 | `RearWrap`、`SideSweep`、`SWeave`、`Converge` 偏技能模板。 | 它是运动/技能模板，不是通用贝塞尔数学核心。 |
| 统计概率测试有随机波动 | 10000 次 50% 概率测试用不可注入随机源。 | 极低概率但仍可能 flaky。 |

## 5. 目标边界

### 5.1 推荐分层

| 子域 | 目标 owner | 说明 |
| --- | --- | --- |
| `Geometry2D` | Math | 纯空间几何和随机采样算法，不接触 `IEntity`。 |
| `BezierCurve` | Math | 通用贝塞尔评估、导数、分割、弧长。 |
| `BezierTemplateBuilder` | Movement/Ability shared helper 或 Math.Bezier.Template | 带玩法语义的模板生成器，文档必须说明不是通用数学核心。 |
| `WaveMath` | Math | 周期波、正弦轨迹和切线。 |
| `Curves` | Math | 轻量曲线结构和采样。 |
| `FormulaMath` | 待确认 | 冷却、属性、护甲公式可保留在 Math，但必须标明 Data/Config 输入来源。 |
| `Probability` | 待确认 | 概率判定需要可注入 RNG，可能单独成 helper。 |

### 5.2 Geometry ownership

裁决：

```text
Geometry2D 属于 Math。
GeometryType / TargetSelectorQuery / EntityTargetSelector 属于 TargetSelector。
GeometryCalculator 只是 TargetSelector 的兼容门面。
```

因此后续文档应避免说“TargetSelector 拥有底层几何算法”。更准确说法是：TargetSelector 使用 Math/Geometry2D，并保留 GeometryCalculator 兼容入口。

### 5.3 RNG policy

后续概率和随机采样统一遵循：

- Gameplay 随机默认可由 caller 传入 `RandomNumberGenerator` 或框架 RNG service。
- Debug/Test 必须能传 seed。
- `GD.Randf()` 只能作为显式 fallback，不作为验证测试默认路径。
- Position/Target random sorting 不在每次 query 内部新建不可控 `Random()`。

## 6. 推荐路线

### Phase 1：文档和测试 hardening

- 更新 DocsAI/Math，明确 Geometry2D ownership。
- 在测试里使用固定 seed 或可注入 RNG。
- 给 `CheckProbability` 增加 deterministic overload。
- 给护甲/冷却/属性公式补边界表：输入、输出、单位、范围、来源。

### Phase 2：公式 owner 收口

- 判断 `MyMath` 是否拆为：
  - `AttributeFormula`
  - `CooldownFormula`
  - `DamageFormula`
  - `ProbabilityTool`
- 如果不拆，也必须在 `MyMath` 文档中说明每个公式的 capability owner 和验证入口。

### Phase 3：Bezier 模板语义收口

- 保留 `BezierCurve` 作为纯数学核心。
- 将 `BezierTemplateBuilder` 文档标注为 Movement/Ability 曲线弹模板 helper。
- 如果后续有更多技能模板，考虑移动到 `Capabilities/Movement/System/Strategies/Curve` 或 `Capabilities/Ability` shared helper。

## 7. Not Recommended

- 不建议引入第三方数学库替代 Godot `Vector2`。
- 不建议把 `GeometryType` 和 `TargetSelectorQuery` 下沉到 Math。
- 不建议让 Math 直接读取 Entity.Data。
- 不建议让 Math 调用 EventBus、EntityManager、ObjectPool 或 Godot SceneTree。
- 不建议继续在测试里依赖不可控随机统计。

## 8. 验证门禁

实现阶段建议：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

Godot 场景验证：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/Math/Tests/MyMathTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

Grep gate：

```bash
rg -n "GD\\.Randf\\(|new Random\\(|RandomNumberGenerator" Src/ECS/Tools/Math Src/ECS/Tools/TargetSelector Src/ECS/Capabilities
rg -n "Geometry2D|GeometryCalculator|TargetSelectorQuery|GeometryType" DocsAI/ECS/Tools/Math DocsAI/ECS/Tools/TargetSelector
```

BDD 预期：

- 0%、100%、负概率、超过 100% 概率行为稳定。
- 固定 seed 下随机采样可复现。
- Circle/Ring/Box/Line/Cone 边界点测试稳定。
- Bezier/Parabola/CircularArc/EllipseArc 的端点、切线和无效参数行为稳定。
