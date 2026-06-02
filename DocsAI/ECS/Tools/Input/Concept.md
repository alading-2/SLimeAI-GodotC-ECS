# Input 概念

> status: current
> sourcePaths: `Src/ECS/Tools/Input/`
> bindingSource: `project.godot` `[input]`
> mainEntry: `DocsAI/ECS/Tools/Input/README.md`
> lastReviewed: 2026-06-02

## 1. 文档定位

本文只是 Input owner 的概念辅助页，不是必须存在的 DocsAI 模板。当前主入口是 [`README.md`](./README.md)，按键和业务功能表以 [`InputMap.md`](./InputMap.md) 为准。

Input 当前真实形态是：

```text
project.godot [input]
  -> Godot action 与物理键盘/手柄/摇杆绑定

InputManager.cs
  -> Godot.Input 的轻量 facade
  -> 暴露移动、瞄准、暂停、主动技能、点选确认等业务语义方法

业务 owner
  -> Movement / Ability / Targeting / UI 消费语义方法
```

这不是完整 Input 系统：没有运行时 `InputContext`、重绑定 UI、本地多人设备隔离、输入录制或 controller glyph profile。

## 2. 概念边界

| 概念 | 当前含义 |
| --- | --- |
| Godot action | `project.godot` 中的 action 名，例如 `BtnX`、`MoveLeft`。当前部分名字仍偏物理兼容名。 |
| Business action | 业务语义，例如 `UseActiveAbility`、`ConfirmTarget`、`Pause`。 |
| Input facade | `InputManager` 中暴露给业务 owner 的 C# 方法。 |
| Context | 当前先通过文档和方法名表达，如 `Gameplay`、`Targeting`、`UI/System`；尚未实现运行时上下文栈。 |
| Consumer | 消费输入的 owner，例如 `ActiveSkillInputComponent`、`TargetingIndicatorControlComponent`、`PauseMenuSystem`。 |

## 3. 当前红线

- `project.godot` 是物理绑定事实源，但不是业务语义文档。
- 业务代码优先调用 `InputManager` 语义方法，不新增裸 `Godot.Input.IsAction*("...")`。
- `BtnA/B/X/Y` 只是兼容 action 名，不代表唯一手柄类型。
- UI 图标不要写死 Xbox / PlayStation / Switch 文本；后续应走 `ControllerGlyphProfile` 或等价显示层。
- Debug/Test 可以直调输入，但不作为框架业务契约。

## 4. 后续扩展方向

扩展路线见 [`README.md`](./README.md) 的“后续真正扩展怎么改”。当前建议顺序是：业务 action 命名收口、manifest 自动校验、运行时 `InputContext`、`ControllerGlyphProfile`，最后再考虑重绑定 UI / 本地多人 / 输入录制。
