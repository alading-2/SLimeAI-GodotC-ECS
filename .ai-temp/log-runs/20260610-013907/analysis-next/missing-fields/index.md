# Missing Fields

## Semantic Totals

| issue | count |
| --- | --- |
| fieldsEmpty | 1109 |
| operationEqualsContext | 1109 |
| flowMissingDurationMs | 3730 |
| missingReasonCode | 11 |
| missingEntityId | 937 |
| missingSource | 4914 |
| unknownOwner | 0 |
| unknownPhase | 0 |

## Owner Tasks

| owner | context | operation | count | issues | owner doc | recommended action |
| --- | --- | --- | --- | --- | --- | --- |
| TargetSelector | TargetQueryEngine | TargetQueryEntities | 3041 | flow_missing_durationMs:3041, missing_source:3041 | DocsAI/ECS/Tools/TargetSelector/README.md | add durationMs and query shape fields; aggregate successful queries and keep warning/failure details. |
| ObjectPool | ObjectPool | ObjectPoolAcquire | 271 | flow_missing_durationMs:271, missing_source:271 | DocsAI/ECS/Tools/ObjectPool/README.md | add durationMs/entryType and poolName-based batch summary; keep skipped/discarded reasonCode. |
| Runtime | HealthBarUI | HealthBarUI | 256 | fields_empty:256, operation_equals_context:256, missing_entityId:256, missing_source:256 | DocsAI/ECS/UI/Usage.md | add HealthBarBind operation and fields {entityId, entityType, poolName, outcome, reasonCode}; success path should be counted, not printed as three text lines. |
| ObjectPool | ObjectPool | ObjectPoolRelease | 179 | flow_missing_durationMs:179, missing_source:179 | DocsAI/ECS/Tools/ObjectPool/README.md | add durationMs/entryType and poolName-based batch summary; keep skipped/discarded reasonCode. |
| Entity | EntityManager | EntityManager | 171 | fields_empty:171, operation_equals_context:171, missing_entityId:171, missing_source:171 | DocsAI/ECS/Runtime/Entity/README.md | define stable operation and fields in the owner Log contract. |
| Runtime | AIComponent | AIComponent | 166 | fields_empty:166, operation_equals_context:166, missing_entityId:166, missing_source:166 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| TargetSelector | TargetQueryEngine | TargetQueryPositions | 123 | flow_missing_durationMs:123, missing_source:123 | DocsAI/ECS/Tools/TargetSelector/README.md | add durationMs and query shape fields; aggregate successful queries and keep warning/failure details. |
| System | SystemManager | SystemManager | 114 | fields_empty:114, operation_equals_context:114, missing_source:114 | DocsAI/ECS/Runtime/System/README.md | replace context-named text logs with stable operations such as SystemStartup, SystemPreflight or SystemDiagnosticsSnapshot. |
| Damage | DamageService | DamageProcess | 111 | flow_missing_durationMs:111, missing_entityId:111, missing_source:111 | DocsAI/ECS/Capabilities/Damage/README.md | add durationMs plus DamageInfo entity/source ids and processor chain digest. |
| Entity | EntitySpawnPipeline | EntitySpawnPipeline | 85 | fields_empty:85, operation_equals_context:85, missing_entityId:85, missing_source:85 | DocsAI/ECS/Runtime/Entity/README.md | define stable operation and fields in the owner Log contract. |
| Runtime | UIManager | UIManager | 85 | fields_empty:85, operation_equals_context:85, missing_source:85 | DocsAI/ECS/UI/Usage.md | define stable operation and fields in the owner Log contract. |
| Runtime | UnitAnimationComponent | UnitAnimationComponent | 84 | fields_empty:84, operation_equals_context:84, missing_entityId:84, missing_source:84 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| TargetSelector | TargetQueryEngine | SuppressedSummary | 75 | missing_source:75 | DocsAI/ECS/Tools/TargetSelector/README.md | add durationMs and query shape fields; aggregate successful queries and keep warning/failure details. |
| Entity | EntityManager_Component | EntityManager_Component | 27 | fields_empty:27, operation_equals_context:27, missing_entityId:27, missing_source:27, missing_reasonCode:11 | DocsAI/ECS/Runtime/Entity/README.md | define stable operation and fields in the owner Log contract. |
| Runtime | LifecycleComponent | LifecycleComponent | 15 | fields_empty:15, operation_equals_context:15, missing_entityId:15, missing_source:15 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| System | SystemConfigService | SystemConfigService | 15 | fields_empty:15, operation_equals_context:15, missing_source:15 | DocsAI/ECS/Runtime/System/README.md | replace context-named text logs with stable operations such as SystemStartup, SystemPreflight or SystemDiagnosticsSnapshot. |
| Runtime | MainTest | MainTest | 14 | fields_empty:14, operation_equals_context:14, missing_entityId:14, missing_source:14 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| Runtime | FeatureHandlerRegistry | FeatureHandlerRegistry | 12 | fields_empty:12, operation_equals_context:12, missing_source:12 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| Runtime | CooldownComponent | CooldownComponent | 10 | fields_empty:10, operation_equals_context:10, missing_source:10 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| Runtime | DashExecutor | DashExecutor | 10 | fields_empty:10, operation_equals_context:10, missing_source:10 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| ObjectPool | ObjectPool | ObjectPool | 8 | fields_empty:8, operation_equals_context:8, missing_source:8 | DocsAI/ECS/Tools/ObjectPool/README.md | add durationMs/entryType and poolName-based batch summary; keep skipped/discarded reasonCode. |
| Runtime | ActiveSkillBarUI | ActiveSkillBarUI | 6 | fields_empty:6, operation_equals_context:6, missing_source:6 | DocsAI/ECS/UI/Usage.md | define stable operation and fields in the owner Log contract. |
| Ability | AbilitySystem | AbilityTryTrigger | 5 | flow_missing_durationMs:5, missing_entityId:5, missing_source:5 | DocsAI/ECS/Capabilities/Ability/README.md | replace context-named text logs with stable operations such as SystemStartup, SystemPreflight or SystemDiagnosticsSnapshot. |
| Runtime | EffectComponent | EffectComponent | 5 | fields_empty:5, operation_equals_context:5, missing_source:5 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| Runtime | EffectTool | EffectTool | 5 | fields_empty:5, operation_equals_context:5, missing_source:5 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| Runtime | EventBus | EventBus | 5 | fields_empty:5, operation_equals_context:5, missing_source:5 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| System | RecoverySystem | RecoverySystem | 3 | fields_empty:3, operation_equals_context:3, missing_source:3 | DocsAI/ECS/Runtime/System/README.md | replace context-named text logs with stable operations such as SystemStartup, SystemPreflight or SystemDiagnosticsSnapshot. |
| System | SpawnSystem | SpawnSystem | 2 | fields_empty:2, operation_equals_context:2, missing_entityId:2, missing_source:2 | DocsAI/ECS/Runtime/System/README.md | replace context-named text logs with stable operations such as SystemStartup, SystemPreflight or SystemDiagnosticsSnapshot. |
| System | SystemPresetService | SystemPresetService | 2 | fields_empty:2, operation_equals_context:2, missing_source:2 | DocsAI/ECS/Runtime/System/README.md | replace context-named text logs with stable operations such as SystemStartup, SystemPreflight or SystemDiagnosticsSnapshot. |
| Damage | DamageNumberSystem | DamageNumberSystem | 1 | fields_empty:1, operation_equals_context:1, missing_entityId:1, missing_source:1 | DocsAI/ECS/Capabilities/Damage/README.md | add durationMs plus DamageInfo entity/source ids and processor chain digest. |
| ObjectPool | ObjectPoolInit | ObjectPoolInit | 1 | fields_empty:1, operation_equals_context:1, missing_source:1 | DocsAI/ECS/Tools/ObjectPool/README.md | add durationMs/entryType and poolName-based batch summary; keep skipped/discarded reasonCode. |
| Runtime | ActiveSkillInputComponent | ActiveSkillInputComponent | 1 | fields_empty:1, operation_equals_context:1, missing_source:1 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| Runtime | RecoveryComponent | RecoveryComponent | 1 | fields_empty:1, operation_equals_context:1, missing_source:1 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| Runtime | TriggerComponent | TriggerComponent | 1 | fields_empty:1, operation_equals_context:1, missing_source:1 | DocsAI/ECS/Runtime/README.md | define stable operation and fields in the owner Log contract. |
| System | FeatureSystem | FeatureSystem | 1 | fields_empty:1, operation_equals_context:1, missing_source:1 | DocsAI/ECS/Runtime/System/README.md | replace context-named text logs with stable operations such as SystemStartup, SystemPreflight or SystemDiagnosticsSnapshot. |
| System | PauseMenuSystem | PauseMenuSystem | 1 | fields_empty:1, operation_equals_context:1, missing_source:1 | DocsAI/ECS/Runtime/System/README.md | replace context-named text logs with stable operations such as SystemStartup, SystemPreflight or SystemDiagnosticsSnapshot. |
| System | TestSystem | TestSystem | 1 | fields_empty:1, operation_equals_context:1, missing_source:1 | DocsAI/ECS/Runtime/System/README.md | replace context-named text logs with stable operations such as SystemStartup, SystemPreflight or SystemDiagnosticsSnapshot. |
| TargetSelector | TargetingManager | TargetingManager | 1 | fields_empty:1, operation_equals_context:1, missing_source:1 | DocsAI/ECS/Tools/TargetSelector/README.md | add durationMs and query shape fields; aggregate successful queries and keep warning/failure details. |

## Envelope Missing

| source | line | missing |
| --- | --- | --- |
| .ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl | 4915 | runElapsedMs, frame, physicsFrame |
