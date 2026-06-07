# NodeLifecycle 与 ParentManager 边界

> 更新：2026-06-04
> 状态：current design input, hard cutover override, user review calibrated
> 裁决：`ParentManager` 的功能必须保留并升级为 Runtime mount 能力；用户截图已证明当前规范路径方向有效。`NodeLifecycle` 功能保留为 Runtime 底层 Node registry，用于统一注册、注销、id、owner metadata 和 diagnostics；但业务查询入口应 hard cutover 到 Entity/UI/TargetSelector typed facade。执行时只保功能，不保旧 API 兼容。

## 1. 当前证据

### 1.1 NodeLifecycle 当前形态

`Src/ECS/Tools/NodeLifecycle/NodeLifecycleManager.cs`：

- `_nodes: Dictionary<string, Node>`，key 为 `GetInstanceId().ToString()`。
- `_nodesByType: Dictionary<string, HashSet<Node>>`，key 为 `node.GetType().Name`。
- 提供 `Register`、`Unregister`、`GetNodeById`、`GetNodesByType<T>`、`GetNodesByInterface<T>`、`GetAllNodes`、`GetStats`、`Clear`、`GetDebugInfo`。

主要调用点：

- `EntityManager.Register/Unregister/GetAllEntities/GetEntitiesByType`
- `ComponentRegistrar.Register/Unregister`
- `UIManager.Register/Unregister/GetAllUI/ClearAllUI`
- `EntityTargetSelector.GetAllNode2DEntities`
- `EffectTool` 注册检查
- 多个 Runtime/Movement 测试断言注册状态

### 1.2 NodeLifecycle 文档漂移

`DocsAI/ECS/Tools/NodeLifecycle/Usage.md` 仍写：

```csharp
NodeLifecycleManager.Register(node, "Enemy");
NodeLifecycleManager.GetNodesByType<Enemy>("Enemy");
```

但源码只有：

```csharp
Register(Node node)
GetNodesByType<T>() where T : Node
```

这是典型 AI-first 缺陷：文档示例会误导 AI 写不存在的 API。

### 1.3 ParentManager 当前形态

`Src/ECS/Tools/ParentManager/ParentManager.cs`：

- `_root: Node?`
- `_parents: Dictionary<string, Node>`
- `_pendingRootNodes: Dictionary<string, Node>`
- `Init(root)` 清空状态。
- `Register(name, path)` 确保路径存在并登记。
- `GetOrRegister(name, path)` 返回父节点。
- `EnsurePath(ancestor, relativePath)` 创建路径节点；如果 current 是 `_root`，使用 `CallDeferred(AddChild)` 并缓存 pending root node。

主要调用点：

- `SystemManager._EnterTree()` 调 `ParentManager.Init(GetTree().Root)`。
- `EntityManager.AddToSceneTree` 用 `GetOrRegister(typeName, $"ECS/Entity/{typeName}")`。
- `ObjectPool` 注册和 lifecycle strategy 查询 parent。
- ObjectPool validation/test 场景手动 `ParentManager.Init(this)`。

## 2. 是否必要

### NodeLifecycle

必要，但不是最终业务查询事实源，也不应继续放在 `Tools/` 语义下让 AI 当普通工具使用。

它现在是 Entity/UI/Component 的底层注册桥，功能上不能直接消失。但用户已经明确不需要旧代码兼容，因此执行型 SDD 不应把 `Register(Node)`、`GetAllNodes()`、`GetNodesByInterface<T>()` 当作长期公共 API 保留。正确目标是迁移调用点后保留一个低层 `NodeLifecycleRegistry`，对外输出 snapshot / diagnostics，而不是给业务做全局查询。

用户 2026-06-04 补充确认：`NodeLifecycle` 的本意是统一 Node 生命周期、注册、维护和 id 管理，避免 Entity、UI、Component 各写一套。这一判断成立。需要修正的是归属和边界，而不是删掉功能。

但 AI-first 的目标不是让所有业务继续扫 `NodeLifecycleManager`，而是：

```text
Entity 查询 -> Entity runtime / TargetSelector
UI 查询     -> UIManager
Component   -> ComponentRegistrar
底层 Node   -> NodeLifecycleManager
```

### ParentManager

必要，而且是 AI-first 工具层的 P0 功能。

运行时需要统一挂载点，否则 Entity、ObjectPool、System、UI 会散挂到任意节点下，AI debug 和场景树观察都困难。

