# AI Context

## Gate Interpretation

- status: no-failure-observed
- confidence: low
- resultSource: structured-log
- This sample has structured logs but no Validation entries and no artifacts, so it can only be treated as no-failure-observed.

## Failure Focus

- No structured failure entries were found.

## Top Noise And Actions

| owner | context | operation | count | action |
| --- | --- | --- | --- | --- |
| TargetSelector | TargetQueryEngine | TargetQueryEntities | 3041 | aggregate TargetQuery success path by window; keep warning/failure query details and include query shape, candidate counts, returned counts, truncated and reasonCode. |
| ObjectPool | ObjectPool | ObjectPoolAcquire | 271 | sample or budget ObjectPool acquire success by poolName; keep capacity and lifecycle anomalies expanded. |
| Runtime | HealthBarUI | HealthBarUI | 256 | replace repeated bind text with one HealthBarBind summary carrying entityId, entityType, outcome and reasonCode. |
| ObjectPool | ObjectPool | ObjectPoolRelease | 179 | batch ObjectPool release success by poolName; keep skipped/discarded releases as structured details. |
| Entity | EntityManager | EntityManager | 171 | consider budget/sample or summary-only stdout; add owner field contract before adding more logs. |
| Runtime | AIComponent | AIComponent | 166 | consider budget/sample or summary-only stdout; add owner field contract before adding more logs. |
| TargetSelector | TargetQueryEngine | TargetQueryPositions | 123 | aggregate TargetQuery success path by window; keep warning/failure query details and include query shape, candidate counts, returned counts, truncated and reasonCode. |
| System | SystemManager | SystemManager | 114 | emit one System diagnostics snapshot instead of repeated line-level status logs. |

## Flow Digest

| owner | context | operation | entries | completed | failed | missingDurationMs |
| --- | --- | --- | --- | --- | --- | --- |
| TargetSelector | TargetQueryEngine | TargetQueryEntities | 3041 | 3041 | 0 | 3041 |
| ObjectPool | ObjectPool | ObjectPoolAcquire | 271 | 271 | 0 | 271 |
| ObjectPool | ObjectPool | ObjectPoolRelease | 179 | 179 | 0 | 179 |
| TargetSelector | TargetQueryEngine | TargetQueryPositions | 123 | 123 | 0 | 123 |
| Damage | DamageService | DamageProcess | 111 | 111 | 0 | 111 |
| Ability | AbilitySystem | AbilityTryTrigger | 5 | 5 | 0 | 5 |

## Semantic Missing Fields

| owner | context | operation | count | issues | action |
| --- | --- | --- | --- | --- | --- |
| TargetSelector | TargetQueryEngine | TargetQueryEntities | 3041 | flow_missing_durationMs:3041, missing_source:3041 | add durationMs and query shape fields; aggregate successful queries and keep warning/failure details. |
| ObjectPool | ObjectPool | ObjectPoolAcquire | 271 | flow_missing_durationMs:271, missing_source:271 | add durationMs/entryType and poolName-based batch summary; keep skipped/discarded reasonCode. |
| Runtime | HealthBarUI | HealthBarUI | 256 | fields_empty:256, operation_equals_context:256, missing_entityId:256, missing_source:256 | add HealthBarBind operation and fields {entityId, entityType, poolName, outcome, reasonCode}; success path should be counted, not printed as three text lines. |
| ObjectPool | ObjectPool | ObjectPoolRelease | 179 | flow_missing_durationMs:179, missing_source:179 | add durationMs/entryType and poolName-based batch summary; keep skipped/discarded reasonCode. |
| Entity | EntityManager | EntityManager | 171 | fields_empty:171, operation_equals_context:171, missing_entityId:171, missing_source:171 | define stable operation and fields in the owner Log contract. |
| Runtime | AIComponent | AIComponent | 166 | fields_empty:166, operation_equals_context:166, missing_entityId:166, missing_source:166 | define stable operation and fields in the owner Log contract. |
| TargetSelector | TargetQueryEngine | TargetQueryPositions | 123 | flow_missing_durationMs:123, missing_source:123 | add durationMs and query shape fields; aggregate successful queries and keep warning/failure details. |
| System | SystemManager | SystemManager | 114 | fields_empty:114, operation_equals_context:114, missing_source:114 | replace context-named text logs with stable operations such as SystemStartup, SystemPreflight or SystemDiagnosticsSnapshot. |
| Damage | DamageService | DamageProcess | 111 | flow_missing_durationMs:111, missing_entityId:111, missing_source:111 | add durationMs plus DamageInfo entity/source ids and processor chain digest. |
| Entity | EntitySpawnPipeline | EntitySpawnPipeline | 85 | fields_empty:85, operation_equals_context:85, missing_entityId:85, missing_source:85 | define stable operation and fields in the owner Log contract. |
| Runtime | UIManager | UIManager | 85 | fields_empty:85, operation_equals_context:85, missing_source:85 | define stable operation and fields in the owner Log contract. |
| Runtime | UnitAnimationComponent | UnitAnimationComponent | 84 | fields_empty:84, operation_equals_context:84, missing_entityId:84, missing_source:84 | define stable operation and fields in the owner Log contract. |

## Owner Docs

- DocsAI/ECS/Runtime/Entity/README.md
- DocsAI/ECS/Runtime/README.md
- DocsAI/ECS/Runtime/System/README.md
- DocsAI/ECS/Tools/ObjectPool/README.md
- DocsAI/ECS/Tools/TargetSelector/README.md
- DocsAI/ECS/UI/Usage.md

## Next Queries

```bash
Workspace/Tools/logctl/logctl query --analysis-dir <analysis> owner=TargetSelector operation=TargetQueryEntities --format md
Workspace/Tools/logctl/logctl query --analysis-dir <analysis> owner=ObjectPool operation=ObjectPoolRelease --format md
Workspace/Tools/logctl/logctl query --analysis-dir <analysis> owner=Runtime operation=HealthBarUI --format md
Workspace/Tools/logctl/logctl query --analysis-dir <analysis> severity>=Warn --format md
```

## Profile Override Candidates

- Prefer owner/context/operation budget rules over raising the global level.
- Keep Validation and structured failures unbudgeted.
- Use logctl suggest --run-dir <run> --dry-run for a reviewable profilePatch.
