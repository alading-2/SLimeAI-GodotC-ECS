# NodeLifecycle 与 ParentManager 边界

> 更新：2026-06-03
> 状态：current design input
> 裁决：两者都需要保留，但都应降级为底层工具，不再承担业务 owner 语义。`NodeLifecycle` 管注册表，`ParentManager` 管挂载点；Entity 生命周期、UI 绑定、对象池状态和业务关系不归它们。

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

必要，但不是最终业务查询事实源。

它现在是 Entity/UI/Component 的底层注册桥，短期不能删除。尤其在 Entity hard cutover 尚未完全清掉旧路径时，它仍承担兼容注册和测试断言。

但 AI-first 的目标不是让所有业务继续扫 `NodeLifecycleManager`，而是：

```text
Entity 查询 -> Entity runtime / TargetSelector
UI 查询     -> UIManager
Component   -> ComponentRegistrar
底层 Node   -> NodeLifecycleManager
```

### ParentManager

必要。

运行时需要统一挂载点，否则 Entity、ObjectPool、System、UI 会散挂到任意节点下，AI debug 和场景树观察都困难。

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
| ParentManager | 自由字符串 name/path | 挂载点不是 manifest，调用点可随意新增。 |
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

保留底层 API，但补显式契约：

```text
NodeLifecycleManager
  -> low-level Node registry
  -> owner/source metadata
  -> snapshot diagnostics
  -> invalid cleanup
  -> no direct business query by default
```

建议新增数据形状：

```text
NodeLifecycleRegistration
  NodeId
  NodeType
  OwnerKind: Entity | Component | UI | System | Tool | Test
  OwnerId
  RegisteredFrame
  Source

NodeLifecycleSnapshot
  Total
  ByOwnerKind
  ByType
  InvalidCount
  DuplicateTypeNameWarnings
```

第一阶段不必删除旧 `Register(Node)`，但新 manager 调用点应尽量传 owner/source。

### 5.2 ParentManager 目标

演进为 manifest 化 mount registry：

```text
RuntimeMountRegistry
  MountId
  Path
  Scope: Root | CurrentScene | SystemHost | PoolParking | Test
  Owner
  CreationMode: Immediate | DeferredRoot
  Status: Pending | InTree | Invalid
```

兼容 facade：

```text
ParentManager.GetOrRegister(name, path)
  -> RuntimeMountRegistry.GetOrCreate(new MountId(name), path)
```

第一阶段可保留 `ParentManager` 名称，先补 diagnostics 和 manifest 文档。

## 6. 调用点迁移策略

### EntityManager

- 继续通过 ParentManager 挂载非对象池 Entity。
- 后续挂载点不应直接用 `typeof(T).Name` 作为唯一 mount id，应由 Entity mount manifest 定义。
- Entity 查询不应直接暴露 NodeLifecycle，目标是走 EntityRegistry / TargetSelector。

### UIManager

- UI 注册可以继续委托 NodeLifecycle。
- UI 查询对外只暴露 UIManager API。
- UI 绑定关系后续应从旧 `EntityRelationshipManager` 迁移到 UI owner registry，不能由 NodeLifecycle 承担。

### TargetSelector

- 第一阶段仍可从 NodeLifecycle/EntityManager 获取候选。
- 但 TargetSelector 必须封装 candidate source，后续替换成 EntityRegistry 或 spatial index 时不影响 Ability/AI 调用点。

### ObjectPool

- ParentManager 只给 pool parent。
- pool state、parking position、collision ready frame 继续归 ObjectPool runtime state，不写到 ParentManager。

## 7. Not Recommended

- 不建议删除 NodeLifecycle 后让所有模块直接维护自己的 Godot node list。
- 不建议业务代码新增 `NodeLifecycleManager.GetAllNodes()` 全局扫描。
- 不建议用 Godot group 替代 Entity registry。
- 不建议 ParentManager 参与 Destroy、Release、Damage、Collision 或 UI Bind。
- 不建议把 `ParentNames` 扩成业务关系常量表。

## 8. 验证门禁

文档/设计阶段：

```bash
rg -n "NodeLifecycleManager\\.Register\\(|NodeLifecycleManager\\.GetAllNodes|NodeLifecycleManager\\.GetNodesByInterface|ParentManager\\.GetOrRegister|ParentManager\\.Register|ParentNames" Src/ECS DocsAI/ECS
python3 Workspace/SDD/sdd.py validate --all
```

实现阶段：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

建议新增 diagnostics 验收：

- NodeLifecycle snapshot 中 invalid node count 为 0。
- Entity/UI/Component/Test 注册来源可区分。
- Parent mount snapshot 能显示 pending / in-tree 数量。
- 同一 mount id 重复注册不会创建重复 root child。
- DocsAI 示例签名与源码一致。
