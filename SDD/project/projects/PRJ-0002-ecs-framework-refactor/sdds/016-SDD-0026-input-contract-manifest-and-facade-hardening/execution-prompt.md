# SDD-0026 Execution Prompt

你正在执行 `SDD-0026 Input Contract Manifest And Facade Hardening`。

## 当前目标

把 Input 从按钮名 wrapper 收口为 AI-first 业务输入契约：

```text
project.godot InputMap
  -> DocsAI InputMap manifest
  -> InputManager business facade
  -> Ability / Targeting / Movement / UI consumers
  -> validation gates
```

不重写 Input 系统，不做本地多人输入，不做重绑定 UI，不做 Steam Input 深集成。

## 必读

1. `../../design/Tool/Input/README.md`
2. `../../design/Tool/Input/01-现状证据与AI-first裁决.md`
3. `../../design/Tool/Input/02-目标架构与优化路线.md`
4. `../../design/Tool/Input/03-调用点迁移与验证计划.md`
5. `../../../../../../DocsAI/ECS/Tools/Input/Concept.md`
6. `../../../../../../DocsAI/ECS/Tools/Input/Usage.md`
7. `../../../../../../DocsAI/ECS/Tools/Input/InputMap.md`
8. `README.md`
9. `design/main.md`
10. `tasks.md`
11. `bdd.md`
12. `Core/progress.md`

## 当前关键事实

- `project.godot` 是物理绑定事实源。
- `DocsAI/ECS/Tools/Input/InputMap.md` 是 AI 改键 manifest。
- `BtnX/BtnY/BtnLB/BtnRB` 已有键盘备用 `J/I/Q/E`。
- `BtnX` 同时用于 `Gameplay.UseActiveAbility` 和 `Targeting.ConfirmTarget`，必须用 context 区分。
- `ActiveSkillInputComponent` 当前路径是 `Src/ECS/Capabilities/Ability/Component/ActiveSkillInputComponent/ActiveSkillInputComponent.cs`。
- `TargetingIndicatorControlComponent` 当前路径是 `Src/ECS/Capabilities/Unit/Component/TargetingIndicatorControlComponent/TargetingIndicatorControlComponent.cs`。
- `PlayerInputStrategy` 当前 `GetMoveInput()` 已是可接受业务语义入口。
- 手柄类型不能写死 Xbox；后续 UI 显示应通过 `ControllerGlyphProfile` 或等价层。

## 禁止

- 不在业务组件里新增 `Godot.Input.IsAction*("...")`。
- 不把 `BtnA/B/X/Y` 写成唯一手柄事实。
- 不把 UI glyph 资源硬编码进 Ability、Movement、Targeting 组件。
- 不删除旧按钮名 API，除非所有调用点和文档都已迁移且验证通过。
- 不改动无关 SDD-0025 目录迁移文件。
- 不回滚工作区既有 `.uid`、`__pycache__`、DocsAI 迁移或 submodule 相关改动。

## 执行起点

先跑 readiness baseline：

```bash
git rev-parse --show-toplevel
git status --short
sed -n '/^\[input\]/,/^\[/p' project.godot
sed -n '1,220p' Src/ECS/Tools/Input/InputManager.cs
sed -n '70,120p' Src/ECS/Capabilities/Ability/Component/ActiveSkillInputComponent/ActiveSkillInputComponent.cs
sed -n '120,150p' Src/ECS/Capabilities/Unit/Component/TargetingIndicatorControlComponent/TargetingIndicatorControlComponent.cs
python3 Workspace/SDD/sdd.py validate SDD-0026
```

记录 baseline 摘要到 `Core/progress.md`，不要复制完整 dirty 列表。

## 实现顺序

1. T1.1 readiness baseline。
2. T1.2 在 `InputManager` 新增业务语义方法：
   - `IsUseActiveAbilityPressed`
   - `IsPreviousActiveAbilityPressed`
   - `IsNextActiveAbilityPressed`
   - `IsTargetConfirmPressed`
   - `IsTargetCancelPressed`
   - `IsPausePressed`
3. T1.3 迁移 `ActiveSkillInputComponent`。
4. T1.4 迁移 `TargetingIndicatorControlComponent` 和 `PauseMenuSystem`。
5. T1.5 同步 DocsAI / SDD / skill（仅规则变化时跑 sync）。
6. T1.6 跑 grep gate 与绑定一致性检查。
7. T1.7 更新 progress，完成验证后再考虑 `sdd.py done SDD-0026`。

## 验证命令

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0026
python3 Workspace/SDD/sdd.py validate --all
rg -n "Input\\.IsAction|Input\\.GetAction|Input\\.GetVector|InputManager\\.IsAction" Src/ECS
rg -n "BtnX|BtnY|BtnLB|BtnRB|MoveLeft|StickRight" project.godot DocsAI/ECS/Tools/Input
git diff --check -- Src/ECS/Tools/Input Src/ECS/Capabilities/Ability/Component/ActiveSkillInputComponent Src/ECS/Capabilities/Unit/Component/TargetingIndicatorControlComponent Src/ECS/Base/System/PauseMenu DocsAI/ECS/Tools/Input SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/016-SDD-0026-input-contract-manifest-and-facade-hardening
```

如果本地存在 Godot CLI，再追加：

```bash
godot --headless --path . --quit
```

若 `godot` 不存在，明确记录为环境缺口。
