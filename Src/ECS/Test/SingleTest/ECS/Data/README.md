# RuntimeDataValidationScene

## expectedInputs

- Typed `DataKey<T>` set/get/remove operations.
- Inline typed runtime snapshot JSON samples.

## expectedObservations

- Typed DataKey access preserves values and category reset semantics.
- Numeric modifiers are accepted while string modifiers are rejected.
- Computed DataKey cache invalidates after dependency updates.
- Typed snapshot apply produces expected structured errors.

## passCriteria

- All scene assertions pass.
- Runner `result.json` reports `status=passed` and `exitCode=0`.
- `scene-artifact.json` reports `status=pass`.

## failCriteria

- Any Runtime Data assertion fails.
- Runner exits non-zero or times out.
- `scene-artifact.json` is missing or has empty oracle fields.

## artifactPath

- `scene-artifact.json` in `GODOT_SCENE_TEST_ARTIFACT_DIR`.
