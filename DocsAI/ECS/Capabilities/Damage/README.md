# Damage Capability

> 状态：current
> 定位：伤害 owner 文档入口。
> 更新：2026-06-08

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [System/Usage.md](System/Usage.md) | DamageService、DamageInfo 和 10 阶段处理管线 |
| [System/Concept.md](System/Concept.md) | Damage pipeline 概念和职责边界 |

## 源码

```text
Src/ECS/Capabilities/Damage/
├── System/
└── Tests/
```

`HealthComponent`、`LifecycleComponent` 等 Unit 组件暂归 `Unit` 能力切片处理；Damage 本轮只迁伤害服务、处理器、统计系统和测试。

## Log

Damage owner 使用 `owner=Damage`。`DamageService.ProcessInternal` 对伤害管线写 `OperationTrace`：

| operation | phase | outcome | 关键字段 |
| --- | --- | --- | --- |
| `DamageProcess` | `Combat` | `Completed` | `processed`、`appliedDamage`、`actualDamage`、`finalDamage`、`isDodged`、`processorCount` |

规则：

- `DamageProcessResult.Processed` 只表示管线处理完成；是否造成有效命中看 `appliedDamage`。
- processor 内部细节默认不逐步 trace；需要深查时先扩展结构化字段或 artifact，不把 `info.Logs` 串成长 stdout。
- Ability、Projectile、Collision 触发伤害时，调用方保留自己的 owner flow；Damage 只记录管线事实。

查询示例：

```bash
Workspace/Tools/logctl/logctl query --analysis-dir <run>/analysis owner=Damage operation=DamageProcess appliedDamage=true
```
