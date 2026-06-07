# Math Formula And Deterministic Random Cutover

## Goal

把 Math 从“杂项公式聚合 + 不可控随机”改成可测试、可复现、owner 清楚的工具层：

- 纯几何继续归 `Geometry2D`。
- Bezier / Wave / Curves 保持纯算法边界。
- `MyMath` 拆分或删除；业务公式归 `AttributeFormula` / `CooldownFormula` / `DamageFormula` / `ProbabilityTool` 或对应 Capability owner。
- `CheckProbability`、随机采样和 Target/Position 随机使用可注入 RNG / seed。
- `GeometryCalculator` 不作为 current 公开入口继续教给 AI。

非目标：

- 不引入第三方数学库。
- 不迁到 `System.Numerics`。
- 不让 Math 直接读取 Entity.Data、EventBus、SceneTree 或 ObjectPool。
- 不把 `GeometryType` / `TargetSelectorQuery` 下沉到 Math。

## Context

必须先读：

- `design/Tool/其他Tool/README.md`
- `design/Tool/其他Tool/01-现状证据与AI-first裁决.md`
- `design/Tool/其他Tool/03-Math目标架构与验证.md`
- `design/Tool/其他Tool/06-实施路线与验证门禁.md`
- `DocsAI/ECS/Tools/Math/`
- `Src/ECS/Tools/Math/**`
- `Src/ECS/Capabilities/Damage/System/Processors/**`
- `Src/ECS/Capabilities/Ability/**`
- `Src/ECS/Capabilities/Movement/System/Strategies/**`
- `Src/ECS/Capabilities/Spawn/System/SpawnPositionCalculator.cs`
- SDD-0036 当前 TargetSelector 状态。

用户已确认：

- Math 功能保留。
- 不需要保留旧 `MyMath` 聚合类。
- AI-first 优先 owner 清晰和可复现验证。

## Design

推荐目标：

```text
Src/ECS/Tools/Math/Geometry/Geometry2D.cs
Src/ECS/Tools/Math/Curves/**
Src/ECS/Tools/Math/Bezier/**
Src/ECS/Tools/Math/Random/DeterministicRandom.cs 或 ProbabilityTool.cs
Src/ECS/Capabilities/<Owner>/Formula/*.cs
```

拆分策略：

- 护甲/伤害公式优先归 Damage owner。
- 冷却/充能公式优先归 Ability owner。
- 属性倍率公式可归 Unit/Feature/Data owner，按实际调用点决定。
- 概率工具留 Math 或 Random helper，但必须可注入 RNG。
- BezierTemplateBuilder 若带技能模板语义，应标注 Movement/Ability shared helper，不当作通用数学核心。

## Verification

基础验证：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0038
```

grep gate：

```bash
rg -n "GD\\.Randf\\(|new Random\\(|Time\\.GetTicksMsec\\(\\)" Src/ECS/Tools/Math Src/ECS/Capabilities Data/Feature
rg -n "MyMath|GeometryCalculator|TargetSelectorQuery|GeometryType" DocsAI/ECS/Tools/Math DocsAI/ECS/Tools/TargetSelector Src/ECS/Tools
```

Godot runner 可用时：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/Math/Tests/MyMathTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
