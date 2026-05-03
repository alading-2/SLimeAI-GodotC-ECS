# DamageSystem 模块契约

本文是 AI 修改伤害入口、伤害处理器、接触伤害和批量 / DoT 伤害时必须阅读的执行契约。碰撞分层见 `DocsAI/Modules/Collision.md`。

## 职责边界

DamageSystem 是伤害计算和生命值结算的唯一入口。

DamageSystem 负责：

- 通过管道处理 `DamageInfo`。
- 执行闪避、暴击、护盾、护甲、易伤、吸血、结算、统计。
- 用 `SystemManager.Execute` 接受外部伤害命令。
- 通过 `DamageTool` 统一批量伤害和 DoT。

DamageSystem 不负责：

- 技能目标选择。
- 直接生成技能投射物。
- 在管道外手写暴击、闪避或扣血。

## 核心入口

- `Src/ECS/Base/System/DamageSystem/DamageService.cs`
- `Src/ECS/Base/System/DamageSystem/DamageInfo.cs`
- `Src/ECS/Base/System/DamageSystem/IDamageProcessor.cs`
- `Src/ECS/Base/System/DamageSystem/DamageTool.cs`
- `Src/ECS/Base/System/DamageSystem/Processors/`
- `Src/ECS/Base/Component/Collision/ContactDamageComponent/ContactDamageComponent.cs`
- 碰撞分层：`DocsAI/Modules/Collision.md`

## 数据 / 事件 / 生命周期

- 外部造成伤害必须通过 `SystemManager.Instance.Execute<DamageService, DamageProcessRequest, DamageProcessResult>(...)`。
- `DamageType` 表示物理、魔法、真实等数值语义。
- `DamageTags` 表示 Attack、Ability、Area、Persistent 等来源和表现语义。
- `BaseDamageProcessor` 只在 `Attacker` 自身死亡且带 `Attack` 标签时阻断，不追拥有者父链。
- 处理器不需要检查 `IsEnd`，主循环会统一在每个处理器后检查。
- 多目标和 DoT 优先使用 `DamageTool.ApplyToList` / `DamageTool.ScheduleDoT`。
- 接触伤害只负责把 Hurtbox 接触转成 DamageSystem 请求；Hurtbox、layer/mask、运动碰撞语义见 `DocsAI/Modules/Collision.md`。

## 禁止事项

- 禁止直接 `victim.Data.Set(DataKey.CurrentHp, ...)` 扣血。
- 禁止手写暴击、闪避、护甲减伤。
- 禁止业务代码直接调用 `DamageService.Process` 绕过 SystemCore 门禁。
- 禁止手写 foreach 加重复伤害逻辑替代 `DamageTool`。
- 禁止手写 TimerManager DoT 调度替代 `DamageTool.ScheduleDoT`。

## 修改流程

1. 判断是新增处理器、调整处理器顺序、修改 DamageInfo、还是业务调用伤害。
2. 新处理器实现 `IDamageProcessor`，选择明确优先级和阶段。
3. 只读检查和写入中间状态要局限在当前处理器职责内。
4. 涉及 Ability 命中时优先走 `AbilityImpactTool`。
5. 补充 DamageSystem 测试或对应技能测试。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/DamageSystemTest/DamageSystemTest.tscn --build`
- 涉及接触伤害时运行碰撞 / MainTest 场景。

## 人工审查重点

- 是否绕过 `SystemManager.Execute`。
- 处理器优先级是否改变了既有伤害语义。
- `DamageType` 和 `DamageTags` 是否混用。
- DoT 是否会在施法者失效后继续执行。
- 是否误把父实体死亡作为所有伤害的统一阻断条件。
