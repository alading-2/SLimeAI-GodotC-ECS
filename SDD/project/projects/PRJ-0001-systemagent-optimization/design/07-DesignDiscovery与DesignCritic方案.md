# Design Discovery 与 DesignCritic 方案

> 日期：2026-05-24  
> 输入：`Resources/Skills/superpowers/skills/brainstorming/SKILL.md`、`SDD/SlimeAI-SDD-MVP设计.md`、`08-SDD独立化与文档迁移方案.md`、`09-WorkflowSkillRole分层模型.md`、SystemAgent 优化问题清单  
> 结论：`DesignDiscovery` 应作为 capability skill，可单独运行，也可被 workflow 调用；`DesignCritic` 是该 skill 内部使用的条件 role

---

## 1. 背景

新功能、重构和行为改动最容易失败的地方，不是代码写不出来，而是开始时没有把这些问题想清楚：

- 用户真正要解决的问题是什么。
- 哪些需求是必须做，哪些只是可能做。
- 当前系统有什么边界不能破坏。
- 方案会不会过度设计。
- 缺少哪些用户决策会影响实现方向。
- 后续验证应该看行为、数据、UI 还是流程。

`superpowers` 的 `brainstorming` skill 对这个问题给出了强约束：任何创意性工作前都要先探索上下文、澄清问题、提出多方案、写设计、用户审批。

这个思想值得吸收，但它的默认形式不适合 SlimeAI SystemAgent 直接照搬。

---

## 2. 对 superpowers brainstorming 的取舍

### 2.1 值得吸收的部分

| 能力 | 采纳原因 |
| --- | --- |
| 先读项目上下文 | 避免脱离现有结构凭空设计 |
| 先判断任务是否过大 | 防止把多个子系统塞进一个计划 |
| 提出 2-3 个方案和取舍 | 用户能看见可选方向，而不是只看到 AI 单一路径 |
| 给出推荐方案 | 用户不应被迫自己做所有架构判断 |
| 设计隔离和边界意识 | 适合 SlimeAI 的多 Git 边界、GameOS capability 和 SystemAgent 事实源边界 |
| 自检 placeholder、矛盾、范围、歧义 | 能减少设计文档成为新噪音 |

### 2.2 不适合照搬的部分

| 行为 | 不采纳原因 |
| --- | --- |
| 依赖 hook 自动触发 | SlimeAI 已经有 hook 可靠性问题，不应再把流程入口放进 hook |
| 每次一个问题逐项确认 | 对熟悉项目的用户太慢，会打断深度工作流 |
| 每个小改动都强制完整设计 | 会把 SDD 变成新的 ritual |
| 默认写到 `docs/superpowers/specs/` | 不符合 SlimeAI 的独立 SDD 设计 |
| 写完设计后自动 commit | SlimeAI 当前规则允许 AI commit，但不应在设计探索阶段默认 commit |
| 视觉 companion 默认询问 | 当前 SystemAgent 优化不是 UI 设计，不应引入额外通道 |

---

## 3. 建议命名：Design Discovery

不建议沿用 `brainstorming` 名称。

原因：

- `brainstorming` 容易被理解为发散聊天，而 SystemAgent 需要的是可落地的设计成形。
- `brainstorming` 在 superpowers 中绑定了 hook 和逐问逐答习惯。
- SlimeAI 的目标不是“多想几个点子”，而是“在实现前识别缺陷、取舍和用户决策”。

建议名称：`Design Discovery`。

中文语义：设计发现 / 方案成形。

职责：

> 在新功能、重构、行为改动进入 SDD 或实现前，集中识别目标、风险、缺陷、可选方案、默认建议和需要用户确认的问题。

定位：

> `DesignDiscovery` 是 capability skill，不是 workflow，也不是 role。

它可以独立运行，也可以被 `NewFeature`、`WorkflowIteration`、`ResearchAdoption` 等 workflow 调用。

---

## 4. Design Discovery 的交互方式

### 4.1 不采用逐问逐答

SystemAgent 不应像 superpowers 那样强制“一次只问一个问题”。

更适合 SlimeAI 的方式是：AI 先完成一轮上下文分析，然后一次性输出“确认包”。

### 4.2 确认包格式

推荐输出：

```text
Design Discovery:
- Goal: 我理解要解决的问题
- Context Read: 已读/未读的关键上下文
- Main Risks: 主要风险
- Options: 2-3 个可选方案
- Recommendation: 我的推荐方案和原因
- Must Confirm: 不确认就不能安全推进的问题
- Should Confirm: 建议确认，但可用默认值推进的问题
- Defaults I Will Use: 如果用户不补充，我会采用的默认假设
- Not Recommended: 我不建议做的方向
```

