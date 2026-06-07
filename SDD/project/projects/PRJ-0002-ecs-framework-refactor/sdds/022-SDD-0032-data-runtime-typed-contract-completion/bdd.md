# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 修改 Runtime Data 公共协议、Ability cost、Feature modifier、Unit 事件监听和 DataOS generated key，必须用行为场景约束回归。

## Scenarios

### Scenario: Ability cost uses generated typed resource keys

Given an ability has cost type Mana, Health, Energy or Ammo
When CostComponent checks and consumes cost
Then it reads and writes the caster resource through `DataKey<float>` generated handles, without naked string resource keys.

### Scenario: System-only runtime object uses typed system write

Given `TargetNode` is runtime_only and system_only
When AI finds a target Node2D
Then it writes through `TrySetSystem(GeneratedDataKey.TargetNode, node, out report)` and runtime source is still rejected.

### Scenario: Default reads use cached typed default

Given a Data slot has never received a runtime value
When code reads float, int, bool, string, string_array, resource_ref or object_ref defaults repeatedly
Then the slot returns typed defaults without repeated boundary conversion, and mutable array defaults cannot be modified globally by the caller.

### Scenario: Computed resolver is typed

Given a computed descriptor resolves through a float resolver
When Data reads the computed field
Then the resolver returns `float`, the typed slot caches the value, and dependency changes dirty and recompute it.

### Scenario: Computed resolver type mismatch fails fast

Given a computed descriptor expects `float`
When its registered resolver outputs another CLR type
Then catalog/runtime validation fails with stable key, compute id, expected type and actual output type.

### Scenario: Data change has typed event and diagnostic event

Given a float Data field changes
When Entity.Events dispatches Data events
Then business code can subscribe to `GameEventType.Data.Changed<float>` for typed old/new values, while TestSystem can still consume diagnostic `PropertyChanged`.

### Scenario: Diagnostic snapshot is clearly named

Given TestSystem or migration needs a boxed view of Data values
When it requests all current values
Then it uses `GetDiagnosticSnapshot()` and any remaining `GetAll()` call is only an obsolete compatibility wrapper.

### Scenario: Modifier source is stable and typed

Given two features add modifiers to the same Data key
When one feature is removed
Then `RemoveModifiersBySource(DataModifierSource)` removes only that feature's modifiers and leaves the other feature's modifiers intact.
