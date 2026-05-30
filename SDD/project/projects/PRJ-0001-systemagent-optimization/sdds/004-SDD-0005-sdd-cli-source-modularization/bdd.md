# BDD

## Applicability

- **Required**: true
- **Reason**: This SDD changes CLI or workflow behavior.

## Scenarios

### Scenario: Run SDD CLI through the stable entrypoint

Given SDD CLI implementation has been split into `Workspace/SDD/Src/`
When a user runs `python3 Workspace/SDD/sdd.py doctor`
Then the command still reports the stable `Workspace/SDD/sdd.py` CLI path and validates the existing SDD root

### Scenario: Keep entrypoint small

Given future SDD CLI behavior is added
When tests parse `Workspace/SDD/sdd.py`
Then only `build_parser` and `main` may be top-level functions in the entrypoint
