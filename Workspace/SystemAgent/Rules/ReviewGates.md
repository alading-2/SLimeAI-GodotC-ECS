# Review Gates

共享 review gate 库。每个 gate 使用统一五字段：`Trigger`、`Context to pass`、`Prompt`、`Verdicts`、`Special handling`。Verdict 输出必须符合 `Workspace/SystemAgent/Rules/VerdictVocabulary.md`。

## Evidence contract

验证结论必须引用可复查证据。`无 error`、`exit code 0`、stdout PASS marker 或 clean log 只能作为诊断信号，不能单独作为正确性证明。

Godot validation claim 必须同时检查：

- run `index.json` 中对应 scene `status=passed` 且 `exitCode=0`。
- per-scene `result.json` 中 `status=passed`、`exitCode=0` 且无阻塞 `firstError`。
- scene artifact 中 `status=pass`、`failureReasons=[]`。
- scene artifact 的 `expectedInputs`、`expectedObservations`、`passCriteria`、`failCriteria`、`artifactPath` 非空。
- scene artifact `checks[]` 覆盖 BDD mapping、manifest 或本轮声明的关键 check。

缺少上述 artifact oracle 时，Reviewer 必须把验证 claim 标为 `REJECT` 或降级为未验证，不得用 process 成功替代。

## Workflow / phase mapping

| Workflow / phase | Gate ID |
| --- | --- |
| NewFeature / Plan / SDD | `RV-PLAN-FEASIBILITY` |
| NewFeature / Implement (pre-implement TDD) | `RV-TEST-COVERAGE` |
| NewFeature / Implement | `RV-IMPL-BOUNDARY` |
| NewFeature / Implement | `RV-INTEGRATION-BEHAVIOR` |
| NewFeature / DebugFix / Implement | `RV-IMPL-BOUNDARY` |
| WorkflowIteration / ValidationRelease / Integration evidence | `RV-INTEGRATION-BEHAVIOR` |
| WorkflowIteration / ValidationRelease / Docs sync | `RV-DOC-SYNC` |
| ConfigMaintenance / sync | `RV-CONFIG-SYNC` |
| WorkflowIteration / Retrospective | `RV-BEHAVIOR-COMPLIANCE` |
| All workflows / completion | `RV-RETROSPECTIVE` |

### Gate: RV-PLAN-FEASIBILITY

**Trigger**: 进入 Implement 之前，或 SDD 设计阶段完成后。

**Context to pass**:
- SDD `design/` / `tasks.md` / `progress.md` 全文
- `python3 Workspace/SDD/sdd.py show <sdd-id>` 输出
- selected workflow 与 owner skill ID（可选）

**Prompt**:
检查任务是否可执行：任务粒度是否独立可验证、设计是否引用真实路径、capability 列表是否与 BDD/设计对齐、是否存在未定义依赖或范围外风险。

**Verdicts**:
- `APPROVE` — 全部检查通过；可进入 Implement。
- `CONCERNS` — 存在非阻塞问题，列出后允许继续，并进入 retrospective 跟进。
- `REJECT` — 存在阻塞问题，必须修订后重跑 gate。

**Special handling**:
- `REJECT` 阻塞项必须写入当前 SDD `tasks.md` 或明确标为阻塞原因。
- `CONCERNS` 使用 `CONCERNS: <issue1>; <issue2>` 便于 grep。

### Gate: RV-TEST-COVERAGE

**Trigger**: Implement 阶段开始前，TestDesigner 的验收标准已写好，Implementer 的 TDD 微循环（RED→GREEN→REFACTOR，见 `Workspace/SystemAgent/Rules/TDDProtocol.md`）中测试已编写并确认 RED 之后。

**Context to pass**:
- SDD `design/` / `bdd.md` 相关段落
- Runtime test、validator 或 Godot scene README / artifact
- Godot scene 的 `index.json`、per-scene `result.json`、scene artifact 和 `checks[]`（如涉及 Godot）
- TDD RED/GREEN evidence path 或无法执行 RED 的明确原因
- SeniorGameDeveloper / SeniorProgrammer findings（如触发）
- 失败模式列表（如有）

**Prompt**:
检查是否存在可复验标准答案：输入、期望观察、passCriteria、failCriteria、artifactPath 是否非空；artifact checks 是否覆盖 BDD/manifest 声明；smoke 是否被错误替代专项验收；新 capability 是否有对应验证；RED/GREEN 证据是否证明测试能失败再通过；高级审查 findings 是否已处理或转为阻塞/follow-up。

**Verdicts**:
- `APPROVE` — 标准答案完整，artifact oracle 对齐，无以 smoke 替代专项的情况，RED/GREEN 证据可复查。
- `CONCERNS` — 字段存在但不够具体，或失败模式覆盖不足。
- `REJECT` — 字段为空、缺 artifactPath、缺 `index.json` / `result.json` / scene artifact、artifact `failureReasons` 非空、缺 RED/GREEN 证据，或用“无 error / exit code 0 / PASS marker”作为 passCriteria。

