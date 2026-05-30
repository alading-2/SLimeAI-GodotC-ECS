# Tasks

## Progress

- **Status**: done
- **Completed**: 6/6
- **Current**: done

## Task List

- [x] T1.1 冻结 DesignDiscovery capability 契约
  - **Validation**: 明确触发条件、输入、输出确认包、small/medium/large 模式和禁止事项
- [x] T1.2 新增或更新 DesignDiscovery capability 正文
  - **Validation**: Capabilities 下存在可复用正文，说明 standalone 与 workflow composed 模式
- [x] T1.3 新增 DesignCritic 角色正文
  - **Validation**: Roles 下明确职责、输入、输出、禁止事项和与 Reviewer/Planner 的边界
- [x] T1.4 接入 NewFeature / WorkflowIteration / catalog
  - **Validation**: workflow 和 workflow-catalog 能说明何时调用 DesignDiscovery 与 DesignCritic
- [x] T1.5 同步 wrapper skill 与 SDD 落盘规则
  - **Validation**: 相关 `.ai-config/skills/*` 短入口不复制正文，并说明结果写入 SDD 的位置
- [x] T1.6 验证并回填项目进度
  - **Validation**: 运行 SDD validate、必要 sync/lint、git diff --check，并更新 progress
