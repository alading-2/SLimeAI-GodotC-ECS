# BDD

## Applicability

- **Required**: true
- **Reason**: This SDD adds a SystemAgent skill that changes AI workflow behavior and Git worktree operation guidance.
- **Source**: `../../design/优化/Worktree激活.FeatureSpec.md`
- **Executed features**: FS-1, FS-2, FS-3, FS-4

## Scenarios

### Scenario: Explicit worktree request creates isolated path

Given a user asks to isolate a task in a worktree
When the target Git boundary is valid and `.worktrees/` is ignored
Then `systemagent-worktree` tells the AI to create `<repo>/.worktrees/<name>-<YYMMDD>` and to continue tool calls from that path.

### Scenario: Dirty workspace is preserved

Given the main workspace has pre-existing dirty files
When a worktree is created or suggested
Then the dirty baseline is recorded in SDD progress and is not cleaned, stashed, reset or overwritten.

### Scenario: Dirty worktree cleanup is blocked

Given a worktree has uncommitted changes
When the user asks to clean that worktree
Then the skill requires reporting `dirty-preserve` and forbids `git worktree remove`.
