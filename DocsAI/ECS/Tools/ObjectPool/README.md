# ObjectPool 工具入口

> 状态：current
> 定位：对象复用、池容量、Node 出入池基础生命周期和 Godot 物理根节点隔离策略入口。
> 更新：2026-06-03

## 一句话裁决

ObjectPool 保留，并继续对根节点为 `CollisionObject2D` 的池化对象默认执行“泊车位 + 脱树 + 挂树后同步禁用 + 两阶段激活”。这不是临时补丁，而是 Godot 物理节点复用时的默认隔离策略。

ObjectPool 需要重构，但重构目标是把隐藏在 `ObjectPool<T>` 内部的 Node lifecycle 和 collision isolation 策略显式化，不是取消对象池、取消脱树或把 ObjectPool 升级成 EntityManager。

2026-06-03 复核后补充：这不应被简单写成“Godot 有 bug，所以脱树”。本地 Godot 4.6.2 源码、Godot 4.6 官方文档和社区 issue 共同指向的是物理 step、broadphase pair、Area query flush 与 Node 场景树出入的时序约束。SlimeAI 的默认做法是显式生命周期隔离，并用 Godot validation artifact 证明旧位置事件不会进入业务。

## 阅读顺序

| 文档 | 用途 |
| ---- | ---- |
| [Concept.md](Concept.md) | 当前概念、职责边界、AI-first 裁决 |
| [Usage.md](Usage.md) | API、初始化、两阶段激活、常见用法 |
| [Tests.md](Tests.md) | `Src/ECS/Tools/ObjectPool/Tests` 当前问题、重构裁决、scene gate 与 artifact 设计 |
| [Concepts/对象池.md](Concepts/对象池.md) | 历史对象池架构说明，保留作追溯 |
| [../../Capabilities/Collision/Concepts/对象池碰撞兼容说明.md](../../Capabilities/Collision/Concepts/对象池碰撞兼容说明.md) | Godot 物理时序、幽灵碰撞和脱树方案细节 |
| [../../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/ObjectPool/README.md](../../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/ObjectPool/README.md) | ObjectPool AI-first 生命周期工具设计包 |

## 当前源码入口

```text
Src/ECS/Tools/ObjectPool/ObjectPool.cs
Src/ECS/Tools/ObjectPool/ObjectPoolManager.cs
Src/ECS/Tools/ObjectPool/ObjectPoolObservability.cs
Src/ECS/Tools/ObjectPool/ObjectPoolInit.cs
Src/ECS/Tools/ObjectPool/Tests/
Src/ECS/Runtime/Entity/Spawn/EntitySpawnPipeline.cs
Src/ECS/Runtime/Entity/Manager/EntityManager.cs
```

## Owner 边界

| Owner | 负责 | 不负责 |
| ---- | ---- | ---- |
| ObjectPool | `Get/Release/Activate`、容量、统计、池归属、Node 基础停用/启用、物理根节点隔离 | Data 初始化、Entity 注册、owner/source/target、阵营、伤害、命中语义 |
| Entity Runtime | `Get(false)` 后的 Data / Visual / Transform / Component / Registry / Lifecycle 编排，完成后调用 `pool.Activate()` | Godot 物理 pair 细节 |
| Collision Capability | `Area2D` 信号桥接、组件退场、事件过滤、layer/mask 约定、场景结构门禁 | 对象复用、池容量、节点创建 |
| Damage / Movement | 伤害结算、有效碰撞解释、移动停止/销毁策略 | 池化节点物理退场 |

## 设计原则

- 根节点是 `CollisionObject2D` 的池化对象默认脱树；非物理 Node 不默认脱树。
- `Get(false)` 只获取并挂树到“碰撞关闭”状态；Entity 初始化完成后才 `Activate()`。
- `SetDeferred` 是安全提交工具，不是完整退场状态机；同帧回收再出池窗口必须有同步禁用防线。
- 泊车位是物理隔离的一层防御，不是业务正确性的唯一证明。
- 业务事件必须二次过滤：实体仍有效、未在池中、目标仍有效、team / owner / lifecycle 仍匹配。
- ObjectPool 观测应继续从池级统计扩展到节点级状态：是否在池中、是否脱树、是否挂树未激活、上次 acquire/release 帧和位置。
- `disable_mode=REMOVE` 保持 `Adopt Later`：它可作为对照验证项，但不替代物理根节点脱树和 `Get(false)` 半初始化窗口控制。

## 不推荐

- 取消脱树，只靠 `Monitoring`、`Monitorable`、`CollisionShape2D.Disabled`、`CollisionLayer=0` 或 `ProcessMode.Disabled`。
- 给所有 Node 一律脱树，导致 UI、纯数据对象、Timer 和非物理节点承担不必要的场景树副作用。
- 让 ObjectPool 写 Data、注册 Component、判断伤害、改阵营或发业务命中事件。
- 用“延迟一帧”替代明确状态机和验证场景。

## Tests / Validation 入口

当前 `Src/ECS/Tools/ObjectPool/Tests` 下的 `ObjectPoolVisualTest` 与 `ObjectPoolManagerTest` 是 legacy/manual demo，不是回归门禁：

- 它们继承 `Control`，依赖 UI、鼠标、随机位置和人工观察。
- 测试对象是 `Node2D`，没有覆盖 `CollisionObject2D` 根节点脱树。
- 没有 README 五字段、PASS artifact、`index.json` / `result.json` / `checks[]`。
- demo 池名接近真实 `ObjectPoolNames`，并且 `DestroyAll()` 有全局污染风险。

新的验证拆三层：

| 层级 | 目标 | 门禁 |
| --- | --- | --- |
| Runtime contract | 验证 `Get/Release/Activate`、统计、容量、重复归还、manager mapping、隔离池名 | 可自动运行的 contract checks，不依赖 UI。 |
| Godot collision validation | 验证 `Area2D` / `CharacterBody2D` 根节点回池脱树、`Get(false)` 窗口碰撞关闭、旧位置不补发 entered | scene README 五字段 + PASS artifact + `checks[]`。 |
| Manual demo | 保留现有可视化面板用于人工观察池复用和统计 | 不作为 PASS/FAIL；可改名或标记 legacy demo。 |

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
