# SDD-0037 Resource Loading And Common Utilities Hard Cutover

## Index Card

- **Status**: done
- **Created**: 2026-06-07
- **Updated**: 2026-06-08
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/tools/resource-loading
- **Tags**: tools, resource-loading, common-utilities, hard-cutover

## What This SDD Is About

本 SDD 执行 `design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md`：把当前 `ResourceManagement` 收敛为极薄 `ResourceLoading` 加载工具，删除 contains fallback，补 `LoadPath` source policy、structured result、ResourceCatalogDiagnostics，并迁出 `CommonTool.LoadPackedScene`。

同时建立受约束的 `Common Utilities` owner：通用工具仍属于 `Src/ECS/Tools/CommonUtilities/`，但不能恢复 `CommonTool.SomeHelper()` 杂物箱。能归入 Resource / Entity / Data / Event / Timer / TargetSelector / ObjectPool / Math / Capability 的函数，不允许放 Common Utilities。

本 SDD 不把 `res://` 当成问题。问题是业务裸加载、资源 key/path 缺 owner、路径移动后缺 migration workflow 和 diagnostics。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — 本 SDD 目标架构和边界
3. `tasks.md` — 当前任务拆分
4. `bdd.md` — 行为场景
5. `progress.md` — 最近结论和恢复点
6. `execution-prompt.md` — 新会话执行提示词
7. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0037 已生成执行胶囊。默认实现 ResourceLoading strict facade、ResourceCatalogDiagnostics、resource-path-migration gate 和 Common Utilities 受约束 owner。
- **Next Action**: 进入实现前先读 `execution-prompt.md`，从 T1.1 readiness baseline 开始。
- **Open Blockers**: none
