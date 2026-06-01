# InputMap Manifest

> status: current
> bindingSource: `project.godot` `[input]`
> facade: `Src/ECS/Tools/Input/InputManager.cs`
> lastReviewed: 2026-06-01

本文是 AI 改键和查输入语义的 manifest。`project.godot` 是物理绑定事实源；本文负责说明业务语义、上下文和调用点。

## 1. 改键速查

| Context | Business Action | Godot Action | 键盘默认 | 手柄默认 | Value | Consumer |
| --- | --- | --- | --- | --- | --- | --- |
| Gameplay | Move.Left | `MoveLeft` | A / Left | Left Stick Left / DPad Left | Vector2 | `PlayerInputStrategy` |
| Gameplay | Move.Right | `MoveRight` | D / Right | Left Stick Right / DPad Right | Vector2 | `PlayerInputStrategy` |
| Gameplay | Move.Up | `MoveUp` | W / Up | Left Stick Up / DPad Up | Vector2 | `PlayerInputStrategy` |
| Gameplay | Move.Down | `MoveDown` | S / Down | Left Stick Down / DPad Down | Vector2 | `PlayerInputStrategy` |
| Targeting | Aim | `StickRightLeft/Right/Up/Down` | - | Right Stick | Vector2 | `TargetingIndicatorControlComponent` |
| Gameplay | UseActiveAbility | `BtnX` | J | Face Button West / Xbox X | JustPressed | `ActiveSkillInputComponent` |
| Targeting | ConfirmTarget | `BtnX` | J | Face Button West / Xbox X | JustPressed | `TargetingIndicatorControlComponent` |
| Gameplay | PreviousActiveAbility | `BtnLB` | Q | Left Bumper | JustPressed | `ActiveSkillInputComponent` |
| Gameplay | NextActiveAbility | `BtnRB` | E | Right Bumper | JustPressed | `ActiveSkillInputComponent` |
| Targeting/UI | Cancel | `BtnB` | Esc | Face Button East / Xbox B | JustPressed | `TargetingIndicatorControlComponent` |
| UI/System | Confirm | `BtnA` | Space / Enter | Face Button South / Xbox A | JustPressed | UI / future consumers |
| UI/System | Pause | `BtnStart` | Esc | Start | JustPressed | `PauseMenuSystem` |
| UI/System | Select | `BtnSelect` | - | Back/Select | JustPressed | future consumers |
| System | Home | `BtnHome` | - | Guide/Home | JustPressed | future consumers |
| Gameplay/Targeting | LeftTrigger | `BtnLT` | - | Left Trigger | Strength/Pressed | future consumers |
| Gameplay/Targeting | RightTrigger | `BtnRT` | - | Right Trigger | Strength/Pressed | future consumers |
| UI | Navigate.Left | `UiLeft` | - | DPad Left | JustPressed/Repeat | UI consumers |
| UI | Navigate.Right | `UiRight` | - | DPad Right | JustPressed/Repeat | UI consumers |
| UI | Navigate.Up | `UiUp` | - | DPad Up | JustPressed/Repeat | UI consumers |
| UI | Navigate.Down | `UiDown` | - | DPad Down | JustPressed/Repeat | UI consumers |

## 2. 当前 project.godot 绑定清单

| Godot Action | 当前 events |
| --- | --- |
| `MoveLeft` | A、Left Arrow、Left Stick Left、DPad Left |
| `MoveRight` | D、Right Arrow、Left Stick Right、DPad Right |
| `MoveUp` | W、Up Arrow、Left Stick Up、DPad Up |
| `MoveDown` | S、Down Arrow、Left Stick Down、DPad Down |
| `BtnA` | Space、Enter、Joypad Button 0 |
| `BtnB` | Esc、Joypad Button 1 |
| `BtnX` | J、Joypad Button 2 |
| `BtnY` | I、Joypad Button 3 |
| `BtnLB` | Q、Joypad Button 9 |
| `BtnRB` | E、Joypad Button 10 |
| `BtnLT` | Joypad Axis 4 +1 |
| `BtnRT` | Joypad Axis 5 +1 |
| `BtnStart` | Esc、Joypad Button 6 |
| `BtnHome` | Joypad Button 4 |
| `BtnSelect` | Joypad Button 4 |
| `StickRightLeft` | Right Stick X -1 |
| `StickRightRight` | Right Stick X +1 |
| `StickRightUp` | Right Stick Y -1 |
| `StickRightDown` | Right Stick Y +1 |
| `UiLeft` | Joypad Button 13 |
| `UiRight` | Joypad Button 14 |
| `UiUp` | Joypad Button 11 |
| `UiDown` | Joypad Button 12 |

`BtnHome` 与 `BtnSelect` 当前同为 Joypad Button 4。这是现状记录，不代表推荐长期设计；后续若要同时使用 Home/Select，必须先明确 Godot button index 和平台映射。

## 3. 手柄命名说明

当前 `BtnA/B/X/Y` 是兼容名，接近 Xbox 标注。AI 不应把它理解成唯一手柄类型。

推荐 mental model：

```text
Business Action: UseActiveAbility / ConfirmTarget / Cancel
Godot Action: BtnX / BtnB / BtnStart
Physical Layout: face button west/east/south/north, bumper, trigger, stick, dpad
Display Profile: Xbox / PlayStation / Nintendo Switch / Steam Input
```

后续 UI 图标应使用 display profile，而不是在业务组件里写死 `X`、`B`、`A` 字样。

## 4. 与参考游戏的差异

本轮参考了本地资源：

- Brotato：`input_service.gd` 会复制 action 到设备后缀，用 `player_index` 和 remapped device 支持本地多人；还维护 gamepad/keyboard/mouse 输入模式切换。
- Slay the Spire 2：`project.godot` 分离 `mega_*` 业务 action 和 `controller_*` 物理 action；代码里有 `ControllerMappingType.Default/Playstation/NintendoSwitch`、Steam Input map 和不同 controller glyph config。

SlimeAI 当前阶段只采纳“物理绑定、业务语义、显示 profile 分层”的原则，不复制多人 remap、Steam Input 深集成或完整重绑定 UI。

## 5. 变更规则

- 改默认键盘/手柄绑定：改 `project.godot`，同步本文件。
- 改业务语义：补 `InputManager` typed 方法，更新 consumer。
- 新增上下文：先写 manifest，再决定是否实现运行时 `InputContext`。
- 新增 UI 图标：单独设计 `ControllerGlyphProfile`，不要污染 `ActiveSkillInputComponent` 或 Movement。
- 不确定 Godot button index 时，不要猜；保留现状或先用 Godot 编辑器/官方常量验证。
