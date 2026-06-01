# TestSystem Capability

> 状态：current
> 定位：运行时调试与测试工具 owner 文档入口。
> 更新：2026-06-01

## 入口

| 文档 | 说明 |
| --- | --- |
| [TestSystem.md](TestSystem.md) | TestSystem 设计目标、实现评估和目标架构 |
| [System/Usage.md](System/Usage.md) | TestSystem 源码目录、模块阅读顺序和使用方式 |
| [System/Concept.md](System/Concept.md) | TestSystem 简短概念入口 |
| [优化/TestSystem重构方案.md](优化/TestSystem重构方案.md) | 历史优化方案 |
| [优化/TestSystem新增对象池信息与单位Asset视觉预览.md](优化/TestSystem新增对象池信息与单位Asset视觉预览.md) | 对象池信息与视觉预览扩展记录 |

## 源码

```text
Src/ECS/Capabilities/TestSystem/
├── System/
└── Tests/
```

鼠标点选/框选能力属于 `DocsAI/ECS/Tools/Input/MouseSelection/` 和 `Src/ECS/Tools/Input/MouseSelection/`；TestSystem 只消费其选择结果，不拥有输入基础设施。

