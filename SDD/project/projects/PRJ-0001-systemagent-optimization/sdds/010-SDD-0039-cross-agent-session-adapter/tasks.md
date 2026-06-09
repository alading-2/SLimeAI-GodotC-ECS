# Tasks

## Progress

- **Status**: done
- **Completed**: 12/12
- **Current**: done

## Task List

- [x] T1.1 填充 SDD 设计、任务、BDD、notes 和恢复点
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0039`
- [x] T1.2 回填 PRJ-0001 roadmap / progress / project metadata
  - **Validation**: `python3 Workspace/SDD/sdd.py validate --root SDD/project/projects/PRJ-0001-systemagent-optimization --all`
- [x] T2.1 创建 `Workspace/SystemAgent/Tools/session-adapter/` 工具入口和 README
  - **Validation**: `python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py --help`
- [x] T2.2 实现 `list --repo . --limit N`
  - **Validation**: `python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list --repo . --limit 5`
- [x] T2.3 实现 `index --session <id>` 生成 ChatHistory sidecar 和 `index.json`
  - **Validation**: 生成 `Workspace/DocsAI/ChatHistory/index.json` 和对应 Markdown
- [x] T2.4 实现 `summarize --session <id>` 作为可读摘要入口
  - **Validation**: `summarize` 能输出 sidecar 路径并刷新 index entry
- [x] T2.5 保留 OpenCode 支持路径和缺样例诊断
  - **Validation**: README 和 CLI metadata 能表达 `opencode`，不要求真实 OpenCode session
- [x] T3.1 添加最小自检或静态验证
  - **Validation**: `python3 -m py_compile Workspace/SystemAgent/Tools/session-adapter/session_adapter.py`
- [x] T3.2 运行功能验证和 SDD 专项校验
  - **Validation**: `list/index/summarize` + `python3 Workspace/SDD/sdd.py validate SDD-0039`
- [x] T4.1 更新 tasks/progress/roadmap，完成 SDD
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0039`
- [x] T5.1 新增 `export-codex-month`，按 `Workspace/DocsAI/ChatHistory/YYYY/MM/DD/` 导出 Codex 可见完整 transcript
  - **Validation**: `python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py --chat-root /tmp/slimeai-chat-export-test export-codex-month --source-root /home/slime/.codex/sessions/2026/06 --limit 1`
- [x] T5.2 导出 `/home/slime/.codex/sessions/2026/06` 全部 63 个 Codex session，并刷新 `ChatHistory/index.json`
  - **Validation**: `python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py export-codex-month --source-root /home/slime/.codex/sessions/2026/06`
