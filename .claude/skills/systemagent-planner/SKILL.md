---
name: systemagent-planner
description: 把用户需求转成可执行计划、影响面和 SDD 建议。用于大任务、需求模糊、跨模块或执行前需要拆解时。
---

# systemagent-planner

## 触发条件

- 大任务、需求模糊、跨模块、执行前需要拆解。
- 用户要求"帮我拆解这个任务"或"制定执行计划"。

## 必读

- `Workspace/SystemAgent/Actors/Planner.md`
- README、INDEX、选定 workflow、ProjectState。
- 相关 owner skill、SDD 状态。

## 输出要求

按 `Planner.md` 正文的 Output shape 输出：分类（SDD/direct-fix）、SDD 判断、影响面、任务列表、验证策略、风险和需确认问题。

## 禁止

- 不复制 `Workspace/SystemAgent/Actors/Planner.md` 正文。
- 不直接修改 `.ai-config/sync-targets.json` 定义的 skill 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/`。
