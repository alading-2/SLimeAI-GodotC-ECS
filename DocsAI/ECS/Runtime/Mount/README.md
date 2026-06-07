# Runtime Mount

> 状态：current
> 更新：2026-06-07
> 源码：`Src/ECS/Runtime/Mount/`

## 定位

Runtime Mount 负责统一创建和诊断运行时 Godot 节点挂载点。默认根节点是：

```text
/root/SlimeAIRuntime
```

`SystemManager._EnterTree()` 初始化 `RuntimeMountService`。Entity、ObjectPool、UI、Tool 和 System 需要稳定挂载点时走 `RuntimeMountService` / `RuntimeMountRegistry`，不再使用自由字符串 `ParentManager` current API。

## Current API

```csharp
RuntimeMountService.Initialize(GetTree().Root);
Node parent = RuntimeMountService.GetOrCreate(RuntimeMountIds.EntityType(typeName), "ECS/Entity/Enemy", "Runtime.Entity", "Enemy Entity 挂载点");
RuntimeMountSnapshot snapshot = RuntimeMountService.GetSnapshot();
```

## 规则

- mount id 使用 `RuntimeMountId`，不要新增散落的自由字符串 parent name。
- 默认 runtime root 是 `/root/SlimeAIRuntime`。
- snapshot 必须能表达 `Pending / InTree / Invalid`。
- ObjectPool parking state 仍归 `ObjectPoolRuntimeStateStore`，不写入 Runtime Mount。
- Runtime Mount 只管理 SceneTree 挂载，不管理 Entity 生命周期、UI bind、Damage、Collision 或 owner projection。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
rg -n "ParentManager\\.GetOrRegister|ParentManager\\.Register|ParentNames" Src/ECS DocsAI/ECS
```
