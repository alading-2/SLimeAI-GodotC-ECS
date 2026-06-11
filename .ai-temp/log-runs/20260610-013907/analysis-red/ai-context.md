# Log Analysis Context

- runDir: .ai-temp/log-runs/20260610-013907
- resultSource: structured-log
- status: passed
- entries: 19660
- validationEntries: 0
- artifacts: 0
- owners: Ability, Damage, Entity, ObjectPool, Runtime, System, TargetSelector, unknown

## Failure Focus

- none

## Query Examples

```bash
logctl query --analysis-dir <run>/analysis owner=Ability
logctl query --analysis-dir <run>/analysis severity>=Warn
```
