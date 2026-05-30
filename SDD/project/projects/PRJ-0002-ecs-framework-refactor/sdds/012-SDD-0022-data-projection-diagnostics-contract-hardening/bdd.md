# BDD

## Applicability

- **Required**: true
- **Reason**: This SDD changes Data runtime behavior, DataOS validation, spawn bootstrap behavior, and current documentation gates.

## Scenarios

### Scenario: Player record starts movement strategy

Given `runtime_snapshot.json` contains `unit.player/player.deluyi`
When the record is applied before `RegisterComponents()`
Then `GeneratedDataKey.DefaultMoveMode` is `PlayerInput`
And the player movement component creates a player input strategy without entity fallback writes

### Scenario: Enemy record starts movement strategy

Given a representative `unit.enemy` record exists in `runtime_snapshot.json`
When the enemy is spawned from the runtime record
Then `GeneratedDataKey.DefaultMoveMode` is `AIControlled` before `OnPoolAcquire()`
And the movement component does not depend on pool acquire to choose its default strategy

### Scenario: Ability record can create an ability entity

Given `ability.dash` exists in `runtime_snapshot.json`
When the ability record is applied through `EntityManager.Spawn<AbilityEntity>()`
Then the ability entity is created successfully
And generated handles read `AbilityType`, `AbilityTriggerMode`, `AbilityFeatureGroup`, `FeatureHandlerId`, and `AbilityIcon` without type fallback

### Scenario: Data write failure is diagnosable

Given a runtime Data write violates descriptor type or write policy
When the write API returns failure
Then the caller can inspect a structured error code, stable key, expected type, actual type, source, and policy context

### Scenario: Current docs do not route AI back to old Data

Given current docs under `SlimeAI/DocsNew` and `SlimeAI/Src/ECS/Base`
When the document gate searches for old Data route examples
Then only historical or SDD problem-description contexts may match
And current entry pages recommend descriptor-first, snapshot-first, generated handle, catalog-bound Data usage
