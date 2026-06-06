# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 改变 Runtime Data typed hot path、modifier、computed cache 和 diagnostics 边界。

## Scenarios

### Scenario: Typed DataKey write uses typed slot

Given a catalog contains `Attribute.BaseHp` as a float descriptor
When runtime code calls `Data.Set(new DataKey<float>("Attribute.BaseHp"), 20f)`
Then `DataRuntimeStorage` writes a `DataSlot<float>`
And the typed set path does not call the untyped boundary writer
And `Data.Get(new DataKey<float>("Attribute.BaseHp"))` returns `20f`

### Scenario: Untyped loader boundary still converts with diagnostics

Given a catalog contains float, string array, modifier list and object ref descriptors
When snapshot loader or debug code calls `SetUntyped`
Then values are converted at the boundary and stored in typed slots
And wrong CLR types still return structured `DataWriteError`
And the boundary API is documented as not for gameplay hot path

### Scenario: Numeric modifiers keep typed effective values

Given a float descriptor allows numeric modifiers
When Additive, Multiplicative, FinalAdditive, Override and Cap modifiers are applied
Then the effective value matches the existing modifier formula
And the slot stores and returns typed `float`
And dependent computed descriptors are marked dirty after modifier changes

### Scenario: Computed values cache in typed storage

Given a computed float descriptor depends on runtime float inputs
When `Data.Get<float>` reads the computed key twice without dependency changes
Then the resolver runs once
And the cached value is stored as typed `float`
When a dependency changes
Then the computed slot is marked dirty and recomputes on next read
