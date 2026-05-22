# AbilitySystemPipelineTest

## expectedInputs

- Manual `CastContext` instances with owner and ability entities.
- `CostComponent`, `FeatureHandler`, and movement helper test doubles.

## expectedObservations

- `AbilitySystem.TryTriggerAbility` returns `TriggerResult` directly.
- Cost checks and consumption run through component methods, not request-response events.
- Feature handlers receive `CastContext` through `FeatureContext.ActivationData`.
- Movement target helper regressions preserve expected behavior.

## passCriteria

- All scene assertions pass.
- Runner `result.json` reports `status=passed` and `exitCode=0`.
- `scene-artifact.json` reports `status=pass`.

## failCriteria

- Any Ability pipeline assertion fails.
- Runner exits non-zero or times out.
- `scene-artifact.json` is missing or has empty oracle fields.

## artifactPath

- `scene-artifact.json` in `GODOT_SCENE_TEST_ARTIFACT_DIR`.
