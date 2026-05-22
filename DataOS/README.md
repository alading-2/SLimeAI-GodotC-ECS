# SlimeAINew DataOS

`SQLite authoring DB` 是旧 ECS 数据系统的唯一事实源。运行时不读取 SQLite，也不读取 `.tres` 数据配置；运行时只读取生成出的 `DataOS/Snapshots/runtime_snapshot.json`。

## 命令

```bash
DataOS/Tools/build-authoring-db.sh
DataOS/Tools/validate-dataos.sh DataOS/Authoring/slimeainew.authoring.db
DataOS/Tools/generate-runtime-snapshot.sh DataOS/Authoring/slimeainew.authoring.db DataOS/Snapshots/runtime_snapshot.json
```

## 边界

- 可维护数据只写 `DataOS/Schema/core.sql` 和 `DataOS/Authoring/SlimeAINew.seed.sql`。
- `Data/DataNew` 只保留 DTO 和旧调用兼容 API，不再写静态 authoring 实例。
- 资源字段只保存 `res://` 字符串路径，使用点显式加载资源。
- `.tres` 数据表和旧编辑器插件不再是 authoring 入口。
