# Refactor Decision Tree

> Baseline 由 `Workspace/SystemAgent/` 与当前 SDD 管理。调整任务分类或入口规则必须走新的 SDD。

读取时机：收到新功能、重构、迁移、bugfix、文档同步或 skill 同步任务后，在 owner skill 之前读取。

## 先分类

| 类型 | 识别信号 | 必读入口 | 额外动作 |
| --- | --- | --- | --- |
| 新功能 / 能力扩展 | 新 service、DataKey、Event、GodotBridge Adapter、场景验证 | owner skill、`validation-closure.md` | 大型或跨模块时建 SDD；补专项 test / scene |
| 重构 | 删除旧入口、改 public API、改变 Runtime 边界、跨目录移动 | `rename-pipeline.md`、`spec-code-alignment-check.md` | 必须全量搜索旧符号；先写旧->新映射 |
| 迁移旧逻辑 | 从 `Resources/Else/brotato-my` 或游戏仓搬能力 | `framework-game-boundary.md`、`typed-value-design.md` | 验证旧输入仓 build；不要复制旧兼容层 |
| Bugfix | 有失败测试、场景错误、日志异常、行为回归 | owner Debug / Tests 文档 | 先复现；如果修复改变架构边界再建 SDD |
| 文档同步 | DocsAI / Contract / ApiIndex / ProjectState 漂移 | `spec-code-alignment-check.md` | 不补未落地事实；标明代码事实源 |
| Skill 同步 | 修改 `.ai-config/skills`、rules、command | `skill-sync-discipline.md` | 只改 `.ai-config/`，跑 sync 脚本 |

## SDD 判断

必须使用 SDD：

- 大型功能、架构变更、跨模块重构、长期设计决策。
- Runtime / DataOS / GodotBridge public API 变更。
- 段落骨架、reference 清单、skill 同步规则变更。
- 需要迁移账本或多仓收尾证据的任务。

可以直接改：

- 拼写、链接、注释、小范围文档事实修正。
- reference 文件内容 typo 或命令示例小修，但不能改骨架或清单。
- 单点 bugfix 且不改变协议边界。

## P1-P4 对照

- P1：`RelationshipManager` 删除并改为 `LifecycleTree`，属于重构 + rename pipeline。历史基线：`openspec/specs/runtime-relationship-lifecycle/spec.md`、`openspec/specs/runtime-business-entity-references/spec.md`。
- P2a：`EntityId` typed value 落地，属于 typed value 设计。历史基线：`openspec/specs/runtime-entity-identity/spec.md`。
- P2b：`RuntimeWorld.Default / CreateScoped()` 落地，属于 Runtime facade 收束。历史基线：`openspec/specs/runtime-world-container/spec.md`。
- P3：游戏事件和输入桥接下沉，属于 framework/game 边界清理。历史基线：`openspec/specs/runtime-event-game-leakage-cleanup/spec.md`。
- P4：`RuntimeCommandBuffer`、`SchedulePhase`、8 种 typed command kind 落地。历史基线：`openspec/specs/runtime-command-buffer/spec.md`、`openspec/specs/runtime-schedule-phase/spec.md`。P4 未做 `IRuntimeSystem -> IRuntimeProcess` rename。
