# ObjectPool Collision ParkedInTree Cutover

## Goal

把 ObjectPool 与 Collision 的碰撞池化策略从旧默认“脱树 / 关碰撞 / deferred 属性切换”迁移到明确的 `ParkedInTree + CollisionLogicGuard + ActivationFrameEmbargo`。

完成后：

- `ObjectPool<T>` public API 基本保持不变：`Get(bool) / Activate / Release / ReleaseAll / Cleanup / Clear` 仍是外部入口。
- 根节点为 `CollisionObject2D` 的池化对象默认仍在树中，隐藏、停处理、移动到分散 parking grid，不默认 `RemoveChild`、不默认关闭碰撞、不改 layer/mask/shape。
- pool runtime state 可观测：`IsInPool`、`CollisionLogicActive`、`CollisionReadyPhysicsFrame`、release/acquire frame、parking position、fallback detach。
- Collision / Movement / ContactDamage / Damage 业务入口必须查 pool runtime state 和 ready frame，再做实体有效性、team、owner、lifecycle 过滤。
- ObjectPool tests 拆为 runtime contract、Godot collision validation 和 manual demo；Godot validation 产出结构化 PASS artifact。

## Context

已读事实源：

- `DocsAI/ECS/Tools/ObjectPool/README.md`、`Concept.md`、`Usage.md`、`Tests.md`
- `DocsAI/ECS/Capabilities/Collision/README.md`
- `DocsAI/ECS/Capabilities/Collision/Concepts/README.md`
- `DocsAI/ECS/Capabilities/Collision/Concepts/Godot物理时序与对象池碰撞.md`
- `DocsAI/ECS/Capabilities/Collision/Concepts/Node2D父链约束.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/ObjectPool/`
- `Src/ECS/Tools/ObjectPool/ObjectPool.cs`
- `Src/ECS/Tools/ObjectPool/ObjectPoolManager.cs`
- `Src/ECS/Tools/ObjectPool/ObjectPoolObservability.cs`
- `Src/ECS/Capabilities/Collision/Component/*`

当前源码仍是旧实现：`ObjectPool.cs` 包含 `NeedsTreeDetach(node is CollisionObject2D)`、`SetCollisionTreeActive`、`RemoveChild`、`ReattachToTree` 和 `ForceDisableCollisionsDirect`。这些是历史防线，不是目标默认策略。

当前文档治理已先行完成一部分：`DocsAI/ECS/Capabilities/Collision/Concepts` 顶层已经精简为 current 入口，旧长文移动到 `History/`。

## Design

### DeepThink

**Goal**

解决 ObjectPool 和 Collision 对 Godot 物理根节点复用的职责混杂问题，形成代码、文档、测试和 skill 都能恢复的执行计划。非目标是不删除对象池、不重写 Entity Runtime、不把 pool state 写入 DataOS、不复制外部物理 API。

**Context Read**

已读取 PRJ-0002 项目入口、ObjectPool 共享设计包、Collision/ObjectPool DocsAI、ObjectPool 源码和 Collision 组件源码。Git boundary 是 `/home/slime/Code/SlimeAI/SlimeAI`。当前工作区存在大量既有 unrelated dirty 项，执行时禁止清理或回滚。

**Problem Shape**

问题由四层叠加：ObjectPool 旧默认脱树实现、Godot physics/signal/deferred 时序、Entity 多阶段初始化、Collision/Damage 入口缺统一 guard。`Node2D` 父链断裂是独立场景结构问题，不能继续归因给对象池。

**Main Risks**

- 只改 ObjectPool 不改业务入口 guard，会让旧 signal 或旧 attacker 引用继续进入业务。
- 只改 Collision 不改 ObjectPool 运行时状态，会让 guard 无可靠事实源。
- 只用 stdout / exit code 证明 Godot validation，会无法区分旧 signal、停车区事件和新命中。
- 默认回到脱树 / 关碰撞，会与当前裁决冲突。

**Options**

