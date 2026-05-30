# BDD

## Applicability

- **Required**: true
- **Reason**: This SDD changes CLI or workflow behavior.

## Scenarios

### Scenario: SystemAgent entry points show the latest information architecture

Given an AI opens `Workspace/SystemAgent/README.md` and `Workspace/SystemAgent/INDEX.md`
When it needs to route a medium or large SystemAgent task
Then it can identify the difference between Workflow, Capability, Role, Artifact, Gate and Subagent
And it does not need to infer those boundaries from historical design documents

### Scenario: Capability docs do not become wrapper skill sources

Given a reusable SystemAgent capability such as DesignDiscovery needs a durable body
When the information architecture is refreshed
Then the durable body has a SystemAgent fact-source location
And `.ai-config/skills/*` remains the wrapper or tool-facing source boundary
And generated skill copies are not edited by hand

### Scenario: Subagent remains an optional execution base

Given a workflow can benefit from independent search or review
When subagent policy is consulted
Then subagent usage defaults to read-only evidence gathering or independent review
And parallel write execution is not introduced without worktree and owner safeguards

### Scenario: Hook P0 has a stable documentation target

Given Hook and Gate reliability is implemented after this SDD
When hook smoke, gate input, or completion checklist rules need a long-term location
Then they can reference the refreshed SystemAgent directory map and catalog
And they do not create a second fact source outside `Workspace/SystemAgent`
