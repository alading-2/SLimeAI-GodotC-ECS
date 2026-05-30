# Data Runtime Slot and Policy Model

## Goal

- **1**. 让 Data.Get/Set 从 DataDefinitionCatalog 读取定义。
- **2**. 让写入、范围、类型和选项约束由 runtime policy 决定。
- **3**. 禁止未知 key 隐式创建和业务新增裸字符串访问。

## Context

- **1**. 依赖 SDD-0012 的 DataDefinitionCatalog。
- **2**. 不实现完整 modifier pipeline 和 computed resolver；这些由 SDD-0015/0016 接续。
- **3**. 旧 Data.LoadFromConfig 不再作为输入通道。

## Design

- **1**. Data 构造时绑定 owner 与 catalog。
- **2**. DataSlot 管理单字段 base value、可选 runtime state 和后续 modifier/computed cache 扩展点。
- **3**. DataValueConverter 负责 strict convert、type compatibility、allowed_values 和 range policy。
- **4**. DataKey<T> 只做 generated thin handle；string API 只保留给 loader、测试 fixture 和审计工具。
- **5**. Data 通过 Entity.Events 发布数据变更事件，但 Data.Get computed 本身保持无副作用。

## Dependencies

- **Previous SDDs**: SDD-0012
- **Shared design**: `../../design/2.Data系统优化/README.md` and related Data rewrite documents.
- **Boundary**: This SDD must not reintroduce DataMeta/DataRegistry/DataKey static definitions as long-term field-definition sources.

## Verification

- **1**. Data core 小测试全部通过。
- **2**. 新增 grep gate 或测试说明业务代码不新增裸字符串 Data.Get/Set。
- **3**. `python3 Workspace/SDD/sdd.py validate SDD-0014` 通过。
