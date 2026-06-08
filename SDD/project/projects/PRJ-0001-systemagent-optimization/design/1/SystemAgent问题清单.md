# SystemAgent 问题清单（合并版）

> 来源：2026-05-24 两份 review 合并（ChatHistory/20260522 复盘 + OpenSpec 流程负担深度分析）
> 范围：Git 边界、Workflow 执行、SDD/OpenSpec、Hook/Gate、文档治理

---

## 核心结论

SystemAgent 已跨过"没有规则"的阶段，进入**"规则存在但执行闭环不稳定、执行成本过高"**的阶段。

- 规则已写进 `Workspace/SystemAgent/`，但 enforcement 能力没有同步增长
- OpenSpec CLI 调用 **207 次/聊天记录**，单次恢复上下文前置开销 **25-30KB**
- `refine-systemagent-workflow` change 自身就是最大负担体现

---

## 问题清单

### Git 边界（3 个）

| ID | 严重度 | 问题 | 证据 |
| --- | --- | --- | --- |
| **SA-GIT-001** | 高 | nested repo / submodule / old mirror 缺少明确判定清单 | 聊天记录多次临场解释 `SlimeAINew/` 边界；依赖 AI 临场判断，非流程保证 |
| **SA-GIT-002** | 中 | 最终 git boundary 摘要未稳定区分存量改动与本轮改动 | `GitPolicy.md` 已要求，但聊天轨迹更像人工自觉，无 gate 稳定检查 |
| **SA-GIT-003** | 中 | 根仓长期未跟踪噪声削弱 boundary 信号 | `Games/BrotatoLikeOld/`、`Resources/*`、`SlimeAIOld/` 常驻 `??`，形成信号疲劳 |

### Workflow（5 个）

| ID | 严重度 | 问题 | 证据 |
| --- | --- | --- | --- |
| **SA-WF-001** | 高 | selected workflow 与 must-read 完成情况未稳定显式输出 | `INDEX.md` 要求先选 workflow，但聊天中极少看到结构化 `selected=...; read=...; pending=...` 声明 |
| **SA-WF-002** | 中 | 角色链主要靠单 agent 自我扮演，缺少独立审查证据 | `WorkflowIteration.md` 要求角色链，但轨迹多为单 agent "我接下来会…"，无角色切换证据 |
| **SA-WF-003** | 中 | gap classification 已定义但未稳定用于问题归因 | 8 种 gap 类型定义在 `WorkflowIteration.md:35-48`，但聊天中问题被自然语言描述，未打标签 |
| **SA-WF-004** | 中 | must-read 清单膨胀到 9 个文件 | `NewFeature` workflow 的 must_read 已有 8 个文件，按 refine 设计还要新增 `Architect.md`；仅 must-read 占 15-30KB 上下文 |
| **SA-WF-005** | 中 | 缺少"中等复杂度任务最小流程" | 当前默认每个任务都进入满配 ritual（README→INDEX→Workflow→Role→Gate），中型任务被过度拖重 |

### SDD / OpenSpec / 执行记忆（5 个）

| ID | 严重度 | 问题 | 证据 |
| --- | --- | --- | --- |
| **SA-SDD-001** | 高 | execution-log 更像额外负担，尚未成为长任务主恢复工具 | 每 task 需同步 `tasks.md` + `execution-log.md`，2 次文件操作； Resume Notes 收益被高估（节省 1K token vs contextFiles 25KB 开销仅 4%） |
| **SA-SDD-002** | 高 | OpenSpec CLI 命令调用过度频繁，上下文消耗过大 | 聊天记录中 `openspec` 命令出现 **207 次**；`instructions apply` 单次返回 **229 行/~12KB**；恢复上下文前置开销 **25-30KB（~12-15% 上下文）** |
| **SA-SDD-003** | 中 | 测试优先虽已写入 gate，但前置触发仍弱 | 聊天轨迹：调研(30%)→盘点(20%)→计划(20%)→失败覆盖(10%)→实现(15%)→补验证(5%) |
| **SA-SDD-004** | 中 | `refine-systemagent-workflow` 的某些设计可能与减负目标冲突 | 新增 Brainstorming 阶段 + Architect 角色 + Design Hard Gate + 4 Discipline skill，增加仪式而非减少 |
| **SA-SDD-005** | 低 | execution-log 模板过长，维护成本高 | 模板要求 10 个字段（Change/Objective/Last updated/Read context/Decisions/Findings/Progress/Validation/Open questions/Resume Notes） |

### Hook / Gate / 自动化（4 个）

