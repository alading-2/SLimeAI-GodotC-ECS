# OpenSpec 退场与兼容策略

> 日期：2026-05-24  
> 目标：明确 OpenSpec 如何从 SystemAgent 默认路径退场，同时保护已有 change 和 baseline 规格

---

## 1. 为什么要退场

OpenSpec 作为通用 spec 工具是有价值的，但对 SlimeAI 当前工作区来说默认路径偏重。

主要成本：

- 命令面宽：`propose/explore/apply/archive/status/instructions/validate/sync/schema`。
- 恢复成本高：`instructions apply` 和 `contextFiles` 让 AI 每次继续前读大量内容。
- artifact 同步复杂：`tasks.md`、`execution-log.md`、delta specs、archive merge 多处维护。
- 容易把“流程正确”误当成“任务推进”。

独立 SDD 的目标是替代 OpenSpec 在 SlimeAI 中的默认任务管理职责，而不是否认 OpenSpec 的参考价值。

---

## 2. 退场边界

### 2.1 不立刻删除 OpenSpec

已有 OpenSpec change 继续按原流程收尾。

不做批量迁移，不直接删除 `openspec/`。

### 2.2 新任务优先 SDD

当新任务满足以下条件时，默认创建或读取 SDD：

- 中大型任务。
- 跨模块或 Git 边界。
- 修改 SystemAgent workflow/rule/hook/skill/gate。
- 修改 GameOS capability contract。
- 用户要求长期计划、深度分析、可恢复执行。

### 2.3 OpenSpec 保留为兼容路径

OpenSpec 继续用于：

- 已创建但未完成的 change。
- 需要 archive 合并 baseline spec 的历史任务。
- 外部工具或历史文档引用。
- 对比参考。

---

## 3. 分阶段策略

### Phase 0：文档确认

当前阶段。

产物：

- `SDD/SlimeAI-SDD-MVP设计.md`
- `01-独立SDD转向方案.md`
- `08-SDD独立化与文档迁移方案.md`
- 本退场策略文档

不改 SystemAgent 正文，不改 `.ai-config`。

### Phase 1：SDD 手动试运行

选择 1 个真实 SystemAgent 优化任务，用独立 `SDD/` 根目录下的手写 SDD 跑完。

目标：

- 验证 `README/design/tasks/progress/bdd/notes/artifacts` 是否足够。
- 验证恢复成本是否明显低于 OpenSpec。
- 验证用户是否更容易 review。
- 验证设计文档进入单个 SDD 的 `design/` 后是否能减少跨目录索引。

不需要先实现 CLI。

### Phase 2：SDD 模板与只读校验

创建模板和只读 validate/list/show。

优先级：

1. `sdd validate`
2. `sdd list`
3. `sdd show`
4. `sdd index`

此阶段仍不替换 rules 默认入口。

### Phase 3：SystemAgent 软接入

修改 SystemAgent workflow 和 wrapper skill：

- 新任务优先判断是否需要 SDD。
- 已有 OpenSpec change 继续使用 OpenSpec。
- Workflow 文档中加入 SDD 读取顺序。
- NewFeature workflow 加入 Design Discovery，但不新增 hook。
- Stop checklist 提醒 SDD 更新，但不阻塞。

### Phase 4：默认入口切换

当 SDD 跑通真实任务后，再修改全局规则：

- 中大型任务默认 SDD。
- OpenSpec 标记为 legacy/compat path。
- `openspec-apply-change` 不再是默认“开始执行”入口。
- Design Discovery 结果默认写入 SDD `design/` 和 `progress.md`。

### Phase 5：历史清理

最后处理：

- 已归档 OpenSpec change 是否保留。
- baseline specs 是否迁移或只读冻结。
- 旧文档中 OpenSpec 默认描述是否更新。

---

## 4. 已有 OpenSpec change 的处理

### 4.1 active change

原则：不强行迁移。

如果一个 active OpenSpec change 已经有完整 tasks 和 specs：

- 继续用 OpenSpec 完成。
- 可以额外创建 SDD 作为恢复辅助，但不要求。
- 不要同时维护两套任务状态，避免分裂。

### 4.2 已归档 change

保持历史记录。

不迁移到 SDD。

### 4.3 未开始或过期 change

可以人工评估：

- 删除。
- 归档为历史。
- 转成新 SDD。

转换时只保留仍有效的设计结论，不逐字搬运 artifact。

---

## 5. SystemAgent 文档需要更新的点

后续实施时应检查：

- `Workspace/SystemAgent/README.md`
- `Workspace/SystemAgent/INDEX.md`
- `Workspace/SystemAgent/Workflows/*.md`
- `Workspace/SystemAgent/Protocols/OpenSpecChangeProtocol.md`
- `Workspace/SystemAgent/Protocols/OpenSpecExecutionMemoryProtocol.md`
- `.ai-config/skills/systemagent/*/SKILL.md`
- `.ai-config/rules/rules.md`

更新方向：

- OpenSpec 从默认路径改成兼容路径。
- SDD 成为中大型任务默认上下文胶囊。
- `execution-log.md` 不再作为新任务默认执行记忆。
- `progress.md` 成为 SDD 运行恢复事实源。

---

## 6. 风险与缓解

### 6.1 风险：SDD 变成新的 OpenSpec

缓解：

- 小任务不强制 SDD。
- 不默认生成过多附属文件。
- CLI MVP 只做少数命令。
- README 限制为入口卡片。

### 6.2 风险：OpenSpec 与 SDD 双轨混乱

缓解：

- 已有 OpenSpec change 继续 OpenSpec。
- 新任务优先 SDD。
- 一个任务只选择一个主任务管理源。
- 在 SDD README 中明确是否关联 OpenSpec。

### 6.3 风险：baseline specs 丢失价值

缓解：

- 不批量删除 baseline specs。
- 长期 contract 仍应落入 `SlimeAI/DocsAI/` 或 SystemAgent 正文。
- SDD 完成后提炼长期结论，不把 SDD 当永久 contract。

---

## 7. 最终建议

OpenSpec 退场应是“默认入口退场”，不是“目录立刻删除”。

推荐策略：

```text
新任务 -> SDD
旧 OpenSpec change -> OpenSpec 收尾
长期知识 -> DocsAI / SystemAgent 正文
历史记录 -> 保留或归档
```

这样能避免一次性迁移风险，也能马上降低新任务的命令和上下文成本。
