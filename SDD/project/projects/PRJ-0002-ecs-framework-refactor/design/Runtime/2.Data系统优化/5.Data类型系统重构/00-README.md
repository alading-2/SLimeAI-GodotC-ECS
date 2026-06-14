# Data 类型系统重构设计入口

> 状态：draft / deepthink 输出，尚未进入 runtime 实施。  
> 日期：2026-06-14  
> 原始问题：见同目录 [`0.Prompt.md`](./0.Prompt.md)。  
> 范围：`Src/ECS/Runtime/Data/`、`DocsAI/ECS/Runtime/Data/`、`Data/DataOS/`、`Data/DataKey/`、`Workspace/Else/参考/Data/`、`Resources/Engine/Docs`、`Resources/Engine/Engine`。  

## 一句话结论

用户判断成立：当前 Data 最大问题不是“字典一定错”，而是 **Data 把类型系统放在运行时转换器里承担**。`DataKey<T>` 已经给调用侧提供了类型，但 DataOS descriptor、snapshot DTO、`DataDefinition`、`DataValueConverter`、`DataRuntimeStorage` 仍反复做 `text/object/valueType -> CLR T` 的转换，导致 Data 这个核心协议比旧系统更冗余、更难确认。

推荐方向不是回退到旧 `DataMeta/DataRegistry`，也不是放弃统一 Data 容器，而是：

```text
DataOS descriptor 保留字段定义事实源
  -> generator/validator 在生成期完成主要类型检查
  -> generated DataKey<T> 成为唯一业务访问入口
  -> runtime catalog 只保留 Data 读写/计算/校验必须字段
  -> DataSlot<T> 保存 typed 默认值、typed 当前值、typed modifier/cache
  -> object/text 只留在 snapshot loader、debug、diagnostic 边界
```

## 文档顺序

1. [`01-需求归纳与真实问题.md`](./01-需求归纳与真实问题.md)  
   先把用户问题翻译成实际设计需求，判断哪些担心成立、哪些需要修正。
2. [`02-DataDefinition瘦身与分层方案.md`](./02-DataDefinition瘦身与分层方案.md)  
   明确 `DataDefinition` 哪些字段应留在 runtime，哪些应迁到 authoring / presentation / validation artifact。
3. [`03-类型系统与运行时存储重构方案.md`](./03-类型系统与运行时存储重构方案.md)  
   说明类型问题的根因、其他框架参考、`DataValueType` / `DataRuntimeStorage` / `DataSlot<T>` 的重构方向。
4. [`04-确认点与后续SDD建议.md`](./04-确认点与后续SDD建议.md)  
   列出必须确认的问题、默认假设和建议执行切片。

## 当前裁决草案

- `RuntimeTypeId` 不应作为所有字段的 runtime 必备字段；只在 `enum`、`object_ref`、`modifier_list` 这类需要 CLR/Godot 类型补充的字段上保留。
- `OwnerDomain` 不应进入 runtime catalog；字段归属可由 `StableKey` 命名、DataOS authoring、DocsAI 和生成报告承载。
- `OwnerCapability` / `OwnerSkill` 对 AI 路由和 DataOS 裁剪有价值，但不应默认进入 `DataDefinition` 热路径对象；建议留在 authoring descriptor / generator manifest。
- `category`、`isPercentage`、`supportsModifiers`、`isComputed`、`options` 是旧 `DataMeta` mirror 残留，应分别被 `uiGroup/unit+format/modifierPolicy/storagePolicy/allowedValues` 替代，不能继续作为第二事实源进入 runtime snapshot。
- `defaultValue` 可以在知识库/SQLite 中以 text 保存，但进入 runtime snapshot / catalog 时必须按 `valueType` 转成 typed default；`DataSlot<T>` 创建后不应再每次从 object/text 转换。
- `DataValueType` 当前作为 schema 枚举可以保留，但它不应继续成为运行时到处 switch 的转换中心；更合理的是生成期映射到 `DataRuntimeType` / typed descriptor / generated handle。
- `throw` 不应消失。坏 catalog、坏 snapshot、typed API 误用仍要 fail-fast；但 fatal 前应先产出 `DataCatalogBuildReport` / `DataApplyReport` / structured Log，便于 AI 通过日志定位问题。

## 非目标

- 不恢复旧 `DataMeta` / `DataRegistry` 作为事实源。
- 不把 Data 改成传统 ECS 的每个字段一个 C# 变量或组件存储。
- 不在本设计阶段直接修改 runtime 代码。
- 不追求全仓零 `object`；只要求业务热路径和 AI 可调用协议零 `object`。
