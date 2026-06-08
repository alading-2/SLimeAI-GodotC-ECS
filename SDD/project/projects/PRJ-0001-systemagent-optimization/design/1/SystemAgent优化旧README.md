# SystemAgent 优化文档索引

> 日期：2026-05-24  
> 范围：`Workspace/SystemAgent`、AI 配置触发链、Workflow/Skill/Role/Subagent 分层、Hook/Gate、Git/worktree、OpenSpec 到独立 SDD 的迁移思路、DesignDiscovery capability skill  
> 状态：方案分析文档，不是已实施的 SystemAgent 正文事实源

---

## 1. 目录定位

本目录用于沉淀 SystemAgent 优化过程中的问题诊断、设计取舍和后续实施路线。

这里的文档与正式 SDD 的分工不同：

- 独立 `SDD/` 根目录：未来保存正式 SDD、任务设计文档、任务列表、进度、BDD 和 artifacts。
- 本目录：保存 SystemAgent 优化的过渡分析、边界讨论和迁移方案。

当前 `SDD/SlimeAI-SDD-MVP设计.md` 仍是输入设计草案的临时位置；正式落地后，它应进入某个具体 SDD 的 `design/` 目录，并由 `design/INDEX.md` 管理。

---

## 2. 推荐阅读顺序

1. `SystemAgent问题清单.md`  
   先看现有问题、证据和优先级。

2. `SDD/SlimeAI-SDD-MVP设计.md`  
   理解为什么要做 SlimeAI 专属 SDD，以及为什么设计文档应进入具体 SDD 的 `design/`。

3. `08-SDD独立化与文档迁移方案.md`  
   看正式 SDD 应如何从本分析目录独立出去，以及当前文档后续如何迁移。

4. `01-独立SDD转向方案.md`  
   看 SystemAgent 与 SDD、OpenSpec、DocsAI 的新边界。

5. `02-Workflow与Skill触发优化方案.md`  
   看 SystemAgent workflow、wrapper skill、rule 的触发方式如何变轻。

6. `09-WorkflowSkillRole分层模型.md`  
   看 workflow、skill、role、artifact、gate、subagent 的新边界，以及 workflow 如何调用 capability skill。

7. `07-DesignDiscovery与DesignCritic方案.md`  
   看如何吸收 superpowers brainstorming 思想，但改成无 hook、非逐问逐答的设计发现阶段。

8. `10-Subagent使用场景与采纳策略.md`  
   看 SystemAgent 是否需要 subagent、哪些场景适合只读研究/独立评审、为什么暂不做并行 dispatcher。

9. `03-Hook与Gate重写方案.md`  
   看 hook 如何从高频提示器改成低频、稳定、可验证的安全栏。

10. `04-Git与Worktree策略.md`  
   看 worktree 是否需要统一、放在哪里、什么时候创建。

11. `05-OpenSpec退场与兼容策略.md`  
   看 OpenSpec 如何从默认路径退到兼容路径。

12. `06-实施路线图.md`  
   看后续从文档方案到真实改动的分阶段执行顺序。

---

## 3. 核心结论摘要

SystemAgent 当前不缺规则，缺的是低成本、可恢复、可验证的执行闭环。

优化方向不是继续增加互相重叠的 workflow、role、gate 和 skill，而是：

- 用独立 SDD 承接中大型任务的上下文、设计、任务、进度和行为约束。
- 正式设计文档进入具体 SDD 的 `design/`，不再长期散落在 Idea 目录。
- Workflow 是完整流程编排，过程会调用 capability skill、使用 role、读写 artifact、经过 gate。
- `DesignDiscovery` 和 `SDD Management` 应作为 capability skill，可单独运行，也可被 workflow 调用。
- `DesignCritic` 是 role，用于查缺陷、风险、遗漏和用户确认项。
- Subagent 不是新顶层概念，而是 workflow/skill 在必要时使用的执行基座；当前只建议只读研究、独立评审和验证设计，不建议并行写代码。
- 让 SystemAgent 回到长期 workflow/policy/gate 事实源的位置。
- 让 OpenSpec 从默认执行入口退为历史兼容和外部参考。
- 让 hook 只做低频安全提示和 trace，不再承担流程执行职责。
- 让 workflow entry skill 变成短触发器，而不是复制流程正文。

---

## 4. 当前边界

这些文档目前只表达方案，不直接修改：

- `Workspace/SystemAgent/`
- `.ai-config/skills/`
- `.claude/`
- `.codex/`
- `.windsurf/`
- `openspec/`

后续如要实施，应先从 `06-实施路线图.md` 中选择阶段，再创建独立 `SDD/` 根和首个正式 SDD。
