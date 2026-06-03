# SDD-0028 ObjectPool Collision ParkedInTree Cutover

## Index Card

- **Status**: done
- **Created**: 2026-06-03
- **Updated**: 2026-06-03
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/tools/objectpool
  - ecs/capabilities/collision
- **Tags**: objectpool, collision, ai-first

## What This SDD Is About

本 SDD 将 ObjectPool 与 Collision 的目标裁决落成可执行任务胶囊：

- `ObjectPool<T>` 保留 public API，但默认物理根节点策略从“脱树 / 关碰撞”迁移到 `ParkedInTree`。
- 新增或拆出 pool runtime state、parking grid、activation-frame embargo、fallback detach 和节点状态观测。
- Collision / Movement / ContactDamage / Damage 入口接入 pool runtime state guard，不再把业务正确性交给对象池隐式保证。
- `Src/ECS/Tools/ObjectPool/Tests` 从 legacy/manual demo 拆为 runtime contract、Godot collision validation 和 manual demo 三层。
- `DocsAI/ECS/Capabilities/Collision/Concepts` 已先行精简为 current 入口 + `History/`，旧脱树分析保留但不再作为默认执行事实源。

## Reading Order

1. `design/README.md` — ObjectPool 共享设计包总裁决副本
2. `design/01-现状证据与AI-first裁决.md` — 当前旧脱树实现、Godot 时序风险和目标裁决
3. `design/02-目标架构与重构路线.md` — PoolNodeLifecycleStrategy、RuntimeStateStore、CollisionLogicGuard、validation 路线
4. `design/03-碰撞停放与逻辑验证结论草案.md` — `ParkedInTree + CollisionLogicGuard + ActivationFrameEmbargo` 最终裁决
5. `design/INDEX.md` — 本 SDD 的设计入口
6. `design/main.md` — 本 SDD 执行级设计摘要
7. `tasks.md` — 可执行任务拆分
8. `bdd.md` — 行为验收场景
9. `execution-prompt.md` — 可直接交给新执行会话的一次性提示词
10. `progress.md` — 最近结论和恢复点
11. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: 已生成 SDD-0028 执行胶囊，目标是把 ObjectPool / Collision 从旧默认脱树和关碰撞方案迁移到 `ParkedInTree`、pool runtime state guard、激活首帧 embargo 和结构化验证。
- **Next Action**: 新执行会话从 `execution-prompt.md` 的 T1.1 readiness baseline 开始；先只读源码和当前文档，记录旧实现、dirty baseline、guard 缺口、测试缺口和 SDD validate 基线。
- **Open Blockers**: none
