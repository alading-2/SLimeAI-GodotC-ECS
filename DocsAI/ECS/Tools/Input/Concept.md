# Input 概念

> status: current
> sourcePaths: Src/ECS/Tools/Input/
> relatedDocs: DocsAI/ECS/Tools/Input/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

输入管理系统，统一键盘/手柄输入映射，支持 PascalCase 命名约定和玩家编号隔离。

## 2. 核心概念

### 输入映射

所有输入 action 定义在 `project.godot` 中，PascalCase 命名：
- MoveUp, MoveDown, MoveLeft, MoveRight
- Attack, Ability1, Ability2, ...
- Interact, Pause, ...

### 玩家编号隔离

支持多玩家本地合作，每个玩家有独立的输入设备绑定。

### 自动生成

`InputManager.md` 由 InputDocGenerator 自动生成，不手动编辑。

## 3. 职责边界

| Input 做 | Input 不做 |
| ---- | ---- |
| 输入映射管理 | 具体业务响应 |
| 多玩家输入隔离 | UI 输入处理 |
