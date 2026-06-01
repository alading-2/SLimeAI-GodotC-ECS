# 现状证据与 AI-first 裁决

> 更新：2026-06-01
> 状态：current design note

## 1. 问题重新定义

Input 当前被直觉描述为“很分散”。更准确的定义是：

```text
Godot InputMap action
  + 静态 InputManager wrapper
  + Gameplay 组件逐帧轮询
  + UI / Debug / Test 直接调用 Godot.Input
  + 文档与源码能力漂移
  = AI 无法稳定判断某个输入属于哪个上下文、业务语义和验证入口
```

Input 的问题不是“不能用”。玩家移动、技能切换、瞄准指示器、暂停菜单都能工作。问题是它对 AI 不够显式：AI 看到 `BtnX`、`BtnY`、`MoveLeft`、`ui_right` 时，必须跨 `project.godot`、`InputManager`、组件代码和测试场景猜语义。

## 2. 当前代码证据

当前 Input 入口集中在：

- `Src/ECS/Tools/Input/InputManager.cs`
- `project.godot` 的 `[input]` 区段
- `DocsAI/ECS/Tools/Input/Concept.md`
- `DocsAI/ECS/Tools/Input/Usage.md`

`InputManager` 当前提供：

- `IsConfirm/IsCancel/IsX/IsY/IsPause/IsSelect/IsHome`
- `IsLeftBumper/IsRightBumper/IsLeftTrigger/IsRightTrigger`
- `GetLeftTriggerStrength/GetRightTriggerStrength`
- `GetMoveInput/GetAimInput/GetMoveInputRaw`
- `Vibrate/StopVibration/VibrateLight/VibrateHeavy/VibrateMedium`
- `GetConnectedJoypads/IsJoypadConnected/GetJoypadName`
- 通用 `IsActionJustPressed/IsActionPressed/IsActionJustReleased/GetActionStrength`

实际调用点包括：

| 调用点 | 当前行为 | 问题 |
| --- | --- | --- |
| `PlayerInputStrategy` | 每帧 `InputManager.GetMoveInput()` 驱动玩家移动 | 业务语义清楚，但缺上下文，例如暂停/菜单时是否屏蔽。 |
| `ActiveSkillInputComponent` | `LB/RB` 切换技能，`X` 释放当前技能 | 输入和业务执行在组件内直接绑定，缺 action manifest。 |
| `TargetingIndicatorControlComponent` | 右摇杆移动指示器，`X/Cancel` 确认或取消 | `X` 同时用于攻击/确认，语义依赖当前模式。 |
| `PauseMenuSystem` | `InputManager.IsPause()` | 暂停属于 UI/System context，但当前和 gameplay 输入共用静态入口。 |
| 测试场景 | 多处直接 `Input.IsActionPressed("ui_right")` 或 `"BtnY"` | Debug/Test 输入没有和框架 Input 契约分层。 |

`project.godot` 当前 action 包含两类：

- UI/方向：`UiLeft/UiRight/UiUp/UiDown`、`MoveLeft/MoveRight/MoveUp/MoveDown`
- 手柄按钮/摇杆：`BtnA/BtnB/BtnX/BtnY/BtnLB/BtnRB/BtnLT/BtnRT/BtnStart/BtnHome/BtnSelect`、`StickRight*`

这说明 `project.godot` 是真实绑定源，但它不表达 owner、上下文、业务语义和允许的消费者。

## 3. 文档漂移证据

`DocsAI/ECS/Tools/Input/Concept.md` 当前写到：

- 支持 PascalCase 命名约定和玩家编号隔离。
- action 示例包括 `Attack/Ability1/Ability2/Interact/Pause`。
- `InputManager.md` 由 `InputDocGenerator` 自动生成。

但当前源码证据显示：

- `InputManager.cs` 没有玩家编号隔离参数。
- `project.godot` 使用 `BtnX/BtnY/BtnA`，不是 `Attack/Ability1`。
- 当前仓未发现实际 `InputDocGenerator` 生成链路作为事实源。

因此 Input 文档需要先纠偏：未来可以支持多人、生成、语义 action，但不能把未来能力标成 current。

## 4. 外部资料校准

外部资料只用于校准边界，不用于复制 API。

| 来源 | 对本设计的约束 |
| --- | --- |
| Godot InputEvent / InputMap 文档 | Godot 的输入系统以 action 映射为核心，`InputMap` 可管理 action 与 event；这支持继续保留 `project.godot` 作为引擎绑定源。 |
| Unity Input System Actions | Unity 把 action、action map、control scheme、binding 分开；这证明“按键”和“业务动作/上下文”应分层，但 SlimeAI 不需要复制 asset/editor 体系。 |
| Unreal Enhanced Input | Unreal 区分 Input Action、Mapping Context、Modifier、Trigger；这支持 SlimeAI 引入轻量 `InputContext` 和触发语义，但不应引入复杂栈和编辑器。 |
| Unity Entities Samples 输入 | 网络输入样例把 input 写成 tick 化数据，这对联网/回放有启发；当前 SlimeAI 单机第一阶段不需要 tick command buffer。 |

## 5. AI-first 风险

### 5.1 语义藏在按钮名里

`BtnX` 到底是攻击、释放技能、确认目标，还是测试触发，由调用点决定。AI 无法从 action 名直接判断业务含义。

### 5.2 上下文不显式

Gameplay、Targeting、PauseMenu、DebugCamera、TestScene 共享 `Godot.Input` 状态。未来如果加暂停、菜单、重绑定或输入录制，没有上下文层会继续扩散分支判断。

### 5.3 允许自由字符串回流

`InputManager` 提供通用 `IsActionJustPressed(string action)`，测试和少量代码也直接使用 `Godot.Input`。这对调试方便，但对框架业务层是隐式入口。

### 5.4 缺验证清单

目前没有稳定 gate 能回答：

- `project.godot` 新增 action 是否有文档。
- 文档 action 是否真的存在。
- Gameplay 代码是否仍直接写 action 字符串。
- 测试输入是否被标注为 Debug/Test context。

## 6. AI-first 裁决

### 6.1 Input 需要优化

需要优化的不是底层按键读取，而是输入契约。当前结构对人足够直观，但对 AI 过于依赖上下文猜测。

### 6.2 保留 Godot InputMap

`project.godot` 继续作为引擎绑定事实源。它天然适配 Godot 编辑器和设备映射，不应被 C# 代码或 DataOS 完全替代。

### 6.3 新增轻量 manifest，而不是重写系统

第一阶段应新增或规划 `InputManifest`，用于描述 action id、业务语义、默认绑定、上下文、owner 和验证信息。manifest 可以先是文档表格，后续再决定是否生成 C# typed id。

### 6.4 业务层不应继续使用裸字符串

框架业务层应只通过 typed facade 读取输入。Debug/Test 代码可以保留 `Godot.Input` 直调，但必须标注为测试上下文，不进入默认框架契约。

## 7. 结论

推荐策略：

1. 先修正 DocsAI Input 文档，移除未实现能力的 current 表述。
2. 在 SDD / DocsAI 中建立 action manifest 和调用点清单。
3. 后续实现 `InputActionId` 与 `InputContext` 的最小 typed 层。
4. 再迁移 gameplay 调用点，保留测试场景的 Debug/Test 例外。
5. 最后补验证：构建、grep gate、Input 场景、BrotatoLike smoke。
