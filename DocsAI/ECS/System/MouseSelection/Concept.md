# MouseSelection 概念

> status: current
> sourcePaths: Src/ECS/Base/System/MouseSelection/
> relatedDocs: DocsAI/ECS/System/MouseSelection/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

鼠标点击/框选目标系统，基于物理的候选过滤，支持单击选择和框选两种模式。

## 2. 核心概念

### 四个部分文件

- **System**：主系统逻辑
- **Interaction**：交互处理
- **Picking**：拾取逻辑（物理射线检测）
- **SelectionBoxUi**：框选 UI

### 工作流程

```
鼠标输入 → 物理射线检测（Picking）→ 候选过滤 → 选中/取消选中 → UI 更新
```

## 3. 职责边界

| MouseSelection 做 | MouseSelection 不做 |
| ---- | ---- |
| 鼠标点击/框选目标 | 具体业务响应 |
| 物理候选过滤 | 目标查询 API（归 TargetSelector） |
| 选中状态管理 | UI 绘制（归 SelectionBoxUi） |
