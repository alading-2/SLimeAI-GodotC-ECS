# Component 代码化组合与参数注入裁决

> 状态：current
> 更新：2026-06-04
> 目标：冻结 Component Preset 纯代码化方向，并明确 Component 参数与 Data 的边界。

## 用户裁决

2026-06-04 用户已确认：

- Component 组合目标是完全代码化。
- SlimeAI Component 是定制生命周期节点，不使用 Godot `_EnterTree()` / `_Ready()` 作为注册入口。
- Component 生命周期只使用 `IComponent.OnComponentRegistered` / `OnComponentUnregistered`。
- Component 参数注入可在添加 Component 和调用 `OnComponentRegistered` 的同一阶段完成。
- 转成纯代码后禁止使用 `[Export]` / Inspector 作为 Component 默认配置来源。
- 需要同步更新 `TemplateComponent`、DocsAI Component 规范和相关文档。

## 一句话结论

性能不是保留 Component Preset 的理由；当前 Unit / Ability Preset 很小，`.tscn` 实例化和代码创建的差异不应作为架构主因。真正的 AI-first 收益来自：

- 组件组合以 C# profile / composer 作为事实源，AI 可直接阅读和生成。
- 组件结构参数以 typed options 注入，不藏在 Inspector。
- DataOS 继续只管理共享业务状态、runtime snapshot 配置和跨系统可观察结果。
- `OnComponentRegistered` 仍是唯一注册初始化入口，不扩展 Godot 生命周期。

## Context Read

本轮新增读取和确认：

- `Src/ECS/Runtime/Component/IComponent.cs`
- `Src/ECS/Runtime/Component/TemplateComponent.cs`
- `DocsAI/ECS/Runtime/Component/README.md`
- `DocsAI/ECS/Runtime/Component/Concepts/IComponent接口说明.md`
- `DocsAI/ECS/Runtime/Component/Concepts/Component规范说明.md`
- `DocsAI/ECS/Runtime/Component/Concepts/Component数据驱动设计理念.md`
- `Src/ECS/Capabilities/Movement/Component/EntityOrientationComponent.cs`
- `Src/ECS/Capabilities/Unit/Presets/UnitCorePreset.tscn`
- Godot 4.6 官方 `PackedScene.instantiate` 文档。
- 本地 Godot 4.6.2 源码 `scene/resources/packed_scene.cpp`。

事实边界：

- 本轮更新设计、规范、模板和 skill，不实施运行时 `ComponentComposer`。
- 不改 `IComponent` 方法签名。
- 不删除现有 `.tscn` Preset；删除或停止引用属于后续执行型 SDD。

## Godot 实例化性能判断

Godot 官方文档说明 `PackedScene.instantiate()` 会实例化场景节点层级，并触发子场景实例化。本地 Godot 4.6.2 源码显示：

```text
PackedScene::instantiate
  -> SceneState::instantiate
  -> 对 scene 中节点逐个 ClassDB::instantiate
  -> Object::set / set_indexed 写入保存的属性
  -> 建立父子关系
```

因此 Preset 不是对象池，也不是免费静态结构。它与代码创建的共同成本都是创建节点、设置属性和挂载子节点。

但复杂场景用 `PackedScene` 通常不比脚本逐条创建差，因为 Godot 后端可以批量处理场景数据。当前 SlimeAI Component Preset 很小，性能差异大概率不是瓶颈。

裁决：是否代码化不以性能为主要依据，而以 AI-first 事实源和参数显式化为主要依据。

## 目标调用顺序

后续实现应遵循：

```text
EntitySpawnPipeline
  -> create entity root
  -> apply runtime snapshot Data
  -> inject visual
  -> ComponentComposer.Compose(entity, profile)
       -> new component
       -> Configure(options) 或构造期传入结构参数
       -> AddChild 到 Entity/Component 容器
  -> EntityRegistry.Register
  -> ComponentRegistrar.RegisterComponents
  -> IComponent.OnComponentRegistered
  -> LifecycleTree.Attach
  -> Activate
```

如果具体实现为了兼容现有 `EntitySpawnPipeline` 需要先 registry 再 compose，也必须保证：

- `Configure(options)` 发生在 `OnComponentRegistered` 前。
- 组件 Entity/Data/Event 初始化只发生在 `OnComponentRegistered`。
- 不要求 Godot `_Ready()` / `_EnterTree()` 完成 SlimeAI 注册。

## 参数注入形态

推荐每个需要结构参数的组件提供 typed options：

