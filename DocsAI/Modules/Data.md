# Data 模块契约

本文是 AI 修改运行时 Data 容器、typed DataKey、DataCatalog 和数据事件时必须阅读的执行契约。数据目录配置、DataNew 运行时外壳、DataKey 定义和 EventType 协议见 `DocsAI/Modules/DataAuthoring.md`。

## 职责边界

Data 是运行时业务状态的唯一来源。Component / System 可以读写 Data，但不应把共享业务状态藏在私有字段里。

Data 负责：

- 存储 Entity 运行时状态。
- 通过 `DataKey<T>` 暴露业务读写入口。
- 绑定 frozen `DataCatalog`，以 typed slot 作为主存储。
- 承载修改器和计算属性。
- 在值变化时通过 `Entity.Events` 发布 `PropertyChanged`。
- 从 typed runtime snapshot 或兼容 DTO 注入初始值。

Data 不负责：

- 直接表达跨系统流程。
- 替代 EventBus 做命令分发。
- 保存绑定旧节点生命周期的临时引用，除非明确是局部运行态例外。

## 核心入口

- `Src/ECS/Base/Data/Data.cs`
- `Src/ECS/Base/Data/DataKeyTyped.cs`
- `Src/ECS/Base/Data/DataCatalog.cs`
- `Src/ECS/Base/Data/DataMeta.cs`（兼容 metadata）
- `Src/ECS/Base/Data/DataRegistry.cs`（兼容注册表）
- 数据目录契约：`DocsAI/Modules/DataAuthoring.md`

## 数据 / 事件 / 生命周期

- 新 DataKey 以 `DataKey<T>` 为主入口；`DataMeta` 只保留兼容 metadata 和编辑器展示。
- `DataKey.Key` 用 `nameof(DataKey.Xxx)`。
- 数值型“不限制”统一使用 `-1`。
- 概率统一使用 `0-100`，计算时再 `/100`。
- 对象池回收时 Data 清理由 EntityManager 统一处理。
- 运行时配置优先来自 `DataOS` 生成的 typed snapshot。
- 场景引用在 Data 中保存 `res://` 字符串路径，最终实例化点再加载。
- Data 变化监听必须通过 `Entity.Events`，不要使用 `Data.On`。

## 禁止事项

- 禁止 `_data.Get<float>("CurrentHp")` 这类字符串字面量。
- 禁止新增普通业务 DataKey 时使用 `const string`。
- 禁止 Component 私有字段保存 HP、速度、状态机等共享业务状态。
- 禁止把旧 `.tres` 配置重新作为运行时主数据源。
- 禁止对象池回收时业务组件手动 Clear 整个 Data。

## 修改流程

1. 判断是运行时容器行为、DataMeta 规则、配置字段、事件类型还是资源映射。
2. 容器行为改 `Src/ECS/Base/Data/` 下的源码，数据目录改动先读 `DocsAI/Modules/DataAuthoring.md`。
3. 修改 DataKey / DataCatalog / 兼容 metadata 时检查默认值、类型转换、计算依赖和修改器语义。
4. 若新增字段会被 UI / Component 监听，补充事件响应或测试。
5. 更新 `DocsAI/Modules/Data.md`、`DocsAI/Modules/DataAuthoring.md` 或相关 Skill。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/Data/DataTestScene.tscn --build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/Data/TestDataKeyMapping.tscn --build`

## 人工审查重点

- DataKey 类型、默认值、分类是否正确。
- 是否误用 `const string` 或字符串字面量。
- 是否把配置字段、运行时状态和事件协议混在一起。
- 是否误把 snapshot-backed DTO 再写成 authoring 源。
- 是否引入对象池复用后的脏数据风险。
