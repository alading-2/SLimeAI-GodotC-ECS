---
name: data-authoring
description: 编写或修改 Data 目录下的数据配置、Config、DataKey、EventType、Resource 映射规则时使用。适用于：新增配置字段、设计 DataKey 分域、决定字段放 Data/Data 还是 Data/Config、编写事件协议。触发关键词：Data目录、Config配置、DataKey定义、EventType、数据配置、Resource映射。
---

# DataAuthoring 入口

## 什么时候用

- 修改 `Data/Config/`、`Data/DataNew/`、`Data/DataKey/`、`Data/EventType/`、`Data/ResourceManagement/`。
- 新增可配置字段、DataKey、事件协议或资源路径映射。
- 判断字段应放 DataNew、DataKey、Config 还是 ResourceManagement。

## 转向其它 Skill

- 运行时 Data 容器实现 -> `@ecs-data`
- 事件发布订阅流程 -> `@ecs-event`
- 技能配置字段 -> `@ability-system`
- 系统注册 / 运行条件 -> `DocsAI/Modules/SystemCore.md`

## 必读

- `DocsAI/Modules/DataAuthoring.md`
- `Data/README.md`
- `Data/DataNew/README.md`
- `Data/DataKey/README.md`
- 涉及运行时容器读写再读 `DocsAI/Modules/Data.md`

## 最短流程

1. 判断字段属于 Entity.Data、系统配置、事件协议还是资源路径。
2. 运行时字段先定义 `DataMeta`，再在 DataNew 表添加配置属性。
3. 属性名不等于 DataKey 时加 `[DataKey(nameof(DataKey.Xxx))]`。
4. 事件协议放 `Data/EventType/` 对应分域。
5. 资源路径放 ResourceManagement 相关入口，不放运行时对象引用。
6. 运行构建和 Data / System 相关测试。
7. 更新 `DocsAI/Modules/DataAuthoring.md` 或 Data 目录 README。

## 禁止事项

- 不要把业务逻辑写进 `Data/`。
- 不要把旧 `.tres` 重新作为新增运行时主数据源。
- 不要新增普通业务 `const string` DataKey。
- 不要用裸字符串替代 `[DataKey(nameof(DataKey.Xxx))]`。
- 不要把 `PackedScene`、Node 或运行时对象引用写进 DataNew 主配置。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/Data/DataTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/Data/TestDataKeyMapping.tscn --build
```
