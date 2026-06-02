# InputMap Manifest

> status: current
> bindingSource: `project.godot` `[input]`
> facade: `Src/ECS/Tools/Input/InputManager.cs`
> mainEntry: `DocsAI/ECS/Tools/Input/README.md`
> lastReviewed: 2026-06-02

本文只是 Input 的 manifest 表，方便快速查“哪个按键对应哪个业务功能”。完整使用规则、扩展路线和验证门禁见 [`README.md`](./README.md)。

`InputMap.md` 不是 DocsAI owner 必须存在的固定文件；Input 当前 action 较多，所以保留一份表格。其他 owner 内容少时可以直接把表格放在 `README.md`。

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

## 3. 变更规则

- 改默认键盘/手柄绑定：改 `project.godot`，同步本文件。
- 改业务语义：同步 [`README.md`](./README.md)、补 `InputManager` typed 方法，更新 consumer。
- 新增上下文：先写 manifest，再决定是否实现运行时 `InputContext`。
- 新增 UI 图标：单独设计 `ControllerGlyphProfile`，不要污染 `ActiveSkillInputComponent` 或 Movement。
- 不确定 Godot button index 时，不要猜；保留现状或先用 Godot 编辑器/官方常量验证。
