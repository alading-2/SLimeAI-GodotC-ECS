# Hook 与 Gate 重写方案

> 日期：2026-05-24  
> 目标：解决 hook error、对话暂停、PostToolUse 提示疲劳，以及 gate 缺少稳定输入的问题

---

## 1. 当前问题

历史记录中已经出现多次：

```text
Stop hook (failed)
error: hook returned invalid stop hook JSON output
```

同时 PostToolUse hook 存在高频重复提示：

- 同类 advisory 在一个会话中出现 50+ 次。
- 输出内容长期相同，AI 容易形成忽略。
- hook 本身不能证明验证输出已读，也不能证明 SDD/progress/tasks 已更新。

这说明 hook 当前承担了过多“流程提醒”职责，却缺少稳定的结构化输入和 smoke 验证。

---

## 2. 重写原则

### 2.1 Hook 只做低频安全栏

Hook 不应推动完整 workflow。

Hook 只做：

- SessionStart：注入最短路由提示。
- PostToolUse：只在关键事件或阈值触发时提示。
- Stop：完成前 checklist，不阻塞，除非 JSON schema 稳定且 smoke 覆盖。

Hook 不做：

- 不自动修改文件。
- 不自动创建 SDD。
- 不自动触发 Design Discovery。
- 不执行长命令。
- 不输出大段流程说明。

### 2.2 Stop hook 必须永远输出合法 JSON

Stop hook 的第一目标不是“提醒完整”，而是“不破坏对话”。

建议策略：

- 输出路径统一经过单一 `emit_json()`。
- 所有异常都 fallback 到最小合法 JSON。
- stdout 只输出 JSON，diagnostic 写 stderr 或忽略。
- Codex/Claude schema 分开 smoke。
- Stop event 不运行耗时命令。

### 2.3 PostToolUse 必须去重

PostToolUse 不应每次命令后重复输出同一段。

建议触发条件：

- 运行验证命令后提醒一次“请读取输出”。
- 连续 N 次工具调用无验证时提醒一次。
- 修改 `.ai-config`、hook、subagent、sync 脚本时提醒一次 config gate。
- 修改 Godot scene 或 validation artifact 时提醒一次 scene gate。

同类提醒在同一 session 中应冷却。

---

## 3. Hook 状态文件设计

Hook 可以维护轻量 state，但只保存计数和最近提示，不保存任务正文。

建议字段：

```json
{
  "session_id": "optional",
  "tool_calls_since_validation": 12,
  "last_validation_command": "Tools/run-tests.sh",
  "last_notice": {
    "post_tool_validation": "2026-05-24T17:00:00",
    "config_gate": "2026-05-24T17:03:00"
  },
  "changed_sensitive_paths": [
    ".ai-config/skills",
    "Workspace/SystemAgent/Tools/systemagent-hooks"
  ]
}
```

状态文件只用于降低噪声，不作为事实源。

---

## 4. Stop hook 输出建议

Stop hook 应输出短 checklist。

示例内容：

```text
Completion checklist:
- git status checked: yes/no
- validation evidence: present/missing/not-applicable
- SDD progress updated: yes/no/not-needed
- tasks updated: yes/no/not-needed
- retrospective needed: yes/no
```

如果当前任务没有 SDD，不要提示 SDD 更新。

如果 hook 无法判断，就输出 `unknown`，不要猜。

---

## 5. Hook smoke 必须成为 P0

任何 hook 改动后必须验证：

- Claude SessionStart。
- Claude PostToolUse。
- Claude Stop。
- Codex SessionStart/UserPromptSubmit。
- Codex PostToolUse。
- Codex Stop。
- 异常 fallback。
- 空 stdin / 非法 stdin。

Smoke 的判断标准：

- exit code 0。
- stdout 是合法 JSON。
- Stop event 不包含不被 schema 接受的字段。
- 不运行超过阈值的命令。

---

## 6. Gate 输入从哪里来

当前 `RV-BEHAVIOR-COMPLIANCE` 需要：

- must-read 记录。
- 角色激活记录。
- 工具调用序列。
- gate 通过/跳过记录。

但这些没有稳定来源。

SDD 接入后，gate 输入应来自：

| Gate 输入 | 来源 |
| --- | --- |
| selected workflow | AI 开始时的 route 输出，写入 `progress.md` |
| must-read 状态 | route 输出或 `progress.md` 记录 |
| 当前任务 | `tasks.md` |
| 关键决策 | `progress.md` |
| 行为预期 | `bdd.md` |
| 验证结果 | `progress.md` validation 条目或 artifacts |

Hook 不负责生成这些事实，只提醒缺失。

---

## 7. Gate 简化方向

### 7.1 Gate 变成 SDD-aware checklist

Gate 不应要求 AI 每次重读大量文件。

它应优先检查当前 SDD 是否提供了必要证据。

例如：

```text
RV-RETROSPECTIVE:
- tasks.md 是否完成或标明剩余项
- progress.md 是否有 Latest Resume
- bdd.md 是否有场景或不适用原因
- 验证证据是否链接到 artifact 或命令输出
```

### 7.2 Gate verdict 保持三态

继续保留：

- `APPROVE`
- `CONCERNS`
- `REJECT`

不要引入更多 verdict。

### 7.3 Gate 不替代用户确认

涉及设计方向、是否替代 OpenSpec、是否迁移已有 change 等问题时，gate 只能标注风险，不能代替用户批准。

### 7.4 Design Discovery 不进入 hook/gate 强制链

Design Discovery 应是 `NewFeature` workflow 的前置 phase，而不是 hook 或 gate 的自动拦截。

原因：

- Hook 触发会重现当前 hook 可靠性问题。
- Gate 触发太晚，容易在方案已成形后才发现方向问题。
- 用户明确不希望逐项弹问，而是希望 AI 深度分析后一次性给出问题、建议和确认项。

因此 hook 最多低频提示“这可能需要 Design Discovery”，gate 最多检查 SDD 是否记录了关键确认项。

---

## 8. 重写优先级

### P0：稳定性

- Stop hook 永远输出合法 JSON。
- 增加 hook smoke。
- Stop event 不运行 skill-test 或长命令。

### P1：降噪

- PostToolUse 去重。
- 只对关键路径和验证命令提示。
- 同类提示加 cooldown。

### P2：SDD-aware

- Stop checklist 根据是否存在当前 SDD 调整。
- 提醒更新 `progress.md/tasks.md/bdd.md`。
- 不自动写 SDD。

### P3：Gate 输入标准化

- `RV-BEHAVIOR-COMPLIANCE` 改为读取 route 输出和 SDD 文件。
- Gate 报告引用具体 SDD 路径。

---

## 9. 不建议的做法

### 9.1 不建议 hook block Stop

除非 schema、fallback、smoke 全覆盖，否则不应阻止 Stop。

过去的 hook error 已经证明：阻塞型 hook 风险高于收益。

### 9.2 不建议 PostToolUse 每次提示完整 gate

高频重复会让提示失效。

### 9.3 不建议 hook 执行 `skill-test` 之类命令

Stop 阶段运行命令容易拖慢或失败。

应改为：

- 用户或 AI 显式运行验证。
- Hook 只提示“如果改了 skill/hook，请运行对应验证”。

---

## 10. 最终建议

Hook 的目标要从“流程执行器”降级为“低频安全栏”。

Gate 的输入要从“AI 临场回忆”迁到“SDD 稳定记录”。

这两者配合后，SystemAgent 才能避免两种极端：

- 完全靠自觉，没有 enforcement。
- hook 过度干预，造成对话暂停和提示疲劳。
