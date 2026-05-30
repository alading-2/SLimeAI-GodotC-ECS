# Tasks

## Progress

- **Status**: done
- **Completed**: 6/6
- **Current**: completed

## Task List

- [x] T1.1 确认多 Git 边界与 worktree 使用策略
  - **Validation**: GitPolicy 或相关文档明确每仓独立 `.worktrees/`、dirty 检查、submodule 边界和不自动清理
- [x] T1.2 同步 SDD 与 workflow 中的 worktree 记录要求
  - **Validation**: SDD README/progress 或指南能记录 worktree path、branch、baseline status、cleanup 状态
- [x] T1.3 完善 SubagentPolicy 与只读输出契约
  - **Validation**: Subagent 输出 Scope/Evidence/Inference/Unknown/Risks/Main-Thread Action/Files Touched
- [x] T1.4 评估现有 Planner/Reviewer/TestDesigner/Retrospective subagent 启动器
  - **Validation**: 确认它们是独立视角或只读辅助，不是并行写入团队
- [x] T1.5 记录 workspace hygiene follow-up
  - **Validation**: 未跟踪 Resources、旧游戏目录等只登记为后续任务，不在本 SDD 清理
- [x] T1.6 验证并回填项目进度
  - **Validation**: 运行 SDD validate、必要配置检查、git diff --check，并更新 progress