| ID | 严重度 | 问题 | 证据 |
| --- | --- | --- | --- |
| **SA-HOOK-001** | 高 | Stop hook 在真实对话中多次失效 | `1.Data重构.md` 和 `3.md` 多次出现 `Stop hook (failed)` / `invalid stop hook JSON output` |
| **SA-HOOK-002** | 高 | advisory hook 不能证明关键验证行为真的发生 | PostToolUse 只提示"请读取输出"，无法阻止"跑了命令却没读结果"的假完成 |
| **SA-HOOK-003** | 中 | PostToolUse 提示疲劳 | 聊天中 PostToolUse 触发 **50+ 次**，输出内容始终相同，AI 形成习惯化忽略 |
| **SA-HOOK-004** | 中 | RV-BEHAVIOR-COMPLIANCE 缺少稳定输入来源 | 需要 must-read 记录、角色激活记录、工具调用序列，但系统无机制产出这些结构化输入 |

### 文档治理（2 个）

| ID | 严重度 | 问题 | 证据 |
| --- | --- | --- | --- |
| **SA-DOC-001** | 中 | 历史 review / prototype / chat 材料仍对正式入口形成检索噪声 | `Workspace/DocsAI/Reviews/`、`Workspace/SlimeAISystemAgent/` 等目录对 AI 仍有"看起来像入口"的诱惑 |
| **SA-DOC-002** | 中 | review 文档与后续修复 change 缺少编号追踪链 | 同类问题在不同 review 里重复出现，用户难以判断"已修 / 在修 / 重新发现" |

---

## 优先级

### P0：先保证"规则能稳定生效"
1. **SA-HOOK-001**：验证 Stop hook 全链路稳定性，纳入 hook smoke
2. **SA-WF-001**：让 selected workflow + must-read 状态成为显式输出
3. **SA-GIT-001**：补 nested repo / submodule / old mirror 的判定清单

### P1：降低长任务执行负担
4. **SA-SDD-002**：修改 `openspec-apply-change` skill，增加 Resume Mode（跳过重复 status/instructions apply）
5. **SA-SDD-001**：用 Resume Notes 替代独立 execution-log.md（已规划在 refine 中，但需验证收益）
6. **SA-GIT-002**：让最终 git boundary 摘要模板标准化
7. **SA-DOC-002**：review 问题带 ID，映射到 change/task

### P2：重新评估 refine-systemagent-workflow 的某些设计
8. **SA-SDD-004**：Brainstorming 阶段改为可选；Architect 角色考虑作为 Planner 子步骤；用户说"开始做"时跳过 Design Hard Gate
9. **SA-WF-005**：明确"中等复杂度任务最小流程"，不要默认满配 ritual
10. **SA-HOOK-003**：PostToolUse 增加内容去重和升级机制

### P3：再考虑更强自动化
11. **SA-HOOK-002**：评估 advisory hook 是否对关键遗漏升级为 block
12. **SA-HOOK-004**：评估是否为 workflow 运行留下轻量 state/trace
13. **SA-GIT-003**：清理根仓长期未跟踪噪声（workspace hygiene）

---

## 结构性悖论（5 个）

这些悖论解释了"为什么改善流程反而增加负担"：

1. **用流程改善流程的元负担**：`refine-systemagent-workflow` change 自身就是最大流程负担（84 行 tasks、229 行 instructions apply、每次继续都要重跑完整流程）
2. **精简 skill 导致单个 skill 更复杂**：砍了 4 个 openspec skill 并入 plan/execute，但 plan/execute 变得更重；新增 4 个 Discipline skill，净概念层级增加
3. **Resume Notes 收益被高估**：声称节省 1K token/会话，但 contextFiles 消耗 25KB，Resume Notes 只占 4%
4. **增加阶段 vs 减少准备时间的冲突**：新增 Brainstorming + Design Hard Gate 提高了质量门槛，但直接对抗"减少准备时间"的目标
5. **验证铁律的理想与现实**：`RV-TEST-COVERAGE` 要求太重（artifact oracle + RED/GREEN + BDD），AI 倾向于先实现再补轻量验证，再增加"铁律"若无 enforcement 只会再增加一条退化规则

---

## 建议的修复路径

如果进入 `WorkflowIteration` 或 OpenSpec change，建议按 4 组拆任务：

1. **Hook reliability**：补全链路 smoke，把 hook 成功率本身作为可验证产物
2. **Workflow traceability**：固化 workflow 选择输出格式，让 RV-BEHAVIOR-COMPLIANCE 能拿到真实输入
3. **Git boundary hardening**：判定清单 + 根仓未跟踪降噪
4. **OpenSpec 减负**：修改 skill 减少命令调用、评估 refine 设计的冲突项

---

## 最根本的原则

> **每增加一个新阶段、新角色、新 gate、新 skill 之前，必须回答："它防止了什么具体错误？节省的认知资源是否大于新增成本？"**

如果不能给出可量化的回答，就不应该新增。
