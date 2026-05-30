# SDD 独立化与文档迁移方案

> 日期：2026-05-24  
> 输入：`SDD/SlimeAI-SDD-MVP设计.md` 最新版本、SystemAgent 优化文档 01-10  
> 结论：正式 SDD 应从 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/` 独立出来；设计文档应进入具体 SDD 的 `design/` 目录

---

## 1. 问题

前一版 SystemAgent 优化文档把 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/` 当作 SDD 制度说明目录，并把 SystemAgent 优化设计文档放在 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/` 根下。

这与最新 SDD 设计已经不完全一致。

最新 SDD 设计的关键变化是：

- SDD 是独立任务管理系统，不是 SystemAgent 优化目录的一个子目录。
- 一个 SDD 文件夹就是一个任务的完整归档单元。
- 设计文档不应长期散落在 `Workspace/DocsAI/Idea/...`。
- 设计文档应放入具体 SDD 的 `design/`，并由 `design/INDEX.md` 管理。
- SystemAgent 只消费 SDD，不负责归档 SDD 设计文档。

因此，`SDD/project/projects/PRJ-0001-systemagent-optimization/design/` 当前更适合作为“过渡分析区”和“迁移说明区”，不应继续被描述成正式 SDD 容器。

---

## 2. 新边界

### 2.1 正式 SDD 根目录

推荐正式根目录：

```text
/home/slime/Code/SlimeAI/SDD/
```

逻辑结构：

```text
SDD/
├── README.md
├── INDEX.md
├── catalog.json
├── pending/
├── active/
├── blocked/
└── done/
```

### 2.2 单个 SDD

SystemAgent 优化本身后续应成为第一个真实 SDD：

```text
SDD/active/SDD-0001-systemagent-optimization/
├── README.md
├── sdd.json
├── design/
│   ├── INDEX.md
│   ├── SystemAgent问题清单.md
│   ├── 独立SDD转向方案.md
│   ├── Workflow与Skill触发优化方案.md
│   ├── Hook与Gate重写方案.md
│   ├── Git与Worktree策略.md
│   ├── OpenSpec退场与兼容策略.md
│   ├── 实施路线图.md
│   ├── DesignDiscovery与DesignCritic方案.md
│   ├── SDD独立化与文档迁移方案.md
│   ├── WorkflowSkillRole分层模型.md
│   └── Subagent使用场景与采纳策略.md
├── tasks.md
├── progress.md
├── bdd.md
├── notes.md
└── artifacts/
```

### 2.3 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/` 的新定位

该目录不再是正式 SDD 目录。

建议定位为：

- 当前过渡分析区。
- 用户可读的问题与方案草稿入口。
- 第一份 SDD 创建前的材料暂存区。
- 后续 SDD 创建后，保留一个短索引指向正式 SDD。

不建议长期保留多份完整设计正文。

---

## 3. 迁移策略选项

### 3.1 选项 A：只改文档，不移动文件

做法：

- 只更新 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/` 下的文档措辞。
- 明确正式 SDD 应在独立 `SDD/` 根目录。
- 当前文件作为过渡材料保留。

优点：

- 风险最低。
- 不破坏用户当前打开的文件。
- 不制造重复文档。

缺点：

- 正式 SDD 还没有落地。
- 设计文档仍临时散落在 Idea 目录。

适用：当前阶段仍是方案确认，尚未开始 SDD MVP 实施。

### 3.2 选项 B：建立独立 SDD，并复制设计文档

做法：

- 创建 `SDD/active/SDD-0001-systemagent-optimization/`。
- 把当前 01-10 文档复制进 `design/`。
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/README.md` 改成短指针。

优点：

- 符合最新 SDD 设计。
- 立即验证 SDD 文件结构。
- 后续恢复上下文更清楚。

缺点：

- 会出现一段时间的双份文档。
- 需要明确哪个是 canonical。
- 如果不删除旧文档，容易漂移。

适用：用户确认要开始 SDD MVP 手动试运行。

### 3.3 选项 C：建立独立 SDD，并移动设计文档

做法：

- 创建独立 SDD。
- 将当前 01-10 和问题清单移动到 SDD `design/`。
- 在 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/` 只留下 README 指针。

优点：

- canonical 清楚。
- 不重复维护。
- 最符合 SDD “完整归档单元”原则。

缺点：

- 会删除/移动用户当前正在看的文件路径。
- 当前根仓已有较多未提交改动，移动大量文件会增加 review 成本。
- 如果 SDD 模板和 validate 尚未落地，可能过早固化目录。

适用：用户明确批准迁移，并接受路径变化。

---

## 4. 推荐方案

当前推荐：先执行选项 A，再把选项 C 作为下一阶段正式实施。

原因：

- 用户当前要求是更新 SystemAgent 优化相关内容，并要求深度分析。
- 独立 `SDD/` 根目录还不存在，贸然移动会制造路径断裂。
- 当前文档仍处于方案评审期，不应马上把它伪装成已落地的 SDD。
- 先改边界表述，可以避免继续写出错误方向。

推荐下一步：

1. 更新 `README.md`、`01`、`02`、`05`、`06`、`07`、`09`、`10` 的路径和边界表述。
2. 明确 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/` 是历史/临时位置，不是未来正式根。
3. 在 `06-实施路线图.md` 中把 Phase 1 改为“创建独立 SDD 根与首个 SDD”。
4. 等用户确认后，再创建 `SDD/active/SDD-0001-systemagent-optimization/` 并迁移设计文档。

---

## 5. 文档更新规则

后续修改 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/` 时应遵守：

- 不再写“`项目级 SDD design/` 是 SDD 制度目录”。
- 不再建议把正式 SDD 放在 SystemAgent 优化目录下。
- 不再使用单个 `design.md` 表示完整设计。
- 改用 `design/` 和 `design/INDEX.md`。
- 明确 SystemAgent 只引用 SDD，不管理 SDD 归档。
- 明确 Design Discovery 的结果应进入 SDD `design/`，而不是永久停留在聊天或 Idea 目录。

---

## 6. 最终建议

SDD 独立化不只是路径替换，而是职责边界调整：

```text
SystemAgent = 流程与治理事实源
SDD = 中大型任务的设计、执行、恢复和归档单元
SDD/project/projects/PRJ-0001-systemagent-optimization/design = 过渡分析与用户评审区
```

正式迁移时，应优先把当前这些设计文档整体收拢到首个 SDD 的 `design/`，再用 `README.md + progress.md + tasks.md` 支撑后续恢复。
