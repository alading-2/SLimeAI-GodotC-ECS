# Runtime Data 文档入口

> 状态：current
> 更新：2026-06-01
> 范围：`Src/ECS/Runtime/Data/`、`Data/DataOS/`、`Data/DataKey/Generated/`

## 入口

| 文档 | 说明 |
| --- | --- |
| [Data系统说明.md](Data系统说明.md) | DataOS、runtime snapshot、typed DataKey 和运行时存储规则 |
| [Data使用说明.md](Data使用说明.md) | Data API 使用方式、字段访问和常见流程 |
| [Concepts/Data系统架构设计.md](Concepts/Data系统架构设计.md) | 为什么 descriptor-first、核心概念、数据分层、设计权衡 |

## 源码

```text
Src/ECS/Runtime/Data/
Data/DataOS/
Data/DataKey/Generated/
```

新增 DataKey 先写 DataOS descriptor，再生成 typed handle；不要恢复旧 `DataMeta / DataRegistry / const string` 作为字段事实源。

## Log

Data owner 使用 `owner=Data`。当前 hard cutover 覆盖 DataOS 场景测试的 `ValidationSession`；Data runtime 热路径不默认为每次 `Get/Set` 写日志。

规则：

- snapshot loader、descriptor 校验、record apply 和 DataOS validation 输出结构化 artifact；不要用 `GD.Print("PASS")` / `GD.PushError("FAIL")` 表达结论。
- Data 写入失败需要给稳定 `reasonCode`、`stableKey`、`expectedType`、`actualType`、`writeSource`，而不是只写自然语言。
- 高频业务读写不打默认日志；排查时优先用 diagnostic snapshot 或测试 artifact。

查询示例：

```bash
Workspace/Tools/logctl/logctl query --analysis-dir <run>/analysis owner=Data
Workspace/Tools/logctl/logctl query --analysis-dir <run>/analysis owner=Data validationStatus=Fail
```
