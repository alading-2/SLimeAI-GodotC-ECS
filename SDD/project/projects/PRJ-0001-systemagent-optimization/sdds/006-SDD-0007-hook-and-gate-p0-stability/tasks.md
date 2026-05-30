# Tasks

## Progress

- **Status**: done
- **Completed**: 6/6
- **Current**: done

## Task List

- [x] T1.1 确认 hook/gate P0 范围、现有入口和 smoke 缺口
  - **Validation**: 记录 Claude/Codex hook 配置、hook script、现有 smoke 或缺失原因
- [x] T1.2 建立或补齐 hook smoke 覆盖
  - **Validation**: 覆盖 SessionStart/PostToolUse/Stop、异常 fallback、空 stdin、非法 stdin
- [x] T1.3 重构 Stop hook 输出路径
  - **Validation**: 所有 Stop 分支 stdout 只输出合法 JSON，异常 fallback 不阻塞对话且不运行长命令
- [x] T1.4 实现 PostToolUse 去重和 cooldown
  - **Validation**: 同类 advisory 在同一 session 内不会高频重复，敏感路径和验证命令仍能触发提示
- [x] T1.5 将 Gate 输入改为 SDD-aware checklist
  - **Validation**: ReviewGates 或相关文档明确 selected workflow、must-read、tasks/progress/bdd/validation 的来源
- [x] T1.6 同步文档、catalog、配置说明并验证
  - **Validation**: 运行 SDD validate、hook smoke、必要 sync/lint、git diff --check
