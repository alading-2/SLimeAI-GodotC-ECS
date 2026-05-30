# PRJ-0002 ECS Framework Optimization

## Index Card

- **Status**: active
- **Created**: 2026-05-25
- **Updated**: 2026-05-30
- **Scope**: SlimeAI
- **Current SDD**: SDD-0023
- **Tags**: ecs, optimization, data, event, entity, relationship

## What This Project Is About

本项目用于重新梳理 `SlimeAI/Src/ECS` 旧 ECS 框架的真实问题，并形成“保留旧 ECS 主线、按问题域优化完善”的设计事实源。

当前方向已经纠偏：不再把旧 ECS 作为迁移输入，不再以整体替换或复制外部参考结构为目标。旧框架整体可保留；Data 子系统已按 SDD-0012 至 SDD-0022 完成 descriptor-first / snapshot-first / no-compat / residual contract hardening 收口。Entity / Relationship 已单独裁决为 hard cutover 完整重构，是下一条 P0 主线。

## Reading Order

1. `design/INDEX.md` — 项目共享设计索引
2. `design/06-ECS完全重构执行原则.md` — Data 无兼容复盘后的 hard cutover 项目级原则
3. `design/2.Data系统优化/README.md` — Data 完整重构设计包入口
4. `design/3.Entity系统优化/README.md` — Entity 完整重构设计包入口
5. `design/4.SystemAgent目录更改到SlimeAI里面/README.md` — 当前 SDD-0023 输入：AI 配置与 SystemAgent 根迁移后的规则同步设计
6. `sdds/013-SDD-0023-systemagent-root-migration-rule-sync/README.md` — 当前执行型 SDD
7. `entity-rewrite-execution-prompt.md` — Entity / Relationship hard cutover 总执行提示词
8. `sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/progress.md` — Data residual contract hardening 已完成记录
9. `sdds/011-SDD-0021-data-no-compatibility-hard-cutover/execution-prompt.md` — Data 无兼容 hard cutover 已完成记录
10. `data-rewrite-execution-prompt.md` — Data Full Rewrite 历史总执行提示词，SDD-0012~SDD-0019 使用
11. `roadmap.md` — 设计文档到 SDD 的映射、执行顺序、依赖和状态
12. `progress.md` — 项目级关键结论和恢复点
13. `sdds/` — 项目内有序 SDD
14. `notes.md` — 参考与开放问题
