# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 改变 AI 主入口、rules 同步语义、SDD/Workspace 根路径和 skill 路由，必须用行为场景约束。

## Scenarios

### Scenario: AI opens SlimeAI as the primary workspace

Given an AI starts in `/home/slime/Code/SlimeAI/SlimeAI`
When it reads `AGENTS.md`
Then the default route points to `DocsNew/README.md`, `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`, `Src/ECS/**` side docs, owner skill, and local validation scripts
And it does not treat `/home/slime/Code/SlimeAI` as the current AI config root.

### Scenario: Rules are generated from the new source

Given `.ai-config/rules/rules.md` is the rule source in the framework repo
When `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` runs from `/home/slime/Code/SlimeAI/SlimeAI`
Then `AGENTS.md`, `CLAUDE.md`, and `.windsurf/rules/windsurfrules.md` are regenerated under `/home/slime/Code/SlimeAI/SlimeAI`
And no rule output is generated into the outer workspace root.

### Scenario: Deleted DocsAI is not restored as current fact source

Given `DocsAI/` is a deleted legacy entry
When rules, current skills, DocsNew, or SDD templates reference framework docs
Then current references point to `DocsNew/`, `SDD/`, or `Src/ECS/**`
And `DocsAI/` only appears in historical notes, archives, or explicitly marked legacy references.

### Scenario: SDD CLI uses the framework repo root

Given `Workspace/SDD/sdd.py` is inside the framework repo
When `python3 Workspace/SDD/sdd.py validate --all` runs
Then the default SDD root is `SDD/` under `/home/slime/Code/SlimeAI/SlimeAI`
And new SDD templates use Git Boundary `/home/slime/Code/SlimeAI/SlimeAI`.

### Scenario: High-risk skill paths are current

Given an owner skill is used for AI config, project index, ECS Data, or Godot scene testing
When the skill is read from `.ai-config/skills/**/SKILL.md`
Then it should not point to old `.ai-config` absolute paths, deleted `SlimeAI/DocsAI`, or framework-internal `SlimeAI/Src/ECS` double paths.