用户已明确：`ParentManager` 有用，它统一管理大量 Entity 节点在 tree 中的路径。旧设计中“必要但降级”的表述不准确；应改为“功能升级、接口 hard cutover”。后续目标不是保留旧 `ParentManager.GetOrRegister(name, path)`，而是建立 `RuntimeMountRegistry` / `SceneMountRegistry`：

- mount id 来自 manifest，不来自散落字符串。
- mount scope 明确：全局 root、当前 scene、system host、pool parking 或 test root。
- creation mode 明确：immediate / deferred root。
- status 可诊断：pending / in-tree / invalid。

用户提供的 SceneTree 截图已经证明当前功能方向可用：`ECS/Entity/Unit/EnemyPool`、`PlayerEntity/Player`、`EnemyEntity/Enemy`、`UI/UI/HealthBarUIPool` 等路径一眼能看懂。后续完善不应破坏这种可观察性；推荐只是加顶层 `/root/SlimeAIRuntime` 和 manifest / diagnostics。

但 ParentManager 不应管理：

- Entity 生命周期。
- Entity/Component 关系。
- ObjectPool runtime state。
- UI 绑定。
- 业务 owner。

## 3. 当前缺陷

| Tool | 缺陷 | 风险 |
| --- | --- | --- |
| NodeLifecycle | string instance id 作为 key | 无 typed id，和 EntityId / UI id / Component id 边界混乱。 |
| NodeLifecycle | type index 用 `GetType().Name` | 重名类型或继承层查询不稳定。 |
| NodeLifecycle | 返回 live enumerable | 调用方遍历期间注册/注销可能带来不稳定行为。 |
| NodeLifecycle | 无 invalid node cleanup | 节点销毁但未注销时可能残留。 |
| NodeLifecycle | 无 owner/source metadata | AI 无法判断节点由 Entity、UI、Component 还是测试注册。 |
| NodeLifecycle | DocsAI 示例错误 | AI 会复制不存在 API。 |
| ParentManager | 自由字符串 name/path | 挂载点不是 manifest，调用点可随意新增；执行时应 hard cutover 到 typed mount id / manifest。 |
| ParentManager | root 下 deferred add 后立即返回 pending node | 节点尚未进 tree 时同帧行为难解释。 |
| ParentManager | pending cache 不暴露状态 | AI 无法确认 mount 是否已实际进入 SceneTree。 |
| ParentManager | `ParentNames` 常量混合对象池、Entity、UI | 易误解为业务关系事实源。 |
| ParentManager | Init 可由测试重复调用 | 测试隔离有用，但正式运行 scope 需明确。 |

## 4. Godot 边界校准

Godot 官方文档说明，在不安全时机或跨线程修改 SceneTree 时，应使用 deferred add child。ParentManager 在 root 第一层使用 `CallDeferred(Node.MethodName.AddChild, next)` 有合理依据。

但对 SlimeAI AI-first 来说，仅“用了 CallDeferred”还不够。还需要：

- 记录 pending mount。
- 说明何时可认为 mount 已进入 tree。
- 给测试和 diagnostics 查询。
- 不让业务逻辑依赖 pending node 的 `_Ready` 或 tree 内状态。

Godot group 可以作为引擎级分类集合，但它不能替代 SlimeAI 的 Entity/Data/owner/lifecycle 语义；因此本设计不建议简单改成 `GetTree().GetNodesInGroup()`。

## 5. 目标架构

### 5.1 NodeLifecycle 目标

保留底层能力，但不保旧业务可见公共扫描 API：

```text
NodeLifecycleRegistry
  -> low-level Node registry
  -> owner/source metadata
  -> snapshot diagnostics
  -> invalid cleanup
  -> no public gameplay global scan
```

建议新增数据形状：

```text
NodeLifecycleRegistration
  NodeId
  NodeType
  NodePath
  OwnerKind: Entity | Component | UI | System | Tool | Test
  OwnerId
  RegisteredFrame
  IsInsideTree
  IsValid
  Source

NodeLifecycleSnapshot
  Total
  ByOwnerKind
  ByType
  InvalidCount
  DuplicateTypeNameWarnings
```

执行型 SDD 的结束条件不是“新调用点尽量传 owner/source”，而是：

- Entity / UI / Component 注册点传 owner/source。
- 业务代码不再直接使用 `GetAllNodes()` / `GetNodesByInterface<T>()` 作为玩法查询。
- TargetSelector 通过 candidate source 访问候选，不直接扫 NodeLifecycle。
- 旧公共查询入口若仍存在，必须降为 internal/test-only 或删除。

推荐源码归属：

