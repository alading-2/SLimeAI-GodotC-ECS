# BDD

## Applicability

- **Required**: true
- **Reason**: Entity / Relationship hard cutover changes runtime identity, lifecycle, ownership, event and attribution behavior.

## Scenarios

### Scenario: Entity identity is typed

Given an entity is spawned through the runtime entrypoint
When a system needs to reference that entity
Then it receives or queries an `EntityId`
And no public capability API accepts raw `string entityId`
And `GeneratedDataKey.Id` is only a DataOS / snapshot / observation projection

### Scenario: Lifecycle parent is not business ownership

Given a projectile is spawned by a weapon owned by a player
When the projectile is attached to a lifecycle parent
Then `LifecycleTree` only decides destroy / detach behavior
And source / owner / credit references are stored through typed runtime reference helpers or owner services
And no `EntityRelationshipType.ENTITY_TO_PROJECTILE` is used

### Scenario: Destroy respects lifecycle and owner cleanup boundaries

Given an owner has spawned children recorded in owner lists
When a child is destroyed
Then owner cleanup removes the child id from owner lists
And owner lists do not decide whether that child is lifecycle-destroyed
And recursive destroy is decided only by `LifecycleTree` destroy policy

### Scenario: Damage attribution does not walk parent chain

Given a projectile damages an enemy
When statistics are credited
Then processors read `DamageAttribution`
And they do not call `GetAncestorChain` or `FindAncestorOfType<IUnit>` to infer the credited unit

### Scenario: Entity lifecycle events use typed payloads

Given an entity is spawned or destroyed
When lifecycle events are emitted
Then the event key is the payload type
And no string event name or `EntitySpawnedEventData` exists
