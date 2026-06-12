# Hook 系统重启设计

> 来源：SystemAgent 深度分析
> 日期：2026-06-11
> 优先级：P0

## 问题陈述

`systemagent-hook.py` 已暂停，原因："disabled until SystemAgent hook policy is redesigned"。

Hook 是 SystemAgent 的**主动提醒机制**。没有 Hook，SystemAgent 的所有规则都依赖 AI 的自觉遵守——这是不可靠的。

## 为什么暂停

原设计有三个问题：

### 1. PostToolUse 触发频率太高

每次工具调用后都触发 Gate 检查。一次典型对话可能有 50-100 次工具调用，大部分是探索性读取。在这些读取后触发 Gate 检查是噪音。

### 2. 判定逻辑不够智能

Hook 无法区分：
- **探索性读取**（读 10 个文件了解现状）→ 不需要 Gate 检查
- **实质性修改**（编辑了核心文件）→ 需要 Gate 检查

### 3. 与 lean review mode 冲突

lean 模式想减少开销（只检查必选门），但 Hook 强制全量检查，无法适配 review mode。

## 设计方案

### 核心思想：Hook 应该是"提醒器"而不是"检查器"

Hook 不应该自己做 Gate 检查（这太复杂了），而应该**提醒 AI 该做检查了**。

### Hook 触发点设计

| 触发点 | 行为 | 频率 |
|--------|------|------|
| **SessionStart** | 输出当前 SDD 状态 + 待办提醒 | 每次会话 1 次 |
| **FirstEdit** | 首次编辑文件时提醒 must-read | 每次会话 1 次 |
| **PostValidation** | 验证命令后提醒 Code Review | 只在验证命令后 |
| **PreStop** | 会话结束前检查 Retrospective | 每次会话 1 次 |

### 各触发点详细设计

#### SessionStart

```
输出：
- 当前活跃 SDD 列表（id, status, Latest Resume）
- 上次会话的未完成项
- 建议的下一步行动

条件：
- 如果有活跃 SDD → 提醒
- 如果没有 → 静默
```

#### FirstEdit

```
输出：
- "首次编辑文件，建议先确认已读取相关 must-read 入口"
- 列出当前工作流的 must-read 文件

条件：
- 只在第一次编辑时触发
- 如果 AI 已经读取了足够的文件 → 静默
```

#### PostValidation

```
输出：
- "验证完成，建议运行 Code Review"
- 提醒 RV-TEST-COVERAGE 门

条件：
- 只在运行了 build/test/validate 命令后触发
- 不在探索性搜索后触发
```

#### PreStop

```
输出：
- "会话即将结束，检查是否需要 Retrospective"
- 当前 SDD 状态摘要
- 本轮改动的 git status

条件：
- 如果本轮有实质性改动（不只是读取）→ 提醒
- 如果只是探索性对话 → 静默
```

### 与 Review Mode 集成

```
full mode  → 所有 4 个触发点都激活
lean mode  → 只激活 SessionStart + PreStop
solo mode  → 只激活 PreStop
```

### 判定逻辑

Hook **不做** Gate 检查，只做**触发判定**：

```python
def should_trigger(hook_type, context):
    if hook_type == "session_start":
        return has_active_sdd()
    elif hook_type == "first_edit":
        return not already_reminded_must_read()
    elif hook_type == "post_validation":
        return is_validation_command(context.last_command)
    elif hook_type == "pre_stop":
        return has_substantive_changes() and review_mode != "solo"
```

### 实现方式

**Claude Code**：通过 `.claude/settings.json` 的 hook 机制

```json
{
  "hooks": {
    "SessionStart": [
      { "type": "command", "command": "python3 Workspace/SystemAgent/Tools/systemagent-hooks/session_start_hook.py" }
    ],
    "PreStop": [
      { "type": "command", "command": "python3 Workspace/SystemAgent/Tools/systemagent-hooks/pre_stop_hook.py" }
    ]
  }
}
```

**FirstEdit 和 PostValidation**：通过 Claude Code 的 `PostToolUse` hook 实现，但增加智能过滤：

```python
def post_tool_use(tool_name, tool_input):
    if tool_name == "Edit" or tool_name == "Write":
        if not first_edit_reminded:
            first_edit_reminded = True
            return must_read_reminder()
    if tool_name == "Bash" and is_validation_command(tool_input.get("command", "")):
        return code_review_reminder()
    return None  # 不输出任何内容
```

## 实施路径

| 阶段 | 内容 | 工作量 |
|------|------|--------|
| Phase 1 | 重设计 Hook 策略文档 | 规则更新 |
| Phase 2 | 实现 SessionStart + PreStop | 2 个 Python 脚本 |
| Phase 3 | 实现 FirstEdit + PostValidation（智能过滤） | 1 个 Python 脚本 |
| Phase 4 | 与 Review Mode 集成 | 配置更新 |
| Phase 5 | 注册到 Registry manifest | 注册更新 |
