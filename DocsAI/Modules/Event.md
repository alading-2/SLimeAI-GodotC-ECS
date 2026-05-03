# Event 模块契约

本文是 AI 使用或修改 EventBus、GameEventType、Entity.Events 和 GlobalEventBus 前必须阅读的执行契约。

## 职责边界

EventBus 是核心逻辑通信通道，用来替代 Godot Signal 处理框架内业务事件。

Event 负责：

- 同一 Entity 内组件解耦通信。
- 跨实体、跨系统的全局通知。
- 通过 `EventContext` 支持请求-响应型检查。
- 隔离 UI、Component、System 的直接依赖。

Event 不负责：

- 保存长期状态。
- 执行业务计算。
- 替代 SystemManager 命令门禁。

## 核心入口

- `Src/ECS/Base/Event/EventBus.cs`
- `Src/ECS/Base/Event/GlobalEventBus.cs`
- `Src/ECS/Base/Event/EventContext.cs`
- `Data/EventType/`

## 数据 / 事件 / 生命周期

- `Entity.Events` 用于同一实体内部组件通信。
- `GlobalEventBus.Global` 用于跨实体、跨系统、关系管理和全局状态通知。
- 事件数据优先使用 `readonly record struct`。
- 全局事件订阅必须在合适生命周期取消。
- 局部事件随 Entity 销毁由 `Events.Clear()` 清理，但 Component 后台失活仍应避免继续处理。
- 带返回值检查使用 `EventContext`，不要通过外部共享字段回填。

## 禁止事项

- 禁止用 Godot `[Signal]` 承载核心业务逻辑。
- 禁止组件间直接互调方法来传递业务事件。
- 禁止全局事件订阅后不取消。
- 禁止事件数据使用 `record class` 或复杂可变对象。
- 禁止把所有子实体事件都发到 Player 这类万能实体上。

## 修改流程

1. 判断事件归属：Entity 局部还是全局。
2. 在 `Data/EventType/` 对应分域定义事件名和事件数据。
3. 明确触发者、监听者和生命周期。
4. 若事件需要返回检查结果，使用 `EventContext`。
5. 补充测试或至少在相关场景中验证日志。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/ECSTest/ECSTestScene.tscn --build`
- 涉及 Ability / Damage / UI 时运行对应模块场景。

## 人工审查重点

- 事件归属是否正确。
- 全局订阅是否有明确取消点。
- 事件数据是否零 GC、简单、稳定。
- 是否把事件总线当状态容器或流程控制器。
- 是否导致循环触发或隐式跨系统依赖。
