# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 改变 Runtime Component 默认组合事实源、Entity spawn 注册时序和 Component 文档/skill 路由。

## Scenarios

### Scenario: Entity spawn composes default components before registration

Given an Entity implements `IComponentCompositionProvider`
When `EntitySpawnPipeline.Spawn` creates that Entity
Then `ComponentComposer` creates the profile components before `ComponentRegistrar.RegisterComponents`
And each composed component receives typed options before `OnComponentRegistered`
And `OnComponentRegistered` runs exactly once per composed component

### Scenario: Unit and Ability profiles replace Component Presets

Given Player, Enemy, TargetingIndicator and Ability entities are spawned from their root `.tscn`
When their default components are needed
Then Player receives UnitCore + Player profile components plus explicit non-preset scene components
And Enemy receives Enemy + UnitCore profile components
And TargetingIndicator receives UnitCore profile components plus explicit control component
And Ability receives Ability profile components
And the root Entity scenes do not instance `UnitCorePreset`, `PlayerPreset`, `EnemyPreset` or `AbilityPreset`

### Scenario: EntityOrientation uses typed options instead of Inspector

Given UnitCore profile creates `EntityOrientationComponent`
When the composer configures the component
Then `EntityOrientationComponentOptions.Sink` is `VisualFlipX`
And the component has no `[Export]` default parameter
And `Sink` is not added as a DataKey or runtime snapshot field

### Scenario: AI can route Component changes from manifest

Given an AI needs to modify a Capability Component
When it opens `DocsAI/ECS/Runtime/Component/ComponentManifest.md`
Then it can see the component owner, source path, process hook, subscription risk, timer risk, dynamic policy and tests
And it can see current `GetComponent<T>()` documented exceptions
And it does not need to infer Component Preset composition from `.tscn` files
