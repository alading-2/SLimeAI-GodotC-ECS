# BDD 场景文档格式

> BDD 在 SystemAgent 中就是"把自然语言需求写成 Given-When-Then 结构化文档"。不需要 parser、runner、框架依赖。对 AI 来说，BDD 场景是可读的行为验收清单。

## 定位

- **SDD** = 计划/进度/技术方案（做什么、怎么做、什么时候做）
- **BDD 场景** = 行为验收契约（在什么情况下、做了什么、期望得到什么结果）
- **TDD/测试** = 代码级验证（断言、scene test、runtime test）

三者互补。SDD 管方向与恢复，BDD 管验收，TDD 管实现。

## 文件位置

```text
SDD/active/<sdd-id>/
  design/
    main.md      <- 技术方案（接口、约束、架构）
  bdd.md         <- 行为场景（可选，验收场景多时才需要）
```

如果 SDD design 里已有清晰的 WHEN/THEN 描述，可以直接写在 design 里，不需要单独 `bdd.md`。

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
4. **不要写实现细节。** "调用 ServiceX.MethodY" 是 TDD 的事，BDD 说"发送攻击请求后敌人血量减少"。
5. **边界条件单独写 Scenario。** "成功登录"和"密码错误"是两个 Scenario。

## 什么时候写 BDD 场景

| 场景 | 是否写 BDD |
|------|-----------|
| 新 capability / 框架级功能 | 推荐写 |
| 用户给了明确验收条件 | 必须写 |
| 现有 spec 的 WHEN/THEN 已足够清晰 | 不需要单独写 |
| typo / 链接 / 注释修复 | 不需要 |
| Godot scene validation 场景 | 推荐写（已有 WHEN/THEN 基础） |

## 与 SDD design 的分工

| 内容 | 写在 SDD design | 写在 bdd.md |
|------|-------------|-------------|
| Requirement 标题 | 是 | 否 |
| 技术约束/接口签名 | 是 | 否 |
| WHEN/THEN 行为描述 | 简化或省略 | 详细写 |
| 具体输入/输出示例 | 简化 | 详细写 |
| 边界条件/错误场景 | 简化 | 详细写 |

## AI 写场景的步骤

1. 读完当前 SDD 的 `design/`、`tasks.md` 和 `progress.md`，理解技术方案。
2. 识别任务中**需要验收的关键行为**（不是全部功能）。
3. 每个关键行为写一个 Scenario。
4. 检查 Then 是否可验证。
5. 在 SDD design 或 `progress.md` 中引用 bdd.md（如单独写了文件）。

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

BDD Scenario 是 TestDesigner 的输出（验收标准），TDD 微循环是 Implementer 的执行方式：
- TestDesigner 写 BDD Scenario → 输出映射表（目标 .tscn + check 名）
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
