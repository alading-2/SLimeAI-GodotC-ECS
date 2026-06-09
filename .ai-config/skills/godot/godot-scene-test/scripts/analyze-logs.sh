#!/usr/bin/env bash
set -euo pipefail

base_dir=".ai-temp/scene-tests/runs"
target_dir=""
manifest_path=""
gate_report=""

usage() {
    cat >&2 <<'USAGE'
Usage:
  analyze-godot-scene-logs.sh [--run-dir <path>] [--base-dir <path>] [--manifest <path>] [--gate-report <path>]

Reads the latest run under .ai-temp/scene-tests/runs by default and prints
a compact PASS/FAIL/error summary. New structured runs use index.json,
emit a durable gate-report.json, and legacy flat run directories are still
summarized for compatibility.
USAGE
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --run-dir)
            target_dir="${2:-}"
            if [ -z "$target_dir" ]; then
                echo "--run-dir requires a path." >&2
                exit 2
            fi
            shift 2
            ;;
        --manifest)
            manifest_path="${2:-}"
            if [ -z "$manifest_path" ]; then
                echo "--manifest requires a path." >&2
                exit 2
            fi
            shift 2
            ;;
        --gate-report)
            gate_report="${2:-}"
            if [ -z "$gate_report" ]; then
                echo "--gate-report requires a path." >&2
                exit 2
            fi
            shift 2
            ;;
        --base-dir)
            base_dir="${2:-}"
            if [ -z "$base_dir" ]; then
                echo "--base-dir requires a path." >&2
                exit 2
            fi
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            usage
            exit 2
            ;;
    esac
done

if [ -z "$target_dir" ]; then
    if [ ! -d "$base_dir" ]; then
        echo "No scene test log directory found: $base_dir" >&2
        exit 1
    fi

    target_dir="$(find "$base_dir" -mindepth 2 -maxdepth 2 -type d | sort | tail -n 1)"
    if [ -z "$target_dir" ]; then
        echo "No scene test runs found under: $base_dir" >&2
        exit 1
    fi
fi

if [ ! -d "$target_dir" ]; then
    echo "Run directory not found: $target_dir" >&2
    exit 1
fi

echo "Scene test run: $target_dir"

resolve_logctl() {
    local candidates=()
    if [ -n "${LOGCTL:-}" ]; then
        candidates+=("$LOGCTL")
    fi
    candidates+=(
        "Workspace/Tools/logctl/logctl"
        "Workspace/Tools/logctl/logctl.mjs"
        "SlimeAI/Workspace/Tools/logctl/logctl"
        "SlimeAI/Workspace/Tools/logctl/logctl.mjs"
    )

    local candidate
    for candidate in "${candidates[@]}"; do
        if [ -f "$candidate" ]; then
            printf '%s\n' "$candidate"
            return 0
        fi
    done

    return 1
}

if [ -f "$target_dir/analysis/gate-report.json" ]; then
    echo "Format: logctl analysis"
    jq -r '"Status: \(.status) resultSource=\(.resultSource) entries=\(.counts.entries) artifacts=\(.counts.artifacts) failures=\(.failures | length)"' "$target_dir/analysis/gate-report.json"
    if [ -f "$target_dir/analysis/ai-context.md" ]; then
        echo
        sed -n '1,80p' "$target_dir/analysis/ai-context.md"
    fi
    exit 0
fi

if logctl_path="$(resolve_logctl)"; then
    if [ "$logctl_path" = "${logctl_path%.mjs}" ]; then
        "$logctl_path" analyze --run-dir "$target_dir" --out "$target_dir/analysis" >/dev/null || true
    else
        node "$logctl_path" analyze --run-dir "$target_dir" --out "$target_dir/analysis" >/dev/null || true
    fi
    if [ -f "$target_dir/analysis/gate-report.json" ]; then
        echo "Format: logctl analysis"
        jq -r '"Status: \(.status) resultSource=\(.resultSource) entries=\(.counts.entries) artifacts=\(.counts.artifacts) failures=\(.failures | length)"' "$target_dir/analysis/gate-report.json"
        if [ -f "$target_dir/analysis/ai-context.md" ]; then
            echo
            sed -n '1,80p' "$target_dir/analysis/ai-context.md"
        fi
        exit 0
    fi
