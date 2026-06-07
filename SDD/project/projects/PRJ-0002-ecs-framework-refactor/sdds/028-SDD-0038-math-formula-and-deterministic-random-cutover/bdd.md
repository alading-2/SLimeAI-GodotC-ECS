# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 改概率、随机和公式边界，必须保证玩法结果可复现、公式 owner 清楚。

## Scenarios

### Scenario: Probability is deterministic under fixed seed

Given a probability helper receives a seeded RNG
When the same sequence is evaluated twice
Then the outcomes are identical

### Scenario: Probability clamps or rejects invalid range

Given chance is below 0 or above 1
When probability helper evaluates it
Then behavior follows documented clamp or validation policy
And tests cover 0%, 100%, negative and over-100% cases

### Scenario: Formula owner is explicit

Given a cooldown, armor or damage formula changes
When an AI looks for the formula owner
Then DocsAI points to the owning capability or Math helper
And `MyMath` is not the current entry for all formulas

### Scenario: Geometry remains query-agnostic

Given TargetSelector needs cone or ring filtering
When it calls geometry helpers
Then Math provides pure geometry only
And query diagnostics remain owned by TargetSelector
