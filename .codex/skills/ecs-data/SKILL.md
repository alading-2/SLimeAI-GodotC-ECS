---
name: ecs-data
description: 在 Entity 或 Component 中读写 Data 数据容器、定义新 DataKey、监听数据变化事件时使用。适用于：存储运行时状态（HP/速度/状态机），跨组件共享数据，从 DataNew/Resource 批量加载初始数据。触发关键词：Data容器、DataMeta、DataKey、读写状态、PropertyChanged、数据驱动。
---

# ECS Data 入口

## 什么时候用

- 修改 `Src/ECS/Base/Data/` 运行时容器、`DataMeta`、`DataRegistry`。
- 在 Entity / Component / System 中读写 `Entity.Data`。
- 处理 `PropertyChanged`、计算属性、修改器或类型转换。
- 判断共享业务状态是否应放 Data。

## 转向其它 Skill

- 修改 `Data/` 目录配置、DataKey、EventType -> `@data-authoring`
- Component 生命周期和事件订阅 -> `@ecs-component`
- EventBus 协议 -> `@ecs-event`
- UI 绑定 Data 变化 -> `@ui-bind`

## 必读

- `DocsAI/Modules/Data.md`
- 数据目录配置读 `DocsAI/Modules/DataAuthoring.md`
- `Src/ECS/Base/Data/README.md`
- 测试选择读 `DocsAI/Tests/测试矩阵.md`

## 最短流程

1. 判断是容器行为、数据读写、DataMeta 规则还是 Data 目录配置。
2. 共享业务状态存 `Entity.Data`，私有字段只放节点引用、缓存或局部公式运行态。
3. 读写用 `DataKey.Xxx` / `DataMeta`，不要用字符串字面量。
4. 数据变化监听通过 `Entity.Events`，不要使用 `Data.On`。
5. 修改容器语义后补 Data 测试或相关模块场景。
6. 更新 `DocsAI/Modules/Data.md` 或 DataAuthoring 契约。

## 禁止事项

- 不要在 Component 私有字段保存 HP、速度、状态机等共享业务状态。
- 不要新增普通业务 `const string` DataKey。
- 不要直接用字符串访问 Data。
- 不要对象池回收时业务组件手动 Clear 整个 Data。
- 不要把旧 `.tres` 作为运行时主数据源。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/Data/DataTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/Data/TestDataKeyMapping.tscn --build
```
