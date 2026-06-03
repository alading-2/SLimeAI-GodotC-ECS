# ObjectPool 工具入口

> 状态：current
> 定位：对象复用、池容量、Node 出入池基础生命周期和 Godot 物理根节点隔离策略入口。
> 更新：2026-06-03

## 一句话裁决

ObjectPool 保留。根节点为 `CollisionObject2D` 的池化对象默认改为 `ParkedInTree` 场外常驻策略：回池后仍在 `SceneTree` 中，仍保留碰撞体，不 `RemoveChild`，不关闭碰撞，不改 layer/mask/shape，只隐藏、停处理、移动到分散 parking grid，并由统一碰撞逻辑 guard 拦截回池对象和激活首帧事件。

ObjectPool 需要重构，但重构目标是把隐藏在 `ObjectPool<T>` 内部的 Node lifecycle、parking grid、runtime state 和 fallback detach 策略显式化，不是取消对象池，也不是把 ObjectPool 升级成 EntityManager。

2026-06-03 重新校准后补充：这不应被简单写成“Godot 有 bug，所以脱树”。Godot 4.6 官方文档和社区 issue 共同指向的是 physics step、deferred 属性、broadphase pair 与 Area query flush 的时序约束。SlimeAI 现在的默认做法是不主动切碰撞属性、不脱树，改用显式 pool runtime state、激活第一 physics frame 不处理碰撞，以及 Godot validation artifact 证明旧事件不会进入业务。

## 阅读顺序

| 文档 | 用途 |
| ---- | ---- |
| [Concept.md](Concept.md) | 当前概念、职责边界、AI-first 裁决 |
| [Usage.md](Usage.md) | API、初始化、两阶段激活、常见用法 |
| [Tests.md](Tests.md) | `Src/ECS/Tools/ObjectPool/Tests` 当前问题、重构裁决、scene gate 与 artifact 设计 |
| [Concepts/对象池.md](Concepts/对象池.md) | 历史对象池架构说明，保留作追溯 |
| [../../Capabilities/Collision/Concepts/Godot物理时序与对象池碰撞.md](../../Capabilities/Collision/Concepts/Godot物理时序与对象池碰撞.md) | Godot 物理时序、场外常驻策略、碰撞逻辑 guard 和 fallback 边界 |
| [../../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/ObjectPool/README.md](../../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/ObjectPool/README.md) | ObjectPool AI-first 生命周期工具设计包 |

## 当前源码入口

```text
Src/ECS/Tools/ObjectPool/Core/ObjectPool.cs
Src/ECS/Tools/ObjectPool/Core/IPoolable.cs
Src/ECS/Tools/ObjectPool/Management/ObjectPoolManager.cs
Src/ECS/Tools/ObjectPool/Management/ObjectPoolInit.cs
Src/ECS/Tools/ObjectPool/Lifecycle/
Src/ECS/Tools/ObjectPool/RuntimeState/
Src/ECS/Tools/ObjectPool/Observability/ObjectPoolObservability.cs
Src/ECS/Tools/ObjectPool/Tests/Contracts/
Src/ECS/Tools/ObjectPool/Tests/Validation/CollisionIsolation/
Src/ECS/Runtime/Entity/Spawn/EntitySpawnPipeline.cs
Src/ECS/Runtime/Entity/Manager/EntityManager.cs
```

## Owner 边界

| Owner | 负责 | 不负责 |
| ---- | ---- | ---- |
| ObjectPool | `Get/Release/Activate`、容量、统计、池归属、Node 基础停用/启用、parking grid、pool runtime state、fallback detach | Data 初始化、Entity 注册、owner/source/target、阵营、伤害、命中语义 |
| Entity Runtime | `Get(false)` 后的 Data / Visual / Transform / Component / Registry / Lifecycle 编排，完成后调用 `pool.Activate()` | Godot 物理 pair 细节 |
| Collision Capability | `Area2D` 信号桥接、组件退场、事件过滤、pool runtime state guard、layer/mask 约定、场景结构门禁 | 对象复用、池容量、节点创建 |
| Damage / Movement | 伤害结算、有效碰撞解释、移动停止/销毁策略 | 池化节点物理退场 |

## 设计原则

- 根节点是 `CollisionObject2D` 的池化对象默认 `ParkedInTree`，不脱树、不关碰撞、不改 layer/mask/shape。
- `Get(false)` 仍只获取对象，不触发业务生命周期；Entity 初始化完成后才 `Activate()`。
- `Activate()` 默认设置 `CollisionReadyPhysicsFrame = currentPhysicsFrame + 1`；激活第一帧不处理业务碰撞。
- parking grid 是降低停车区 pair 压力的手段，不是业务正确性的唯一证明。
- 业务事件必须二次过滤：实体仍有效、未在池中、已过 ready frame、目标仍有效、team / owner / lifecycle 仍匹配。
- ObjectPool 观测应从池级统计扩展到节点级状态：是否在池中、`CollisionLogicActive`、`CollisionReadyPhysicsFrame`、parking position、上次 acquire/release 帧和位置。
- `Detach` / `disable_mode=REMOVE` 保持 fallback / control check：只在 validation 证明需要时显式启用，不再作为根 `CollisionObject2D` 默认路径。

## 不推荐

- 回到默认脱树 / 默认关碰撞；这会让 `ParkedInTree` 方案失去意义。
- 只靠 `Monitoring`、`Monitorable`、`CollisionShape2D.Disabled`、`CollisionLayer=0` 或 `ProcessMode.Disabled` 当默认策略。
- 给所有 Node 一律脱树，导致 UI、纯数据对象、Timer 和非物理节点承担不必要的场景树副作用。
- 让 ObjectPool 写 Data、注册 Component、判断伤害、改阵营或发业务命中事件。
- 把 pool runtime state 写进 `Entity.Data` 的不清空分区。

## Tests / Validation 入口

当前 `Src/ECS/Tools/ObjectPool/Tests` 只保留两个自动验证入口：

- `Tests/Contracts/ObjectPoolContractRuntimeTest.tscn`
- `Tests/Validation/CollisionIsolation/ObjectPoolCollisionIsolationValidation.tscn`

验证拆两层：

| 层级 | 目标 | 门禁 |
| --- | --- | --- |
| Runtime contract | `Tests/Contracts/` 验证 `Get/Release/Activate`、统计、容量、重复归还、manager mapping、隔离池名 | 可自动运行的 contract checks，不依赖 UI。 |
| Godot collision validation | `Tests/Validation/CollisionIsolation/` 验证 `Area2D` / `CharacterBody2D` 根节点回池场外常驻、激活首帧不进业务、raw callback 到 business event oracle、parking grid 压力和 fallback detach 对照 | scene README 五字段 + PASS artifact + `checks[]`。 |

历史 UI demo 和 demo fixture 已删除，不再作为当前事实源。

详细测试设计见 [Tests.md](Tests.md)。

## 验证入口

文档阶段：

```bash
python3 Workspace/SDD/sdd.py validate --all
git diff --check
```

代码阶段：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

涉及 Godot 物理行为时，还需要在承载游戏仓补 scene smoke 和日志分析。
