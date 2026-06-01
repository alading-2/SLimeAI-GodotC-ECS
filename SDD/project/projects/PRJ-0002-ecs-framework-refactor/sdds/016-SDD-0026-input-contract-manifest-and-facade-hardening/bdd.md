# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 改变 Input 业务语义 facade、DocsAI manifest 和 AI 改键工作流，需要行为验收。

## Scenarios

### Scenario: AI can find where to change active skill input

Given an AI needs to change the active skill release key
When it reads `DocsAI/ECS/Tools/Input/InputMap.md` and `Usage.md`
Then it can identify `Gameplay.UseActiveAbility`, `BtnX`, default `J + Xbox X`, and `ActiveSkillInputComponent` as the consumer
And it does not need to infer behavior from `BtnX` alone

### Scenario: Active skill input uses business facade

Given `ActiveSkillInputComponent` handles player active skills
When it processes previous, next, and use ability input
Then it calls business semantic `InputManager` methods
And it does not call `IsX`, `IsLeftBumper`, or `IsRightBumper` directly after migration

### Scenario: Targeting input is context separated

Given a point targeting session is active
When the player confirms or cancels targeting
Then `TargetingIndicatorControlComponent` calls target semantic input methods
And `BtnX` is documented as `Targeting.ConfirmTarget`, separate from `Gameplay.UseActiveAbility`

### Scenario: Controller type is not hardcoded as Xbox

Given UI needs to show a controller button prompt
When it resolves a glyph
Then it should use a display/profile layer such as `ControllerGlyphProfile`
And business components must not hardcode Xbox/PlayStation/Switch glyph resources

### Scenario: InputMap and manifest stay aligned

Given a Godot action binding changes in `project.godot`
When validation runs
Then `DocsAI/ECS/Tools/Input/InputMap.md` reflects the default binding and consumer
And SDD progress records the verification result
