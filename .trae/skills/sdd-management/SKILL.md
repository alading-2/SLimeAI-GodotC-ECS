---
name: sdd-management
description: 管理 SlimeAI SDD artifact、CLI、索引、状态、任务、进度、阻塞和验证时使用。可被 sdd-workflow 或其他 SystemAgent workflow 调用。
---

# sdd-management

## 触发条件

- 创建、列出、查看、校验或索引 SDD 和项目级 SDD 容器。
- 更新 SDD 状态、任务 checkbox、progress 记录或 blocker。
- 恢复某个 SDD 的当前上下文。
- 检查 SDD 根目录或实例结构是否一致。

## 必读

- `Workspace/SDD/README.md`
- `Workspace/SDD/docs/SDDFormat.md`
- `Workspace/SDD/docs/CLI.md`
- `Workspace/SDD/docs/ValidationRules.md`

## 常用命令

```bash
python3 Workspace/SDD/sdd.py init-root
python3 Workspace/SDD/sdd.py project-new "Project Title"
python3 Workspace/SDD/sdd.py project-list
python3 Workspace/SDD/sdd.py project-show PRJ-0001
python3 Workspace/SDD/sdd.py project-archive PRJ-0001
python3 Workspace/SDD/sdd.py new "Task Title"
python3 Workspace/SDD/sdd.py new "Task Title" --project PRJ-0001
python3 Workspace/SDD/sdd.py list
python3 Workspace/SDD/sdd.py show SDD-0001
python3 Workspace/SDD/sdd.py start SDD-0001
python3 Workspace/SDD/sdd.py note SDD-0001 --type decision "..."
python3 Workspace/SDD/sdd.py task SDD-0001 list
python3 Workspace/SDD/sdd.py block SDD-0001 "..."
python3 Workspace/SDD/sdd.py done SDD-0001 --validation "..."
python3 Workspace/SDD/sdd.py done SDD-0001 --validation "..." --conclusion "..." --next-action "..."
python3 Workspace/SDD/sdd.py validate --all
python3 Workspace/SDD/sdd.py index
```

## 管理规则

1. `README.md` 是入口卡片，不写完整设计正文；CLI 写操作不得整体覆盖人工维护摘要。
2. `design/` 保存任务特定设计差异或项目级共享设计引用，`design/INDEX.md` 标注 main/current/reference。
3. `tasks.md` 只表达任务、依赖、验证和 checkbox 状态。
4. `progress.md` 是状态面板，只记录 current / next / blocker / 少量真正改变方向的 decision / 最终 validation summary；CLI 状态命令不追加 task timeline、完整命令日志或文件清单。
5. `bdd.md` 只摘录本任务要执行的关键行为，优先引用设计文档旁的 `.FeatureSpec.md`；纯研究、文档治理或配置治理可标记不适用。
6. 项目级任务优先放入 `SDD/project/projects/<project>/sdds/`；项目完成后使用 `project-archive` 归档。
7. SDD 和项目真实状态以 `sdd.json.status` / `project.json.status` 为准，不以目录名推断。
8. 写操作后运行 `index` 或确认 CLI 已自动更新索引。
9. `validate` 只证明 SDD 结构和恢复信息质量，不证明业务实现正确；代码/数据/Godot/skill 仍需各自验证。
10. 完成前可运行目标 SDD 或 `--all` validate；有 error 时不应标记 `done`，warning 需要解释是否影响恢复。
11. `artifacts/` 只保存必要证据；多个 artifact 必须在 progress 或 notes 中引用。
12. validation 摘要只需包含命令、结果和 artifact/ref；不要复制完整输出。
13. `Key Files` 不复制 git diff；同步副本、自动生成文件和机械路径替换通常不列为核心文件。
14. 项目子 SDD 默认引用项目级 `design/`，不复制完整设计快照；只在本 SDD 的 `design/` 写任务特定差异。
15. 历史 SDD 的 `Latest Resume` / `Pxxx` 只作读取兼容；新建或状态流转不再写入旧 timeline。

## 输出要求

- 操作前后的 SDD/项目状态、路径和关键文件。
- 变更的任务编号、progress 记录类型和验证命令。
- `validate` 的 error/warn 摘要。
- 明确区分 SDD validate 结果和业务验证结果。

## 禁止

- 不替代业务 owner skill 做具体实现决策。
- 不绕过 `sdd.json` / `project.json` 手写状态迁移。
- 不把任务实例写入 `.ai-config/skills/` 或 `Workspace/SystemAgent/`。
