# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 会改变框架目录、DocsAI 路由、skill 指向和 Godot 资源路径，是高影响架构重构。

## Scenarios

### Scenario: AI routes a feature task through Capability

Given a task asks to modify Ability, Damage, Movement or another gameplay owner
When an AI reads DocsAI and Src after SDD-0025 is complete
Then it can enter `DocsAI/ECS/Capabilities/<Owner>/` and `Src/ECS/Capabilities/<Owner>/` without first searching across top-level Component/System/Event/Test directories

### Scenario: AI still understands ECS boundaries

Given a Capability contains Component, System, Events, Tests and DataKeys
When an AI modifies the Capability
Then it still treats Entity, Component, Data, Event and System as required ECS contracts instead of replacing them with an unstructured feature folder

### Scenario: Runtime is not polluted by gameplay owner logic

Given Runtime contains Entity, Data, Event and System Core infrastructure
When Ability, Damage or Movement behavior changes
Then the change stays in `Capabilities/<Owner>/` unless it genuinely changes shared ECS infrastructure

### Scenario: Godot scenes survive path migration

Given `.tscn` files reference scripts or PackedScenes under old `res://Src/ECS/Base/...` paths
When a migration slice moves those files
Then the same slice updates scene paths and verifies the affected build or scene smoke

### Scenario: Historical concepts do not become current execution docs

Given DocsOld contains historical concept and design documents
When the directory restructure is complete
Then current routing does not include `DocsAI/ECS/Foundations/`, and any preserved historical material is either placed under an owner `Concepts/` directory or marked as non-execution context in Archive/Thinking
