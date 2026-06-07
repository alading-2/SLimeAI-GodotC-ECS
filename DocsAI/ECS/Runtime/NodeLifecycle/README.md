# Runtime NodeLifecycle

> 状态：current
> 更新：2026-06-07
> 源码：`Src/ECS/Runtime/NodeLifecycle/`

## 定位

Runtime NodeLifecycle 是底层 Godot `Node` 注册表，用于记录注册、注销、owner/source metadata、invalid cleanup 和 diagnostics。它不是 gameplay 查询 API。

业务查询入口：

| 场景 | Current 入口 |
| --- | --- |
| Entity 查询 | `EntityManager` / `EntityRegistry` / `TargetQueryEngine` |
| Component owner 反查 | `ComponentRegistrar` / `EntityManager.GetEntityByComponent` |
| UI 查询 | `UIManager` |
| 目标选择 | `TargetQueryEngine` candidate source |
| 底层 Node 注册诊断 | `NodeLifecycleRegistry` / `NodeLifecycleManager.GetSnapshot()` |

## Current API

```csharp
NodeLifecycleManager.Register(node, NodeLifecycleOwner.Entity(entityId.Value), "EntitySpawnPipeline.Spawn");
NodeLifecycleSnapshot snapshot = NodeLifecycleManager.GetSnapshot();
int removed = NodeLifecycleManager.CleanupInvalid();
```

`NodeLifecycleManager.GetAllNodes()`、`GetNodesByInterface<T>()`、`GetNodesByType<T>()` 不作为业务 current API；新增 Ability、AI、Feature、TargetSelector 调用点不得直接扫 NodeLifecycle。

## 诊断字段

- `NodeId`
- `NodeType`
- `Owner.Kind`
- `Owner.OwnerId`
- `Source`
- `RegisteredFrame`
- `IsInsideTree`
- `IsValid`
- `NodePath`

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
rg -n "NodeLifecycleManager\\.GetAllNodes|NodeLifecycleManager\\.GetNodesByInterface" Src/ECS/Capabilities Src/ECS/Tools/TargetSelector DocsAI/ECS
```
