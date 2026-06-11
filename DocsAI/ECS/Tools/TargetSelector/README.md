# TargetSelector

> status: current
> sourcePaths: Src/ECS/Tools/TargetSelector/
> lastReviewed: 2026-06-07

## 定位

`TargetSelector` 是框架目标查询 owner。current API 是 `TargetQueryEngine.QueryEntities(...)` / `TargetQueryEngine.QueryPositions(...)`，返回 `TargetQueryResult<T>` 和 `TargetQueryDiagnostics`。

旧 `EntityTargetSelector.Query(...)` / `PositionTargetSelector.Query(...)` list-only facade 已删除；Ability、AI、Feature 调用点必须直接读取 `TargetQueryResult<T>.Items` 与 `Diagnostics`。

## 当前契约

- 查询会先 resolve `OriginProvider` / `ForwardProvider`，diagnostics 记录 `ResolvedOrigin` / `ResolvedForward`。
- diagnostics 覆盖 source candidate count、geometry hit count、team/type/lifecycle 过滤计数、sort、limit、warnings 和 errors。
- `Single` 查询必须通过 `ExplicitTarget` 或 `ExplicitCandidates` 指定候选。
- 默认 candidate source 走 EntityManager；显式候选走 `ExplicitTargetCandidateSource`。
- `TargetSorting.Random` 与位置采样支持 `RandomSeed` / `RandomSource`，查询内部不使用当前毫秒播种。

## Log

TargetSelector owner 使用 `owner=TargetSelector`。当前 `TargetQueryEngine` 写 `OperationTrace`：

| operation | phase | outcome | 关键字段 |
| --- | --- | --- | --- |
| `TargetQueryEntities` | `Targeting` | `Completed` / `Failed` | `sourceCandidates`、`geometryHits`、`returned`、`truncated`、`warningCount`、失败时 `errorCount`、`maxTargets` |
| `TargetQueryPositions` | `Targeting` | `Completed` / `Failed` | `returned`、`maxTargets`、`warningCount`、失败时 `errorCount` |

规则：

- diagnostics 仍是查询结果合同的一部分；日志只输出 AI-first flow summary，不能替代 `TargetQueryResult<T>.Diagnostics`。
- `TargetSorting.Random` 问题必须同时记录 seed/source；不要用当前时间补随机。
- Ability / AI / Feature 调用 TargetSelector 时，调用方保留 owner flow；TargetSelector 只记录查询事实。
- 默认 profile 对 `owner=TargetSelector` 使用 `minimumSeverity=Info` 和较低 `budgetPerSecond`；成功查询由 budget / `logctl analyze` success template 聚合，失败、warning、truncated 仍保留结构化字段。

```bash
Workspace/Tools/logctl/logctl query --analysis-dir <run>/analysis owner=TargetSelector operation=TargetQueryEntities
```

## 入口文档

- 概念与边界：`Concept.md`
- 用法示例：`Usage.md`
