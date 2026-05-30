# SDD

SDD 是 SlimeAI 工作区的中大型任务上下文胶囊系统，用于保存设计、任务、进度、行为约束和验证证据。

## 目录职责

- `pending/`：已创建但尚未开始执行的 SDD。
- `active/`：正在执行的 SDD。
- `blocked/`：因缺信息、失败或外部条件暂停的 SDD。
- `done/`：已完成并记录验证结果的 SDD。
- `templates/`：手动创建 SDD 时使用的模板。
- `INDEX.md`：CLI 生成的人类可读索引。
- `catalog.json`：CLI 生成的机器可读索引。

## 常用命令

```bash
python3 Workspace/SDD/sdd.py new "Task Title"
python3 Workspace/SDD/sdd.py list
python3 Workspace/SDD/sdd.py show SDD-0001
python3 Workspace/SDD/sdd.py validate --all
python3 Workspace/SDD/sdd.py index
```

## 使用原则

- 小任务不强制创建 SDD。
- 中大型任务、跨模块重构、AI 配置治理和长期恢复任务应创建 SDD。
- 单个 SDD 的 `README.md` 是入口卡片，不承载完整设计正文。
- 完整设计放入 `design/`，运行结论和恢复点写入 `progress.md`。
