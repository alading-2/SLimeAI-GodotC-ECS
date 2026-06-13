---
name: sdd-workflow
description: SlimeAI SDD 中大型任务流程入口。用户要求使用 SDD、创建/继续 SDD、深度设计后实施或需要跨会话恢复上下文时使用。
---

# sdd-workflow

## 触发条件

- 用户明确说“用 SDD”、“创建 SDD”、“继续 SDD”、“按 SDD 流程执行”。
- 任务是中大型设计、重构、迁移、AI 配置治理或跨模块实施。
- 任务需要长期任务状态、设计引用、阻塞或验证入口；不因为想记录过程就创建 SDD。
- 用户要求“深度思考”且后续需要落地执行。

## 必读

- `Workspace/SDD/README.md`
- `Workspace/SDD/docs/SDDFormat.md`
- `Workspace/SDD/docs/CLI.md`
- `Workspace/SDD/docs/ValidationRules.md`
- `Workspace/SystemAgent/README.md`
- `Workspace/SystemAgent/Docs/11-FeatureSpec功能实现规格.md`

## 流程

1. 判断任务是否需要 SDD；小修、拼写、一次性排查不强制创建。
2. 读取或创建匹配的项目 / SDD 实例；优先使用 `project-list`、`list`、`project-show` 和 `show` 恢复上下文。
3. 若任务尚无设计，先调用 `systemagent-deepthink` 形成问题分析、解决思路、确认点和默认假设；需要写设计文档时使用 `systemagent-design-document` 保证保留用户原始问题并避免模板化冗余。
4. 项目级共享设计写入项目 `design/`；设计冻结后默认在同目录写 `.FeatureSpec.md`，当前 SDD 只写任务级差异、tasks、状态面板、FeatureSpec 行为摘录或 Source 引用。
5. 实现前检查 readiness：目标和边界明确、design 非模板或有有效项目级 design 引用、tasks 可执行、FeatureSpec 或 `bdd.md` 有场景/引用或不适用原因、当前 next/blocker 清楚、目标 SDD validate 无 error。
6. 实施时按 `tasks.md` 小步推进；`progress.md` 只更新 current / next / blocker / 少量真正改变方向的 decision / 最终 validation summary，不记录每个 task command。
7. 完成前运行新鲜验证证据；`python3 Workspace/SDD/sdd.py validate --all` 或目标 SDD 校验只证明 SDD 结构和恢复信息质量，不能替代代码/数据/Godot/skill 的实际验证。
8. 完成后根据验证结果决定是否运行 `done`，或保留 `active/blocked` 状态；项目完成后使用 `project-archive` 归档。

## 信息质量规则

- 不把 SDD 写成流水账，不复制完整命令输出、全量 diff 或同步副本清单。
- `progress.md` 保存状态面板和少量关键裁决，不保存每个临时操作、任务完成流水账、完整命令输出或文件清单。
- `README.md` 是人工可维护入口卡片；状态流转只能更新 CLI 拥有字段。
- 项目子 SDD 默认引用项目级 `design/`，不复制完整设计快照；本地 `design/main.md` 可只写局部差异和不做什么。
- SDD 和项目真实状态来自 `sdd.json.status` / `project.json.status`，目录只表达组织或归档位置。
- `Key Files` 只列改变系统行为、事实源、门禁、公共接口或验证方式的文件；超过 8 个时改写 `Key Areas`。
- `start/block/task/done/note` 只维护 State / Decisions / Validation；不期待或补写 `Latest Resume` / `P001` timeline。
- `done` 只需要最终 validation summary 和 next=none / follow-up；必要时使用 `--conclusion` 和 `--next-action` 明确最终结论，不生成长 Resume。

## CLI 入口

```bash
python3 Workspace/SDD/sdd.py list
python3 Workspace/SDD/sdd.py project-list
python3 Workspace/SDD/sdd.py project-show PRJ-0001
python3 Workspace/SDD/sdd.py show SDD-0001
python3 Workspace/SDD/sdd.py validate --all
python3 Workspace/SDD/sdd.py index
python3 Workspace/SDD/sdd.py new "Task Title" --project PRJ-0001
python3 Workspace/SDD/sdd.py done SDD-0001 --validation "..." --conclusion "..." --next-action "..."
```

## 输出要求

- 当前 SDD ID、状态、路径和 State / Next / Blocker。
- 本轮任务列表、已完成项、阻塞项和下一步。
- 修改文件范围和验证命令结果。
- 若改动 `.ai-config/skills/`，必须同步并报告 skill-test 结果。

## 禁止

- 不把 SDD 实例放入 `Workspace/SystemAgent/`。
- 不把 SDD 设计文档交给 SystemAgent 归档。
- 不直接修改 `.ai-config/sync-targets.json` 定义的 skill 同步副本作为源。
- `openspec/` 已废弃，新任务不进入 OpenSpec。
