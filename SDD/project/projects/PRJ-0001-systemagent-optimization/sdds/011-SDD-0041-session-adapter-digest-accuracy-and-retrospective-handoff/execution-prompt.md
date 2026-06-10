# SDD-0041 Execution Prompt

你是 SDD-0041 的主执行者。目标是完成 `Session Adapter Digest Accuracy and Retrospective Handoff`：按新契约完整重构 session-adapter digest 准确性、ChatHistory stale 检查和 Retrospective / DeepThink 会话证据交接。

核心裁决：要重构就完全重构，不维护旧 digest / index schema fallback。旧 ChatHistory 产物只能作为迁移输入或只读证据；完成后默认入口必须使用新契约。

## 必读

1. `AGENTS.md`
2. `SDD/project/projects/PRJ-0001-systemagent-optimization/README.md`
3. `SDD/project/projects/PRJ-0001-systemagent-optimization/design/INDEX.md`
4. `SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计/2026-06-10-Session-Adapter二次审查与会话分析流程设计.md`
5. `SDD/project/projects/PRJ-0001-systemagent-optimization/sdds/011-SDD-0041-session-adapter-digest-accuracy-and-retrospective-handoff/README.md`
6. 同目录下 `design/main.md`、`tasks.md`、`bdd.md`、`progress.md`、`notes.md`
7. `Workspace/SystemAgent/Tools/session-adapter/README.md`
8. `Workspace/SystemAgent/Tools/session-adapter/session_adapter.py`
9. `Workspace/SystemAgent/Tools/session-adapter/test_session_adapter.py`
10. `Workspace/SystemAgent/Actors/Retrospective.md`
11. `.ai-config/skills/systemagent-skill/systemagent-retrospective/SKILL.md`
12. `.ai-config/skills/systemagent-skill/systemagent-deepthink/SKILL.md`
13. `Workspace/SystemAgent/Rules/Git.md`

## 执行边界

- 只在当前仓 `/home/slime/Code/SlimeAI/SlimeAI` 操作。
- 不改 `/home/slime/.codex/AGENTS.md`。
- 不删除原始 Codex JSONL。
- 不接常驻 hook/watch。
- 不做 Claude/OpenCode 高保真 digest。
- 不调用外部 LLM 摘要 API。
- 修改 skill/rule 只改 `.ai-config/` 源，改后运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`。
- 不直接编辑 `.codex/skills`、`.claude/skills`、`.trae/skills`、`AGENTS.md`、`CLAUDE.md` 等同步副本作为源；副本只能由同步脚本生成。

## 任务顺序

按 `tasks.md` 执行：

1. T1.1 先补失败先行测试。至少覆盖：
   - `sdd.py validate/show/list/project-show` 不算 edit。
   - resume boilerplate 不做 title。
   - 纯 `continue` 不覆盖真实 User Goal。
   - 相邻重复 user/assistant 去重。
   - tool failure 可判断 category / recovered / final impact。
   - ChatHistory stale report 能发现 6/10 source 有 JSONL 但 index 无 digest。
2. T2.1 重构 command category 和 schema。
3. T3.1 重构 digest 默认入口。
4. T4.1 重构 tool failure 和 stale report。
5. T5.1 更新 Retrospective / DeepThink / GitPolicy handoff。
6. T6.1 用临时 chat-root 重建代表性 digest 并检查质量。
7. T7.1 更新 SDD progress/tasks/bdd/notes，运行验证并收尾。

## 推荐实现要点

- 将命令分类抽成显式函数，不继续依赖两个正则承载所有判断。
- `verification_loop` 只从 `edit` 或 `sdd_state_write` 后的 `validation` 计算。
- `git diff/status/log` 归为 `git_inspection`，可统计但不等同 build/test validation。
- `sdd.py task/note/start/block/done/new/design-import/index` 归为 `sdd_state_write`。
- `sdd.py validate` 归为 `validation`；`sdd.py show/list/project-show` 归为 `read`。
- `ai-context.md` 的 Outcome 找不到最终结论时写 `incomplete`，不要拿中间状态充当完成结论。
- stale report 必须能输出 source count、digest count、missing session id、coverage 状态。
- Retrospective 输出增加 `sessionEvidence`：digest path、source session、coverage、stale/missing、partial/current、efficiency summary、failure summary。

## 验证命令

完成时至少运行：

```bash
python3 -m unittest Workspace/SystemAgent/Tools/session-adapter/test_session_adapter.py
python3 -m py_compile Workspace/SystemAgent/Tools/session-adapter/session_adapter.py
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py digest-codex --session /home/slime/.codex/sessions/2026/06/10/rollout-2026-06-10T09-51-43-019eaf3a-7184-7b33-a35b-1f921cf2a282.jsonl --chat-root /tmp/sdd-0041-chat
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list-digests --chat-root /tmp/sdd-0041-chat
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
python3 Workspace/SDD/sdd.py validate SDD-0041
```

如运行 `python3 Workspace/SDD/sdd.py validate --all` 命中 PRJ-0002 既有错误，必须明确区分是否与 SDD-0041 有关，不要为通过全仓验证而回滚用户已有改动。

## 完成标准

- `tasks.md` 对应任务完成并记录验证。
- `progress.md` Latest Resume 能说明当前状态、验证摘要、剩余风险和下一步。
- session-adapter 新 digest 不再把 `sdd.py validate/show` 计为 edit。
- digest 默认入口不会把 resume boilerplate、`continue` 或中间状态当真实目标/结果。
- tool failure 输出能支持 AI 判断失败根因和恢复状态。
- Retrospective / DeepThink 能定位或报告 ChatHistory coverage。
- 同步副本由 sync 脚本生成，skill-test critical 为 0。
