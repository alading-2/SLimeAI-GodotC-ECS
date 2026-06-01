# Input 使用说明

> status: current
> bindingSource: `project.godot` `[input]`
> facade: `Src/ECS/Tools/Input/InputManager.cs`
> relatedDocs: `DocsAI/ECS/Tools/Input/Concept.md`, `DocsAI/ECS/Tools/Input/InputMap.md`
> lastReviewed: 2026-06-01

## 1. 改键入口

AI 改 Input 时按这个顺序做：

1. 查 `DocsAI/ECS/Tools/Input/InputMap.md`，确认业务动作、Godot action、默认键位和消费者。
2. 改 `project.godot` 的 `[input]` action events。
3. 如果业务语义变了，同步 `InputManager.cs` 的 typed facade 或新增方法。
4. 同步 `InputMap.md` 和相关 SDD design，不让文档与绑定漂移。
5. 运行构建/验证，至少确认 `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`。

不要只在组件里写新的 `Input.IsActionPressed("...")` 字符串。

## 2. 当前常用业务入口

| 目标 | 当前入口 | 默认绑定 |
| --- | --- | --- |
| 玩家移动 | `InputManager.GetMoveInput()` | WASD / 方向键 / 左摇杆 / DPad |
| 点选瞄准移动 | `InputManager.GetAimInput()` | 右摇杆 |
| 主动技能切换 | `InputManager.IsLeftBumper()` / `IsRightBumper()` | Q/E + LB/RB |
| 主动技能释放 | `InputManager.IsX()` | J + X |
| 瞄准确认 | `InputManager.IsX()` | J + X |
| 取消 | `InputManager.IsCancel()` | Esc + B |
| 暂停 | `InputManager.IsPause()` | Esc + Start |

`BtnX` 同时用于释放技能和确认目标，这是当前兼容设计，语义由 `Gameplay` / `Targeting` context 区分。

## 3. 调用点

### 3.1 移动

文件：`Src/ECS/Capabilities/Movement/System/Strategies/Base/PlayerInputStrategy.cs`

当前读取：

```csharp
Vector2 inputDir = InputManager.GetMoveInput();
```

改移动键位时，不改这里；改 `project.godot` 的 `MoveLeft/MoveRight/MoveUp/MoveDown`，再同步 manifest。

### 3.2 主动技能

文件：`Src/ECS/Capabilities/Ability/Component/ActiveSkillInputComponent/ActiveSkillInputComponent.cs`

当前读取：

```csharp
if (InputManager.IsLeftBumper()) CycleActiveAbility(-1);
if (InputManager.IsRightBumper()) CycleActiveAbility(1);
if (InputManager.IsX()) TryUseCurrentActiveAbility();
```

改技能释放键时：

1. 改 `project.godot` 的 `BtnX` 默认绑定，或新增更明确的 action。
2. 同步 `InputMap.md` 中 `Gameplay.UseActiveAbility`。
3. 如果新增业务 action，先给 `InputManager` 增加语义方法，例如 `IsUseActiveAbilityPressed()`，再迁移组件调用。

### 3.3 点选瞄准

文件：`Src/ECS/Capabilities/Unit/Component/TargetingIndicatorControlComponent/TargetingIndicatorControlComponent.cs`

当前读取：

```csharp
var aimInput = InputManager.GetAimInput();
if (InputManager.IsX()) { /* ConfirmTarget */ }
if (InputManager.IsCancel()) { /* CancelTarget */ }
```

改瞄准确认/取消时，必须同步 `Targeting` context，避免和 Gameplay 语义混淆。

### 3.4 暂停菜单

文件：`Src/ECS/UI/PauseMenu/PauseMenuSystem.cs`

当前读取：

```csharp
if (!InputManager.IsPause()) return;
```

改暂停键时改 `BtnStart`，并确认 `BtnB`/`BtnStart` 与菜单返回逻辑没有冲突。

## 4. 添加新输入

推荐流程：

1. 在 `InputMap.md` 先补一行 manifest，写清 `Context`、`Business Action`、`Godot Action`、默认键位、consumer。
2. 在 `project.godot` 添加 action 和 events。
3. 在 `InputManager.cs` 添加 typed 方法；方法名表达业务语义，不只表达按钮名。
4. 在具体 owner 组件中消费 typed 方法。
5. 跑 `rg -n "Input\\.IsAction|Input\\.GetAction|Input\\.GetVector|InputManager\\.IsAction" Src/ECS` 检查业务层是否引入新的裸字符串。

Debug/Test 场景可例外直调 Godot 输入，但应在测试文档或注释里标明不是框架业务契约。

## 5. 手柄与 UI 图标

当前 action 名兼容 Xbox 布局，但手柄类型至少包括 Xbox/XInput、PlayStation、Nintendo Switch、Steam Input/Steam Deck、generic SDL gamepad。

规则：

- 业务组件只关心 `UseActiveAbility`、`ConfirmTarget`、`Cancel` 等语义。
- `project.godot` 只关心物理 action 绑定。
- UI 显示按钮图标时不要直接复用 `BtnA/B/X/Y` 文本；后续应通过 `ControllerGlyphProfile` 或等价映射显示 Xbox/PlayStation/Switch/Steam 图标。

## 6. 验证

常规验证：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate --all
```

若改了 skill 源：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

若改动影响 Godot 场景输入，还需要进入承载游戏仓跑场景 smoke。
