# BDD

## Applicability

- **Required**: true
- **Reason**: This SDD changes SDD CLI behavior, directory semantics and validation rules.

## Scenarios

### Scenario: Project SDD container owns shared design

Given a project is created with `project-new`
When a child SDD is created with `new --project PRJ-0001`
Then the child SDD is placed under `SDD/project/projects/<project>/sdds/`
And the child SDD records `project_id`, `project_order` and shared design references.

### Scenario: Status is metadata-driven

Given an SDD exists in a legacy or project directory
When `start`, `block` or `done` is executed
Then `sdd.json.status` is updated
And the SDD directory is not moved between status folders.

### Scenario: Project archive is explicit

Given a project is complete
When `project-archive PRJ-0001` is executed
Then the project moves from `SDD/project/projects/` to `SDD/project/archived/`
And `project.json.status` remains the source of truth.
