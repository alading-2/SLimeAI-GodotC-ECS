# BDD 场景文档格式

> BDD 在当前 SystemAgent 中是 FeatureSpec 的行为场景层：把自然语言需求写成 Given-When-Then 或等价结构化行为例子。不需要 parser、runner、框架依赖。对 AI 来说，BDD 场景是可读的行为验收清单。

## 定位

- **设计文档** = 问题、方向、取舍和边界
- **FeatureSpec** = 功能、实现指引和 TDD 交接
- **BDD 场景** = FeatureSpec 中的行为验收契约（在什么情况下、做了什么、期望得到什么结果）
- **SDD** = 任务状态、进度、阻塞和验证入口
- **TDD/测试** = 代码级验证（断言、scene test、runtime test）

这些 artifact 互补。设计文档管方向，FeatureSpec 管功能与代码落点，BDD 场景管行为验收，TDD 管验证执行，SDD 管任务恢复。

## 文件位置

```text
SDD/project/projects/<project>/design/
  <design>.md              <- 设计文档（问题、方向、取舍）
  <design>.FeatureSpec.md  <- 功能实现规格（功能、行为、代码落点、TDD 交接）

SDD/project/projects/<project>/sdds/<sdd-id>/
  bdd.md                   <- 当前任务行为摘录或 FeatureSpec Source 引用
```

如果 FeatureSpec 里已有清晰的行为场景，SDD `bdd.md` 只需要引用 Source 和列出本轮执行的 feature / scenario。

## 格式：Markdown + Gherkin 代码块

```markdown
# Feature: <能力名>

> 一句话描述这个 Feature 的业务价值

## Background（可选）

所有 Scenario 共享的前置条件。放在这里避免重复写 Given。

## Scenario: <场景名>

描述一个具体的行为。用 bullet list 写 Given-When-Then，不要用 fenced code block（AI 读 list 更直观）。

- **Given** <前置状态或条件>
- **And** <额外前置条件>
- **When** <动作或事件>
- **Then** <期望结果>
- **And** <额外期望>

## Scenario: <另一个场景>

- **Given** <前置状态>
- **When** <动作>
- **Then** <期望结果>
```

## 写场景的原则

1. **一个 Scenario 只测一个行为。** 不要把多个独立行为塞到一个 Scenario 里。
2. **Given 描述状态，When 描述动作，Then 描述可验证的结果。** 不要混用。
3. **Then 必须能验收。** 写完后问自己："AI 能根据这个 Then 判断是否做对吗？"
4. **不要把 BDD 场景写成完整实现方案。** 代码落点写在 FeatureSpec 的 Implementation Guidance；BDD 场景说"发送攻击请求后敌人血量减少"。
5. **边界条件单独写 Scenario。** "成功登录"和"密码错误"是两个 Scenario。

## 什么时候写 BDD 场景

| 场景 | 是否写 BDD |
|------|-----------|
| 新 capability / 框架级功能 | 推荐写 |
| 用户给了明确验收条件 | 必须写 |
| FeatureSpec 的 WHEN/THEN 已足够清晰 | 不需要单独写 |
| typo / 链接 / 注释修复 | 不需要 |
| Godot scene validation 场景 | 推荐写（已有 WHEN/THEN 基础） |

## 与 FeatureSpec / SDD 的分工

| 内容 | 写在 FeatureSpec | 写在 SDD bdd.md |
|------|-------------|-------------|
| Feature 标题 | 是 | 可摘录 |
| 技术约束/接口签名 | 是 | 否 |
| WHEN/THEN 行为描述 | 是 | 当前任务摘录 |
| 具体输入/输出示例 | 是 | 当前任务摘录 |
| 边界条件/错误场景 | 是 | 当前任务摘录 |
| SDD 当前执行范围 | 否 | 是 |

## AI 写场景的步骤

1. 读完设计文档、FeatureSpec、当前 SDD 的 `tasks.md` 和 `progress.md`，理解方向、功能和当前任务范围。
2. 识别任务中**需要验收的关键行为**（不是全部功能）。
3. 每个关键行为写一个 Scenario。
4. 检查 Then 是否可验证。
5. 在 FeatureSpec、SDD design 或 `progress.md` 中引用 bdd.md（如单独写了文件）。