### 4.3 用户可以一次性回答

用户不需要逐项点选。

用户可以：

- 只回答 `Must Confirm`。
- 覆盖某个默认假设。
- 补充一个遗漏约束。
- 直接批准推荐方案。

AI 再把结论写入 SDD 的 `design/`、`progress.md` 或 `tasks.md`。

---

## 5. 什么时候触发

### 5.1 必须触发

- 新功能会改变用户、AI、运行时或工具行为。
- 跨模块、跨 Git 边界、跨 SystemAgent workflow。
- 任务需要创建 SDD。
- 用户说“深度思考”“分析缺陷”“设计方案”“不要急着实现”。
- 方案存在明显产品/架构取舍。

### 5.2 建议触发

- Debug fix 可能暴露设计缺陷。
- Hook、skill、rule、workflow、gate 改动。
- GameOS capability contract 改动。
- UI/HUD 或 Godot scene 行为改动。

### 5.3 不触发

- 单文件小修。
- 拼写、链接、格式修复。
- 明确指定的机械迁移。
- 用户明确要求直接执行且风险很低。

---

## 6. Skill 与 Role 的拆分

### 6.1 为什么 DesignDiscovery 是 skill

`DesignDiscovery` 满足 capability skill 的条件：

- 用户可能单独请求它。
- `NewFeature`、`WorkflowIteration`、`ResearchAdoption` 都可能调用它。
- 它有明确输入：用户目标、上下文、边界、约束。
- 它有明确输出：确认包、推荐方案、待确认问题、SDD 更新建议。
- 它会读写稳定 artifact：SDD `design/`、`progress.md`、`tasks.md`、`bdd.md`。

因此它不应只埋在 workflow 文档里。

### 6.2 为什么 DesignCritic 是 role

`DesignCritic` 的职责是用批判视角审查方案：

- 找缺陷。
- 找遗漏。
- 找风险。
- 找替代方案。
- 找用户必须确认的问题。

它本身不是完整流程，也不直接管理 artifact 生命周期。

因此它更适合作为 `DesignDiscovery` skill 内部使用的 role，也可以被 workflow 在设计阶段显式调用。

### 6.3 选项 A：不新增角色，只改 Planner

优点：

- 最少改动。
- 不增加角色数量。
- 容易接入当前 workflow。

缺点：

- Planner 已经承担计划拆分、影响分析、OpenSpec/SDD 判断，继续塞深度批判会变重。
- “挑缺陷、找遗漏、反驳默认方案”容易被执行计划覆盖。

结论：适合作为 lean mode，但不适合作为大任务的长期方案。

### 6.4 选项 B：新增 `DesignCritic` 角色

优点：

- 职责清楚：专门找缺陷、风险、遗漏和更好方案。
- 能在实现前暴露用户需要确认的问题。
- 与 Reviewer 不冲突：Reviewer 查已形成的计划/实现，DesignCritic 查尚未冻结的设计。
- 适合写入 SDD `design/` 和 `progress.md`。

缺点：

- 增加一个 role 文件和 catalog 维护成本。
- 如果强制每个任务都跑，会再次 ritual 化。

结论：推荐采纳，但只在 medium/large 或用户明确要求深度分析时触发。

### 6.5 选项 C：只新增 Gate，不新增角色

优点：

- Gate 比 role 更像检查表。
- 不改变执行角色链。

缺点：

- Gate 往往发生在计划后，太晚。
- Gate 更适合判断合不合格，不适合生成替代方案。

结论：不推荐作为主方案。

---

## 7. 推荐方案

新增 capability skill：`design-discovery`。

新增轻量角色：`DesignCritic`。

不新增 hook。

Workflow 通过 phase 调用 `design-discovery`：

```text
NewFeature:
1. Route and task size classification
2. Call skill: design-discovery
3. Call skill: sdd-management
4. Design document update
5. Task plan
6. Implementation
7. Validation and retrospective
```

在 standalone 模式中，用户可以直接要求运行 `design-discovery`。

在 workflow composed 模式中，workflow 负责决定什么时候调用它、调用后写入哪些 artifact、是否进入下一阶段。

在 small mode 中，`design-discovery` 可以压缩成 3-5 行自检。

在 medium/large mode 中，必须输出确认包。

---

## 8. DesignCritic 角色职责

### 8.1 触发条件

- 用户要求新功能、重构或行为改变。
- 用户要求“深度思考”“分析缺陷”“完善方案”。
- SystemAgent 判断任务需要 SDD。
- Planner 的计划存在关键未知、方案选择或跨边界风险。

