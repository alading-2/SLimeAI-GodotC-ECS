# Tasks

## Progress

- **Status**: active
- **Completed**: 4/11
- **Current**: T1.5

## Task List

- [x] T1.1 Readiness baseline
  - **Scope**: 确认 Git boundary、当前 dirty 范围、Data/Event/DocsAI current 入口、Entity 设计包和旧 API baseline grep。
  - **Checks**: `EntityRelationshipManager`、`EntityRelationshipType`、`ParentRelationTypes`、`DataKey.Id`、`GetInstanceId().ToString()`、`XxxEventData`、`DocsNew` / 旧路径命中分桶。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0024`

- [x] T1.2 EntityId 与 EntityRegistry RED tests
  - **Scope**: 新增测试覆盖 `EntityId.Empty/New/From`、无 implicit string、registry register/unregister/snapshot、重复 id / duplicate node 拒绝。
  - **Validation**: 新 Entity runtime tests 先 RED；记录失败摘要。

- [x] T1.3 EntityId 与 EntityRegistry 实现
  - **Scope**: 新增 `EntityId`、`EntityRegistry`，替换 Entity core 内部 raw string id；`GeneratedDataKey.Id` 只由 initializer 投影。
  - **Validation**: T1.2 tests 通过；grep 不再有新 raw string entity id public API。

- [x] T1.4 LifecycleTree RED tests 与实现
  - **Scope**: attach/detach/get parent/get children、single parent、self/cycle/second parent 拒绝、destroy policy typed field、detach all。
  - **Validation**: LifecycleTree tests 通过；旧 `PARENT` relationType 写入退出 lifecycle 路径。

- [ ] T1.5 EntityDestroyPipeline RED tests 与实现
  - **Scope**: recursive child destroy、detach child 存活、children snapshot、重复 destroy result、owner cleanup 顺序、component unregister 在 Data clear 前。
  - **Validation**: Destroy pipeline tests 通过；不调用 `EntityRelationshipManager.RemoveAllRelationships`。

- [ ] T1.6 ComponentRegistrar 拆分
  - **Scope**: 从 `EntityManager_Component*` 拆出 Component 注册、反查、卸载和 internal owner index；不公开 Relationship。
  - **Validation**: Component tests 通过；`ENTITY_TO_COMPONENT` 退出 public relationship。

- [ ] T1.7 EntitySpawnPipeline 拆分
  - **Scope**: `EntityNodeFactory`、`EntityDataInitializer`、`EntityVisualInitializer`、`EntityTransformInitializer`、registry register、component register、lifecycle attach。
  - **Validation**: Spawn tests 通过；`EntitySpawnConfig` 删除 `ParentEntity / AutoAddParentRelation / ParentRelationTypes`。

- [ ] T1.8 OwnedReferenceRegistry 与 typed references
  - **Scope**: `EntityIdList`、`OwnedReferenceDescriptor`、owner cleanup hook；Projectile / Effect / Ability owner list 通过 generated string/string_array projection helper 封装。
  - **Validation**: owner list add/remove/destroy cleanup tests 通过；owner list 不决定 lifecycle destroy。

- [ ] T1.9 Ability owner service 替换 EntityManager_Ability
  - **Scope**: `AddAbility / RemoveAbility / GetAbilities` 迁到 Ability owner service；Feature/TestSystem/UI 调用点迁移。
  - **Validation**: Ability service tests 通过；删除 `EntityManager_Ability.cs` runtime 调用。

- [ ] T1.10 Projectile / Effect / Damage / Movement 调用点迁移
  - **Scope**: Projectile source、Effect source/host、DamageAttribution、Movement source owner 全部停止使用 parent-chain Relationship。
  - **Validation**: 对应 system tests 通过；`GetAncestorChain / FindAncestorOfType<IUnit>` 不再作为业务路径。

- [ ] T1.11 DocsAI、SDD、最终验证和 handoff
  - **Scope**: 同步 `DocsAI/ECS/Entity/`、受影响 owner 文档、tasks/progress、项目 progress；运行 full grep gate、build/test、必要 Godot scene smoke。
  - **Validation**: `Tools/run-build.sh`、`Tools/run-tests.sh`、`python3 Workspace/SDD/sdd.py validate --all`，必要时 BrotatoLike Godot smoke。
