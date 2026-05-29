# AI 原生数据层协议

本文定义迁移后的 DataOS 目标。旧 C# 表和旧 `.tres` 都是迁移输入，不是最终形态。

## 核心判断

AI-first 数据层应优先让 AI 查询、批量修改、校验、diff、迁移和生成，而不是优先让 C# 初始化器好写。

目标形态：

```text
SQLite Authoring DB -> validate -> generate snapshot -> runtime load
```

## 数据允许存什么

允许：

- `string`
- `int`
- `float`
- `bool`
- enum 文本
- `res://` 资源路径
- 稳定 id
- 关系表记录
- 少量 AI 说明字段，例如 `ai_notes`

禁止作为主数据：

- Godot `Node`
- `PackedScene`
- 运行时对象引用
- C# delegate / callback
- 随意对象图
- 未约束的大 JSON 字符串

## 分层职责

- SQLite：authoring 真相源，适合 AI 查询、修改、约束和迁移。
- schema：字段、类型、默认值、约束、外键、枚举域。
- generator：把数据库生成 C# / JSON 快照、DataKey registry、enum catalog。
- runtime snapshot：游戏运行时读取的稳定输入。
- Entity.Data：运行时动态状态，不等同于数据库。

## 设计规则

- 每张表使用稳定 `id`，显示名用 `display_name`。
- enum 存文本，并和 C# enum / enum catalog 校验。
- 概率存 `0-100`。
- 数值“不限制”统一存 `-1`。
- 资源路径存字符串，验证阶段检查文件存在。
- 多对多和归属关系用关系表，不用逗号字符串。
- JSON 只能作为实验字段，稳定后迁为正式列。

## 验证门禁

DataOS 改动至少运行：

```bash
Tools/ai-game-os/validate-data.sh
Tools/ai-game-os/generate-data-snapshot.sh
dotnet build
```

涉及能力数据时追加：

```bash
Tools/ai-game-os/run-capability-test.sh <Capability>
```

## 迁移顺序

1. 先迁 SystemData / SystemPresetData。
2. 再迁 Unit / Ability / Feature。
3. 再迁 ResourceCatalog / EventType / DataKey registry。
4. 最后删除旧 `.tres` 和手写 RuntimeTables 当前入口。

## 人工审查重点

- schema 是否能表达真实约束。
- AI 是否能用 SQL 一次查清影响范围。
- 生成物是否稳定、可 diff、可构建。
- 运行时是否绕过快照直接查数据库。
