# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 改 Runtime mount 和 Node lifecycle 运行时行为，必须用行为场景约束 AI-first 可观察性。

## Scenarios

### Scenario: Runtime mount exposes deferred status

Given a root-level mount is created through deferred add
When diagnostics snapshot is requested in the same frame
Then the mount status is Pending rather than silently treated as fully in tree

### Scenario: Runtime mount uses manifest id

Given an Entity mount uses a declared MountId
When the Entity is spawned
Then the node is added under the manifest path below /root/SlimeAIRuntime
And no code path constructs the mount from typeof(T).Name alone

### Scenario: NodeLifecycle diagnostics identify stale nodes

Given a node was registered and then became invalid without unregister
When diagnostics snapshot is requested
Then invalid node count is reported

### Scenario: NodeLifecycle is not a gameplay query API

Given gameplay code needs targets or UI nodes
When it queries runtime state
Then it uses Entity/UI/TargetSelector typed facade
And it does not call NodeLifecycle global scan APIs directly
