# Log Run Summary
## Gate
- status: no-failure-observed
- confidence: low
- resultSource: structured-log
- entries: 4915
- invalidJsonl: 1
- validationEntries: 0
- artifacts: 0
- structuredFailures: 0
> No Validation channel pass/fail or artifact was found. This means no structured failure was observed; it is not a behavior pass.

## Gate Warnings

- invalid-jsonl: raw JSONL contains invalid or truncated lines; inspect raw/entries.jsonl diagnostics before trusting exact counts. (1)
- no-validation-evidence: no Validation channel pass/fail and no artifact were found; this run cannot be reported as passed.
## Top Owner
| owner | count |
| --- | --- |
| TargetSelector | 3240 |
| Runtime | 676 |
| ObjectPool | 459 |
| Entity | 283 |
| System | 139 |
## Top Operation
| owner/context/operation | count |
| --- | --- |
| TargetSelector/TargetQueryEngine/TargetQueryEntities | 3041 |
| ObjectPool/ObjectPool/ObjectPoolAcquire | 271 |
| Runtime/HealthBarUI/HealthBarUI | 256 |
| ObjectPool/ObjectPool/ObjectPoolRelease | 179 |
| Entity/EntityManager/EntityManager | 171 |
## Top Phase
| phase | count |
| --- | --- |
| Targeting | 3164 |
| Runtime | 1559 |
| Combat | 111 |
| Diagnostics | 75 |
| Ability | 5 |
## Top Noise
| owner | context | operation | count | recommended action |
| --- | --- | --- | --- | --- |
| TargetSelector | TargetQueryEngine | TargetQueryEntities | 3041 | aggregate TargetQuery success path by window; keep warning/failure query details and include query shape, candidate counts, returned counts, truncated and reasonCode. |
| ObjectPool | ObjectPool | ObjectPoolAcquire | 271 | sample or budget ObjectPool acquire success by poolName; keep capacity and lifecycle anomalies expanded. |
| Runtime | HealthBarUI | HealthBarUI | 256 | replace repeated bind text with one HealthBarBind summary carrying entityId, entityType, outcome and reasonCode. |
| ObjectPool | ObjectPool | ObjectPoolRelease | 179 | batch ObjectPool release success by poolName; keep skipped/discarded releases as structured details. |
| Entity | EntityManager | EntityManager | 171 | consider budget/sample or summary-only stdout; add owner field contract before adding more logs. |
## Semantic Missing Fields
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
## Read Next
- ai-context.md
- noise/top-contexts.md
- missing-fields/index.md
- flows/index.md
- failures/index.md
Do not read raw/entries.jsonl by default. Use logctl query first when these digest files are insufficient.