fi

write_gate_report() {
    local run_dir="$1"
    local manifest="$2"
    local report="$3"

    node - "$run_dir" "$manifest" "$report" <<'NODE'
const fs = require("fs");
const path = require("path");

const [runDirArg, manifestArg, reportArg] = process.argv.slice(2);
const cwd = process.cwd();
const runDir = path.resolve(cwd, runDirArg);
const indexPath = path.join(runDir, "index.json");
const index = JSON.parse(fs.readFileSync(indexPath, "utf8"));
const requiredFields = ["expectedInputs", "expectedObservations", "passCriteria", "failCriteria", "artifactPath"];

function rel(filePath) {
  return path.relative(cwd, filePath).split(path.sep).join("/");
}

function exists(filePath) {
  return Boolean(filePath) && fs.existsSync(path.resolve(cwd, filePath));
}

function readJsonMaybe(filePath) {
  if (!exists(filePath)) return null;
  try {
    const text = fs.readFileSync(path.resolve(cwd, filePath), "utf8").replace(/^\uFEFF/, "");
    return JSON.parse(text);
  } catch {
    return null;
  }
}

function nonEmpty(value) {
  if (Array.isArray(value)) return value.length > 0;
  if (typeof value === "string") return value.trim().length > 0;
  return value !== null && value !== undefined;
}

function normalizeStatus(value) {
  return String(value ?? "").trim().toLowerCase();
}

function resToLocal(scene) {
  return scene.startsWith("res://") ? scene.slice("res://".length) : scene;
}

function sceneReadmePath(scene) {
  return path.join(path.dirname(resToLocal(scene)), "README.md").split(path.sep).join("/");
}

function catalogPathFor(entry) {
  if (!entry) return "";
  if (entry.catalogOwner === "framework") {
    return "Src/Validation/ValidationCatalog.md";
  }
  if (entry.catalogOwner === "game") {
    return "DocsAI/ValidationCatalog.md";
  }
  return "";
}

function mtimeMs(filePath) {
  if (!filePath) return null;
  const absolute = path.resolve(cwd, filePath);
  if (!fs.existsSync(absolute)) return null;
  return fs.statSync(absolute).mtimeMs;
}

function loadManifest() {
  const metadataManifest = index.metadata?.manifest?.path;
  const manifestPath = manifestArg || metadataManifest || "";
  if (!manifestPath) return { path: "", entries: [], selectedEntries: [] };
  const absolute = path.resolve(cwd, manifestPath);
  if (!fs.existsSync(absolute)) return { path: manifestPath, entries: [], selectedEntries: [] };
  const data = JSON.parse(fs.readFileSync(absolute, "utf8"));
  const entries = Array.isArray(data.scenes) ? data.scenes : [];
  const metadata = index.metadata?.manifest ?? {};
  const selectedSet = new Set(index.metadata?.scenes ?? []);
  const selectedEntries = entries.filter((entry) => {
    if (selectedSet.has(entry.scene)) return true;
    if (metadata.releaseBatch && entry.releaseBatch !== true) return false;
    if (Array.isArray(metadata.tags) && metadata.tags.length > 0) {
      const tags = new Set((entry.tags ?? []).map((tag) => String(tag).toLowerCase()));
      if (!metadata.tags.every((tag) => tags.has(String(tag).toLowerCase()))) return false;
    }
    if (Array.isArray(metadata.scopes) && metadata.scopes.length > 0) {
      const scopes = metadata.scopes.map((scope) => String(scope).toLowerCase());
      if (!scopes.includes(String(entry.scope ?? "").toLowerCase())) return false;
    }
    if (metadata.filter && !String(entry.scene).toLowerCase().includes(String(metadata.filter).toLowerCase())) return false;
    return Boolean(metadata.releaseBatch || metadata.tags?.length || metadata.scopes?.length || metadata.filter);
  });
  return { path: rel(absolute), entries, selectedEntries };
}

function findArtifact(result, manifestEntry) {
  const files = result?.artifacts?.files ?? [];
  const candidates = files.filter((file) => file.endsWith(".json") && !file.includes("/logs/"));
  const preferred = manifestEntry?.artifactFilename
    ? candidates.find((file) => file.endsWith(`/${manifestEntry.artifactFilename}`) || file.endsWith(manifestEntry.artifactFilename))
    : null;
  for (const file of [preferred, ...candidates].filter(Boolean)) {
    const parsed = readJsonMaybe(file);
    if (parsed && Object.prototype.hasOwnProperty.call(parsed, "status")) {
      return { path: file, data: parsed };
    }
  }
  return { path: "", data: null };
}

function readmeFieldStatus(readmePath) {
  const absolute = path.resolve(cwd, readmePath);
  if (!fs.existsSync(absolute)) {
    return Object.fromEntries(requiredFields.map((field) => [field, false]));
  }
  const text = fs.readFileSync(absolute, "utf8");
  return Object.fromEntries(requiredFields.map((field) => [field, text.includes(field)]));
}

function catalogContains(catalogPath, scene) {
  if (!catalogPath) return false;
  const absolute = path.resolve(cwd, catalogPath);
  if (!fs.existsSync(absolute)) return false;
  return fs.readFileSync(absolute, "utf8").includes(scene);
}

const manifest = loadManifest();
const manifestByScene = new Map(manifest.entries.map((entry) => [entry.scene, entry]));
const selectedRequired = manifest.selectedEntries.length > 0 ? manifest.selectedEntries : [];
const entries = index.entries ?? [];
const entriesByScene = new Map(entries.map((entry) => [entry.scene, entry]));
const requestedScenes = selectedRequired.length > 0
  ? selectedRequired.map((entry) => entry.scene)
  : (index.metadata?.scenes ?? entries.map((entry) => entry.scene));
const missingScenes = requestedScenes.filter((scene) => !entriesByScene.has(scene));
const sceneReports = [];
let hasBlock = missingScenes.length > 0;
let hasWarn = false;

for (const scene of requestedScenes) {
  const entry = entriesByScene.get(scene);
  const manifestEntry = manifestByScene.get(scene);
  const resultPath = entry?.logFiles?.result ?? "";
  const result = readJsonMaybe(resultPath);
  const artifact = findArtifact(result, manifestEntry);
  const readmePath = sceneReadmePath(scene);
  const catalogPath = catalogPathFor(manifestEntry);
  const requiresSceneGate = Boolean(manifestEntry) || scene.includes("/Validation/");
  const readmeFields = readmeFieldStatus(readmePath);
  const artifactFields = artifact.data
    ? Object.fromEntries(requiredFields.map((field) => [field, nonEmpty(artifact.data[field])]))
    : Object.fromEntries(requiredFields.map((field) => [field, false]));
  const requiredChecks = manifestEntry?.checks ?? [];
  const artifactChecks = Array.isArray(artifact.data?.checks)
    ? artifact.data.checks.map((check) => check.name).filter(Boolean)
    : [];
  const missingChecks = requiredChecks.filter((check) => !artifactChecks.includes(check));
  const freshnessInputs = [readmePath, catalogPath, manifest.path].filter(Boolean);
  const artifactMtime = mtimeMs(artifact.path);
  const staleAgainst = artifactMtime === null
    ? []
    : freshnessInputs.filter((file) => {
        const value = mtimeMs(file);
        return value !== null && value > artifactMtime;
      });
  const catalogPresent = catalogPath ? catalogContains(catalogPath, scene) : false;
  const resultPassed = result?.status === "passed" && result?.exitCode === 0 && !result?.firstError;
  const indexPassed = entry?.status === "passed" && entry?.exitCode === 0;
  const artifactStatus = normalizeStatus(artifact.data?.status);
  const artifactPassed = artifactStatus === "pass" && Array.isArray(artifact.data?.failureReasons) && artifact.data.failureReasons.length === 0;
  const readmeComplete = Object.values(readmeFields).every(Boolean);
  const artifactComplete = Object.values(artifactFields).every(Boolean);
  let status = "pass";

  if (!entry || !indexPassed || !resultPassed || !artifactPassed || !artifactComplete || (requiresSceneGate && !readmeComplete) || missingChecks.length > 0) {
    status = "block";
    hasBlock = true;
  } else if ((requiresSceneGate && catalogPath && !catalogPresent) || staleAgainst.length > 0) {
    status = "warn";
    hasWarn = true;
  }

  sceneReports.push({
    scene,
    status,
    scope: manifestEntry?.scope ?? null,
    releaseBatch: manifestEntry?.releaseBatch ?? null,
    evidence: {
      indexJson: rel(indexPath),
      resultJson: resultPath,
      artifactJson: artifact.path,
      jsonlLog: (result?.artifacts?.files ?? []).find((file) => file.endsWith("scene-log.jsonl")) ?? "",
    },
    index: {
      status: entry?.status ?? "missing",
      exitCode: entry?.exitCode ?? null,
      firstError: entry?.firstError ?? null,
    },
    result: {
      status: result?.status ?? "missing",
      exitCode: result?.exitCode ?? null,
      firstError: result?.firstError ?? null,
    },
    artifact: {
      status: artifact.data?.status ?? "missing",
      failureReasons: Array.isArray(artifact.data?.failureReasons) ? artifact.data.failureReasons.length : null,
      fields: artifactFields,
      checks: artifactChecks,
      missingChecks,
    },
    readme: {
      path: readmePath,
      fields: readmeFields,
    },
    catalog: {
      path: catalogPath,
      present: catalogPresent,
    },
    freshness: {
      artifactMtime: artifactMtime === null ? null : new Date(artifactMtime).toISOString(),
      staleAgainst,
    },
  });
}

const failedScenes = sceneReports.filter((scene) => scene.index.status === "failed" || scene.result.status === "failed" || normalizeStatus(scene.artifact.status) === "fail").map((scene) => scene.scene);
const passedScenes = sceneReports.filter((scene) => scene.status === "pass").map((scene) => scene.scene);
const skippedScenes = index.skippedScenes ?? [];
const verdict = hasBlock ? "block" : hasWarn ? "warn" : "pass";
const actionItems = [];

for (const scene of missingScenes) actionItems.push(`Run missing required scene: ${scene}`);
for (const report of sceneReports) {
  if (report.status === "block") {
    if (Object.values(report.readme.fields).some((value) => !value)) actionItems.push(`Fix README standard-answer fields: ${report.readme.path}`);
    if (Object.values(report.artifact.fields).some((value) => !value)) actionItems.push(`Rerun or fix artifact standard-answer fields: ${report.scene}`);
    if (report.artifact.missingChecks.length > 0) actionItems.push(`Fix artifact check coverage for ${report.scene}: ${report.artifact.missingChecks.join(", ")}`);
  }
  if (report.status === "warn") {
    if (!report.catalog.present) actionItems.push(`Sync validation catalog for ${report.scene}`);
    if (report.freshness.staleAgainst.length > 0) actionItems.push(`Rerun stale scene evidence for ${report.scene}`);
  }
}

const output = {
  generatedAt: new Date().toISOString(),
  runDir: rel(runDir),
  manifestPath: manifest.path,
  verdict,
  summary: {
    requested: requestedScenes.length,
    passed: passedScenes.length,
    failed: failedScenes.length,
    skipped: skippedScenes.length,
    missing: missingScenes.length,
  },
  requestedScenes,
  passedScenes,
  failedScenes,
  skippedScenes,
  missingScenes,
  scenes: sceneReports,
  actionItems: [...new Set(actionItems)],
};

const reportPath = reportArg || path.join(runDir, "gate-report.json");
fs.mkdirSync(path.dirname(path.resolve(cwd, reportPath)), { recursive: true });
fs.writeFileSync(path.resolve(cwd, reportPath), `${JSON.stringify(output, null, 2)}\n`, "utf8");
console.log(`Gate report: ${rel(path.resolve(cwd, reportPath))}`);
console.log(`Gate verdict: ${verdict}`);
NODE
}