```csharp
public readonly record struct EntityOrientationComponentOptions(
    OrientationSink Sink
);

public partial class EntityOrientationComponent : Node, IComponent
{
    private OrientationSink _sink = OrientationSink.RootRotation;

    public void Configure(EntityOrientationComponentOptions options)
    {
        _sink = options.Sink;
    }
}
```

Options 可以是 `readonly record struct`、普通 `struct` 或小型 class；关键是 typed、显式、由 composer 在注册前调用。

不推荐把参数塞进 `OnComponentRegistered(Node entity, object options)` 之类扩签名：

- 会污染所有组件。
- 会把参数协议变成弱类型。
- 会让无参数组件也承担配置复杂度。

## Data 边界

判断一个值是否应进入 Data：

| 问题 | 是 | 否 |
| --- | --- | --- |
| 其他 Component / System / UI / Test 是否需要读取？ | Data | options/private |
| 是否来自 runtime snapshot record？ | Data | options/private |
| 是否是业务状态真相？ | Data | options/private |
| 是否需要对象池统一清理或恢复？ | Data 或生命周期服务 | options/private |
| 是否只影响本组件如何桥接 Godot 节点？ | 通常不是 Data | options |

## `EntityOrientationComponent.Sink` 裁决

`Sink` 表达朝向输出落点：

- `RootRotation`：写 Entity root rotation。
- `VisualFlipX`：写 VisualRoot / AnimatedSprite2D flip。

它不是移动状态，不是朝向运行时结果，不是 System 查询条件，也不应由 UI/TestSystem 作为业务状态读取。它只决定这个 Component 如何把已存在的朝向意图桥接到 Godot 节点。

因此：

- 不新增 DataKey。
- 不进入 runtime snapshot。
- 不使用 `[Export]` / Inspector。
- 作为 `EntityOrientationComponentOptions.Sink` 由 Unit / Projectile / Effect composition profile 注入。

这个裁决对 AI 更好，因为 AI 在 profile 中能直接看到“Player 用 VisualFlipX、Projectile 用 RootRotation”之类结构差异，而不用扫描 `.tscn` Inspector override。

## Preset 迁移策略

当前 Preset 状态：

- `Src/ECS/Capabilities/Unit/Presets/UnitCorePreset.tscn`
- `Src/ECS/Capabilities/Unit/Presets/EnemyPreset.tscn`
- `Src/ECS/Capabilities/Unit/Presets/PlayerPreset.tscn`
- `Src/ECS/Capabilities/Ability/Presets/AbilityPreset.tscn`

后续迁移：

1. 建立 `ComponentComposer` / `ComponentCompositionProfile`。
2. 用代码 profile 复刻当前 Preset 组合。
3. 加 runtime validation，证明注册组件数量、类型和关键 options 与旧 Preset 一致。
4. 让 Entity scene 停止 instance Component Preset。
5. 删除或 archive Preset `.tscn` 和 `ResourceCategory.Preset` 中仅用于 Component 组合的路径。

注意：本裁决只针对 Component Preset。Entity root `.tscn`、视觉 scene、碰撞 shape、Camera 等 Godot 场景资源是否代码化，是 Entity / Visual authoring 另一个决策，不在本文件直接裁掉。

## 后续执行任务

建议把原执行型 SDD 标题调整为：

```text
Component Code Composition And Contract Hardening
```

首切片任务：

- 新增代码化 `ComponentCompositionProfile` / `ComponentComposer` 设计。
- 为 `EntityOrientationComponent` 增加 typed options 并删除 `[Export]`。
- 为现有 Unit / Ability Preset 建等价 profile。
- 调整 `EntitySpawnPipeline` 或 entity factory，让 composition 在 register 前完成。
- 增加对比验证：旧 Preset 与新 profile 注册组件集合一致。
- 更新 DocsAI / skill / manifest / preflight。

## Must Confirm

暂无阻塞性 Must Confirm。用户已确认“完全代码化”和“参数注入在注册同一阶段”。

## Defaults I Will Use

若进入实现，默认采用：

- 不改 `IComponent` 方法签名。
- 参数注入用 `Configure(TOptions)` 或构造期 typed options。
- 禁止新增 `[Export]`。
- 新 Component 不新增 `.tscn` Preset。
- `EntityOrientationComponent.Sink` 不进 Data。
- 先保留 Entity root `.tscn`，只代码化 Component Preset。

## Not Recommended

- 不建议把所有 Component options 放进 DataOS。
- 不建议用 `object` / `Dictionary` 传参。
- 不建议为了“纯代码化”立刻删除 Entity root scene。
- 不建议保留 Inspector override 作为默认配置入口。
- 不建议让 `_Ready()` / `_EnterTree()` 承担 SlimeAI 注册初始化。
