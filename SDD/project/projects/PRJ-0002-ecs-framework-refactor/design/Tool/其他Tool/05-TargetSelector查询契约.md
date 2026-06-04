# TargetSelector 查询契约

> 更新：2026-06-03
> 状态：current design input
> 裁决：TargetSelector 是剩余 Tools 中最需要优先完善的工具。它不是普通几何 helper，而是 Ability、AI、Feature、Movement 等 owner 共用的目标查询契约。

## 1. 当前证据

源码入口：

```text
Src/ECS/Tools/TargetSelector/TargetSelectorQuery.cs
Src/ECS/Tools/TargetSelector/EntityTargetSelector.cs
Src/ECS/Tools/TargetSelector/PositionTargetSelector.cs
Src/ECS/Tools/TargetSelector/GeometryCalculator.cs
Src/ECS/Tools/TargetSelector/GeometryType.cs
Src/ECS/Tools/TargetSelector/TargetSorting.cs
Src/ECS/Tools/TargetSelector/Tests/TargetSelectorTest.cs
```

DocsAI 入口：

```text
DocsAI/ECS/Tools/TargetSelector/Concept.md
DocsAI/ECS/Tools/TargetSelector/Usage.md
```

主要调用点：

- `Src/ECS/Capabilities/Ability/System/AbilityImpactTool.cs`
- `Src/ECS/Capabilities/AI/System/Actions/Combat/FindEnemyAction.cs`
- `Src/ECS/Capabilities/AI/System/Actions/Movement/RandomWanderAction.cs`
- 多个 `Data/Feature/Ability/*` handler
- `Src/ECS/Capabilities/Ability/Tests/AbilitySystemPipelineTest.cs`

这说明 TargetSelector 已经是框架级能力，不是某个技能的局部工具。

## 2. 当前执行流程

`EntityTargetSelector.Query(query)` 当前流程：

```text
Geometry != Single
  -> GetAllNode2DEntities()
     -> NodeLifecycleManager.GetNodesByInterface<Node2D>().OfType<IEntity>()
  -> GeometryCalculator.IsPointInGeometry(node.GlobalPosition, query)
  -> FilterTargets(team/type/lifecycle)
  -> SortTargets(origin, sorting)
  -> MaxTargets 截断
```

`PositionTargetSelector.Query(query)` 当前流程：

```text
count = MaxTargets > 0 ? MaxTargets : 1
RandomNumberGenerator.Seed = Time.GetTicksMsec()
loop count:
  GeometryCalculator.GetRandomPointInGeometry(query, rng)
```

## 3. 是否必要

必要，且应优先完善。

AI-first ECS 需要 TargetSelector，因为：

- Ability/AI 不应在业务 handler 里手写距离、角度、阵营和排序。
- TargetSelector 是从 DataOS ability fields 到 runtime query 的自然落点。
- AI debug 需要回答“为什么这个技能没有目标”。
- 后续如果加入空间索引、视线、障碍、黑名单、威胁权重，必须有统一入口。

## 4. 当前缺陷

| 缺陷 | 证据 | 影响 |
| --- | --- | --- |
| `OriginProvider` 未实际用于几何/排序 | `TargetSelectorQuery.ResolveOrigin()` 存在，但 `EntityTargetSelector` 和 `GeometryCalculator` 使用 `query.Origin`。 | 跟随型范围查询会失真。 |
| 全局扫描 NodeLifecycle | `GetAllNode2DEntities()` 扫 `NodeLifecycleManager.GetNodesByInterface<Node2D>()`。 | 热路径规模不可控，也绕过 EntityRegistry。 |
| 查询没有 diagnostics | 返回 `List<IEntity>`，没有候选数、过滤原因、排序结果、失败原因。 | AI 只能靠日志猜。 |
| `Single` 返回空 | `GeometryType.Single` 分支直接空列表。 | 单体目标语义不完整。 |
| Sorting 直接读 Data | Lowest/Highest HP 直接 `Data.Get<float>`。 | 缺字段时可能失败，且无过滤原因。 |
| 随机不可控 | `new Random()`、`Time.GetTicksMsec()`。 | 重复调用可能同 seed，测试和 replay 不可复现。 |
| TeamFilter 逻辑重复 | AbilityTool、MovementCollisionPolicy、EntityTargetSelector 各自实现。 | 阵营语义漂移风险。 |
| Query validation 缺失 | Range/Width/Length/Angle/Forward 缺少 shape-specific 校验。 | AI 不知道参数无效导致空结果还是逻辑无目标。 |
| 无 candidate source 抽象 | 候选来源写死 NodeLifecycle。 | 后续空间索引或 EntityRegistry 替换成本高。 |

