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

## 入口文档

- 概念与边界：`Concept.md`
- 用法示例：`Usage.md`