```text
Src/ECS/Runtime/NodeLifecycle/
  NodeLifecycleRegistry.cs
  NodeLifecycleRegistration.cs
  NodeLifecycleOwnerKind.cs
  NodeLifecycleSnapshot.cs
  NodeLifecycleDiagnostics.cs

DocsAI/ECS/Runtime/NodeLifecycle/
```

理由：它是 Runtime 基础设施，服务 Entity / UI / Component / System / Tool 的 Godot Node 注册，不是普通 Tools helper。

### 5.2 ParentManager 目标

升级为 manifest 化 mount registry：

```text
RuntimeMountRegistry
  MountId
  Path
  Scope: RuntimeRoot | CurrentScene | SystemHost | PoolParking | Test
  Owner
  CreationMode: Immediate | DeferredRoot
  Status: Pending | InTree | Invalid
```

用户已确认默认 root scope：

```text
/root/SlimeAIRuntime
  ECS
    Entity
    Pool
  UI
  Tool
```

说明：这不要求抹掉截图中已有 `ECS/Entity/...`、`UI/...` 层级，只是把这些运行时节点统一收进框架根，避免散落在 root 或当前场景下。

旧 API 不作为长期目标：

```text
ParentManager.GetOrRegister(name, path)
  -> 迁移到 RuntimeMountRegistry.GetOrCreate(MountId)
```

如果执行时需要临时 facade 辅助编译，切片完成前必须删除或标记 internal，不进入 DocsAI current 入口。DocsAI current 入口应讲 `RuntimeMountRegistry` / `SceneMountRegistry` 功能语义，而不是让 AI 继续学习自由字符串 `ParentManager` API。

## 6. 调用点迁移策略

### EntityManager

- 非对象池 Entity 继续需要统一 mount，但入口应迁到 RuntimeMountRegistry。
- 挂载点不应直接用 `typeof(T).Name` 作为唯一 mount id，应由 Entity mount manifest 定义。
- Entity 查询不应直接暴露 NodeLifecycle，目标是走 EntityRegistry / TargetSelector。

### UIManager

- UI 注册可以使用 NodeLifecycleRegistry 作为底层登记，但对外只暴露 UIManager / UI owner API。
- UI 查询对外只暴露 UIManager API。
- UI 绑定关系后续应从旧 `EntityRelationshipManager` 迁移到 UI owner registry，不能由 NodeLifecycle 承担。

### TargetSelector

- 默认从 EntityRegistryCandidateSource 或显式 candidate source 获取候选。
- 如果执行早期临时接 NodeLifecycle，必须封装为 candidate source，切片结束前不让 Ability/AI 调用点知道 NodeLifecycle。

### ObjectPool

- RuntimeMountRegistry 只给 pool parent / parking mount。
- pool state、parking position、collision ready frame 继续归 ObjectPool runtime state，不写到 ParentManager。

## 7. Not Recommended

- 不建议删除 NodeLifecycle 后让所有模块直接维护自己的 Godot node list。
- 不建议继续把 NodeLifecycle 作为 `Tools/` 下的普通业务工具。
- 不建议业务代码新增 `NodeLifecycleManager.GetAllNodes()` 全局扫描。
- 不建议用 Godot group 替代 Entity registry。
- 不建议 RuntimeMountRegistry / ParentManager 参与 Destroy、Release、Damage、Collision 或 UI Bind。
- 不建议把 `ParentNames` 扩成业务关系常量表。
- 不建议长期保留 `ParentManager.GetOrRegister(name, path)` 作为 current API。

## 8. 验收关注点

完整命令统一放在 [06-实施路线与验证门禁.md](06-实施路线与验证门禁.md)，本功能文档只记录 Runtime mount 与 NodeLifecycle 切片必须证明什么：

- NodeLifecycle snapshot 中 invalid node count 为 0。
- Entity/UI/Component/Test 注册来源可区分。
- Parent mount snapshot 能显示 pending / in-tree 数量。
- 同一 mount id 重复注册不会创建重复 root child。
- DocsAI 示例签名与源码一致。
- `rg -n "ParentManager\\.GetOrRegister|ParentManager\\.Register|ParentNames" Src/ECS DocsAI/ECS` 在 current 代码/文档中只剩迁移说明或 0 命中，具体口径由执行型 SDD 定义。
- `rg -n "NodeLifecycleManager\\.GetAllNodes|NodeLifecycleManager\\.GetNodesByInterface" Src/ECS/Capabilities Src/ECS/Tools/TargetSelector DocsAI/ECS` 在业务路径中 0 命中。