## 5. 目标架构

### 5.1 TargetQueryEngine

推荐目标：

```text
TargetQueryEngine
  -> Validate query
  -> Resolve origin/forward/context
  -> Collect candidates from ITargetCandidateSource
  -> Geometry filter
  -> Team/type/lifecycle filter
  -> Optional custom predicate
  -> Sort
  -> Limit
  -> Return TargetQueryResult with diagnostics
```

目标数据形状：

```text
TargetQueryRequest
  Geometry
  OriginProvider / Origin
  ForwardProvider / Forward
  Range / InnerRange / Width / Length / Angle
  CenterEntity
  TeamFilter
  TypeFilter
  Sorting
  MaxTargets
  ExplicitCandidates
  ExplicitTarget
  RandomSource
  Source

TargetQueryResult
  Targets
  ResolvedOrigin
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

第一阶段可不引入新名字，只要现有 `TargetSelectorQuery` 能被增强，并提供 `QueryWithDiagnostics`。

### 5.2 Candidate source

建议封装候选来源：

```text
ITargetCandidateSource
  IEnumerable<IEntity> GetCandidates(TargetQueryContext context)

NodeLifecycleCandidateSource
EntityRegistryCandidateSource
SpatialIndexCandidateSource
ExplicitCandidateSource
```

默认第一阶段：

- 仍使用 NodeLifecycle/EntityManager 作为 candidate source。
- 但调用点只依赖 TargetSelector，不直接知道候选来源。

后续触发条件：

- 单帧 query 次数和实体数量超过阈值。
- Godot scene artifact 显示 TargetSelector 成为瓶颈。
- AI/Ability 开始出现大范围高频查询。

满足任一条件再引入空间索引。

### 5.3 RNG policy

目标：

- `TargetSorting.Random` 使用 caller/context 提供的 RNG。
- `PositionTargetSelector` 可传 seed 或 RNG。
- 测试默认固定 seed。
- gameplay 默认 RNG source 由后续系统统一，不在 query 内部用当前毫秒重复播种。

### 5.4 TeamFilter policy

阵营过滤应收口为共享策略，避免三个模块各写一份：

```text
TeamFilterPolicy.Pass(source, target, filter)
```

TargetSelector、AbilityTool、MovementCollisionPolicy 可以共享或至少复用同一测试 fixture。

## 6. 分阶段路线

### Phase 1：低风险 hardening

- 修复 `ResolveOrigin()` 使用：几何、排序、位置采样全部用 resolved origin。
- 增加 query validation：按 GeometryType 检查必填参数。
- 增加 `QueryWithDiagnostics`，保留 `Query` 返回 list facade。
- 增加 RNG 注入或 seed overload。
- HP/Threat sorting 改为安全读取，缺字段进入 warnings 或降级。
- `Single` 支持 `ExplicitTarget` 或 `ExplicitCandidates`。
- 更新 DocsAI/TargetSelector。

### Phase 2：candidate source 抽象

- 引入 `ITargetCandidateSource`。
- 默认 source 仍接 NodeLifecycle/EntityManager。
- TargetSelector tests 覆盖 explicit source 和 NodeLifecycle source。

### Phase 3：性能和 spatial index

只有在 profiling 或场景 artifact 证明需要时再做：

- 按 Team/EntityType/LifecycleState 的 entity index。
- 位置空间索引。
- query result buffer 复用。
- no-allocation 热路径。

## 7. Not Recommended

- 不建议让 Ability/AI handler 重新手写 `GetTree().GetNodesInGroup()` 和距离判断。
- 不建议第一阶段直接上复杂空间索引。
- 不建议把 TargetSelector 合并进 Math。
- 不建议让 `NodeLifecycleManager.GetNodesByInterface<Node2D>()` 成为长期热路径事实源。
- 不建议查询失败只打印日志，不返回结构化原因。

## 8. 验证门禁

文档/设计阶段：

```bash
rg -n "EntityTargetSelector|PositionTargetSelector|TargetSelectorQuery|GeometryType|TargetSorting|TeamFilter" Src/ECS Data DocsAI/ECS
python3 Workspace/SDD/sdd.py validate --all
```

实现阶段：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

Godot 场景验证：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/TargetSelector/Tests/TargetSelectorTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

建议新增 BDD：

- `OriginProvider` 动态变化后，同一个 query 使用新 origin。
- Circle/Ring/Box/Line/Cone 参数非法时 diagnostics 输出错误。
- TeamFilter / TypeFilter / Lifecycle 过滤计数正确。
- Random sorting 固定 seed 可复现。
- `Single` query 能返回显式目标。
- 缺少 HP/Threat 数据时排序不崩溃，并输出 warning。
