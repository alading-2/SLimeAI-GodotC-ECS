---
name: ecs-event
description: 使用 EventBus 进行组件间通信、发布/订阅事件、定义新事件类型时使用。适用于：跨组件通信，替代 Godot Signal，使用 Entity.Events 局部事件或 GlobalEventBus 全局事件。触发关键词：EventBus、发布事件、订阅事件、GlobalEventBus、Entity.Events、定义事件类型、GameEventType。
---

# ECS Event 入口

## 什么时候用

- 使用 `Entity.Events` 或 `GlobalEventBus`。
- 定义 `GameEventType` 和事件数据。
- 修改 EventBus、EventContext 或事件订阅生命周期。
- 用事件替代组件间直接调用。

## 转向其它 Skill

- Data 字段变化监听 -> `@ecs-data`
- Component 生命周期 -> `@ecs-component`
- UI 绑定事件 -> `@ui-bind`
- 数据目录事件协议 -> `@data-authoring`

## 必读

- `DocsAI/Modules/Event.md`
- 修改 EventBus / GlobalEventBus 核心时读 `DocsAI/Workflows/ECS核心修改门禁.md`
- `Src/ECS/Base/Event/README_EventBus.md`
- 测试选择读 `DocsAI/Tests/测试矩阵.md`

## 最短流程

1. 判断事件归属：同一 Entity 内用 `Entity.Events`，跨实体 / 系统用 `GlobalEventBus.Global`。
2. 在 `Data/EventType/` 对应域定义事件名和载荷。
3. 事件数据优先 `readonly record struct`。
4. 全局订阅必须有取消点。
5. 需要返回检查结果时用 `EventContext`。
6. 运行构建和相关模块场景。

## 禁止事项

- 不要用 Godot Signal 承载核心业务逻辑。
- 不要组件间直接互调传递业务事件。
- 不要全局订阅后不取消。
- 不要把 EventBus 当状态容器。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/ECSTest/ECSTestScene.tscn --build
```
