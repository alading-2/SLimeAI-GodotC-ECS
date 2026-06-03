# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 会改变 Runtime System 的 AI-first 合同、diagnostics 和验证方式。

## Scenarios

### Scenario: DocsAI manifest routes an AI to the right system owner

Given `DocsAI/ECS/Runtime/System/SystemManifest.md` exists
When an AI needs to modify `SpawnSystem`
Then the manifest shows its owner, source path, system.config record, run condition, command handlers and tests
And the AI does not need to start with a global grep

### Scenario: Preflight catches config and registry drift

Given runtime snapshot contains `system.config` records
And code has registered descriptors through `SystemRegistry`
When `SystemPreflight` runs
Then every required system has config and descriptor
And every dependency resolves to config and descriptor
And the preflight report has zero error

### Scenario: Runtime diagnostics explains blocked commands

Given `DamageService` is loaded and enabled
And `ProjectState` is `FrontEnd`
When a damage command is executed through `SystemManager.Execute`
Then the command is blocked
And diagnostics contains a stable reason code
And the human-readable message is not empty

### Scenario: TestSystem and artifact share one diagnostics contract

Given `SystemManager` has bootstrapped
When TestSystem renders system info
And SystemCore validation dumps JSON diagnostics
Then both views derive from the same diagnostics contract
And required systems cannot be disabled or removed