### 8.2 输入

- 用户原始请求。
- 当前 selected workflow。
- 相关 DocsAI/SystemAgent/SDD 文件。
- 当前系统边界和 Git 边界。
- 已知约束、禁止事项、验证要求。

### 8.3 输出

```text
DesignCritic:
- Assumptions: 当前方案依赖的假设
- Missing Context: 缺少但重要的上下文
- Design Defects: 可能的设计缺陷
- Better Options: 可选改进方案
- Trade-offs: 关键取舍
- User Decisions: 需要用户确认的问题
- Recommendation: 推荐路径
- SDD Updates: 应写入哪些 SDD 文件
```

### 8.4 禁止事项

- 不直接写实现代码。
- 不要求用户逐个问题回答。
- 不把建议伪装成事实。
- 不阻塞小任务。
- 不新增 hook 自动触发。
- 不替代用户对方向的最终决定。

---

## 9. 与 SDD 的关系

Design Discovery 的结果不应散落在聊天里。

如果任务使用正式 SDD：

- 关键设计分析写入 `design/`。
- 用户确认和默认假设写入 `progress.md`。
- 可执行拆分写入 `tasks.md`。
- 行为预期写入 `bdd.md`。
- 参考来源和开放问题写入 `notes.md`。

如果任务不使用正式 SDD：

- 最终回复中给出简短确认包。
- 不创建额外文档。

---

## 10. 与 Hook 的关系

DesignDiscovery skill 不由 hook 驱动。

Hook 最多只做低频提醒：

```text
This looks like a medium/large new feature. Consider design-discovery and SDD.
```

但不应：

- 弹出逐项问题。
- 阻塞工具调用。
- 自动创建 SDD。
- 自动写设计文档。

---

## 11. 需要写入 SystemAgent 的位置

后续正式实施时应修改：

- `Workspace/SystemAgent/Workflows/NewFeature.md`：加入 `Design Discovery` phase。
- `Workspace/SystemAgent/Skills/DesignDiscovery.md` 或 `.ai-config/skills/systemagent/design-discovery/SKILL.md`：新增 capability skill 正文。
- `Workspace/SystemAgent/Skills/SDDManagement.md` 或 `.ai-config/skills/systemagent/sdd-management/SKILL.md`：新增 SDD 管理 capability skill 正文。
- `Workspace/SystemAgent/Roles/DesignCritic.md`：新增角色正文。
- `Workspace/SystemAgent/Catalog/workflow-catalog.yaml`：把 `design-discovery`、`sdd-management` 和 `DesignCritic` 设为 NewFeature 条件能力。
- `Workspace/SystemAgent/Catalog/systemagent-catalog.yaml`：区分 workflow entry skill、capability skill 和 role。
- `.ai-config/skills/systemagent/systemagent-new-feature/SKILL.md`：短入口中提到 NewFeature 会调用 `design-discovery`。

不建议新增：

- `systemagent-brainstorming` skill。
- brainstorming hook。
- 每问题确认的交互器。

---

## 12. 行为场景

### 场景 1：中大型新功能

```gherkin
Given 用户提出一个会改变多个模块的新功能
When SystemAgent 进入 NewFeature workflow
Then workflow 调用 `design-discovery` skill
And AI 输出 Design Discovery 确认包
And 确认包包含推荐方案、必须确认项、可默认假设和不建议方向
And AI 不开始实现，直到关键确认项被用户接受或默认假设被记录
```

### 场景 2：用户不想逐项回答

```gherkin
Given Design Discovery 发现多个待确认问题
When 用户只回复“按你的建议执行”
Then AI 使用 Recommendation 和 Defaults I Will Use 推进
And 把默认假设写入 SDD progress.md
```

### 场景 3：小任务

```gherkin
Given 用户请求一个低风险单文件小修
When SystemAgent 判断 task_size=small
Then AI 不强制输出完整 Design Discovery
And 只在最终说明中报告关键假设
```

---

## 13. 最终建议

建议将 superpowers brainstorming 的思想转译为 SystemAgent 的 `design-discovery` capability skill，并新增 `DesignCritic` 作为条件角色。

核心取舍是：

- 要“实现前深度思考”。
- 不要“hook 强制逐问逐答”。
- 要“AI 先给完整问题包和推荐”。
- 不要“用户被迫一轮一轮回答”。
- 要“DesignDiscovery 可独立运行，也可被 workflow 调用”。
- 要“结果进入独立 SDD”。
- 不要“散落在 SystemAgent 优化目录或聊天记录”。
