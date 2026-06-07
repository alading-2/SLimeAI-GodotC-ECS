# Data 系统问题分析

> 更新：2026-05-28
> 状态：历史入口。完整设计已重构到 `design/Runtime/2.Data系统优化/`。
> 结论更新：旧结论“强化 C# DataKey 作为推荐入口”已升级为“以 `runtime_snapshot.json.descriptors` 作为 Data 字段定义事实源，并按完整重构删除旧 Data 输入路径”。

## 1. 新设计入口

请从以下目录阅读完整设计：

```text
SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/2.Data系统优化/
```

阅读顺序：

1. `README.md`
2. `01-代码实现说明.md`
3. `02-DataMeta属性审计与Feature计算边界.md`
4. `03-完全重构范围与TDD测试计划.md`

## 2. 为什么保留本文件

本文件仅保留为历史入口，避免已有 roadmap、progress 或对话中引用 `design/Foundation/01-Data系统问题分析.md` 时断链。

后续新增设计内容不再写入本文件，应写入 `design/Runtime/2.Data系统优化/`。

## 3. 裁决摘要

新的 Data 系统优化裁决如下：

```text
DataOS SQLite authoring
    -> runtime_snapshot.json
         ├── descriptors   字段定义事实源
         └── records       字段值事实源
    -> DataDefinitionCatalog
    -> Data.Get / Data.Set
```

不再推荐把以下链路作为长期事实源：

```text
Data/DataKey/*.cs
    -> DataRegistry.Register(new DataMeta { ... })
    -> RuntimeDataSnapshot 再与 JSON descriptor 做 drift check
```

也不再保留以下旧 Data 输入路径：

```text
SlimeAI/Data/Data/
SlimeAI/Data/DataNew/
Data.LoadFromConfig
```

## 4. 后续执行方向

后续第一个 Data 执行型 SDD 应以 TDD 开始：

- 先写新 Data 系统红灯测试。
- 建立 `runtime_snapshot.json.descriptors -> DataDefinitionCatalog` 最小闭环。
- 建立一次性旧 Data 审计工具，只用于生成迁移清单。
- 后续切片删除 `SlimeAI/Data/Data/`、`SlimeAI/Data/DataNew/`，并重建 `SlimeAI/Src/ECS/Test/SingleTest/ECS/Data/`。

不再以“长期兼容旧 `DataKey` / `DataMeta` / `DataNew`”作为目标。
