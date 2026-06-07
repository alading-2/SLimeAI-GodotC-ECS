# SDD-0020 Data Snapshot-First Usage Cutover

## Index Card

- **Status**: done
- **Created**: 2026-05-29
- **Updated**: 2026-05-29
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - SlimeAI/Src/ECS/Base/Data
  - SlimeAI/Data/DataOS
  - SlimeAI/Src/ECS/Base/System
  - SlimeAI/Src/ECS/Base/Entity/Core
  - SlimeAI/Data/ResourceManagement
  - SlimeAI/addons/DataConfigEditor
  - SlimeAI/DocsAI
- **Tags**: data, ecs, refactor

## What This SDD Is About

本 SDD 收口 SDD-0012~SDD-0019 后暴露的 Data 兼任问题：descriptor-first DataOS / runtime snapshot 主链路已经建立，但 RuntimeTables、DataTable 反射静态表、`EntityManager` config 推断、`DataRegistry/DataMeta` fallback、DataConfigEditor 旧路径和部分文档仍让旧写法继续承担数据事实源职责。

目标是把 Data 取用方式改成最新 Data 系统形式：运行时只从 `runtime_snapshot.json` / `DataRuntimeBootstrap` / catalog-bound `Data` 获取定义和记录；业务系统不再从手写 C# table、旧 DataMeta registry 或无 catalog `Data` fallback 读取配置。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — 深度设计、风险、切换方案和调用点迁移策略
3. `execution-prompt.md` — 一次性完成 SDD-0020 全任务的执行提示词
4. `tasks.md` — 当前任务拆分
5. `bdd.md` — 行为验收场景
6. `Core/progress.md` — 最近结论和恢复点
7. `Core/notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: complete
- **Last Conclusion**: SDD-0020 已完成 Data snapshot-first usage hard cutover：运行时配置、系统/生成/技能/资源目录和 Entity 初始化记录均改走 `runtime_snapshot.json` / `DataRuntimeBootstrap` / `RuntimeDataRecordQuery` / typed projection / catalog-bound `Data`。
- **Next Action**: Data 取用层收口完成；PRJ-0002 下一步可进入 Entity / Relationship hard cutover SDD。
- **Open Blockers**: none
