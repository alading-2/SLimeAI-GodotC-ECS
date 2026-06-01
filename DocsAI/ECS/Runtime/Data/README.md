# Runtime Data 文档入口

> 状态：current
> 更新：2026-06-01
> 范围：`Src/ECS/Runtime/Data/`、`Data/DataOS/`、`Data/DataKey/Generated/`

## 入口

| 文档 | 说明 |
| --- | --- |
| [Data系统说明.md](Data系统说明.md) | DataOS、runtime snapshot、typed DataKey 和运行时存储规则 |
| [Data使用说明.md](Data使用说明.md) | Data API 使用方式、字段访问和常见流程 |

## 源码

```text
Src/ECS/Runtime/Data/
Data/DataOS/
Data/DataKey/Generated/
```

新增 DataKey 先写 DataOS descriptor，再生成 typed handle；不要恢复旧 `DataMeta / DataRegistry / const string` 作为字段事实源。

