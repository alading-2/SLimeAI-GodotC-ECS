# Input 概念

> status: current
> sourcePaths: `Src/ECS/Tools/Input/`
> bindingSource: `project.godot` `[input]`
> relatedDocs: `DocsAI/ECS/Tools/Input/Usage.md`, `DocsAI/ECS/Tools/Input/InputMap.md`
> lastReviewed: 2026-06-01

## 1. 一句话定位

Input 是 Godot `InputMap` 到 SlimeAI 业务输入语义之间的轻量契约层。当前实现以 `project.godot` 保存物理按键/手柄绑定，以 `InputManager` 提供 C# typed facade，让 AI 能快速知道“在哪里改键、哪个组件消费、怎么验证”。

当前不是本地多人输入系统，也没有可确认的 `InputDocGenerator` 生成链路；这些只能作为后续路线，不作为 current 能力。

## 2. 当前事实源

| 层 | 文件 | 当前职责 |
| --- | --- | --- |
| 物理绑定 | `project.godot` `[input]` | Godot action 与键盘、手柄按钮、摇杆轴的真实绑定源。 |
| 业务 facade | `Src/ECS/Tools/Input/InputManager.cs` | 封装 `Godot.Input`，提供移动、瞄准、确认、取消、暂停、手柄状态和震动查询。 |
| Gameplay 消费 | `Src/ECS/Capabilities/Movement/System/Strategies/Base/PlayerInputStrategy.cs` | 读取 `GetMoveInput()` 驱动玩家移动。 |
| Ability 消费 | `Src/ECS/Capabilities/Ability/Component/ActiveSkillInputComponent/ActiveSkillInputComponent.cs` | 读取 `LB/RB/X` 完成主动技能切换和释放。历史路径 `Src/ECS/Capabilities/Ability/Component/ActiveSkillInputComponent/` 已迁移。 |
| Targeting 消费 | `Src/ECS/Capabilities/Unit/Component/TargetingIndicatorControlComponent/TargetingIndicatorControlComponent.cs` | 读取右摇杆、`X` 和取消键完成点选瞄准。 |
| UI/System 消费 | `Src/ECS/UI/PauseMenu/PauseMenuSystem.cs` | 读取 `BtnStart` 暂停/继续。 |

## 3. 输入命名边界

当前 `project.godot` 采用兼容 Xbox 的 `BtnA/BtnB/BtnX/BtnY/LB/RB/LT/RT` 命名，以及 `Move*`、`StickRight*` 等方向 action。

这些名字是 **Godot action 兼容名**，不是完整业务语义：

- `BtnX` 在 Gameplay context 中是 `UseActiveAbility`。
- `BtnX` 在 Targeting context 中是 `ConfirmTarget`。
- `BtnB` 在 Targeting/UI context 中是 `Cancel`。
- `BtnStart` 在 UI/System context 中是 `Pause`。

AI 修改输入时，必须先看 context 和 consumer，不能只按 `BtnX` 字面含义判断功能。

## 4. 手柄类型结论

手柄确实分多种，不能只按 Xbox 名称理解：

| 类型 | 对 SlimeAI 的影响 |
| --- | --- |
| Xbox / XInput / default SDL gamepad | 当前 `BtnA/B/X/Y` 命名最接近这一层，可继续作为兼容 action 名。 |
| PlayStation / DualShock / DualSense | 物理位置可由 Godot/SDL 映射到 action，但 UI glyph 和确认/取消习惯可能不同。 |
| Nintendo Switch / Pro / Joy-Con | A/B、X/Y 标注与 Xbox 不同，UI 显示必须单独处理。 |
| Steam Input / Steam Deck / Steam Controller | 可能提供独立的动作层和设备类型，适合后续做平台集成，不作为第一阶段必需。 |
| Generic SDL gamepad | Godot 会尽量通过 SDL controller database 识别名称和映射；不应在业务层硬编码品牌。 |

当前裁决：第一阶段只维护 **物理绑定 + 业务语义 manifest**。UI 图标和平台显示建议后续单独建立 `ControllerGlyphProfile`，不要混入技能/移动组件。

## 5. AI-first 规则

| 规则 | 原因 |
| --- | --- |
| 改默认键位先改 `project.godot`，再同步 `InputMap.md` | `project.godot` 是 Godot 真实绑定源，文档是 AI 可读 manifest。 |
| 业务代码优先调用 `InputManager` typed 方法 | 避免组件里散落 action 字符串。 |
| 新业务输入先补 manifest，再补 facade | 让 AI 能查到 owner、context、consumer 和验证入口。 |
| Debug/Test 可以直调 `Godot.Input`，但必须标注测试上下文 | 测试便利和框架业务契约分开。 |
| 不把多人设备隔离写成 current | 当前 `InputManager` 没有 player/device 参数。 |

## 6. 职责边界

| Input 做 | Input 不做 |
| --- | --- |
| 维护 Godot action 与业务 action 的可读契约 | 直接执行技能、移动、UI 行为 |
| 提供统一 typed facade | 在业务组件里扫描 `project.godot` |
| 记录键盘、手柄、摇杆默认绑定 | 负责 UI glyph 资源和平台图标显示 |
| 提供手柄状态、名称和震动入口 | 第一阶段实现本地多人设备隔离 |
| 给 AI 提供改键路径和验证清单 | 替代 Godot InputMap 或 DataOS |
