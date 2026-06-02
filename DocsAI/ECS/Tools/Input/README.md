# Input

> status: current
> sourcePaths: `Src/ECS/Tools/Input/`
> bindingSource: `project.godot` `[input]`
> currentShape: Godot InputMap + `InputManager` semantic facade
> lastReviewed: 2026-06-02

## 1. 当前结论

Input 现在不是完整的输入框架，也不是重绑定系统。当前真实形态是：

```text
project.godot [input]
  -> Godot action 与键盘/手柄/摇杆绑定

Src/ECS/Tools/Input/InputManager.cs
  -> 对 Godot.Input 的静态 facade
  -> 少量业务语义方法，避免 Ability / Targeting / UI 直接猜 BtnX / BtnB / BtnStart

业务 consumer
  -> Movement / Ability / Targeting / PauseMenu 等 owner 调用 InputManager
```

上一轮 SDD-0026 只是把旧按钮名 API 上补了业务语义 facade，并迁移了关键调用点；它没有实现运行时 `InputContext`、可重绑定 UI、本地多人设备隔离、输入录制或 controller glyph 系统。

## 2. 文件怎么读

本目录不强制拆成 `Concept.md / Usage.md / InputMap.md`。这三个文件只是当前 Input owner 的辅助分层：

| 文件 | 当前用途 |
| --- | --- |
| `README.md` | 主入口。说明当前事实、按键功能、使用规则和扩展路线。 |
| `InputMap.md` | manifest 表。只查 action、默认绑定、业务语义、consumer 时读它。 |
| `Usage.md` | 调用示例和改键流程。内容较少时可合并回 README。 |
| `Concept.md` | 背景和职责边界。内容较少时可合并回 README。 |
| `MouseSelection/` | 鼠标选择系统，和键盘/手柄 action 契约分开维护。 |

DocsAI owner 可以按内容大小灵活组织：短 owner 用一个 `README.md` 就够；复杂 owner 可以拆 `Usage.md`、`Concept.md`、`Tests.md`、manifest 或原文件名。不要为了模板强行拆文件。

## 3. 当前按键功能表

`project.godot` 是物理绑定事实源；下表是 AI 可读业务语义。

| 业务功能 | Context | Godot action | 默认键盘 | 默认手柄 | 入口方法 | Consumer |
| --- | --- | --- | --- | --- | --- | --- |
| 玩家移动左 | Gameplay | `MoveLeft` | A / Left | Left Stick Left / DPad Left | `GetMoveInput()` | `PlayerInputStrategy` |
| 玩家移动右 | Gameplay | `MoveRight` | D / Right | Left Stick Right / DPad Right | `GetMoveInput()` | `PlayerInputStrategy` |
| 玩家移动上 | Gameplay | `MoveUp` | W / Up | Left Stick Up / DPad Up | `GetMoveInput()` | `PlayerInputStrategy` |
| 玩家移动下 | Gameplay | `MoveDown` | S / Down | Left Stick Down / DPad Down | `GetMoveInput()` | `PlayerInputStrategy` |
| 点选瞄准移动 | Targeting | `StickRightLeft/Right/Up/Down` | - | Right Stick | `GetAimInput()` | `TargetingIndicatorControlComponent` |
| 释放当前主动技能 | Gameplay | `BtnX` | J | Face Button West / Xbox X | `IsUseActiveAbilityPressed()` | `ActiveSkillInputComponent` |
| 确认点选目标 | Targeting | `BtnX` | J | Face Button West / Xbox X | `IsTargetConfirmPressed()` | `TargetingIndicatorControlComponent` |
| 上一个主动技能 | Gameplay | `BtnLB` | Q | Left Bumper | `IsPreviousActiveAbilityPressed()` | `ActiveSkillInputComponent` |
| 下一个主动技能 | Gameplay | `BtnRB` | E | Right Bumper | `IsNextActiveAbilityPressed()` | `ActiveSkillInputComponent` |
| 取消点选 | Targeting | `BtnB` | Esc | Face Button East / Xbox B | `IsTargetCancelPressed()` | `TargetingIndicatorControlComponent` |
| UI 确认 | UI/System | `BtnA` | Space / Enter | Face Button South / Xbox A | `IsConfirm()` | UI / future consumers |
| 暂停/继续 | UI/System | `BtnStart` | Esc | Start | `IsPausePressed()` | `PauseMenuSystem` |
| 选择 | UI/System | `BtnSelect` | - | Back/Select | `IsSelect()` | future consumers |
| Home | System | `BtnHome` | - | Guide/Home | `IsHome()` | future consumers |
| 左扳机 | Gameplay/Targeting | `BtnLT` | - | Left Trigger | `IsLeftTrigger()` / `GetLeftTriggerStrength()` | future consumers |
| 右扳机 | Gameplay/Targeting | `BtnRT` | - | Right Trigger | `IsRightTrigger()` / `GetRightTriggerStrength()` | future consumers |

