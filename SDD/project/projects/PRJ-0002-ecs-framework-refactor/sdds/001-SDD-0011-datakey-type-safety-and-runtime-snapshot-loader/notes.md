# Notes

## References

- 设计文档：`../../design/Runtime/2.Data系统优化/README.md`（§4 采纳方向、§6 需要做什么）
- 运行时核心：`SlimeAI/Src/ECS/Base/Data/`（DataMeta / DataRegistry / Data / DataModifier）
- DataKey 定义：`SlimeAI/Data/DataKey/*.cs`（已使用 `DataKey<T>` 格式，底层类型待补齐）
- DataOS 数据库：`SlimeAI/Data/Data/`（Authoring DB / Schema / Snapshots / Tools）
- 旧 loader 参考（待删除）：`SlimeAI/Data/RuntimeSnapshot/RuntimeDataSnapshot.cs`
  - 可参考：`ApplyRecordToData()` 的 field 遍历、类型转换、descriptor 校验逻辑
- runtime_snapshot.json 格式摘要：
  - `schemaVersion`：int
  - `manifest`：profile / enabledCapabilities / descriptorCount / recordCount
  - `descriptors[]`：key / type / defaultValue / supportsModifiers / isComputed 等
  - `records[]`：table / id / fields[]（key / value / type）
  - `resources[]`：资源路径映射

## Open Questions

- `SnapshotLoader` 是否需要支持增量加载（追加 record 而不是全量覆盖）？当前任务按全量覆盖实现即可。
- `DataApplyReport` 是否需要暴露给调用层？当前任务只需 warning log，可以后续再抽接口。
