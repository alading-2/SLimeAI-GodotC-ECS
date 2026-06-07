---
name: ecs-component
description: 修改 SlimeAI ECS Runtime Component 契约、IComponent、TemplateComponent、ComponentRegistrar 或 GodotBridge Adapter 时使用；skill ID 暂保留 ecs-component 以覆盖旧查询。
---

# Runtime Component / GodotBridge Adapter 入口

## 必读入口

- `DocsAI/README.md`
- `DocsAI/ECS/Runtime/Component/README.md`
- `DocsAI/ECS/Runtime/Entity/`
- `DocsAI/ECS/Runtime/Event/`
- `DocsAI/ECS/Capabilities/Unit/README.md`
- `DocsAI/ECS/Capabilities/Movement/README.md`

## 源码位置

- `Src/ECS/Runtime/Component/`（`IComponent` / `TemplateComponent`）
- `Src/ECS/Runtime/Component/ComponentComposition.cs`（`IComponentCompositionProvider` / `ComponentComposer` / `ComponentCompositionProfile`）
- `Src/ECS/Runtime/Entity/Components/`（`ComponentRegistrar` / EntityManager component partial）
- `Src/ECS/Runtime/NodeLifecycle/`（Component 底层 Node 注册 diagnostics）
- `Src/ECS/Runtime/Entity/`
- `Src/ECS/Runtime/Event/`
- `Src/ECS/Runtime/System/`
- `Src/ECS/Capabilities/*/Component/`
- `Src/ECS/Capabilities/Unit/Entity/UnitComponentCompositionProfiles.cs`
- `Src/ECS/Capabilities/Ability/Entity/AbilityComponentCompositionProfiles.cs`
- `Src/ECS/Capabilities/*/Presets/`（legacy Component Preset 迁移对照；新组合不再新增）
- `Src/ECS/UI/`
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/Src/Game/`
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/Src/Validation/Game/`

## 规则

- `IComponent` 是 SlimeAI 自定义 Component 接入 Runtime Entity 的生命周期契约；新组件优先实现它，旧命名兼容只作为过渡。
- Component 注册初始化只使用 `OnComponentRegistered`，注销清理只使用 `OnComponentUnregistered`；不要用 Godot `_EnterTree()` / `_Ready()` 初始化 Entity、Data 或 Event。
- Component owner 反查走 `EntityManager.GetEntityByComponent` / `ComponentRegistrar`，不要恢复旧 Entity relationship 常量。
- Component 注册到 NodeLifecycle 时必须由 `ComponentRegistrar` 写入 `NodeLifecycleOwner.Component(ownerId, componentId)` 和 source；业务代码不要通过 NodeLifecycle 全局扫描找 Component。
- 具体业务组件放 `Src/ECS/Capabilities/<owner>/Component/`；Runtime/Component 只放接口、模板和通用规则。
- Component 组合目标是纯代码化 `ComponentCompositionProfile` / `ComponentComposer`；旧 Preset `.tscn` 只作为迁移期对照，不新增 Component Preset。
- `EntitySpawnPipeline` 和 `EntityManager.RegisterComponents(entity)` 都必须在 `ComponentRegistrar.RegisterComponents` 前调用 `ComponentComposer.Compose(entity)`；composition provider 路径不得依赖预热缓存，否则新建组件可能漏注册。
- 固定结构参数由代码化 composer/profile 在注册前 typed 注入，优先使用 `Configure(TOptions)` 或构造期 options；不要扩展 `OnComponentRegistered(Node)` 签名传弱类型参数。
- 禁止使用 Inspector 导出参数作为 Component 默认配置来源。
- Component options 不等于 Data：共享业务状态、runtime snapshot 配置、跨系统可观察结果进入 `Entity.Data`；仅影响本组件 Godot bridge 策略的值留在 options。`EntityOrientationComponent.Sink` 属于 options，不新增 DataKey。
- 新文档和新 API 优先称为 GodotBridge Adapter；`IGodotComponent` / `Godot*Component` 是 legacy compatibility name。
- 框架 bridge adapter 当前仍实现 `IGodotComponent`，注册时接入 Runtime Entity。
- scoped bridge 修改必须使用 `GodotBridgeContext` 和 context-owned `GodotBridgeNodeRegistry`；static `GodotNodeRegistry` 只代表默认 context。
- adapter callback guard 必须来自目标 context 的 `RuntimeWorld.Commands.EnterGuard("godot-bridge-callback")`。
- Adapter 业务状态写入 `Entity.Data` / DataKey，不要用私有字段作为长期状态真相。
- Adapter 间通信走 `Entity.Events` 或 Capability / Runtime 服务，不直接互调具体节点方法。
- `_Process` 中避免分配对象和 LINQ。
- 单位组合入口归本 skill 路由：`ComponentComposer`、`ComponentCompositionProfile`、`GodotUnitComposer`、`GodotUnitCompositionProfile`、`GodotUnitCompositionResult` 只能组合框架通用 adapter，不引用 `BrotatoLike.*` namespace，不挂游戏输入、主动技能、HUD、波次或游戏数值逻辑。
- `GodotUnitComposer.Compose(GodotEntity2D, GodotUnitCompositionProfile)` 若在 entity 已进入 SceneTree 后追加 adapter，必须使用当前 `BridgeContext.RegisterComponents(entity, entity)` 重新注册；若未进树，调用方应在 `AddChild(entity)` 前完成 composition。
- `VisualRoot` 从 `UnitDataKeys.VisualScenePath` 加载；hurtbox circle 半径优先读 `CollisionDataKeys.CollisionRadius`，缺失或无效时才用 profile fallback。
- 修改 `GodotUnitAnimationComponent` 的 locomotion、damage/death 或 Unit animation event 行为时，要验证旧 `PlayAnimationRequested / StopAnimationRequested / AnimationFinished` 兼容性。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
rg -n "\\[Export\\]" Src/ECS/Capabilities/*/Component Src/ECS/Runtime/Component -g '*.cs'
rg -n "Capabilities/.*/Presets/.*Preset\\.tscn" Src/ECS/Capabilities/*/Entity -g '*.tscn'
rg -n "EntityRelationshipType\\.|ENTITY_TO_" Src/ECS/Runtime/Component Src/ECS/Runtime/Entity/Components DocsAI/ECS/Runtime/Component -g '*.cs' -g '*.md'
# 如果承载游戏提供 runner，再执行专项场景和 smoke：
# cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
# Tools/run-godot-scene.sh run res://Src/Validation/Game/UnitComposition/BrotatoLikeUnitCompositionValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
# Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
# Tools/analyze-godot-scene-logs.sh
```
