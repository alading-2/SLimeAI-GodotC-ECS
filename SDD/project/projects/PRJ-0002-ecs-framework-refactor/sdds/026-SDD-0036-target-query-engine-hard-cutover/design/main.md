# Target Query Engine Hard Cutover

## Goal

把 TargetSelector 从“返回 List 的静态工具”改成 AI-first 查询引擎：

- 新 current 入口是 `TargetQueryEngine` / `TargetQueryResult`。
- 查询输出包含 targets/items 和 diagnostics。
- 几何与排序必须使用 resolved origin / resolved forward。
- 候选来源通过 `ITargetCandidateSource` 或等价抽象进入，不由 Ability/AI 知道 NodeLifecycle。
- 随机排序和位置采样可注入 seed/RNG，测试可复现。
- `Single` 支持 explicit target / candidates。

非目标：

- 不直接上复杂空间索引，除非 profiling 或场景 artifact 证明需要。
- 不把 TargetSelector 合并进 Math。
- 不恢复 `EntityTargetSelector.Query(query)` 作为最终 public API。

## Context

必须先读：

- `design/Tool/其他Tool/README.md`
- `design/Tool/其他Tool/01-现状证据与AI-first裁决.md`
- `design/Tool/其他Tool/05-TargetSelector查询契约.md`
- `design/Tool/其他Tool/06-实施路线与验证门禁.md`
- `DocsAI/ECS/Tools/TargetSelector/`
- `Src/ECS/Tools/TargetSelector/**`
- `Src/ECS/Capabilities/Ability/System/AbilityImpactTool.cs`
- `Src/ECS/Capabilities/AI/System/Actions/Combat/FindEnemyAction.cs`
- `Src/ECS/Capabilities/AI/System/Actions/Movement/RandomWanderAction.cs`
- `Data/Feature/Ability/**`

用户已确认：

- TargetSelector 要完全重构，不做兼任。
- 不设计 `EntityTargetSelector.Query(query)` 临时桥或最终 public/current API。
- AI 需要清楚知道“为什么没找到目标”。

## Design

目标 API 形态：

```text
TargetQueryEngine
  QueryEntities(TargetQueryRequest request)
  QueryPositions(TargetPositionQueryRequest request)

TargetQueryResult<T>
  Items
  Diagnostics
  HasItems / HasTargets

TargetQueryDiagnostics
  ResolvedOrigin
  ResolvedForward
  CandidateCount
  GeometryHitCount
  FilteredByTeamCount
  FilteredByTypeCount
  FilteredByLifecycleCount
  SortApplied
  LimitApplied
  Warnings
  Errors
```

hard cutover 口径：

- Ability / AI / Feature / Tests 调用点直接迁移到 `TargetQueryEngine`。
- 业务 handler 不直接知道 candidate source。
- `NodeLifecycleManager.GetNodesByInterface<Node2D>()` 不作为 TargetSelector public fallback。
- HP/Threat sorting 安全读取缺失数据，缺字段进入 warnings 或可解释降级。
- Random sorting 使用 caller/context RNG 或 fixed seed。

## Verification

基础验证：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0036
```

grep gate：

```bash
rg -n "EntityTargetSelector\\.Query|PositionTargetSelector\\.Query|NodeLifecycleManager\\.GetNodesByInterface" Src/ECS Data DocsAI/ECS
rg -n "new Random\\(|GD\\.Randf\\(|Time\\.GetTicksMsec\\(\\)" Src/ECS/Tools/TargetSelector Src/ECS/Capabilities Data/Feature
```

Godot 场景验证需要承载游戏 runner：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/TargetSelector/Tests/TargetSelectorTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
