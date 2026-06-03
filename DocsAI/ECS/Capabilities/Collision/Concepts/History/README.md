# Collision Concepts History

> 状态：historical reference
> 定位：保留旧碰撞、幽灵碰撞、对象池兼容和默认脱树分析原文。
> 更新：2026-06-03

本目录只作追溯。当前执行入口是上一层：

- `../README.md`
- `../Godot物理时序与对象池碰撞.md`
- `../Node2D父链约束.md`

这些历史文档中可能仍保留旧标题、旧状态行或旧默认策略描述。读取时必须按当前裁决校准：

- 默认策略不再是脱树 / 关碰撞。
- `Monitoring/Monitorable`、shape disabled、layer/mask 清零不再作为对象池默认退场。
- `SetDeferred` 是安全提交工具，不是完整物理退场证明。
- `Node2D` 父链问题和对象池旧 signal 问题需要分别归因。
