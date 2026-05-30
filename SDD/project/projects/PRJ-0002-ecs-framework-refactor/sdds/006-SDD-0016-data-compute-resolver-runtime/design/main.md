# Data Compute Resolver Runtime

## Goal

- **1**. 让 computed 字段由 descriptor dependencies 与 C# resolver 绑定。
- **2**. 让 Data.GetComputed 缓存到依赖变化前，并支持递归标脏。
- **3**. 禁止普通 Set computed 字段，resolver 缺失 fail fast。

## Context

- **1**. 依赖 SDD-0012 的 catalog/compute registry、SDD-0014 的 DataSlot、SDD-0015 的 modifier dirty 接口。
- **2**. computed 必须无副作用：不发事件、不写 Data、不创建实体、不访问场景树。

## Design

- **1**. IDataComputeResolver 暴露 ComputeId 与 Compute(Data, DataDefinition)。
- **2**. DataComputeRegistry 注册 resolver，重复 ComputeId fail fast。
- **3**. Catalog 建立依赖图和反向依赖索引，BuildCatalog 检测循环。
- **4**. Data.Get computed 读取依赖 effective value，使用 cache，依赖 Set 或 modifier change 后 transitive dirty。
- **5**. 副作用契约以接口文档、测试 double 或 guard 规则验证。

## Dependencies

- **Previous SDDs**: SDD-0012
- **Shared design**: `../../design/2.Data系统优化/README.md` and related Data rewrite documents.
- **Boundary**: This SDD must not reintroduce DataMeta/DataRegistry/DataKey static definitions as long-term field-definition sources.

## Verification

- **1**. Compute 小测试通过。
- **2**. AttributeBonus/Percent/AttackInterval 等基础 resolver 示例通过。
- **3**. `python3 Workspace/SDD/sdd.py validate SDD-0016` 通过。
