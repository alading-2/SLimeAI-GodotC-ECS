# Runtime Mount And Node Lifecycle Hard Cutover

## Goal

把 SceneTree 运行时挂载和 Godot Node 生命周期注册从“Tools 杂项工具 + 自由字符串 API”改成 AI-first Runtime 基础设施：

- `ParentManager` 功能升级为 `RuntimeMountRegistry` / `SceneMountRegistry`。
- 默认运行时 root 为 `/root/SlimeAIRuntime`。
- mount 由 manifest 的 `MountId` / scope / path / creation mode 定义，不靠散落字符串。
- `NodeLifecycleManager` 迁到 Runtime registry 语义，记录 owner/source metadata、snapshot diagnostics 和 invalid cleanup。
- Entity / UI / Component 对外查询继续走各自 manager；业务路径不再直接使用 NodeLifecycle 全局扫描。

非目标：

- 不重写 Entity 生命周期、ObjectPool runtime state、UI bind 或 Damage/Collision 业务关系。
- 不保留 `ParentManager.GetOrRegister(name, path)` 作为 current public API。
- 不把 Godot group 当作 SlimeAI gameplay 查询事实源。

## Context

必须先读：

- 项目共享设计：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/README.md`
- 总览：`design/Tool/其他Tool/01-现状证据与AI-first裁决.md`
- 功能文档：`design/Tool/其他Tool/04-NodeLifecycle与ParentManager边界.md`
- 实施门禁：`design/Tool/其他Tool/06-实施路线与验证门禁.md`
- DocsAI：`DocsAI/ECS/Tools/ParentManager/`、`DocsAI/ECS/Tools/NodeLifecycle/`
- 源码：`Src/ECS/Tools/ParentManager/**`、`Src/ECS/Tools/NodeLifecycle/NodeLifecycleManager.cs`
- 调用点：`Src/ECS/Runtime/System/SystemManager.cs`、`Src/ECS/Runtime/Entity/Manager/EntityManager.cs`、`Src/ECS/UI/Core/UIManager.cs`、`Src/ECS/Tools/ObjectPool/**`

用户已确认：

- `ParentManager` 的路径规范化功能有用。
- 运行时生成的 Entity / Pool / UI 节点默认统一挂到 `/root/SlimeAIRuntime`。
- 可以完全重构，不要求兼容旧 API。
- `NodeLifecycle` 的统一注册、维护、id 管理本意成立，但应迁到 Runtime 语义。

## Design

目标目录建议：

```text
Src/ECS/Runtime/Mount/
  RuntimeMountRegistry.cs
  RuntimeMountManifest.cs
  RuntimeMountId.cs
  RuntimeMountSnapshot.cs
  RuntimeMountStatus.cs

Src/ECS/Runtime/NodeLifecycle/
  NodeLifecycleRegistry.cs
  NodeLifecycleRegistration.cs
  NodeLifecycleOwnerKind.cs
  NodeLifecycleSnapshot.cs
  NodeLifecycleDiagnostics.cs
```

目标 API 形态：

```text
RuntimeMountRegistry.GetOrCreate(RuntimeMountId id)
RuntimeMountRegistry.GetSnapshot()
NodeLifecycleRegistry.Register(Node node, NodeLifecycleOwner owner, string source)
NodeLifecycleRegistry.Unregister(Node node)
NodeLifecycleRegistry.GetSnapshot()
```

hard cutover 口径：

- 旧 `ParentManager.GetOrRegister(name, path)` 最终删除或 internal/test-only，不作为 DocsAI current 示例。
- `ParentNames` 不再扩展为业务关系常量表。
- `NodeLifecycleManager.GetAllNodes()` / `GetNodesByInterface<T>()` 不作为业务查询入口。
- TargetSelector 候选来源后续由 SDD-0036 通过 candidate source 处理，不直接依赖 NodeLifecycle。

## Verification

基础验证：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0035
```

grep gate：

```bash
rg -n "ParentManager\\.GetOrRegister|ParentManager\\.Register|ParentNames" Src/ECS DocsAI/ECS
rg -n "NodeLifecycleManager\\.GetAllNodes|NodeLifecycleManager\\.GetNodesByInterface" Src/ECS/Capabilities Src/ECS/Tools/TargetSelector DocsAI/ECS
```

验收必须证明：

- mount snapshot 能显示 pending / in-tree / invalid。
- 同一个 mount id 重复注册不会创建重复 root child。
- Entity / UI / Component / Test 注册来源可区分。
- NodeLifecycle snapshot 中 invalid node count 可诊断。
- DocsAI 示例签名与源码一致。
