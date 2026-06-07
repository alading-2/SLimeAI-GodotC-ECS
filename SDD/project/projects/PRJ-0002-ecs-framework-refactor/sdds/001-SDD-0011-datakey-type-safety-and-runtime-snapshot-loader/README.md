# SDD-0011 DataKey Type Safety and Runtime Snapshot Loader

## Index Card

- **Status**: done
- **Created**: 2026-05-27
- **Updated**: 2026-05-27
- **Type**: implementation
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - SlimeAI/Src/ECS/Base/Data
  - SlimeAI/Data/DataKey
  - SlimeAI/Data/Data
- **Tags**: data, datakey, runtime_snapshot

## What This SDD Is About

修复当前约 196 个编译错误并建立 Data 系统类型安全基线：

1. 补齐 `DataKey<T>` / `IDataKey` 类型（`Src/ECS/Base/Data/`），让 `Data/DataKey/*.cs` 编译通过
2. 新增 `SnapshotLoader`（`Src/ECS/Base/Data/`），从 `Data/Data/Snapshots/runtime_snapshot.json` 加载 entity 模板数值
3. 删除过时的 `Data/RuntimeSnapshot/RuntimeDataSnapshot.cs`

设计来源：`../../design/Runtime/2.Data系统优化/README.md`（§4 采纳方向 / §6 需要做什么）

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `Core/progress.md` — 最近结论和恢复点
4. `bdd.md` — 行为场景或不适用说明
5. `Core/notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0011 已创建并填充完整任务结构（3 组 8 个任务）。设计来源 `design/Runtime/2.Data系统优化/README.md §6`，已对齐 C#-first 方向。
- **Next Action**: 从 T1.1 开始实施 — 新增 `SlimeAI/Src/ECS/Base/Data/DataKey.cs`。
- **Open Blockers**: none
