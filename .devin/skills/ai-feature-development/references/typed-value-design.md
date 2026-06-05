# Typed Value Design

> Baseline 由当前 SDD design、owner capability contract 与 `DocsAI/ECS/` 文档管理。新增 weakly-typed Runtime API 前必须先更新对应设计或 contract。

读取时机：新增 Runtime / Capability 数据结构、public payload、DataKey、命令、事件、identifier、配置 record 时读取。

## 默认选择

- identifier：用 typed wrapper，例如 `readonly record struct EntityId(string Value)`；不要传裸 `string`。
- 有限状态：用 `enum`；不要用魔法字符串。
- Capability runtime 数据：用 `DataKey<T>` + frozen `DataCatalog`；不要新增 string/object 数据入口。
- 命令和观察：用 typed payload record / report record；不要用 `Dictionary<string, object>`、`object Payload`、`PayloadKey`。
- 可选引用：用 `EntityId?`、`EntityId.Empty` 或 typed `EntityIdList`，按 owner capability 语义选择。

## 必须拒绝

- `Dictionary<string, object>` 作为 Runtime public API。
- `object` payload 加 type switch / reflection 恢复语义。
- `string` id、`string` event name、`string` command kind 作为新协议入口。
- 让 DataOS authoring DB 在 Runtime 热路径中回读。
- 为兼容旧项目保留弱类型别名。

## 已落地案例

- `SlimeAI/GameOS/Runtime/Entity/EntityId.cs`：typed `EntityId`，替代旧 string entity id。
- `SlimeAI/GameOS/Runtime/Entity/LifecycleLink.cs`：typed lifecycle link，替代旧 `RelationshipRecord + Dictionary<string, object>`。
- `SlimeAI/GameOS/Runtime/CommandBuffer/DeferredRuntimeCommand.cs`：typed deferred command，包含 8 种 nullable typed payload fields。
- `SlimeAI/GameOS/Runtime/CommandBuffer/CommandPlaybackReport.cs`：typed observation report，替代自由格式日志。

## 设计检查

写新类型前先回答：

1. 这个值的 owner capability 是谁？
2. 是否需要跨 DataOS authoring / generated snapshot / runtime loader？
3. 是否需要被 scene artifact 或 observation dump 检查？
4. 是否能用 record struct / enum / typed record 表达全部状态？
5. 是否有旧 string/object 入口需要删除或标记 deprecated？

如果第 4 点答案是否，先补设计；不要用 `object` 逃避边界。
