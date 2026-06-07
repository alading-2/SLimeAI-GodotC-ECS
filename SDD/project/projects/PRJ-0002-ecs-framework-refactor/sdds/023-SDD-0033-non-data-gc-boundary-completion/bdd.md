# BDD

## Applicability

- **Required**: true
- **Reason**: This SDD changes Runtime Event, Feature / Ability execution, ObjectPool manager, and TargetSelector query behavior.

## Scenarios

### Scenario: Ability execution writes typed Feature result

Given an Ability handler receives a FeatureContext with an Ability cast payload
When the handler executes through FeatureSystem
Then it reads the payload with a typed activation helper
And it writes `AbilityExecutedResult` with a typed execution result helper
And AbilitySystem reads the result without casting from raw `ExecuteResult`

### Scenario: Event dynamic API is no longer a gameplay protocol

Given framework gameplay code emits or subscribes to an event
When it uses EventBus
Then it uses typed `Emit<T>` / `On<T>` APIs
And dynamic object APIs are not used by Capability main flows

### Scenario: ObjectPool manager does not reflect generic pools

Given ObjectPoolManager owns multiple generic pools
When it releases, cleans up, clears, or reports stats
Then it calls `IObjectPoolRuntime` members directly
And wrong untyped release input is rejected deterministically.

### Scenario: Target query exposes ownership and diagnostics

Given a target query has more candidates than the requested max target count
When TargetQueryEngine returns query results
Then the result exposes read-only items
And diagnostics report candidate count, returned count, max target, and truncated status.
