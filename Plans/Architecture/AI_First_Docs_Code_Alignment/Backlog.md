# Backlog

## 后续事项

- 分模块评估 `Src/**/*.md` 中哪些长设计说明应迁入 `Docs/`。
- 分模块评估哪些 AI 执行规则应继续从 `Src/**/*.md` 下沉到 `DocsAI/Modules/`。
- 当前计划不修复运行时代码问题；历史 `MainTest` 失败需作为独立 Debug 任务处理。
- 若后续继续收敛文档，可优先处理 Movement 长设计文档、AI 行为树细节、Collision 设计总览和 `Docs/` 中对应人类设计文档。
- 旧 `Data/Data/` Resource / `.tres` 路线仍保留归档，后续新增数据应继续走 DataNew。
- 下一步应按模块逐个审计剩余 `Src/**/*.md`，每次只处理一个模块族，避免一次性大迁移造成索引失真。
