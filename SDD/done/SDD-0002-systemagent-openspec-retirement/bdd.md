# BDD

## Applicability

- **Required**: true
- **Reason**: This SDD changes CLI or workflow behavior.

## Scenarios

### Scenario: New medium task uses SDD by default

Given a user requests a medium or large SystemAgent task
When the AI follows the workspace rules and SystemAgent entry documents
Then the AI should route to `sdd-workflow` / `sdd-management`
And it should maintain `SDD/active/<sdd>/tasks.md` and `progress.md`
And it should not route to OpenSpec unless the user explicitly requests historical compatibility work

### Scenario: OpenSpec remains explicit compatibility only

Given `openspec/` and `.ai-config/skills/openspec/` still exist
When SystemAgent catalogs and skills are inspected
Then OpenSpec entries are labeled as legacy compatibility
And SDD entries remain the default workflow entry

### Scenario: Validation evidence is recoverable

Given this SDD is resumed by another AI session
When the AI reads `README.md`, `tasks.md`, `progress.md` and `design/main.md`
Then it can identify completed changes, remaining compatibility boundaries and validation commands