## BDD Scenario 到 Godot 验证场景的映射

BDD 场景不能只停留在文档中。每个 BDD Scenario 必须能追溯到具体的自动化验证。

### Validation scope

每个 BDD Scenario 必须声明验证 scope：

| Scope | 含义 |
| --- | --- |
| `owner` | 单 Runtime layer、Capability、GodotBridge adapter、DataOS validator 或游戏侧 adapter |
| `interaction` | 两到三个系统之间的已知契约边界 |
| `feature-slice` | 用户可见游戏循环行为，例如死亡/复活、暂停/恢复、HUD、输入、Camera 连续性 |
| `release-batch` | 归档、发布或高风险合并前必须跑的回归集合 |

scope 必须和验证命令分开报告。一个场景 pass 不等于 release-batch pass，owner check 也不能替代 feature-slice evidence。

### 映射规则

1. **每个 BDD Scenario 必须对应至少一个可执行检查**：Runtime assertion、DataOS validator check 或 `SceneValidationSession.Check()`。
2. **映射表列**：Scenario | Scope | 验证场景/测试路径 | Check/Assert 名 | Artifact evidence | 状态
3. **覆盖状态**：`COVERED`（已实现且有最新可接受 evidence）| `PENDING`（已设计未实现，必须引用阻塞 task）| `PARTIAL`（部分覆盖，必须说明缺哪个 check）
4. `COVERED` 不允许只有自然语言说明；必须命名 exact check/assert，并能在 artifact `checks[]` 或 test output 中找到。
5. `PENDING` 必须写明 blocker，例如 SDD task id、scene path 或缺失 owner。

### 映射表模板

```markdown
## BDD Scenario → 验证场景映射

| Scenario | Scope | 验证场景/测试路径 | Check/Assert 名 | Artifact evidence | 状态 |
|----------|-------|------------------|-------------------|-------------------|------|
| 玩家死亡后禁止移动 | feature-slice | res://Src/Validation/Game/GameLifecycle/BrotatoLikeGameplayLifecycleValidation.tscn | death_blocks_movement_input | `.ai-temp/.../artifacts/brotatolike-gameplay-lifecycle-validation.json` `checks[].name` | COVERED |
| 玩家死亡后禁止技能 | feature-slice | res://Src/Validation/Game/GameLifecycle/BrotatoLikeGameplayLifecycleValidation.tscn | death_blocks_skill_input | `.ai-temp/.../artifacts/brotatolike-gameplay-lifecycle-validation.json` `checks[].name` | COVERED |
```

### 连接到 TDD 微循环

BDD Scenario 是 FeatureSpec / TestDesigner 的验收标准，TDD 微循环是 Implementer 的执行方式：
- FeatureSpec / TestDesigner 写 BDD Scenario → 输出映射表（目标 .tscn + check 名）
- Implementer 按 TDDProtocol 在目标 .tscn 中先写 `SceneValidationSession.Check()` → RED
- Implementer 实现功能 → GREEN
- Verifier 跑 Godot scene → 确认 artifact `status=pass` → DONE

Verifier 引用 BDD evidence 时必须同时引用 run `index.json`、per-scene `result.json` 和 scene artifact；`无 error`、`exit code 0` 或 PASS marker 不足以证明 `COVERED`。

## 示例

```markdown
# Feature: 事件总线行为

## Scenario: 订阅者按优先级顺序接收事件

- **Given** 事件总线上有三个订阅者，优先级分别为 High、Normal、Low
- **When** 发布一个测试事件
- **Then** 订阅者按 High → Normal → Low 的顺序被调用

## Scenario: 异常订阅者不影响其他订阅者

- **Given** 事件总线上有订阅者 A（会抛异常）和订阅者 B（正常）
- **When** 发布一个测试事件
- **Then** 订阅者 A 的异常被捕获记录
- **And** 订阅者 B 仍然被正常调用
```
