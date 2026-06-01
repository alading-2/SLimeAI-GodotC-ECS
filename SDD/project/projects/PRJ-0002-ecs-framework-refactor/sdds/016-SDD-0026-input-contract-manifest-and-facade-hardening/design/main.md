# Input Contract Manifest And Facade Hardening

## Goal

让 SlimeAI Input 成为 AI 可读、可验证、可渐进扩展的输入契约，而不是让 AI 从 `BtnX`、`BtnB`、`LB/RB` 等按钮名推断业务语义。

本 SDD 的执行目标：

1. 保留 `project.godot` `[input]` 作为 Godot 物理绑定事实源。
2. 保留 `DocsAI/ECS/Tools/Input/InputMap.md` 作为 AI 可读 manifest。
3. 在 `InputManager` 中补齐业务语义 facade，减少业务组件对按钮名 API 的依赖。
4. 迁移 `ActiveSkillInputComponent`、`TargetingIndicatorControlComponent`、`PauseMenuSystem` 等调用点到语义方法。
5. 建立 grep gate、构建、SDD validate 和可选 Godot scene smoke 的验证闭环。

非目标：

- 不重写 Godot InputMap。
- 不做本地多人设备隔离。
- 不做重绑定 UI。
- 不做 Steam Input 深集成。
- 不在业务 owner 中硬编码 Xbox/PlayStation/Switch glyph。

## Context

必读事实源已复制到本 SDD `design/`，并保留来源路径：

- `design/README.md`：来源 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/Input/README.md`
- `design/01-现状证据与AI-first裁决.md`
- `design/02-目标架构与优化路线.md`
- `design/03-调用点迁移与验证计划.md`
- `DocsAI/ECS/Tools/Input/Concept.md`
- `DocsAI/ECS/Tools/Input/Usage.md`
- `DocsAI/ECS/Tools/Input/InputMap.md`
- `Src/ECS/Tools/Input/InputManager.cs`
- `project.godot` `[input]`

当前关键事实：

- `project.godot` 已包含 `Move*`、`BtnA/B/X/Y`、`BtnLB/RB/LT/RT`、`BtnStart/Home/Select`、`StickRight*`。
- `BtnX/BtnY/BtnLB/BtnRB` 已补键盘备用 `J/I/Q/E`。
- `BtnX` 当前同时承担 `Gameplay.UseActiveAbility` 与 `Targeting.ConfirmTarget`，语义由 context 区分。
- `ActiveSkillInputComponent` 当前真实路径是 `Src/ECS/Capabilities/Ability/Component/ActiveSkillInputComponent/ActiveSkillInputComponent.cs`，历史 `Src/ECS/Base/Component/Player/...` 已迁移。
- 手柄类型至少要按 Xbox/XInput、PlayStation、Nintendo Switch、Steam Input、generic SDL gamepad 分层；当前 `BtnA/B/X/Y` 只是兼容 action 名。

研究采纳：

- Brotato 证明键盘、摇杆、DPad、多设备 remap 和输入模式检测是成熟游戏常见需求；SlimeAI 当前只采纳 manifest/分层原则。
- Slay the Spire 2 证明业务 action、物理 controller action、Steam Input、glyph profile 应分层；SlimeAI 当前只规划 `ControllerGlyphProfile`，不在本 SDD 实现完整平台 UI。

## Design

### 分层

```text
project.godot InputMap
  -> DocsAI InputMap manifest
  -> InputManager business facade
  -> Capability/UI consumers
  -> validation gates
```

### InputManager facade

保留现有按钮名 API 作为兼容层，但新增业务语义方法。推荐最小方法：

| 方法 | 当前 Godot action | 语义 |
| --- | --- | --- |
| `IsUseActiveAbilityPressed()` | `BtnX` | Gameplay context 释放当前主动技能 |
| `IsPreviousActiveAbilityPressed()` | `BtnLB` | Gameplay context 切到上一个主动技能 |
| `IsNextActiveAbilityPressed()` | `BtnRB` | Gameplay context 切到下一个主动技能 |
| `IsTargetConfirmPressed()` | `BtnX` | Targeting context 确认点选目标 |
| `IsTargetCancelPressed()` | `BtnB` | Targeting context 取消点选 |
| `IsPausePressed()` | `BtnStart` | UI/System context 暂停切换 |

`GetMoveInput()` 和 `GetAimInput()` 已经接近业务语义，可保留。

### 调用点迁移

| 调用点 | 迁移方向 |
| --- | --- |
| `ActiveSkillInputComponent` | `IsLeftBumper/IsRightBumper/IsX` -> `IsPreviousActiveAbilityPressed/IsNextActiveAbilityPressed/IsUseActiveAbilityPressed` |
| `TargetingIndicatorControlComponent` | `IsX/IsCancel` -> `IsTargetConfirmPressed/IsTargetCancelPressed` |
| `PauseMenuSystem` | `IsPause` -> `IsPausePressed`，旧 `IsPause` 可暂留兼容 |
| `PlayerInputStrategy` | 保持 `GetMoveInput()`，只同步文档和验证 |

### 文档同步

每次改 Input 行为必须同步：

- `DocsAI/ECS/Tools/Input/InputMap.md`
- `DocsAI/ECS/Tools/Input/Usage.md`
- 必要时同步项目级 `design/Tool/Input/03-调用点迁移与验证计划.md`
- 若 owner skill 路由或规则变化，同步 `.ai-config/skills/core/tools/SKILL.md` 并运行 sync

### 取舍

- 先新增业务 facade，再迁移调用点，避免一次性删除按钮名 API 导致大面积风险。
- 不实现运行时 context stack；context 先通过方法名和 manifest 表达。
- 不实现 glyph profile，但保留设计入口，防止后续 UI 把 Xbox 文本写死进业务代码。

## Verification

必跑：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0026
python3 Workspace/SDD/sdd.py validate --all
rg -n "Input\\.IsAction|Input\\.GetAction|Input\\.GetVector|InputManager\\.IsAction" Src/ECS
rg -n "BtnX|BtnY|BtnLB|BtnRB|MoveLeft|StickRight" project.godot DocsAI/ECS/Tools/Input
```

判定：

- 构建 0 errors。
- SDD validate 0 errors / 0 warnings。
- 业务层新增代码不直接调用 `Godot.Input.IsAction*` 或拼 action 字符串；`InputManager` 和 Debug/Test 例外。
- `project.godot` 与 `InputMap.md` 的默认绑定一致。

可选：

```bash
godot --headless --path . --quit
```

如果本地没有 Godot CLI，记录为环境缺口，不把场景 smoke 伪装为通过。
