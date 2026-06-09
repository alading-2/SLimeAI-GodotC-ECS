# Log Analysis Context

- runDir: .ai-temp/logctl-sample-run
- resultSource: artifact
- status: passed
- entries: 2
- validationEntries: 1
- artifacts: 1
- owners: Ability, Damage

## Failure Focus

- none

## Query Examples

```bash
logctl query --analysis-dir <run>/analysis owner=Ability
logctl query --analysis-dir <run>/analysis severity>=Warn
```