1. **最小文档收口 + SDD 生成**：先归档旧 Concepts、生成执行 SDD，不动源码。成本低，适合当前任务，但不解决运行时。
2. **一次性 full cutover**：直接改 ObjectPool、Collision、Movement、ContactDamage、tests、DocsAI 和 skill。完整但影响面大，当前用户要求仍是生成 SDD。
3. **拆分成两个 SDD**：ObjectPool runtime state 一个、Collision guard 一个。边界清晰，但 guard 与 state 强耦合，容易跨 SDD 漂移。

**Recommendation**

采用方案 1 作为本轮完成范围，并把方案 2 写入 SDD-0028 执行计划。SDD 内部按 T1.1~T1.12 小步实施，避免实际执行时只完成 ObjectPool 半边。

**Must Confirm**

无阻塞确认项。当前已按用户要求默认执行：精简 Concepts、归档 History、生成 SDD 和提示词。

**Should Confirm**

- 后续执行是否在 SDD-0028 中一次性实现 ObjectPool runtime state 与 Collision guard。
- Godot validation runner 是否仍使用 BrotatoLike 当前仓，还是等承载游戏恢复 runner 后再跑。

**Defaults I Will Use**

- SDD-0028 先保持 `pending`，只生成执行胶囊，不立即实施代码迁移。
- 项目级 current SDD 切到 SDD-0028；SDD-0027 仍保留 blocked。
- `Detach` 只写成 fallback / control check，不写成默认策略。
- `Entity.Data` 不保存 pool runtime state。

**Not Recommended**

- 不把旧 `对象池碰撞兼容说明.md` 继续作为 current 顶层入口。
- 不把当前源码旧实现描述成已经完成的目标状态。
- 不把 ObjectPool validation 和 Collision scene structure gate 混成一个测试。

**Artifact Updates**

本轮写入：`DocsAI/ECS/Capabilities/Collision/Concepts/`、本 SDD 的 `design/`、`tasks.md`、`bdd.md`、`progress.md`、`notes.md`、`execution-prompt.md`，以及 PRJ-0002 项目索引/路线图/进度。

### 目标模块

```text
ObjectPool<T>
  -> public API, stack/capacity/stats/lifecycle hook

PoolNodeLifecycleStrategy
  -> Node meta, visible, process mode, parent, discard

PoolParkingStrategy
  -> pool-aware parking grid, ForceUpdateTransform, parking observation

ObjectPoolRuntimeStateStore
  -> PoolRuntimeState by node instance id

CollisionLogicGuard
  -> CanProcessCollision(self, target, currentPhysicsFrame)

DetachFallbackStrategy
  -> explicit fallback / validation control, not default
```

### 关键取舍

- 保留 `ObjectPool<T>` 外部 API，先拆内部策略，减少调用点 blast radius。
- 默认不再 `RemoveChild`、不 `SetCollisionTreeActive(false)`、不禁用 `CollisionShape2D`、不改 layer/mask。
- 激活第一 physics frame 默认不处理业务碰撞，用低成本吸收旧 dispatch / 同帧复用风险。
- 若 validation 证明仍不够，再引入 acquire token / generation；不在 P0 先复杂化。
- ContactDamage 的 timer 旧 attacker 引用按独立 bug 处理，tick 前必须重新查 pool state。

## Verification

文档/SDD 阶段：

```bash
python3 Workspace/SDD/sdd.py validate SDD-0028
python3 Workspace/SDD/sdd.py validate --all
git diff --check
```

代码阶段：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
rg -n "NeedsTreeDetach|SetCollisionTreeActive|RemoveChild\\(|ForceDisableCollisionsDirect|CollisionShape2D\\.PropertyName\\.Disabled|Area2D\\.PropertyName\\.Monitoring|Area2D\\.PropertyName\\.Monitorable" Src/ECS/Tools/ObjectPool Src/ECS/Capabilities/Collision
```

Godot 场景阶段：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/ObjectPool/Tests/ObjectPoolCollisionIsolationValidation.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

如果当前承载游戏没有 runner 或 Godot CLI，必须在 progress 中记录阻塞，不能声称 scene validation 已通过。
