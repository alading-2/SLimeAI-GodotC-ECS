---
name: data-authoring
description: 编写或修改 Data 目录下的数据配置、Config、DataKey、EventType、Resource 映射规则时使用。适用于：新增配置字段、设计 DataKey 分域、决定字段放 Data/Data 还是 Data/Config、编写事件协议。触发关键词：Data目录、Config配置、DataKey定义、EventType、数据配置、Resource映射。
---

# DataAuthoring 入口

## 什么时候用

- 修改 `Data/Config/`、`Data/DataNew/`、`Data/DataKey/`、`Data/EventType/`、`Data/ResourceManagement/`。
- 迁移旧数据到 `/home/slime/Code/SlimeAI/Games/BrotatoLike/DataOS/Authoring/`。
- 新增可配置字段、DataKey、事件协议或资源路径映射。
- 判断字段应放 DataNew、DataKey、Config 还是 ResourceManagement。

## 转向其它 Skill

- 运行时 Data 容器实现 -> `@ecs-data`
- 事件发布订阅流程 -> `@ecs-event`
- 技能配置字段 -> `@ability-system`
- 系统注册 / 运行条件 -> `DocsAI/Modules/SystemCore.md`

## 必读

- 新框架 DataOS：`/home/slime/Code/SlimeAI/SlimeAI/DataOS/README.md`
- BrotatoLike DataOS seed：`/home/slime/Code/SlimeAI/Games/BrotatoLike/DataOS/Authoring/BrotatoLike.seed.sql`
- `DocsAI/Modules/DataAuthoring.md`
- `Data/README.md`
- `Data/DataNew/README.md`
- `Data/DataKey/README.md`
- 涉及运行时容器读写再读 `DocsAI/Modules/Data.md`

## 最短流程

1. 判断字段属于 Entity.Data、系统配置、事件协议还是资源路径。
2. 新仓库优先写 DataOS authoring seed / migration，生成 Runtime snapshot；旧仓库 DataNew 只作为迁移输入。
3. 运行时字段先在 `SlimeAI.GameOS` 定义 `DataMeta`，snapshot 字段键使用 Runtime DataKey 字符串。
4. 系统配置 / 预设 / Spawn config 优先使用 `ScheduleDataKeys`；Unit / Ability / Feature 字段优先使用对应 Capability DataKey。
5. 事件协议放新框架 `GameOS/Runtime/Event/` 或旧仓库 `Data/EventType/` 对应分域，按迁移目标选择。
6. 资源路径进入 DataOS `resource_entry` 或 `ResourceCatalog` 生成入口，不放运行时对象引用。
7. 正式游戏入口消费 generated snapshot，例如 `BrotatoLikeDataOSBootstrap`，不要在 Runtime 热路径访问 SQLite。
8. 运行框架和游戏验证命令。
9. 更新 DataOS / DocsAI / 迁移进度文档。

## BrotatoLike 迁移状态提示

- M27 第三段已覆盖当前 GameOS 可表达的 Ability / Projectile / Effect / Movement handler authoring 参数。
- `MovementDataKeys` 已包含 SineWave / Orbit / Boomerang / Bezier / Parabola / CircularArc 的标量化 handler 参数；`BrotatoLikeAbilityHandlers` 已把 `sine_wave_shot / boomerang_throw / bezier_shot / parabola_shot(CircularArc) / arc_shot(CircularArc) / orbit_skill` 接入真实 Movement 执行闭环，把 `dash` 接入真实 Charge 位移闭环，并已把 `chain_lightning` 接入真实延迟弹跳伤害闭环，把 `slam / circle_damage` 接入真实范围伤害与 Effect 闭环。下一步不要重复补 seed，优先迁剩余 Feature actions 和具体 Ability 逻辑。

## 禁止事项

- 不要把业务逻辑写进 `Data/`。
- 不要把旧 `.tres` 重新作为新增运行时主数据源。
- 不要在旧仓库继续扩大 DataNew 作为长期主数据源。
- 不要新增普通业务 `const string` DataKey。
- 不要用裸字符串替代 `[DataKey(nameof(DataKey.Xxx))]`。
- 不要把 `PackedScene`、Node 或运行时对象引用写进 DataNew 主配置。

## 推荐验证

```bash
cd /home/slime/Code/SlimeAI/SlimeAI && Tools/run-tests.sh
cd /home/slime/Code/SlimeAI/Games/BrotatoLike && Tools/run-build.sh
cd /home/slime/Code/SlimeAI/Games/BrotatoLike && Tools/run-godot-smoke.sh
```
