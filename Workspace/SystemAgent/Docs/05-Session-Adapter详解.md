# Session Adapter 详解

## 什么是 Session Adapter

Session Adapter 是一个 Python CLI 工具，将 AI CLI 会话数据（Claude Code、Codex、OpenCode）转化为结构化的、AI 可读的 SlimeAI ChatHistory 副本。

### 一句话定位

**让 AI 的历史对话从"工具私有格式"变成"可搜索、可分析的结构化数据"。**

### 解决的核心问题

AI 会话结束后，知识被困在工具私有格式中：
- Claude Code 的会话在 `~/.claude/projects/` 中，格式为 JSONL
- Codex 的会话在 `~/.codex/sessions/` 中，格式也是 JSONL
- OpenCode 的会话在各自目录中

没有 Session Adapter，这些会话是不可检索的黑盒。有了它，Retrospective 和 DeepThink 可以跨会话恢复知识。

## 架构

### 两个架构层

```
Session Adapter
├── Summary 层（跨工具兼容）
│   ├── list —— 列出所有会话
│   ├── index —— 生成摘要级 sidecar + index.json
│   └── summarize —— 生成摘要级 Markdown
│
└── Digest 层（Codex 专用，AI-first）
    ├── digest-codex —— 单会话深度解析
    └── digest-codex-month —— 月度批量解析
```

### 三层证据模型

| 层级 | 命令 | 内容 | 适用场景 |
|------|------|------|----------|
| summary | `summarize` | 截断摘要 | 快速浏览 |
| visible-transcript | 内部 | 完整可见内容 | 详细回溯 |
| digest | `digest-codex` | AI-first 结构化摘要 | Retrospective 分析 |

### Digest 输出结构

每个被 digest 的会话产生一个文件夹：

```
<session-folder>/
├── manifest.json              ← 会话元数据
├── raw/
│   ├── transcript.visible.md  ← 完整可见转录
│   └── source-locator.md      ← 源文件引用
└── derived/
    ├── ai-context.md          ← AI-first 上下文摘要
    ├── summary.md             ← 会话摘要
    ├── events.jsonl           ← 结构化事件日志
    ├── user-requests.md       ← 用户请求分类
    ├── assistant-results.md   ← AI 响应分类
    ├── tools.md               ← 工具使用分类
    ├── files.md               ← 文件操作分类
    ├── validation.md          ← 验证活动分类
    ├── interruptions.md       ← 中断分析
    ├── noise.md               ← 噪音分析
    └── tool-failures.md       ← 工具失败分析（条件生成）
```

## 关键设计决策

### 1. Digest Gate（短会话过滤）

短会话不会被 digest，只在 index.json 中留一个定位器：

```
条件（全部满足才跳过）：
- meaningful_user_turns < 2
- tool_calls < 5
- 无代码/验证信号
- 无最终结论
```

**原因**：短会话通常是探索性的，digest 的成本不值得。

### 2. 命令分类系统

使用 8 个显式类别替代早期的宽泛正则匹配：

| 类别 | 含义 |
|------|------|
| `read` | 读取文件、搜索 |
| `edit` | 编辑文件 |
| `sdd_state_write` | SDD 状态更新（tasks/progress） |
| `validation` | 验证命令（build/test/validate） |
| `git_inspection` | git status/diff/log |
| `git_write` | git add/commit/push |
| `external_probe` | 外部命令（curl/npm/pip） |
| `unknown` | 无法分类 |

### 3. 工具失败分析

当 `tool_failed_count > 0` 时生成 `tool-failures.md`，分类包括：

| 失败类型 | 含义 |
|----------|------|
| `build_failure` | 构建失败 |
| `path_error` | 路径错误 |
| `command_misuse` | 命令用法错误 |
| `patch_context_mismatch` | 补丁上下文不匹配 |
| `search_no_result` | 搜索无结果 |
| `tool_unavailable` | 工具不可用 |
| `timeout` | 超时 |
| `network_or_fetch` | 网络/获取错误 |
| `unknown` | 未知 |

每个失败记录包含：重试次数、恢复状态、最终影响评估。

### 4. Index v4 Schema

```json
{
  "schema_version": 4,
  "source_locator": { "path": "...", "tool": "codex", "session_id": "..." },
  "digest_locator": { "folder": "...", "manifest": "..." },
  "gate": { "meaningful_user_turns": 5, "tool_calls": 23, "passed": true },
  "tool_status": { "tool_total": 23, "tool_failed": 2, "tool_failed_categories": {...} },
  "command_categories": { "read": 10, "edit": 5, "validation": 3, ... },
  "recovery_signals": { "retry_count": 2, "recovered": true }
}
```

## 与 Retrospective 的关系

Session Adapter 是 Retrospective 的**数据源**。

```
Session Adapter (数据生产)
    ↓
ChatHistory sidecar (结构化存储)
    ↓
Retrospective (数据消费)
    ├── derived/ai-context.md —— 理解会话做了什么
    ├── derived/tool-failures.md —— 分析工具失败模式
    ├── derived/events.jsonl —— 效率分析
    └── index.json —— 会话发现
```

### Retrospective 使用 Session Adapter 的流程

1. `session_adapter.py stale-report` —— 检查哪些会话还没有 digest
2. `session_adapter.py list-digests` —— 列出已有 digest
3. 读取 digest 中的 `ai-context.md`、`tool-failures.md`、`events.jsonl`
4. 分析效率（验证循环、文件放大、工具失败）
5. 将 `sessionEvidence` 写入 Retrospective 输出

## 当前状态

- **实现**：`session_adapter.py`（2595 行）
- **SDD 历史**：SDD-0039（基础实现）→ SDD-0041（digest 准确性重构）
- **支持工具**：Claude Code, Codex, OpenCode
- **外部依赖**：无（纯 Python，无 LLM 调用、无 hook、无 watcher）

## 未完成的部分

1. **Claude Code digest 不完整**：digest 层目前只支持 Codex，Claude Code 只有 summary 层
2. **没有自动触发**：需要手动运行命令，没有 hook 自动在会话结束后触发
3. **没有跨会话搜索**：只能按时间浏览，不能按关键词或文件路径搜索
4. **没有合并分析**：不能跨多个会话做趋势分析
