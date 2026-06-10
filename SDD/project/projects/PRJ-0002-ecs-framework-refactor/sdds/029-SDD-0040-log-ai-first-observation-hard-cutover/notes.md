# Notes

## References

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/01-现状分析与AI-first裁决.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/02-目标架构与数据契约.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/03-控制面与CLI设计.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/04-测试统一与Observation接入.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/05-调用点迁移与验证计划.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/06-功能OwnerLog文档与分析流程.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/07-当前样本日志问题与整理方案.md`
- `DocsAI/ECS/Tools/Logger/README.md`
- `.ai-config/skills/godot/godot-scene-test/SKILL.md`
- `.ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl`

## Open Questions

- T2 是否继续使用 Node.js 单文件 `logctl.mjs`，还是拆成子模块；默认先不拆，避免在 analyzer digest 修复前做工具架构重构。
- `TargetSelector` 高频成功查询默认采样比例和 aggregate window 需要实施时根据样本调参；默认先按 owner/context/operation 做 summary，再保留失败和 warning 明细。
- `OperationTrace` 是否新增显式 `entryType=flow_start|flow_step|flow_summary|suppressed_summary`；默认新增，避免 analyzer 靠 message 或 outcome 猜。
- 第一批 owner hot-spot cleanup 只覆盖 TargetSelector、ObjectPool、HealthBarUI、Damage、System；其他 owner 等样本 digest 证明后再扩展。
- 如果 Godot runner 或 Godot CLI 仍不可用，最终场景验证应记录 blocker，不能把 stdout fallback 当作结构化验证通过。
