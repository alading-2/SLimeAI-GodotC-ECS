# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 改目标查询运行时契约，Ability/AI/Feature 依赖其行为。

## Scenarios

### Scenario: TargetSelector uses dynamic origin provider

Given a query has OriginProvider following a moving entity
When the entity moves before the query executes
Then geometry filtering and sorting use the latest resolved origin

### Scenario: TargetSelector explains empty result

Given enemies exist but all are filtered by team or lifecycle
When TargetQueryEngine runs
Then the result includes candidate count and filter counts
And the caller can read warnings or errors without parsing logs

### Scenario: Random target order is reproducible

Given a fixed random seed
When random sorting runs twice with the same candidates
Then both result orders are identical

### Scenario: Single query supports explicit target

Given a query asks for a single explicit target
When TargetQueryEngine runs
Then the explicit target is returned if filters pass
And diagnostics explain any rejection reason
