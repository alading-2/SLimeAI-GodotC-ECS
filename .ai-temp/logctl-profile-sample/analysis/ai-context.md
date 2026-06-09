# Log Analysis Context

- runDir: .ai-temp/logctl-profile-sample
- resultSource: structured-log
- status: passed
- entries: 11
- validationEntries: 0
- artifacts: 0
- owners: Tools.Logger

## Failure Focus

- none

## Query Examples

```bash
logctl query --analysis-dir <run>/analysis owner=Ability
logctl query --analysis-dir <run>/analysis severity>=Warn
```
