# Notes

## References

- 项目级设计：`../../design/Runtime/2.Data系统优化/4.Data验证与Registry简化/01-DataComputeRegistry单例与Catalog验证收敛.md`
- Data current 文档：`DocsAI/ECS/Runtime/Data/Data系统说明.md`
- 主要源码：`Src/ECS/Runtime/Data/DataComputeRegistry.cs`
- 主要源码：`Src/ECS/Runtime/Data/DataDefinitionCatalog.cs`
- 主要源码：`Src/ECS/Runtime/Data/RuntimeSnapshot/RuntimeDataSnapshotLoader.cs`
- 主要源码：`Src/ECS/Runtime/Data/RuntimeSnapshot/DataRuntimeBootstrap.cs`

## Open Questions

- 默认假设：`DataComputeRegistry.Default` 必须 frozen；custom registry 可构造期注册，传给 bootstrap 后建议 frozen。
- 默认假设：先允许以现有 catalog 类返回 report 的小步实现；若实现复杂度继续上升，再拆出 `DataDefinitionCatalogBuilder`。
- 默认假设：循环引用校验保留，但输出为 `catalog.computed_cycle`，不再散落为直接异常消息。
- 后续实现注意：当前工作区已有非本轮 `Src/ECS/Runtime/Data/DataRuntimeStorage.cs` 修改，不能覆盖或回滚。