**Special handling**:
- `artifactPath` 为空一律 `REJECT`。
- Godot 场景只看到 stdout PASS marker 但没有完整 artifact oracle，一律 `REJECT`。
- SeniorGameDeveloper 或 SeniorProgrammer 对测试覆盖给出阻塞 finding 时，聚合 verdict 不得高于 `REJECT`；非阻塞 finding 必须进入 `CONCERNS` 或 action item。

### Gate: RV-IMPL-BOUNDARY

**Trigger**: Implement 阶段完成后，提交或最终汇报之前。

**Context to pass**:
- `git status --short` 或 `git diff --name-only`
- proposal impact 段或用户授权范围
- 触发的 owner skill ID 与文件范围

**Prompt**:
检查修改范围是否越界、是否触碰禁改副本、是否跨 git 边界混改、`.ai-config` 改动是否已准备同步。

**Verdicts**:
- `APPROVE` — 修改范围在预期内；未触碰禁改副本。
- `CONCERNS` — 有少量范围外修改但理由明确，或同步已记录待完成。
- `REJECT` — 触碰禁改副本作为源，或修改了未授权跨仓库文件。

**Special handling**:
- 发现直接修改 `.ai-config/sync-targets.json` 中定义的 skill/rule 同步副本作为源时一律 `REJECT`。

### Gate: RV-INTEGRATION-BEHAVIOR

**Trigger**: Implement 阶段完成后，当本轮改动涉及 ≥2 个 Capability 或 GodotBridge 表现层（UI、Camera、Input、Animation、Collision）时。

**Context to pass**:
- `git diff --name-only` 或本轮修改文件清单
- 涉及的 Capability / GodotBridge 组件列表
- 相关 gameplay lifecycle BDD 或当前 SDD `bdd.md` 全文
- `SlimeAI/DocsAI/GameOS/GodotPitfalls.md`（如涉及 UI/Camera/Input）
- 相关 validation scene 的 `index.json`、`result.json`、scene artifact、manifest/catalog/README 路径和 artifact check names
- SeniorGameDeveloper / SeniorProgrammer findings（如触发）

**Prompt**:
以资深游戏开发者和 bug 审查者的角度，检查本轮改动：
1. 是否破坏了其他系统的状态假设？（例如：改了 UI 没有检查 Camera 是否仍然跟随玩家；改了输入没有检查死亡状态下是否仍然响应）
2. 是否引入了跨系统状态冲突？（例如：死亡时多个系统各自修改 `CanMoveInput`，互相覆盖）
3. 对照 `gameplay-lifecycle-integration/bdd.md`，本轮改动的系统组合是否通过了相关集成场景，且证据是否来自 artifact checks 而不是 PASS marker？
4. 对照 `GodotPitfalls.md`，是否触碰了已知陷阱（坐标系、Camera2D 启用、生命周期异常、死亡输入门控）？
5. 如果高级审查角色被触发，Reviewer 是否聚合了 SeniorGameDeveloper / SeniorProgrammer findings？

**Verdicts**:
- `APPROVE` — 跨系统状态无冲突；关键集成场景已有完整 artifact oracle 证据；无已知陷阱被触发；高级审查 findings 已处理。
- `CONCERNS` — 存在潜在集成风险但本轮改动范围不直接触发，或风险已记录 follow-up action。
- `REJECT` — 现有改动直接破坏了集成行为（如死亡后可移动、血条坐标系错误、Camera 被意外禁用）、触及已知陷阱且无防护、缺少完整 artifact oracle，或高级审查发现阻塞且未解决。

**Special handling**:
- `REJECT` 时必须列出具体被破坏的集成场景（引用 bdd.md Scenario 名）和触及的陷阱（引用 GodotPitfalls.md 陷阱编号）。
- 纯数据（DataOS seed）或纯 Runtime（不涉及 GodotBridge 表现层）的改动可跳过本 gate。
- Debug fix workflow 中本 gate 为 optional（bug 修复本身就是集成问题驱动的），但 Reviewer 仍应检查修复是否引入新的集成问题。
- `无 error`、`exit code 0`、stdout PASS marker 或 clean analyzer summary 不能替代 `index.json` + `result.json` + scene artifact checks。

### Gate: RV-DOC-SYNC

**Trigger**: Validate 阶段完成后或文档治理 change 收尾前。

**Context to pass**:
- 本轮修改文档清单
- `Workspace/SystemAgent/README.md` 与相关目录 `INDEX.md`
- SDD 影响的 design / DocsAI 路径

