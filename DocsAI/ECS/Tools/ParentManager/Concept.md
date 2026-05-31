# ParentManager 概念

> 状态：current
> sourcePaths: `Src/ECS/Tools/ParentManager/`
> relatedDocs: [Usage.md](./Usage.md)
> lastReviewed: 2026-05-31

## 一句话定位

`ParentManager` 管理运行时节点挂载层级，为对象池、生成物和持久挂载点提供统一父节点解析。

## 职责边界

| ParentManager 做 | ParentManager 不做 |
| ---- | ---- |
| 保持运行时节点层级整洁 | Entity 生命周期管理 |
| 按路径解析或创建父节点 | 对象池对象创建与回收 |
| 支持对象池 `ParentPath` 挂载 | 场景业务逻辑 |

## 使用入口

核心职责和对象池协作方式见 [Usage.md](./Usage.md)。