注意：

- `BtnX` 当前同时承担 `UseActiveAbility` 和 `ConfirmTarget`，靠调用上下文区分。
- `BtnB` 和 `BtnStart` 当前都绑定了 Esc，因此暂停和取消点选会共享键盘 Esc。改这里必须先确认目标模式和 UI 行为。
- `BtnHome` 与 `BtnSelect` 当前同为 Joypad Button 4，这是现状记录，不是推荐长期设计。
- `BtnA/B/X/Y` 是当前兼容 action 名，接近 Xbox 命名；业务层不要把它当成唯一手柄类型。

## 4. 业务代码怎么用

业务 owner 只调用语义方法，不直接调用 `Godot.Input.IsAction*("...")`。

```csharp
// 主动技能
if (InputManager.IsPreviousActiveAbilityPressed()) CycleActiveAbility(-1);
if (InputManager.IsNextActiveAbilityPressed()) CycleActiveAbility(1);
if (InputManager.IsUseActiveAbilityPressed()) TryUseCurrentActiveAbility();

// 点选瞄准
var aimInput = InputManager.GetAimInput();
if (InputManager.IsTargetConfirmPressed()) ConfirmTarget();
if (InputManager.IsTargetCancelPressed()) CancelTargeting();

// 暂停
if (InputManager.IsPausePressed()) TogglePauseMenu();
```

Debug/Test 场景可以直调 `Godot.Input` 或 `InputManager.IsActionJustPressed(string)`，但这只是测试便利，不是框架业务契约。

## 5. 改键怎么做

### 只改默认键位

1. 改 `project.godot` 的 `[input]` action events。
2. 同步 `README.md` 和 `InputMap.md` 中对应 action 的默认绑定。
3. 不改业务组件。
4. 运行：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
rg -n "BtnX|BtnY|BtnLB|BtnRB|MoveLeft|StickRight" project.godot DocsAI/ECS/Tools/Input
```

### 改业务语义

例如把“释放主动技能”从 `BtnX` 独立成 `UseActiveAbility`：

1. 在 `project.godot` 新增或改名 action。
2. 在 `InputManager.cs` 增加或修改语义方法，方法名表达业务含义。
3. 更新 `README.md` / `InputMap.md` 的 action 表。
4. 更新 consumer 调用点。
5. 跑构建和 grep gate。

### 新增输入

1. 先在文档表里写清：业务功能、context、Godot action、默认键盘/手柄、value type、consumer。
2. 再改 `project.godot`。
3. 再补 `InputManager` 语义方法。
4. 最后接入具体 owner。

不要先在组件里写自由字符串输入，再回头补文档。

## 6. 后续真正扩展怎么改

当前 facade 足够支撑小规模输入。如果要继续做成 AI-first Input Contract，按下面顺序推进，不要一次性重写：

1. **业务 action 命名收口**：把 `BtnX/BtnB/BtnStart` 这类物理兼容名逐步迁到 `UseActiveAbility/TargetConfirm/Pause` 等业务 action，或保留物理名但生成 typed action id。
2. **manifest 自动校验**：写脚本检查 `project.godot` action 是否都在文档 manifest 中，文档 action 是否真实存在。
3. **InputContext**：引入轻量运行时上下文，明确 `Gameplay / Targeting / UI / Debug / Test` 的输入屏蔽规则。
4. **ControllerGlyphProfile**：UI 显示层按 Xbox / PlayStation / Nintendo Switch / Steam / Generic SDL 显示正确按钮图标，业务组件只传业务 action。
5. **重绑定 UI / 本地多人 / 输入录制**：只有真实需求出现时再单独设计，不作为当前 Input 默认能力。

## 7. 验证门禁

常规文档或 facade 改动：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate --all
```

Input 专项 grep：

```bash
rg -n "Input\\.IsAction|Input\\.GetAction|Input\\.GetVector|InputManager\\.IsAction" Src/ECS
rg -n "InputManager\\.Is(LeftBumper|RightBumper|X|Cancel|Pause)\\(" Src/ECS/Capabilities Src/ECS/UI Src/ECS/Runtime
rg -n "BtnX|BtnY|BtnLB|BtnRB|MoveLeft|StickRight" project.godot DocsAI/ECS/Tools/Input
```

判定：

- `Src/ECS/Tools/Input/InputManager.cs` 命中 `Godot.Input` 允许。
- `Src/ECS/**/Tests/**` 和 `Src/ECS/Test/**` 的直调输入允许，但要视为 Debug/Test 例外。
- `Capabilities / Runtime / UI` 业务层不应新增裸 action 字符串。
- 旧按钮名 API 在业务层不应回流；优先使用语义方法。
