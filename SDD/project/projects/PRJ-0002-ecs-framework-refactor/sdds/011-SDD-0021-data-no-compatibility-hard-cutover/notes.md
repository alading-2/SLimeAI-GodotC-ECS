# Notes

## References

- `design/README.md`：06 无兼容完全重构总审计副本。
- `../../design/2.Data系统优化/05-Data重构运行报错根因分析.md`：`AbilityIcon` / `AvailableAnimations` 运行报错根因。
- `../../design/2.Data系统优化/06-无兼容完全重构总审计/README.md`：项目级最新 Data 无兼容审计事实源。
- `Workspace/DocsAI/Temp/2-SDD0020后Data类型回归分析.md`：SDD-0020 后 Data 类型回归临时分析。

## Open Questions

- `object_ref` 的最终 CLR 契约需要在 T1.3 中明确：`AbilityIcon` 可走资源引用，`TargetNode` 可能应迁出 Data slot 或使用 runtime-only `NodeRef`。
- `modifier_list` 的最终 CLR 契约需要在 T1.3 中明确：如果仅为 authoring blob，则业务不能继续 `Data.Get<object>`；如果 runtime 要读，需定义 typed DTO。
