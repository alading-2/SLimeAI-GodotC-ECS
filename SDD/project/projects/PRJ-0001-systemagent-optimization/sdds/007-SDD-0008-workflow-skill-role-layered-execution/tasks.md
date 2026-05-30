# Tasks

## Progress

- **Status**: done
- **Completed**: 6/6
- **Current**: done

## Task List

- [x] T1.1 确认 route 输出格式和任务规模分级
  - **Validation**: 文档明确 workflow/task_size/sdd/must_read/mode/subagent 字段和 small/medium/large 规则
- [x] T1.2 重写核心 workflow 第一屏和 phase 结构
  - **Validation**: NewFeature、WorkflowIteration、ValidationRelease 等入口能直接说明执行顺序
- [x] T1.3 同步 workflow-catalog 与分层模型
  - **Validation**: workflow-catalog 中 required_roles、must_read、gates、completion_artifact 与新 phase 一致
- [x] T1.4 检查和精简 workflow entry wrapper skill
  - **Validation**: `.ai-config/skills/systemagent/*` 不复制 workflow 正文，仍为短入口；如修改则运行 sync/lint
- [x] T1.5 同步 Roles、Capabilities、Gates 的引用边界
  - **Validation**: role 不被误当 skill；capability 与 owner skill 边界在文档和 catalog 中一致
- [x] T1.6 验证并更新项目恢复信息
  - **Validation**: 运行 SDD validate、必要 sync/lint、git diff --check，并回填 progress