normalize_status() {
    local value="$1"
    printf '%s\n' "$value" | tr '[:upper:]' '[:lower:]'
}

artifact_status_for_result() {
    local result_file="$1"
    jq -r '
      (.artifacts.files // [])
      | map(select(endswith(".json") and (endswith("scene-log.jsonl") | not)))
      | .[]
    ' "$result_file" 2>/dev/null | while IFS= read -r artifact_file; do
        if [ -f "$artifact_file" ]; then
            jq -r '.status // empty' "$artifact_file" 2>/dev/null || true
        fi
    done | head -n 1
}

marker_status_for_combined() {
    local combined_file="$1"
    if [ ! -f "$combined_file" ]; then
        printf 'unknown\n'
        return
    fi

    if rg -q "\\[FAIL\\]| playable slice FAIL| validation FAIL|GameOS smoke FAIL|BrotatoLike GameOS smoke FAIL" "$combined_file"; then
        printf 'fail\n'
    elif rg -q "\\[PASS\\]| playable slice PASS| validation PASS|BrotatoLike GameOS smoke PASS" "$combined_file"; then
        printf 'pass\n'
    else
        printf 'unknown\n'
    fi
}

stdout_pattern_fallback_for_combined() {
    local combined_file="$1"
    if [ ! -f "$combined_file" ]; then
        return
    fi

    # legacy stdout-pattern-fallback only; logctl gate report is the primary source.
    rg -n -m 1 "ERROR:|\\[ERROR\\]|\\[FAIL\\]|FAIL:|Exception|Cannot instantiate|Failed to load|scene not found" "$combined_file" || true
}

if [ -f "$target_dir/index.json" ]; then
    echo "Format: structured index.json"
    echo
    if [ -z "$gate_report" ]; then
        gate_report="$target_dir/gate-report.json"
    fi

    failed=0
    while IFS= read -r entry; do
        scene="$(jq -r '.scene' <<<"$entry")"
        attempt="$(jq -r '.attempt' <<<"$entry")"
        result_file="$(jq -r '.logFiles.result // empty' <<<"$entry")"
        combined_file="$(jq -r '.logFiles.combined // empty' <<<"$entry")"
        exit_code="$(jq -r '.exitCode // empty' <<<"$entry")"
        first_error="$(jq -r '.firstError // empty' <<<"$entry")"
        failure_reason="$(jq -r '.failureReason // empty' <<<"$entry")"
        artifact_dir="$(jq -r '.artifactDirs.artifacts // empty' <<<"$entry")"
        artifact_status=""
        jsonl_count=0

        if [ -n "$result_file" ] && [ -f "$result_file" ]; then
            artifact_status="$(artifact_status_for_result "$result_file" | head -n 1)"
        fi

        marker_status="$(marker_status_for_combined "$combined_file")"
        error_marker="$(stdout_pattern_fallback_for_combined "$combined_file")"

        if [ -n "$artifact_dir" ] && [ -f "$artifact_dir/logs/scene-log.jsonl" ]; then
            jsonl_count="$(wc -l < "$artifact_dir/logs/scene-log.jsonl" | tr -d ' ')"
        fi

        if [ -n "$exit_code" ] && [ "$exit_code" != "0" ]; then
            status="fail"
            [ -n "$failure_reason" ] || failure_reason="ExitCodeNonZero:$exit_code"
        elif [ -n "$artifact_status" ] && [ "$(normalize_status "$artifact_status")" = "fail" ]; then
            status="fail"
            [ -n "$failure_reason" ] || failure_reason="ArtifactStatusFail"
        elif [ -n "$artifact_status" ] && [ "$(normalize_status "$artifact_status")" = "pass" ]; then
            status="pass"
        elif [ "$marker_status" = "fail" ]; then
            status="fail"
            [ -n "$failure_reason" ] || failure_reason="ExplicitFailMarker"
        elif [ "$marker_status" = "pass" ]; then
            status="pass"
        elif [ -n "$error_marker" ]; then
            status="needs_review"
            [ -n "$failure_reason" ] || failure_reason="ErrorMarker"
        else
            status="unknown"
        fi

        if [ "$status" != "pass" ]; then
            failed=1
        fi

        echo "Scene: $scene (attempt $attempt)"
        echo "  status: $status"
        echo "  exitCode: ${exit_code:-unknown}"
        echo "  failureReason: ${failure_reason:-none}"
        echo "  firstError: ${first_error:-${error_marker:-none}}"
        echo "  combinedLog: ${combined_file:-none}"
        echo "  jsonlLogCount: $jsonl_count"

        if [ -n "$result_file" ] && [ -f "$result_file" ]; then
            artifacts="$(jq -r '(.artifacts.files // [])[]' "$result_file" 2>/dev/null || true)"
            if [ -n "$artifacts" ]; then
                echo "  artifacts:"
                printf '%s\n' "$artifacts" | sed 's/^/    /'
            else
                echo "  artifacts: none"
            fi
        else
            echo "  artifacts: result.json not found"
        fi
        echo
    done < <(jq -c '.entries[]' "$target_dir/index.json")

    write_gate_report "$target_dir" "$manifest_path" "$gate_report"
    gate_verdict="$(jq -r '.verdict // "block"' "$gate_report" 2>/dev/null || printf 'block')"
    if [ "$gate_verdict" = "block" ]; then
        failed=1
    fi

    if [ "$failed" -ne 0 ]; then
        exit 1
    fi
    exit 0
fi

stdout_file="$target_dir/stdout.log"
stderr_file="$target_dir/stderr.log"
acceptance_file="$target_dir/artifacts/scene-acceptance.json"
artifacts_dir="$target_dir/artifacts"

echo "Format: legacy flat run directory"

if [ -f "$stdout_file" ] && rg -q "BrotatoLike playable slice PASS" "$stdout_file"; then
    echo "Status: playable slice PASS marker found"
elif [ -f "$stdout_file" ] && rg -q "BrotatoLike playable slice FAIL" "$stdout_file"; then
    echo "Status: playable slice FAIL marker found"
elif [ -f "$stdout_file" ] && rg -q "BrotatoLike GameOS smoke PASS|PASS|\\[PASS\\]" "$stdout_file"; then
    echo "Status: PASS marker found"
else
    echo "Status: PASS marker not found"
fi

if [ -f "$acceptance_file" ]; then
    artifact_status="$(jq -r '.status // "unknown"' "$acceptance_file" 2>/dev/null || printf 'unreadable')"
    echo "Acceptance artifact: $acceptance_file ($artifact_status)"
elif [ -d "$artifacts_dir" ] && find "$artifacts_dir" -maxdepth 1 -name '*.json' -type f | rg -q .; then
    echo "Artifacts:"
    while IFS= read -r artifact_file; do
        artifact_status="$(jq -r '.status // "unknown"' "$artifact_file" 2>/dev/null || printf 'unreadable')"
        artifact_logs="$(jq -r '(.logs // []) | length' "$artifact_file" 2>/dev/null || printf '0')"
        artifact_failures="$(jq -r '(.failureReasons // .failure_reasons // []) | length' "$artifact_file" 2>/dev/null || printf '0')"
        echo "  $artifact_file ($artifact_status, logs=$artifact_logs, failures=$artifact_failures)"
    done < <(find "$artifacts_dir" -maxdepth 1 -name '*.json' -type f | sort)
else
    echo "Artifacts: not found"
fi

matches="$(rg -n -m 40 "ERROR:|\\[ERROR\\]|\\[FAIL\\]|FAIL:|Exception|Cannot instantiate|Failed to load|scene not found" "$target_dir" || true)" # legacy stdout-pattern-fallback only
if [ -n "$matches" ]; then
    echo
    echo "First stdout-pattern-fallback markers:"
    printf '%s\n' "$matches"
else
    echo "Error markers: none"
fi
