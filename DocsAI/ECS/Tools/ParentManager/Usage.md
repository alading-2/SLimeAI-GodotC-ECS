<!-- migrated-from: Src/ECS/Tools/ParentManager/README.md -->

> 迁移来源：`Src/ECS/Tools/ParentManager/README.md`
> 迁移说明：本文主体从原 `Src/ECS` 文档迁入 `DocsAI` 统一管理；原 `Src/ECS` Markdown 文件已删除。

# ParentManager 历史迁移说明

## 概述

旧路径 `Src/ECS/Tools/ParentManager/` 已退出 current 源码入口。当前入口是：

```text
DocsAI/ECS/Runtime/Mount/README.md
Src/ECS/Runtime/Mount/
```

## 核心职责

1. **保持场景整洁**：防止生成的数百个子弹、敌人直接堆积在根节点或某个特定节点下。
2. **场景切换保护**：确保对象池根节点挂载在 `/root` 下，避免随当前场景切换而被销毁（如果支持持久化对象）。
3. **层级查找**：提供统一的接口获取特定类型的父节点。

## 配合对象池使用

在 `ObjectPool` 中，如果配置了 `ParentPath`，`RuntimeMountService` 会负责解析路径并确保父节点存在，并通过 `RuntimeMountSnapshot` 暴露 pending / in-tree / invalid diagnostics。
