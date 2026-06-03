# Node2D 父链约束

> 状态：current
> 定位：说明普通 `Node` 插入 `Node2D` / `Area2D` 父链导致 transform 断链的问题。
> 更新：2026-06-03

## 1. 问题一句话

在 Godot 4 中，需要继承 2D 空间变换的节点（例如 `Node2D`、`Area2D`、`CollisionShape2D` 所在感应链路）不能把普通 `Node` 放在空间父链中间。普通 `Node` 没有 2D transform，会阻断子节点从实体根节点继承位置、旋转和缩放。

## 2. 典型错误结构

```text
CharacterBody2D (Enemy / Player)
└── Component (Node)
    └── ActionPreset (Node)
        └── HurtboxComponent (Area2D)
            └── CollisionShape2D
```

症状：

- 实体在世界中移动，但 Hurtbox / Pickup / Hitbox 仍停在错误全局坐标。
- 敌人靠近玩家却打不中。
- 玩家经过原点或某个固定坐标时触发不可见接触伤害。
- 调试时看业务数据正常，但可见碰撞形状不跟随实体。

## 3. 正确结构

```text
CharacterBody2D (Enemy / Player)
└── Component (Node2D)
    └── ActionPreset (Node2D)
        └── HurtboxComponent (Area2D)
            └── CollisionShape2D
```

规则：

- 只要子树里有物理、视觉或需要空间坐标的节点，中间容器必须是 `Node2D` 或更具体的空间节点。
- 纯逻辑容器可以是 `Node`，但不能插在空间节点需要继承 transform 的路径中。
- 场景结构问题不应由 ObjectPool 继续兜底；应通过 scene gate、README 五字段和可见碰撞形状验证发现。

## 4. 与对象池问题的区别

`Node2D` 父链断裂是场景结构 bug；对象池碰撞旧 signal 是生命周期 / 物理时序 bug。两者会产生相似症状，但修复边界不同：

| 问题 | 归属 | 修复 |
| --- | --- | --- |
| `Node` 阻断 transform | 场景结构 / Component prefab | 改父链为 `Node2D`，补场景门禁 |
| 回池对象旧 signal 进入业务 | ObjectPool + Collision + Damage / Movement | pool runtime state guard、ready frame、旧引用清理 |
| parking grid 产生事件压力 | ObjectPool validation | 分散 parking grid，artifact 记录事件和帧耗时 |
| ContactDamage timer 持有旧 attacker | Collision / Damage timer 入口 | tick 前验证 attacker pool state，无效则取消 timer |

## 5. 验证方式

最小验证应覆盖：

- 打开 Godot 可见碰撞形状或输出全局坐标，确认 `HurtboxComponent.GlobalPosition` 跟随实体根节点。
- scene README 记录 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath`。
- 负向场景允许人工或自动构造普通 `Node` 阻断链路，验证 gate 能报告问题。
- 修复后不要把“没有幽灵碰撞”归因给对象池；应明确是父链结构修复。

历史排查原文见 [History/碰撞问题需要注意.md](History/碰撞问题需要注意.md) 和 [History/幽灵碰撞问题深度分析.md](History/幽灵碰撞问题深度分析.md)。
