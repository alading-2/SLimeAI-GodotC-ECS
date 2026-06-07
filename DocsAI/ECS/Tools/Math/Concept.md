# Math 概念

> status: current
> sourcePaths: Src/ECS/Tools/Math/
> relatedDocs: DocsAI/ECS/Tools/Math/Usage.md, DocsAI/ECS/Tools/Math/Concepts/通用数学工具重构执行方案.md
> lastReviewed: 2026-06-07

## 1. 一句话定位

Math 只保留纯数学、曲线、几何和可复现随机工具；Damage / Ability / Movement 等业务公式归各自 capability owner。

## 2. 当前 owner

| 子域 | 当前入口 | 说明 |
| ---- | ---- | ---- |
| 概率 / 随机 | `Random/ProbabilityTool.cs`, `Random/DeterministicRandom.cs` | 百分比概率 0-100，支持显式 `RandomNumberGenerator` 或 seed |
| Wave | `WaveMath.cs` | 正弦波采样、导数、速度与切线 |
| Bezier 核心 | `Bezier/BezierCurve.cs` | 通用贝塞尔求值、切线、曲率、分裂和弧长 |
| Bezier 模板 | `Bezier/BezierTemplateBuilder.cs` | Movement / Ability 曲线弹模板 helper，带玩法模板语义，不是通用数学核心 |
| Curves | `Curves/EllipseArc2D.cs`, `Curves/Parabola2D.cs`, `Curves/CircularArc2D.cs` | 点采样、切线采样、弧长近似 |
| Geometry | `Geometry/Geometry2D.cs` | Circle / Ring / Box / Capsule / Cone 判定与随机采样 |

## 3. 职责边界

| Math 做 | Math 不做 |
| ---- | ---- |
| 纯数学、曲线、几何、deterministic RNG helper | 物理引擎调用 |
| 不读 Entity/Data 的通用概率工具 | Damage / Ability / Movement 业务公式 ownership |
| 几何计算与随机采样 | AI 路径规划、TargetSelector query 语义 |

当前公式 owner：

- Damage 护甲倍率：`Src/ECS/Capabilities/Damage/System/Formula/DamageFormula.cs`
- Ability 冷却 / 充能时间：`Src/ECS/Capabilities/Ability/System/Formula/AbilityFormula.cs`
- TargetSelector 查询语义：`Src/ECS/Tools/TargetSelector/`

## 4. 随机策略

- gameplay 概率统一用 `ProbabilityTool.RollPercent(chancePercent, rng)`，概率单位为百分比 0-100。
- 测试和 replay 使用 `DeterministicRandom.Create(seed)` 注入随机源。
- 未显式传 RNG 的 Math 随机采样走 `DeterministicRandom.Shared`，调用方需要隔离序列时必须自行持有 RNG。
- Math 测试不依赖 `GD.Randf()` 统计波动。
