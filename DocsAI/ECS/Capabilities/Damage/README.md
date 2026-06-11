# Damage Capability

> 状态：current
> 定位：伤害 owner 文档入口。
> 更新：2026-06-10

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
| `DamageProcess` | `Combat` | `Completed` | `damageId`、`attackerId`、`victimId`、`entityId`、`damageType`、`damageTags`、`baseDamage`、`processed`、`appliedDamage`、`actualDamage`、`finalDamage`、`isCritical`、`isDodged`、`isBlocked`、`isSimulation`、`isEnd`、`processorCount`、`processorDigest`、`reasonCode`、`durationMs`、`sourceFile`、`sourceLine` |

规则：

- `DamageProcessResult.Processed` 只表示管线处理完成；是否造成有效命中看 `appliedDamage`。
- processor 内部细节默认不逐步 trace；需要深查时先扩展结构化字段或 artifact，不把 `info.Logs` 串成长 stdout。
- `processorDigest` 只作为一行摘要字段，用于让 `logctl analyze` 判断 processor chain 是否完整；不要恢复逐 processor 文本刷屏。
- invalid `DamageInfo` 或缺 victim 走 `outcome=Skipped`，必须写 `reasonCode`。
- Ability、Projectile、Collision 触发伤害时，调用方保留自己的 owner flow；Damage 只记录管线事实。
- 没有 Validation artifact 的 run 不能因为 `DamageProcess` 全是 completed 就报告 `passed`；只能由 `ValidationSession` / artifact 明确通过。

查询示例：

```bash
Workspace/Tools/logctl/logctl query --analysis-dir <run>/analysis owner=Damage operation=DamageProcess appliedDamage=true
```
