# ECS Framework Optimization

## Goal

重新分析 `SlimeAI/Src/ECS` 旧 ECS 框架的真实问题，形成“保留旧框架、按问题域优化完善”的设计事实源。

本项目不再推动整体 ECS 目录重构，不再搬到另一套参考结构，也不再以删除旧 ECS 为目标。例外是 Data 子系统：Data 已重新裁决为完整重构，旧 `SlimeAI/Data/Data`、`SlimeAI/Data/DataNew` 和旧 Data 测试场景不作为新 Data 系统兼容路径保留。

## Context

- 用户已经将 `SlimeAI/Src` 的 ECS 框架回退到旧实现，并明确认为原方向错误。
- 当前旧 ECS 的核心结构仍有价值：Godot Node 即实体/组件、`EntityManager` 统一生命周期、`Data` 作为运行时数据容器、`EventBus` 做模块间通知、`EntityRelationshipManager` 管理关系索引。
- 真正需要解决的是框架内部的长期一致性问题，尤其是字符串键、事件常量树、DataKey 使用不统一、关系类型字符串、Entity 生命周期和 Relationship 生命周期边界。

## Design

设计原则：

1. **保留旧 ECS 主线结构**：`Src/ECS`、`EntityManager`、`EventBus`、`RelationshipManager` 都是当前主线，不做整体替换；`Data` 作为子系统按 `2.Data系统优化/` 裁决完整重构。
2. **问题导向**：只记录旧框架里真实存在、会影响维护和 AI 修改稳定性的问题。
3. **类型化优先**：优先解决字符串变量名统一问题，让 Data/Event/Relationship 的主键逐步从裸字符串转向统一常量或类型化句柄。
4. **小步验证**：每类问题单独拆 SDD；Data 完整重构也必须拆成 TDD 小切片，不做无测试的大爆炸删除。
5. **边界清晰**：框架通用能力与游戏专属逻辑、Entity 生命周期与 Relationship 生命周期、事件通知与同步请求返回必须分清。
6. **无兼容 hard cutover 必须有 kill gate**：凡是标记为“完全重构 / hard cutover / 无兼容”的任务，执行前必须读取 `06-ECS完全重构执行原则.md`，并同时证明新主链路可用、旧入口不可达、current 文档不推荐旧写法、最终交付物被验证。

## Verification

本轮是文档重构，验证以 SDD 元数据和 Markdown 一致性为主：

- `python3 Workspace/SDD/sdd.py validate --all`
- `git diff --check`

后续执行代码优化时，再按影响面运行 `SlimeAI/Tools/run-build.sh`、`SlimeAI/Tools/run-tests.sh` 和必要的 Godot scene 验证。
