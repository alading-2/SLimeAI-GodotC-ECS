# ParentManager 历史说明

> 状态：historical
> current: `DocsAI/ECS/Runtime/Mount/README.md`
> relatedDocs: [Usage.md](./Usage.md)
> lastReviewed: 2026-06-07

## 一句话定位

旧 `ParentManager` 已被 `RuntimeMountService` / `RuntimeMountRegistry` hard cutover 取代。当前运行时挂载事实源是 `DocsAI/ECS/Runtime/Mount/README.md` 和 `Src/ECS/Runtime/Mount/`。

## 职责边界

| Runtime Mount 做 | Runtime Mount 不做 |
| ---- | ---- |
| 保持运行时节点层级整洁 | Entity 生命周期管理 |
| 按路径解析或创建父节点 | 对象池对象创建与回收 |
| 支持对象池 `ParentPath` 挂载 | 场景业务逻辑 |

## 使用入口

核心职责和对象池协作方式见 `DocsAI/ECS/Runtime/Mount/README.md`。
