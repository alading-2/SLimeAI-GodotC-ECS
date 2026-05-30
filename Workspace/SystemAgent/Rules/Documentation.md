# Documentation

> 从 DocumentationManagement.md 提取的独特内容。事实源边界和 AI 配置规则已由 README.md + AIConfig.md 覆盖。

## 旧路径命中分类

文档治理 change 的旧路径搜索命中必须分类：

| 分类 | 含义 | 动作 |
| --- | --- | --- |
| historical | 已完成 SDD、归档目录或 git log 中的旧路径引用 | 不修改 |
| migration pointer | 旧入口文件保留为迁移说明（如 `Workspace/DocsAI/AgentWorkflow/`） | 不修改 |
| archive | `openspec/` 归档目录中的旧路径 | 不修改 |
| violation | 当前正文或运行配置中的旧路径 | 必须修复 |

## 事实源层级

| 层 | 用途 | 事实源 | 规则 |
| --- | --- | --- | --- |
| Working | 当前会话判断 | chat session / IDE metadata | 不落盘即不作为长期事实 |
| Persistent Review | 研究、复盘、评估证据 | `Workspace/DocsAI/Reviews/` | 只能作为历史分析或证据来源 |
| Semantic Baseline | 当前规则、流程和规格 | `Workspace/SystemAgent/`、`SlimeAI/DocsAI/` | 长期规则必须落在这一层 |
| Event Log | 历史操作记录 | git log、已完成 SDD | 只追加和追溯，不作为当前入口 |

写入新文档前先判断内容属于哪一层；不得把 Persistent Review 当作 Semantic Baseline 使用。

## Schema migration

`Registry/manifest.yaml` schema_version `3` 表示 SystemAgent 已从 13 目录重构为 Routes/Actors/Rules/Tools/Registry 五目录。

`Registry/skills.yaml` schema_version `3` 只作用于 skill catalog。v3 字段成熟度：`function_category` 用于已纳入 rubric 覆盖的 SystemAgent-owned 高风险入口、收尾、维护或 wrapper skill，未纳入覆盖时可省略；`spec`、`last_spec`、`last_category` 属于 Behavioral Spec / Category Rubric pilot 或人工评审记录；`last_static` 是 Static Lint 可选人工快照。非 pilot skill 的空 `spec` / `last_*` 不视为失败。

## Rule action semantics

| action | 含义 | 边界 |
| --- | --- | --- |
| `read_reference` | 读取参考资料、历史分析或外部资源 | 不把读取结果直接当作当前事实源 |
| `edit_source` | 修改明确维护源 | 只写 `Workspace/SystemAgent/`、`.ai-config/`、SDD 或用户授权的源文件 |
| `sync_generated` | 由脚本生成副本 | 不手写 `.claude/.codex/.windsurf` skill 副本或 rule 副本 |
| `advisory_check` | hook、review 或 lint 给出提醒 | 默认不自动改写文件，不替代人工或 SDD 决策 |

## 禁止

- 不保留同一正文的双事实源。
- 不把已完成 SDD 的历史记录当长期入口。
- 不在 README 复制所有 route/actor/rule 正文。