**Prompt**:
检查 README/INDEX、DocsAI、SDD、ProjectState、Contract、Debug、ApiIndex 是否与实现和路径迁移一致；旧路径是否只剩历史、迁移指针或 archive 命中。

**Verdicts**:
- `APPROVE` — 文档均已同步或无需更新且有理由。
- `CONCERNS` — 非阻塞文档滞后已记录 action item。
- `REJECT` — 当前入口仍指向旧事实源或关键状态文档过时。

**Special handling**:
- 纯文档 change 轻量执行：重点检查入口路径、旧路径命中分类和 SDD/DocsAI 状态。

### Gate: RV-CONFIG-SYNC

**Trigger**: `.ai-config`、hook、subagent、sync 脚本或 skill-test 修改后。

**Context to pass**:
- `git status --short`
- `sync-ai-config.sh` 输出
- skill-test lint 输出
- hook smoke 输出（如修改 hook）

**Prompt**:
检查 `.ai-config` 是否为源、同步副本是否由脚本生成、hook/subagent 是否直接维护、skill-test 是否运行并报告可解释。

**Verdicts**:
- `APPROVE` — sync/lint/smoke 已跑或不适用，副本一致。
- `CONCERNS` — sync 有 advisory warning 但无 critical failure。
- `REJECT` — sync 未跑、源副本不一致、或 hook 路径不可执行。

**Special handling**:
- `REJECT` 时输出第一个不一致文件或失败命令。

### Gate: RV-BEHAVIOR-COMPLIANCE

**Trigger**: WorkflowIteration 执行期间，或任何 workflow 的 Retrospective 阶段发现 AI 行为偏差。

**Context to pass**:
- 当前 workflow 的 must_read 列表与 AI 实际读取记录
- required_roles 列表与 AI 实际激活的角色
- 工具调用序列（是否存在连续写操作无验证的模式）
- gate 通过/跳过记录
- 如当前任务使用 SDD：当前 SDD `progress.md` 的 selected workflow / must-read 状态、`tasks.md`、`bdd.md` 和 validation evidence 摘要

**Prompt**:
检查 AI 是否遵守当前 workflow 的行为约束：must-read 是否全部被读取、角色是否按 required_roles 顺序激活、是否存在连续写操作无验证的模式、是否跳过了 workflow 规定的 gate。当前任务使用 SDD 时，优先检查 SDD artifact 是否记录 selected workflow、must-read 状态、tasks/progress/bdd/validation；不得只凭聊天记忆证明流程已完成。

**Verdicts**:
- `APPROVE` — 行为轨迹与 workflow 规定一致，无跳过。
- `CONCERNS` — 存在非阻塞偏差（如 must-read 延迟读取、角色顺序微调），列出偏差项。
- `REJECT` — 跳过 gate、未读 must-read 直接行动、或连续写操作无验证超过 3 次。

**Special handling**:
- `REJECT` 时必须列出具体跳过的 gate ID 和未读的 must-read 路径。
- `CONCERNS` 使用 `CONCERNS: <issue1>; <issue2>` 便于 grep。
- SDD artifact 缺失 selected workflow、must-read 状态或 validation evidence 时，至少标为 `CONCERNS`；如果因此无法复查任务完成声明，标为 `REJECT`。

### Gate: RV-RETROSPECTIVE

**Trigger**: 所有实现和验证完成后，最终报告前。

**Context to pass**:
- retrospective 报告草稿
- SDD `tasks.md` 完成情况
- 验证结果摘要
- SeniorGameDeveloper / SeniorProgrammer findings 与处理状态（如触发）
- 外部资源使用记录（如有）
- SDD `progress.md`（如任务使用 SDD）
- SDD `bdd.md` 与 validation evidence 摘要（如任务使用 SDD）

**Prompt**:
检查 retrospective 是否覆盖计划偏差、测试覆盖、日志质量、文档同步、skill/rule/hook/subagent 缺口、外部资源策略、触发的 senior findings 处理状态和剩余风险。
若当前任务使用 SDD，还必须检查 `progress.md` 是否存在、是否包含 Latest Resume、selected workflow / must-read 状态、关键决策、验证证据和下一步，且是否与 `tasks.md`、`bdd.md` 状态一致；缺失或明显过期时不得把结论判为通过。

**Verdicts**:
- `APPROVE` — 缺口已处理或无缺口，证据完整。
- `CONCERNS` — 存在非阻塞 follow-up。
- `REJECT` — 关键字段为空、verdict 不合法或未处理阻塞缺口。

**Special handling**:
- `verdict` 必须是 `APPROVE / CONCERNS / REJECT`；与 conclusion 不一致一律 `REJECT`。
- 未聚合已触发 SeniorGameDeveloper / SeniorProgrammer findings 时，completion/retrospective gate 一律 `REJECT`。
