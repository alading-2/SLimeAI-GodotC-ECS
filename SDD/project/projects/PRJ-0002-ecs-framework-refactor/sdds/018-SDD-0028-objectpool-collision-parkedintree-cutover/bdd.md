# BDD

## Applicability

- **Required**: true
- **Reason**: ObjectPool 与 Collision 是 gameplay/runtime 热路径，本 SDD 修改池化物理根节点默认生命周期、碰撞业务入口 guard、ContactDamage timer 清理、验证场景和 DocsAI/skill 契约。

## Scenarios

### Scenario: Collision roots stay parked in tree on release

Given an `Area2D` or `CharacterBody2D` root node is released to ObjectPool
When the default ObjectPool release strategy runs
Then the node remains inside the tree, is hidden, stops processing, moves to a parking grid position, and records `CollisionLogicActive=false` without disabling layer/mask/shape.

### Scenario: Activation first frame does not process business collision

Given a pooled collision root is acquired with `Get(false)` and activated after Entity initialization
When a stale `entered` signal dispatches in the first physics frame after `Activate`
Then `CollisionLogicGuard` rejects it and no MovementCollision, Damage, destroy or contact damage timer is triggered.

### Scenario: Ready frame handles only new collisions

Given an activated object reaches `CollisionReadyPhysicsFrame`
When it overlaps an expected target at the new spawn position
Then Collision / Movement / Damage process the event only after entity validity, target validity, team, owner and lifecycle checks pass.

### Scenario: ContactDamage cancels stale attacker timers

Given `ContactDamageComponent` has a timer for an attacker
When the attacker is returned to ObjectPool or reused before the next tick
Then the timer tick checks pool runtime state, cancels the timer, removes stale contact state and does not apply damage.

### Scenario: Parking grid does not become a hidden battlefield

Given many collision roots are released to ObjectPool
When they are parked
Then parking positions are distributed, business collision events from parking are rejected, and validation records event counts and frame pressure within thresholds.

### Scenario: Fallback detach is explicit

Given a validation case enables detach fallback
When the fallback path runs
Then artifact marks it as fallback/control, records detach status, and the default `ParkedInTree` path remains the required success path.

### Scenario: ObjectPool tests are machine-readable

Given ObjectPool collision validation runs headless
When all checks pass
Then it writes a PASS JSON artifact with expectedInputs, expectedObservations, passCriteria, failCriteria, artifactPath, checks, poolStats, nodeStates, collisionEvents, businessCollisionEvents and failureReasons.

### Scenario: AI reads current Collision Concepts

Given an AI reads `DocsAI/ECS/Capabilities/Collision/Concepts/README.md`
When it looks for the current collision/object-pool strategy
Then it finds the three current entries and sees old default detach / collision-disable analysis only under `History/`.
