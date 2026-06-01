# Tasks

## Progress

- **Status**: active
- **Completed**: 0/7
- **Current**: T1.1

## Task List

- [ ] T1.1 Readiness baseline
  - **Goal**: 确认 git 边界、dirty 范围、Input 事实源和当前验证基线。
  - **Validation**: `git status --short`; `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`; `python3 Workspace/SDD/sdd.py validate SDD-0026`
- [ ] T1.2 InputManager 业务语义 facade
  - **Goal**: 新增 `IsUseActiveAbilityPressed`、`IsPreviousActiveAbilityPressed`、`IsNextActiveAbilityPressed`、`IsTargetConfirmPressed`、`IsTargetCancelPressed`、`IsPausePressed` 等方法，保留旧按钮名 API。
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`
- [ ] T1.3 Ability 调用点迁移
  - **Goal**: `ActiveSkillInputComponent` 改用主动技能业务语义方法。
  - **Validation**: 构建通过；grep 旧按钮名调用仅剩兼容层或未迁移记录。
- [ ] T1.4 Targeting/UI 调用点迁移
  - **Goal**: `TargetingIndicatorControlComponent`、`PauseMenuSystem` 改用 Targeting/UI 语义方法。
  - **Validation**: 构建通过；`DocsAI/ECS/Tools/Input/Usage.md` 与调用点一致。
- [ ] T1.5 DocsAI / SDD / skill 同步
  - **Goal**: 同步 InputMap manifest、Usage、项目级 Input 设计包；若 skill 规则变化，改 `.ai-config/skills/core/tools/SKILL.md` 并 sync。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`（仅 skill/rule/command 改动时）；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`
- [ ] T1.6 Input grep gate 与绑定一致性
  - **Goal**: 检查业务层没有新增裸字符串输入，`project.godot` 和 `InputMap.md` 一致。
  - **Validation**: `rg -n "Input\\.IsAction|Input\\.GetAction|Input\\.GetVector|InputManager\\.IsAction" Src/ECS`; `rg -n "BtnX|BtnY|BtnLB|BtnRB|MoveLeft|StickRight" project.godot DocsAI/ECS/Tools/Input`
- [ ] T1.7 完成验证和恢复记录
  - **Goal**: 更新本 SDD `progress.md`、项目级 `progress.md`，必要时标记 done。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0026`
