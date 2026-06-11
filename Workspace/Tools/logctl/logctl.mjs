#!/usr/bin/env node

import { mkdir, readFile, readdir, rm, writeFile } from "node:fs/promises";
import fs from "node:fs";
import path from "node:path";

const cwd = process.cwd();
const severityRank = new Map([
  ["trace", 0],
  ["debug", 1],
  ["info", 2],
  ["warn", 3],
  ["warning", 3],
  ["error", 4],
  ["fatal", 5],
]);

function usage() {
  console.error(`Usage:
  logctl profile show [--config-dir <path>] [--format json|md]
  logctl analyze --run-dir <path> [--out <path>]
  logctl query (--analysis-dir <path>|--file <jsonl>|--run-dir <path>) [owner=...] [sourceFile=...] [operation=...] [entityId=...] [severity>=Warn] [--format json|md]
  logctl ingest --stdin --source legacy-stdout --out <run-dir>
  logctl suggest --run-dir <path> [--dry-run]`);
}

function parseArgs(argv) {
  const [command, ...rest] = argv;
  const options = { command, subcommand: null, filters: [], format: "md", dryRun: false, stdin: false };

  for (let i = 0; i < rest.length; i += 1) {
    const value = rest[i];
    if (command === "profile" && !options.subcommand && !value.startsWith("--")) {
      options.subcommand = value;
    } else if (value === "--run-dir") {
      options.runDir = rest[++i];
    } else if (value === "--analysis-dir") {
      options.analysisDir = rest[++i];
    } else if (value === "--file") {
      options.file = rest[++i];
    } else if (value === "--config-dir") {
      options.configDir = rest[++i];
    } else if (value === "--out") {
      options.out = rest[++i];
    } else if (value === "--source") {
      options.source = rest[++i];
    } else if (value === "--format") {
      options.format = rest[++i];
    } else if (value === "--dry-run") {
      options.dryRun = true;
    } else if (value === "--stdin") {
      options.stdin = true;
    } else if (value.includes("=") || value.includes(">=")) {
      options.filters.push(value);
    } else {
      throw new Error(`Unknown argument: ${value}`);
    }
  }

  return options;
}

function resolveInside(input, label = "path") {
  if (!input) {
    throw new Error(`${label} is required.`);
  }
  return path.resolve(cwd, input);
}

function toRel(filePath) {
  return path.relative(cwd, filePath).split(path.sep).join("/");
}

async function listFiles(root) {
  if (!fs.existsSync(root)) {
    return [];
  }

  const files = [];
  async function walk(dir) {
    const entries = await readdir(dir, { withFileTypes: true });
    for (const entry of entries) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        if (entry.name.startsWith("analysis")) {
          continue;
        }
        await walk(full);
      } else if (entry.isFile()) {
        files.push(full);
      }
    }
  }
  await walk(root);
  return files.sort((a, b) => a.localeCompare(b));
}

function readJsonMaybe(filePath) {
  try {
    return JSON.parse(fs.readFileSync(filePath, "utf8").replace(/^\uFEFF/, ""));
  } catch {
    return null;
  }
}

function loadJsonWithStatus(filePath) {
  if (!fs.existsSync(filePath)) {
    return { path: toRel(filePath), exists: false, data: null, error: null };
  }

  try {
    return {
      path: toRel(filePath),
      exists: true,
      data: JSON.parse(fs.readFileSync(filePath, "utf8").replace(/^\uFEFF/, "")),
      error: null,
    };
  } catch (error) {
    return { path: toRel(filePath), exists: true, data: null, error: error.message };
  }
}

async function readJsonl(filePath) {
  const text = await readFile(filePath, "utf8");
  const entries = [];
  for (const [index, line] of text.split(/\r?\n/).entries()) {
    const trimmed = line.trim();
    if (!trimmed) {
      continue;
    }
    try {
      const parsed = JSON.parse(trimmed.replace(/^\uFEFF/, ""));
      parsed.__source = toRel(filePath);
      parsed.__line = index + 1;
      entries.push(parsed);
    } catch (error) {
      entries.push({
        channel: "Diagnostics",
        severity: "Warn",
        owner: "LogAnalyzer",
        context: "JsonlParser",
        operation: "InvalidJsonlLine",
        phase: "Diagnostics",
        message: "invalid jsonl line",
        fields: { line: index + 1, error: error.message, value: trimmed },
        __invalidJsonl: true,
        __source: toRel(filePath),
        __line: index + 1,
      });
    }
  }
  return entries;
}

async function readJsonlIfExists(filePath) {
  if (!fs.existsSync(filePath)) {
    return [];
  }

  return readJsonl(filePath);
}

function normalizeStatus(value) {
  return String(value ?? "").trim().toLowerCase();
}

function artifactStatus(artifact) {
  const value = artifact?.validationStatus ?? artifact?.status ?? artifact?.result ?? artifact?.outcome;
  const normalized = normalizeStatus(value);
  if (["pass", "passed", "success", "succeeded", "ok"].includes(normalized)) {
    return "pass";
  }
  if (["fail", "failed", "error"].includes(normalized)) {
    return "fail";
  }
  if (Array.isArray(artifact?.failures) && artifact.failures.length > 0) {
    return "fail";
  }
  if (Array.isArray(artifact?.checks) && artifact.checks.some((check) => normalizeStatus(check.status) === "fail")) {
    return "fail";
  }
  return null;
}

function entryStatus(entry) {
  const status = normalizeStatus(getField(entry, "validationStatus"));
  if (status === "pass") {
    return "pass";
  }
  if (status === "fail") {
    return "fail";
  }
  return null;
}

function entryKey(entry, key) {
  return getField(entry, key);
}

function matchesFilter(entry, filter) {
  const comparison = filter.match(/^([^><=]+)(>=)(.+)$/);
  if (comparison) {
    const [, key, , expected] = comparison;
    if (key.trim() !== "severity") {
      return true;
    }
    const actualRank = severityRank.get(String(getField(entry, "severity") ?? "").toLowerCase()) ?? -1;
    const expectedRank = severityRank.get(String(expected).toLowerCase()) ?? Number.POSITIVE_INFINITY;
    return actualRank >= expectedRank;
  }

  const match = filter.match(/^([^=]+)=(.+)$/);
  if (!match) {
    return true;
  }

  const [, key, expected] = match;
  const actual = entryKey(entry, key.trim());
  return String(actual ?? "").toLowerCase() === expected.trim().toLowerCase();
}

function groupBy(entries, key) {
  const map = new Map();
  for (const entry of entries) {
    const value = String(entryKey(entry, key) ?? "unknown").replace(/[^a-zA-Z0-9._-]+/g, "_");
    if (!map.has(value)) {
      map.set(value, []);
    }
    map.get(value).push(entry);
  }
  return map;
}

async function writeJson(filePath, value) {
  await mkdir(path.dirname(filePath), { recursive: true });
  await writeFile(filePath, `${JSON.stringify(value, null, 2)}\n`, "utf8");
}

async function writeJsonl(filePath, entries) {
  await mkdir(path.dirname(filePath), { recursive: true });
  await writeFile(filePath, `${entries.map((entry) => JSON.stringify(entry)).join("\n")}${entries.length > 0 ? "\n" : ""}`, "utf8");
}

async function writeText(filePath, text) {
  await mkdir(path.dirname(filePath), { recursive: true });
  await writeFile(filePath, text.endsWith("\n") ? text : `${text}\n`, "utf8");
}

// 复跑到旧 analysis 目录时，先移除已废弃的 raw 复制入口，避免 AI 误读 stale 产物。
async function removeDeprecatedAnalysisCopies(outDir) {
  await Promise.all([
    rm(path.join(outDir, "by-owner"), { recursive: true, force: true }),
    rm(path.join(outDir, "by-phase"), { recursive: true, force: true }),
    rm(path.join(outDir, "flows", "flows.json"), { force: true }),
  ]);
}

async function loadRun(runDir) {
  const root = resolveInside(runDir, "run-dir");
  const files = await listFiles(root);
  const jsonlFiles = files.filter((file) => file.endsWith(".jsonl"));
  const resultFiles = files.filter((file) => file.endsWith("result.json") || file.endsWith("index.json"));
  const artifactFiles = files.filter((file) => file.endsWith(".json") && file.includes(`${path.sep}artifacts${path.sep}`));
  const stdoutFiles = files.filter((file) => file.endsWith("stdout.log") || file.endsWith("combined.log") || file.endsWith("legacy-stdout.log"));

  const entries = [];
  for (const file of jsonlFiles) {
    entries.push(...await readJsonl(file));
  }

  const results = resultFiles
    .map((file) => ({ path: toRel(file), data: readJsonMaybe(file) }))
    .filter((item) => item.data);
  const artifacts = artifactFiles
    .map((file) => ({ path: toRel(file), data: readJsonMaybe(file), status: artifactStatus(readJsonMaybe(file)) }))
    .filter((item) => item.data);

  return { root, files, jsonlFiles, stdoutFiles, entries, results, artifacts };
}

function getField(entry, key) {
  if (!entry) {
    return undefined;
  }

  if (Object.prototype.hasOwnProperty.call(entry, key)) {
    return entry[key];
  }

  const pascalKey = `${key.charAt(0).toUpperCase()}${key.slice(1)}`;
  if (Object.prototype.hasOwnProperty.call(entry, pascalKey)) {
    return entry[pascalKey];
  }

  if (entry.fields && Object.prototype.hasOwnProperty.call(entry.fields, key)) {
    return entry.fields[key];
  }

  if (entry.fields && Object.prototype.hasOwnProperty.call(entry.fields, pascalKey)) {
    return entry.fields[pascalKey];
  }

  return undefined;
}

function fieldsOf(entry) {
  return entry?.fields && typeof entry.fields === "object" && !Array.isArray(entry.fields) ? entry.fields : {};
}

function hasValue(value) {
  return value !== undefined && value !== null && value !== "";
}

function displayValue(value, fallback = "unknown") {
  return hasValue(value) ? String(value) : fallback;
}

function mdCell(value) {
  return String(value ?? "").replace(/\|/g, "\\|").replace(/\r?\n/g, " ");
}

function countBy(entries, keyFn, limit = 10) {
  const counts = new Map();
  for (const entry of entries) {
    const key = keyFn(entry);
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  return [...counts.entries()]
    .map(([key, count]) => ({ key, count }))
    .sort((a, b) => b.count - a.count || a.key.localeCompare(b.key))
    .slice(0, limit);
}

function ownerDoc(owner, context = "") {
  const normalizedOwner = normalizeStatus(owner);
  const normalizedContext = normalizeStatus(context);
  if (normalizedOwner === "targetselector") return "DocsAI/ECS/Tools/TargetSelector/README.md";
  if (normalizedOwner === "objectpool") return "DocsAI/ECS/Tools/ObjectPool/README.md";
  if (normalizedOwner === "damage") return "DocsAI/ECS/Capabilities/Damage/README.md";
  if (normalizedOwner === "ability") return "DocsAI/ECS/Capabilities/Ability/README.md";
  if (normalizedOwner === "entity") return "DocsAI/ECS/Runtime/Entity/README.md";
  if (normalizedOwner === "system") return "DocsAI/ECS/Runtime/System/README.md";
  if (normalizedOwner === "runtime" && normalizedContext.includes("healthbar")) return "DocsAI/ECS/UI/Usage.md";
  if (normalizedOwner === "runtime" && normalizedContext.includes("ui")) return "DocsAI/ECS/UI/Usage.md";
  if (normalizedOwner === "runtime") return "DocsAI/ECS/Runtime/README.md";
  return "DocsAI/ECS/Tools/Logger/README.md";
}

function buildGateReport(run) {
  const structuredValidation = run.entries.filter((entry) => normalizeStatus(getField(entry, "channel")) === "validation");
  const invalidJsonl = run.entries.filter((entry) => entry.__invalidJsonl);
  const entryFailures = run.entries.filter((entry) => {
    const severity = normalizeStatus(getField(entry, "severity"));
    const outcome = normalizeStatus(getField(entry, "outcome"));
    return entryStatus(entry) === "fail" || outcome === "failed" || severity === "error" || severity === "fatal";
  });
  const artifactFailures = run.artifacts.filter((artifact) => artifact.status === "fail");
  const artifactPasses = run.artifacts.filter((artifact) => artifact.status === "pass");
  const validationPasses = structuredValidation.filter((entry) => entryStatus(entry) === "pass");
  const validationFailures = structuredValidation.filter((entry) => entryStatus(entry) === "fail");
  const hasStdoutFallback = run.results.some((result) => {
    const data = result.data;
    return Boolean(data?.firstError || String(data?.failureReason ?? "").includes("TestFailMarker"));
  });

  let resultSource = "none";
  if (run.artifacts.length > 0) {
    resultSource = "artifact";
  } else if (structuredValidation.length > 0) {
    resultSource = "validation";
  } else if (run.entries.length > 0) {
    resultSource = "structured-log";
  } else if (hasStdoutFallback || run.stdoutFiles.length > 0) {
    resultSource = "stdout-pattern-fallback";
  }

  let status = "invalid-input";
  if (artifactFailures.length > 0 || validationFailures.length > 0 || entryFailures.length > 0) {
    status = "failed";
  } else if (artifactPasses.length > 0 || validationPasses.length > 0) {
    status = "passed";
  } else if ((hasStdoutFallback || run.stdoutFiles.length > 0) && run.entries.length === 0 && run.artifacts.length === 0) {
    status = "stdout-pattern-fallback";
  } else if (run.entries.length > invalidJsonl.length) {
    status = "no-failure-observed";
  }

  if (invalidJsonl.length > 0 && run.entries.length === invalidJsonl.length) {
    status = "invalid-input";
    resultSource = "invalid-input";
  }

  const warnings = [];
  if (invalidJsonl.length > 0) {
    warnings.push({
      code: "invalid-jsonl",
      count: invalidJsonl.length,
      message: "raw JSONL contains invalid or truncated lines; inspect raw/entries.jsonl diagnostics before trusting exact counts.",
    });
  }
  if (status === "no-failure-observed") {
    warnings.push({
      code: "no-validation-evidence",
      message: "no Validation channel pass/fail and no artifact were found; this run cannot be reported as passed.",
    });
  }

  const confidence = status === "passed" || status === "failed"
    ? (resultSource === "artifact" || resultSource === "validation" ? "high" : "medium")
    : (status === "no-failure-observed" ? "low" : "very-low");

  return {
    generatedAtUtc: new Date().toISOString(),
    runDir: toRel(run.root),
    resultSource,
    status,
    confidence,
    warnings,
    counts: {
      entries: run.entries.length,
      invalidJsonl: invalidJsonl.length,
      validationEntries: structuredValidation.length,
      artifacts: run.artifacts.length,
      artifactPasses: artifactPasses.length,
      artifactFailures: artifactFailures.length,
      validationPasses: validationPasses.length,
      validationFailures: validationFailures.length,
      structuredFailures: entryFailures.length,
      stdoutFallbackFiles: run.stdoutFiles.length,
    },
    failures: [
      ...artifactFailures.map((artifact) => ({ source: "artifact", path: artifact.path, status: artifact.status })),
      ...entryFailures.map((entry) => ({ source: "structured-log", path: entry.__source, line: entry.__line, owner: getField(entry, "owner"), operation: getField(entry, "operation"), message: getField(entry, "message") })),
    ],
  };
}

function missingFields(entries) {
  const required = ["runElapsedMs", "frame", "physicsFrame", "severity", "owner", "context", "operation", "message"];
  return entries
    .map((entry) => ({
      source: entry.__source,
      line: entry.__line,
      missing: required.filter((field) => !hasValue(getField(entry, field))),
    }))
    .filter((item) => item.missing.length > 0);
}

function semanticMissingFields(entries) {
  const groups = new Map();
  const totals = {
    fieldsEmpty: 0,
    operationEqualsContext: 0,
    flowMissingDurationMs: 0,
    missingReasonCode: 0,
    missingEntityId: 0,
    missingSource: 0,
    unknownOwner: 0,
    unknownPhase: 0,
  };

  for (const entry of entries) {
    if (entry.__invalidJsonl) {
      continue;
    }

    const owner = displayValue(getField(entry, "owner"));
    const context = displayValue(getField(entry, "context"));
    const operation = displayValue(getField(entry, "operation"));
    const phase = displayValue(getField(entry, "phase"));
    const fields = fieldsOf(entry);
    const issues = [];
    const fieldCount = Object.keys(fields).length;
    const severity = normalizeStatus(getField(entry, "severity"));
    const outcome = normalizeStatus(getField(entry, "outcome"));

    if (fieldCount === 0) {
      issues.push("fields_empty");
      totals.fieldsEmpty += 1;
    }
    if (normalizeStatus(operation) !== "unknown" && normalizeStatus(operation) === normalizeStatus(context)) {
      issues.push("operation_equals_context");
      totals.operationEqualsContext += 1;
    }
    if (isFlowCompletion(entry) && !hasValue(getField(entry, "durationMs"))) {
      issues.push("flow_missing_durationMs");
      totals.flowMissingDurationMs += 1;
    }
    if ((severity === "warn" || severity === "warning" || severity === "error" || severity === "fatal" || outcome === "failed" || outcome === "skipped" || outcome === "suppressed") && !hasValue(getField(entry, "reasonCode"))) {
      issues.push("missing_reasonCode");
      totals.missingReasonCode += 1;
    }
    if (needsEntityId(owner, context, operation) && !hasValue(getField(entry, "entityId"))) {
      issues.push("missing_entityId");
      totals.missingEntityId += 1;
    }
    if (!hasValue(getField(entry, "sourceFile")) || !hasValue(getField(entry, "sourceLine"))) {
      issues.push("missing_source");
      totals.missingSource += 1;
    }
    if (normalizeStatus(owner) === "unknown") {
      issues.push("unknown_owner");
      totals.unknownOwner += 1;
    }
    if (normalizeStatus(phase) === "unknown") {
      issues.push("unknown_phase");
      totals.unknownPhase += 1;
    }

    if (issues.length === 0) {
      continue;
    }

    const key = [owner, context, operation].join("|");
    if (!groups.has(key)) {
      groups.set(key, {
        owner,
        context,
        operation,
        count: 0,
        issues: {},
        examples: [],
        ownerDoc: ownerDoc(owner, context),
      });
    }

    const group = groups.get(key);
    group.count += 1;
    for (const issue of issues) {
      group.issues[issue] = (group.issues[issue] ?? 0) + 1;
    }
    if (group.examples.length < 3) {
      group.examples.push({
        source: entry.__source,
        line: entry.__line,
        message: getField(entry, "message"),
      });
    }
  }

  return {
    totals,
    groups: [...groups.values()]
      .sort((a, b) => b.count - a.count || `${a.owner}/${a.context}/${a.operation}`.localeCompare(`${b.owner}/${b.context}/${b.operation}`))
      .slice(0, 100)
      .map((group) => ({ ...group, recommendedAction: recommendMissingAction(group) })),
  };
}

function needsEntityId(owner, context, operation) {
  const text = `${owner}/${context}/${operation}`.toLowerCase();
  return /(entity|unit|healthbar|damage|ability|ai|movement|lifecycle|spawn)/.test(text);
}

function noiseSummary(entries) {
  const groups = new Map();
  for (const entry of entries) {
    if (entry.__invalidJsonl) {
      continue;
    }

    const owner = displayValue(getField(entry, "owner"));
    const context = displayValue(getField(entry, "context"));
    const operation = displayValue(getField(entry, "operation"));
    const key = [owner, context, operation].join("|");
    if (!groups.has(key)) {
      groups.set(key, {
        owner,
        context,
        operation,
        count: 0,
        severities: {},
        outcomes: {},
        phases: {},
        sampleMessages: [],
        ownerDoc: ownerDoc(owner, context),
      });
    }
    const group = groups.get(key);
    group.count += 1;
    const severity = displayValue(getField(entry, "severity"));
    const outcome = displayValue(getField(entry, "outcome"));
    const phase = displayValue(getField(entry, "phase"));
    group.severities[severity] = (group.severities[severity] ?? 0) + 1;
    group.outcomes[outcome] = (group.outcomes[outcome] ?? 0) + 1;
    group.phases[phase] = (group.phases[phase] ?? 0) + 1;
    const message = displayValue(getField(entry, "message"), "");
    if (message && !group.sampleMessages.includes(message) && group.sampleMessages.length < 5) {
      group.sampleMessages.push(message);
    }
  }

  return [...groups.values()]
    .sort((a, b) => b.count - a.count || `${a.owner}/${a.context}/${a.operation}`.localeCompare(`${b.owner}/${b.context}/${b.operation}`))
    .slice(0, 50)
    .map((group) => ({ ...group, recommendedAction: recommendNoiseAction(group) }));
}

function recommendNoiseAction(group) {
  const key = `${group.owner}/${group.context}/${group.operation}`.toLowerCase();
  if (key.includes("targetselector") || key.includes("targetquery")) {
    return "aggregate TargetQuery success path by window; keep warning/failure query details and include query shape, candidate counts, returned counts, truncated and reasonCode.";
  }
  if (key.includes("objectpool") && key.includes("release")) {
    return "batch ObjectPool release success by poolName; keep skipped/discarded releases as structured details.";
  }
  if (key.includes("objectpool") && key.includes("acquire")) {
    return "sample or budget ObjectPool acquire success by poolName; keep capacity and lifecycle anomalies expanded.";
  }
  if (key.includes("healthbar")) {
    return "replace repeated bind text with one HealthBarBind summary carrying entityId, entityType, outcome and reasonCode.";
  }
  if (key.includes("damage")) {
    return "keep DamageProcess digest but add processor chain, base/final/actual damage, dodge/critical and blocked reason fields.";
  }
  if (key.includes("system")) {
    return "emit one System diagnostics snapshot instead of repeated line-level status logs.";
  }
  return "consider budget/sample or summary-only stdout; add owner field contract before adding more logs.";
}

function recommendMissingAction(group) {
  const key = `${group.owner}/${group.context}/${group.operation}`.toLowerCase();
  if (key.includes("healthbar")) {
    return "add HealthBarBind operation and fields {entityId, entityType, poolName, outcome, reasonCode}; success path should be counted, not printed as three text lines.";
  }
  if (key.includes("targetselector") || key.includes("targetquery")) {
    return "add durationMs and query shape fields; aggregate successful queries and keep warning/failure details.";
  }
  if (key.includes("objectpool")) {
    return "add durationMs/entryType and poolName-based batch summary; keep skipped/discarded reasonCode.";
  }
  if (key.includes("damage")) {
    return "add durationMs plus DamageInfo entity/source ids and processor chain digest.";
  }
  if (key.includes("system")) {
    return "replace context-named text logs with stable operations such as SystemStartup, SystemPreflight or SystemDiagnosticsSnapshot.";
  }
  return "define stable operation and fields in the owner Log contract.";
}

function isFlowEntry(entry) {
  const channel = normalizeStatus(getField(entry, "channel"));
  const entryType = normalizeStatus(getField(entry, "entryType"));
  if (channel === "flow") {
    return true;
  }
  if (["flow_start", "flow_step", "flow_summary", "flow_suppressed_summary"].includes(entryType)) {
    return true;
  }
  return hasValue(getField(entry, "correlationId"))
    && hasValue(getField(entry, "operation"))
    && (hasValue(getField(entry, "durationMs")) || hasValue(getField(entry, "stepCount")))
    && ["started", "completed", "succeeded", "failed", "skipped", "suppressed"].includes(normalizeStatus(getField(entry, "outcome")));
}

function isFlowCompletion(entry) {
  const outcome = normalizeStatus(getField(entry, "outcome"));
  const entryType = normalizeStatus(getField(entry, "entryType"));
  return isFlowEntry(entry) && (entryType === "flow_summary" || ["completed", "succeeded", "failed", "skipped"].includes(outcome));
}

function rawRef(entries) {
  if (entries.length === 0) {
    return "";
  }

  const source = entries[0].__source ?? "unknown";
  const firstLine = entries[0].__line;
  const lastLine = entries[entries.length - 1].__line;
  return firstLine === lastLine ? `${source}:${firstLine}` : `${source}:${firstLine}-${lastLine}`;
}

function numericValue(value) {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function rawKeyFields(entry) {
  const ignored = new Set([
    "entryType",
    "durationMs",
    "stepIndex",
    "stepName",
    "stepCount",
    "reasonCode",
    "failedStep",
    "lastSuccessfulStep",
  ]);
  const fields = {};
  for (const [key, value] of Object.entries(fieldsOf(entry))) {
    if (!ignored.has(key)) {
      fields[key] = value;
    }
  }

  for (const key of ["entityId", "targetId", "sourceEntityId", "ownerEntityId", "abilityId", "poolName"]) {
    const value = getField(entry, key);
    if (hasValue(value) && fields[key] === undefined) {
      fields[key] = value;
    }
  }

  return fields;
}

function firstValue(entries, keys) {
  for (const entry of entries) {
    for (const key of keys) {
      const value = getField(entry, key);
      if (hasValue(value)) {
        return value;
      }
    }
  }
  return undefined;
}

function buildFlowConclusions(entries) {
  const groups = new Map();
  let syntheticIndex = 0;
  for (const entry of entries.filter(isFlowEntry)) {
    const correlationId = getField(entry, "correlationId");
    const groupKey = hasValue(correlationId)
      ? String(correlationId)
      : `single-${syntheticIndex += 1}-${entry.__source}:${entry.__line}`;
    if (!groups.has(groupKey)) {
      groups.set(groupKey, []);
    }
    groups.get(groupKey).push(entry);
  }

  return [...groups.entries()]
    .map(([groupKey, groupEntries]) => {
      groupEntries.sort((a, b) => (a.__line ?? 0) - (b.__line ?? 0));
      const completion = [...groupEntries].reverse().find(isFlowCompletion) ?? groupEntries[groupEntries.length - 1];
      const failedEntry = groupEntries.find((entry) => normalizeStatus(getField(entry, "outcome")) === "failed")
        ?? groupEntries.find((entry) => ["warn", "warning", "error", "fatal"].includes(normalizeStatus(getField(entry, "severity"))));
      const warningCount = groupEntries.filter((entry) => ["warn", "warning"].includes(normalizeStatus(getField(entry, "severity")))).length;
      const failedCount = groupEntries.filter((entry) => normalizeStatus(getField(entry, "outcome")) === "failed" || ["error", "fatal"].includes(normalizeStatus(getField(entry, "severity")))).length;
      const stepEntries = groupEntries.filter((entry) => normalizeStatus(getField(entry, "entryType")) === "flow_step");
      const frames = groupEntries.map((entry) => numericValue(getField(entry, "frame"))).filter((value) => value !== null);
      const runElapsedValues = groupEntries.map((entry) => numericValue(getField(entry, "runElapsedMs"))).filter((value) => value !== null);
      const outcome = failedCount > 0 ? "Failed" : displayValue(getField(completion, "outcome"), "Completed");
      const reasonCode = firstValue(failedEntry ? [failedEntry, completion] : [completion], ["reasonCode", "reason"]);
      const failedStep = failedEntry
        ? firstValue([failedEntry], ["failedStep", "stepName", "operation"])
        : undefined;
      const stepCount = numericValue(getField(completion, "stepCount")) ?? stepEntries.length;
      const qualityFlags = [];
      if (groupEntries.length === 1) {
        qualityFlags.push("single_entry_flow");
      }
      if (stepEntries.length === 0) {
        qualityFlags.push("flow_steps_missing");
      }
      if (!hasValue(getField(completion, "durationMs"))) {
        qualityFlags.push("duration_missing");
      }
      if ((outcome === "Failed" || warningCount > 0) && !hasValue(reasonCode)) {
        qualityFlags.push("reason_missing");
      }

      return {
        flowId: hasValue(getField(completion, "correlationId")) ? String(getField(completion, "correlationId")) : groupKey,
        type: `${displayValue(getField(completion, "owner"))}/${displayValue(getField(completion, "operation"))}`,
        owner: displayValue(getField(completion, "owner")),
        context: displayValue(getField(completion, "context")),
        operation: displayValue(getField(completion, "operation")),
        phase: displayValue(getField(completion, "phase")),
        outcome,
        durationMs: numericValue(getField(completion, "durationMs")),
        frameRange: frames.length === 0 ? null : [Math.min(...frames), Math.max(...frames)],
        runElapsedRangeMs: runElapsedValues.length === 0 ? null : [Math.min(...runElapsedValues), Math.max(...runElapsedValues)],
        steps: stepCount,
        failedStep: failedStep === undefined ? null : String(failedStep),
        reasonCode: reasonCode === undefined ? null : String(reasonCode),
        keyFields: rawKeyFields(completion),
        entryCount: groupEntries.length,
        warningCount,
        failedCount,
        qualityFlags,
        rawRef: rawRef(groupEntries),
      };
    })
    .sort((a, b) => {
      const aStart = a.runElapsedRangeMs?.[0] ?? 0;
      const bStart = b.runElapsedRangeMs?.[0] ?? 0;
      return aStart - bStart || a.rawRef.localeCompare(b.rawRef);
    });
}

function flowDigest(flowConclusions) {
  const groups = new Map();
  for (const flow of flowConclusions) {
    const owner = flow.owner;
    const context = flow.context;
    const operation = flow.operation;
    const key = [owner, context, operation].join("|");
    if (!groups.has(key)) {
      groups.set(key, {
        owner,
        context,
        operation,
        count: 0,
        completed: 0,
        succeeded: 0,
        failed: 0,
        skipped: 0,
        warnings: 0,
        singleEntry: 0,
        missingDuration: 0,
        sampleLines: [],
        ownerDoc: ownerDoc(owner, context),
      });
    }
    const group = groups.get(key);
    group.count += 1;
    const outcome = normalizeStatus(flow.outcome);
    if (outcome === "completed") group.completed += 1;
    if (outcome === "succeeded") group.succeeded += 1;
    if (outcome === "skipped") group.skipped += 1;
    if (outcome === "failed") group.failed += 1;
    if (flow.warningCount > 0) group.warnings += 1;
    if (flow.qualityFlags.includes("single_entry_flow")) group.singleEntry += 1;
    if (flow.qualityFlags.includes("duration_missing")) group.missingDuration += 1;
    if (group.sampleLines.length < 3) {
      group.sampleLines.push(flow.rawRef);
    }
  }

  return [...groups.values()]
    .sort((a, b) => b.count - a.count || `${a.owner}/${a.context}/${a.operation}`.localeCompare(`${b.owner}/${b.context}/${b.operation}`))
    .slice(0, 100);
}

function metricSummary(values) {
  if (values.length === 0) {
    return null;
  }

  const min = Math.min(...values);
  const max = Math.max(...values);
  const avg = Number((values.reduce((sum, value) => sum + value, 0) / values.length).toFixed(3));
  return { min, avg, max };
}

function buildNoiseTemplates(flowConclusions) {
  const groups = new Map();
  for (const flow of flowConclusions) {
    if (normalizeStatus(flow.outcome) === "failed" || flow.warningCount > 0 || flow.failedCount > 0) {
      continue;
    }

    const key = [flow.owner, flow.context, flow.operation, flow.outcome].join("|");
    if (!groups.has(key)) {
      groups.set(key, {
        owner: flow.owner,
        context: flow.context,
        operation: flow.operation,
        outcome: flow.outcome,
        count: 0,
        rawRefs: [],
        metricValues: {},
        ownerDoc: ownerDoc(flow.owner, flow.context),
      });
    }

    const group = groups.get(key);
    group.count += 1;
    if (group.rawRefs.length < 5) {
      group.rawRefs.push(flow.rawRef);
    }
    for (const [field, value] of Object.entries(flow.keyFields ?? {})) {
      const numeric = numericValue(value);
      if (numeric === null) {
        continue;
      }
      if (!group.metricValues[field]) {
        group.metricValues[field] = [];
      }
      group.metricValues[field].push(numeric);
    }
    if (flow.durationMs !== null) {
      if (!group.metricValues.durationMs) {
        group.metricValues.durationMs = [];
      }
      group.metricValues.durationMs.push(flow.durationMs);
    }
  }

  return [...groups.values()]
    .map((group) => {
      const metrics = {};
      for (const [field, values] of Object.entries(group.metricValues)) {
        const summary = metricSummary(values);
        if (summary) {
          metrics[field] = summary;
        }
      }
      return {
        groupKey: [group.owner, group.context, group.operation, group.outcome].join("|"),
        owner: group.owner,
        context: group.context,
        operation: group.operation,
        outcome: group.outcome,
        count: group.count,
        metrics,
        rawRefs: group.rawRefs,
        ownerDoc: group.ownerDoc,
        recommendedAction: recommendNoiseAction(group),
      };
    })
    .sort((a, b) => b.count - a.count || `${a.owner}/${a.context}/${a.operation}`.localeCompare(`${b.owner}/${b.context}/${b.operation}`))
    .slice(0, 100);
}

function flowTemplateKey(flow) {
  return [flow.owner, flow.context, flow.operation, flow.outcome].join("|");
}

function shouldKeepFlowConclusion(flow, templatedSuccessKeys) {
  if (normalizeStatus(flow.outcome) === "failed" || flow.warningCount > 0 || flow.failedCount > 0) {
    return true;
  }

  return !templatedSuccessKeys.has(flowTemplateKey(flow));
}

function mergeProfileView(configDir) {
  const root = resolveInside(configDir ?? "Config/Log", "config-dir");
  const profile = loadJsonWithStatus(path.join(root, "log.profile.json"));
  const rules = loadJsonWithStatus(path.join(root, "log.rules.json"));
  const overrides = loadJsonWithStatus(path.join(root, "log.overrides.json"));
  const profileRules = Array.isArray(profile.data?.rules) ? profile.data.rules : [];
  const sharedRules = Array.isArray(rules.data?.rules) ? rules.data.rules : [];
  const overrideRules = Array.isArray(overrides.data?.rules) ? overrides.data.rules : [];

  return {
    configDir: toRel(root),
    profile,
    rules,
    overrides,
    effective: {
      profile: overrides.data?.profile ?? profile.data?.profile ?? "ai-default",
      defaultSeverity: overrides.data?.defaultSeverity ?? overrides.data?.minimumSeverity ?? profile.data?.defaultSeverity ?? profile.data?.minimumSeverity ?? "Debug",
      sinks: {
        ...profile.data?.sinks,
        ...overrides.data?.sinks,
      },
      budget: {
        ...profile.data?.budget,
        ...overrides.data?.budget,
      },
      rules: [...profileRules, ...sharedRules, ...overrideRules],
    },
  };
}

async function profile(options) {
  if (options.subcommand !== "show") {
    throw new Error("profile requires subcommand: show");
  }

  const view = mergeProfileView(options.configDir);
  if (options.format === "json") {
    console.log(JSON.stringify(view, null, 2));
    return;
  }

  const missing = [view.profile, view.rules, view.overrides].filter((item) => !item.exists).map((item) => item.path);
  const invalid = [view.profile, view.rules, view.overrides].filter((item) => item.error).map((item) => `${item.path}: ${item.error}`);
  const lines = [
    "# logctl profile show",
    "",
    `configDir: ${view.configDir}`,
    `profile: ${view.effective.profile}`,
    `defaultSeverity: ${view.effective.defaultSeverity}`,
    `rules: ${view.effective.rules.length}`,
    `sinks: ${JSON.stringify(view.effective.sinks)}`,
    `budget: ${JSON.stringify(view.effective.budget)}`,
    "",
    "## Files",
    "",
    `- profile: ${view.profile.path} ${view.profile.exists ? "ok" : "missing"}`,
    `- rules: ${view.rules.path} ${view.rules.exists ? "ok" : "missing"}`,
    `- overrides: ${view.overrides.path} ${view.overrides.exists ? "ok" : "missing"}`,
    "",
    "## Missing",
    "",
    missing.length === 0 ? "- none" : missing.map((item) => `- ${item}`).join("\n"),
    "",
    "## Invalid",
    "",
    invalid.length === 0 ? "- none" : invalid.map((item) => `- ${item}`).join("\n"),
  ];

  console.log(lines.join("\n"));
}

function buildDigest(run, gateReport, flowConclusions, noise, semanticMissing, templates) {
  const validEntries = run.entries.filter((entry) => !entry.__invalidJsonl);
  const flowStats = flowConclusions.reduce((stats, flow) => {
    stats.total += 1;
    const outcome = normalizeStatus(flow.outcome);
    if (outcome === "failed") stats.failed += 1;
    else if (outcome === "skipped") stats.skipped += 1;
    else if (outcome === "succeeded") stats.succeeded += 1;
    else stats.completed += 1;
    if (flow.warningCount > 0) stats.warned += 1;
    if (flow.qualityFlags.includes("single_entry_flow")) stats.singleEntry += 1;
    return stats;
  }, { total: 0, completed: 0, succeeded: 0, failed: 0, skipped: 0, warned: 0, singleEntry: 0 });

  return {
    generatedAtUtc: gateReport.generatedAtUtc,
    runDir: gateReport.runDir,
    gate: {
      status: gateReport.status,
      confidence: gateReport.confidence,
      resultSource: gateReport.resultSource,
      warnings: gateReport.warnings,
    },
    counts: gateReport.counts,
    topOwners: countBy(validEntries, (entry) => displayValue(getField(entry, "owner")), 8),
    topOperations: countBy(validEntries, (entry) => `${displayValue(getField(entry, "owner"))}/${displayValue(getField(entry, "context"))}/${displayValue(getField(entry, "operation"))}`, 10),
    topPhases: countBy(validEntries, (entry) => displayValue(getField(entry, "phase")), 8),
    topNoise: noise.slice(0, 8),
    semanticMissing: semanticMissing.totals,
    semanticMissingGroups: semanticMissing.groups.slice(0, 12),
    flowStats,
    flowConclusions,
    flows: flowDigest(flowConclusions),
    noiseTemplates: templates.slice(0, 12),
    failedFlows: flowConclusions.filter((flow) => normalizeStatus(flow.outcome) === "failed" || flow.warningCount > 0).slice(0, 20),
    failures: gateReport.failures.slice(0, 20),
  };
}

function markdownTable(headers, rows) {
  if (rows.length === 0) {
    return "- none";
  }

  return [
    `| ${headers.map(mdCell).join(" | ")} |`,
    `| ${headers.map(() => "---").join(" | ")} |`,
    ...rows.map((row) => `| ${row.map(mdCell).join(" | ")} |`),
  ].join("\n");
}

function countRows(items) {
  return items.map((item) => [item.key, item.count]);
}

function summaryMd(digest) {
  return [
    "# Log Run Summary",
    "",
    "## Gate",
    "",
    `- status: ${digest.gate.status}`,
    `- confidence: ${digest.gate.confidence}`,
    `- resultSource: ${digest.gate.resultSource}`,
    `- entries: ${digest.counts.entries}`,
    `- invalidJsonl: ${digest.counts.invalidJsonl}`,
    `- validationEntries: ${digest.counts.validationEntries}`,
    `- artifacts: ${digest.counts.artifacts}`,
    `- structuredFailures: ${digest.counts.structuredFailures}`,
    "",
    digest.gate.status === "no-failure-observed"
      ? "> No Validation channel pass/fail or artifact was found. This means no structured failure was observed; it is not a behavior pass."
      : "",
    digest.gate.warnings.length === 0 ? "" : ["", "## Gate Warnings", "", ...digest.gate.warnings.map((warning) => `- ${warning.code}: ${warning.message}${warning.count ? ` (${warning.count})` : ""}`)].join("\n"),
    "",
    "## Flow Outcomes",
    "",
    markdownTable(
      ["total", "completed", "succeeded", "failed", "skipped", "warned", "singleEntry"],
      [[digest.flowStats.total, digest.flowStats.completed, digest.flowStats.succeeded, digest.flowStats.failed, digest.flowStats.skipped, digest.flowStats.warned, digest.flowStats.singleEntry]],
    ),
    "",
    "## Failed Or Warned Flows",
    "",
    digest.failedFlows.length === 0
      ? "- none"
      : markdownTable(
        ["flow", "outcome", "failedStep", "reasonCode", "rawRef"],
        digest.failedFlows.slice(0, 12).map((flow) => [flow.type, flow.outcome, flow.failedStep ?? "", flow.reasonCode ?? "", flow.rawRef]),
      ),
    "",
    "## Top Owner",
    "",
    markdownTable(["owner", "count"], countRows(digest.topOwners.slice(0, 5))),
    "",
    "## Top Operation",
    "",
    markdownTable(["owner/context/operation", "count"], countRows(digest.topOperations.slice(0, 5))),
    "",
    "## Top Phase",
    "",
    markdownTable(["phase", "count"], countRows(digest.topPhases.slice(0, 5))),
    "",
    "## Top Noise",
    "",
    markdownTable(
      ["owner", "context", "operation", "count", "recommended action"],
      digest.topNoise.slice(0, 5).map((item) => [item.owner, item.context, item.operation, item.count, item.recommendedAction]),
    ),
    "",
    "## Aggregated Success Templates",
    "",
    markdownTable(
      ["owner", "context", "operation", "outcome", "count", "metrics"],
      digest.noiseTemplates.slice(0, 5).map((template) => [template.owner, template.context, template.operation, template.outcome, template.count, Object.entries(template.metrics).map(([key, value]) => `${key}:${value.min}/${value.avg}/${value.max}`).join(", ")]),
    ),
    "",
    "## Semantic Missing Fields",
    "",
    markdownTable(
      ["issue", "count"],
      Object.entries(digest.semanticMissing).map(([issue, count]) => [issue, count]),
    ),
    "",
    "## Read Next",
    "",
    "- ai-context.md",
    "- noise/top-contexts.md",
    "- missing-fields/index.md",
    "- flows/index.md",
    "- failures/index.md",
    "- noise/templates.md",
    "",
    "Do not read raw/entries.jsonl by default. Use logctl query first when these digest files are insufficient.",
    "",
  ].filter((line) => line !== "").join("\n");
}

function aiContext(digest) {
  const ownerDocs = [...new Set([
    ...digest.topNoise.slice(0, 8).map((item) => item.ownerDoc),
    ...digest.semanticMissingGroups.slice(0, 8).map((item) => item.ownerDoc),
  ])].sort();

  return [
    "# AI Context",
    "",
    "## Gate Interpretation",
    "",
    `- status: ${digest.gate.status}`,
    `- confidence: ${digest.gate.confidence}`,
    `- resultSource: ${digest.gate.resultSource}`,
    digest.gate.status === "no-failure-observed"
      ? "- This sample has structured logs but no Validation entries and no artifacts, so it can only be treated as no-failure-observed."
      : "- Use the gate status together with failures/index.md before deciding whether this is code, test, log, runner or profile work.",
    "",
    "## Failure Focus",
    "",
    digest.failedFlows.length === 0 && digest.failures.length === 0
      ? "- No failed/warned flow or structured failure entries were found."
      : [
        ...digest.failedFlows.map((flow) => `- flow ${flow.type} outcome=${flow.outcome} failedStep=${flow.failedStep ?? ""} reasonCode=${flow.reasonCode ?? ""} rawRef=${flow.rawRef}`),
        ...digest.failures.map((failure) => `- ${failure.source}: ${failure.owner ?? ""}/${failure.operation ?? ""} ${failure.message ?? failure.path}`),
      ].join("\n"),
    "",
    "## Top Noise And Actions",
    "",
    markdownTable(
      ["owner", "context", "operation", "count", "action"],
      digest.topNoise.map((item) => [item.owner, item.context, item.operation, item.count, item.recommendedAction]),
    ),
    "",
    "## Flow Digest",
    "",
    markdownTable(
      ["owner", "context", "operation", "flows", "completed", "succeeded", "failed", "singleEntry", "missingDuration"],
      digest.flows.slice(0, 12).map((item) => [item.owner, item.context, item.operation, item.count, item.completed, item.succeeded, item.failed, item.singleEntry, item.missingDuration]),
    ),
    "",
    "## Aggregated Success Templates",
    "",
    markdownTable(
      ["owner", "context", "operation", "outcome", "count", "metrics"],
      digest.noiseTemplates.slice(0, 12).map((template) => [template.owner, template.context, template.operation, template.outcome, template.count, Object.entries(template.metrics).map(([key, value]) => `${key}:${value.min}/${value.avg}/${value.max}`).join(", ")]),
    ),
    "",
    "## Semantic Missing Fields",
    "",
    markdownTable(
      ["owner", "context", "operation", "count", "issues", "action"],
      digest.semanticMissingGroups.slice(0, 12).map((item) => [item.owner, item.context, item.operation, item.count, Object.entries(item.issues).map(([key, count]) => `${key}:${count}`).join(", "), item.recommendedAction]),
    ),
    "",
    "## Owner Docs",
    "",
    ownerDocs.length === 0 ? "- none" : ownerDocs.map((doc) => `- ${doc}`).join("\n"),
    "",
    "## Next Queries",
    "",
    "```bash",
    "Workspace/Tools/logctl/logctl query --analysis-dir <analysis> owner=TargetSelector operation=TargetQueryEntities --format md",
    "Workspace/Tools/logctl/logctl query --analysis-dir <analysis> owner=ObjectPool operation=ObjectPoolRelease --format md",
    "Workspace/Tools/logctl/logctl query --analysis-dir <analysis> owner=Runtime operation=HealthBarUI --format md",
    "Workspace/Tools/logctl/logctl query --analysis-dir <analysis> severity>=Warn --format md",
    "```",
    "",
    "## Profile Override Candidates",
    "",
    "- Prefer owner/context/operation budget rules over raising the global level.",
    "- Keep Validation and structured failures unbudgeted.",
    "- Use logctl suggest --run-dir <run> --dry-run for a reviewable profilePatch.",
    "",
  ].join("\n");
}

function noiseMd(noise) {
  return [
    "# Top Log Noise",
    "",
    markdownTable(
      ["rank", "owner", "context", "operation", "count", "sample messages", "recommended action"],
      noise.slice(0, 25).map((item, index) => [
        index + 1,
        item.owner,
        item.context,
        item.operation,
        item.count,
        item.sampleMessages.slice(0, 3).join(" / "),
        item.recommendedAction,
      ]),
    ),
    "",
  ].join("\n");
}

function templatesMd(templates) {
  return [
    "# Aggregated Success Templates",
    "",
    markdownTable(
      ["rank", "owner", "context", "operation", "outcome", "count", "metrics", "raw refs"],
      templates.slice(0, 25).map((item, index) => [
        index + 1,
        item.owner,
        item.context,
        item.operation,
        item.outcome,
        item.count,
        Object.entries(item.metrics).map(([key, value]) => `${key}:${value.min}/${value.avg}/${value.max}`).join(", "),
        item.rawRefs.join(", "),
      ]),
    ),
    "",
  ].join("\n");
}

function missingFieldsMd(semanticMissing, requiredMissing) {
  return [
    "# Missing Fields",
    "",
    "## Semantic Totals",
    "",
    markdownTable(
      ["issue", "count"],
      Object.entries(semanticMissing.totals).map(([issue, count]) => [issue, count]),
    ),
    "",
    "## Owner Tasks",
    "",
    markdownTable(
      ["owner", "context", "operation", "count", "issues", "owner doc", "recommended action"],
      semanticMissing.groups.slice(0, 50).map((item) => [
        item.owner,
        item.context,
        item.operation,
        item.count,
        Object.entries(item.issues).map(([key, count]) => `${key}:${count}`).join(", "),
        item.ownerDoc,
        item.recommendedAction,
      ]),
    ),
    "",
    "## Envelope Missing",
    "",
    requiredMissing.length === 0
      ? "- none"
      : markdownTable(
        ["source", "line", "missing"],
        requiredMissing.slice(0, 50).map((item) => [item.source, item.line, item.missing.join(", ")]),
      ),
    "",
  ].join("\n");
}

function flowsIndexMd(flows, flowConclusions) {
  return [
    "# Flow Index",
    "",
    "Each JSONL line in `flows.jsonl` is one flow conclusion object. Raw entries are only referenced by rawRef.",
    "",
    "## Outcome Summary",
    "",
    markdownTable(
      ["owner", "context", "operation", "flows", "completed", "succeeded", "failed", "skipped", "warnings", "singleEntry", "missingDuration", "sample refs"],
      flows.slice(0, 50).map((item) => [
        item.owner,
        item.context,
        item.operation,
        item.count,
        item.completed,
        item.succeeded,
        item.failed,
        item.skipped,
        item.warnings,
        item.singleEntry,
        item.missingDuration,
        item.sampleLines.join(", "),
      ]),
    ),
    "",
    "## Failed Or Warned Flow Conclusions",
    "",
    flowConclusions.filter((flow) => normalizeStatus(flow.outcome) === "failed" || flow.warningCount > 0).length === 0
      ? "- none"
      : markdownTable(
        ["flowId", "type", "outcome", "failedStep", "reasonCode", "rawRef"],
        flowConclusions
          .filter((flow) => normalizeStatus(flow.outcome) === "failed" || flow.warningCount > 0)
          .slice(0, 50)
          .map((flow) => [flow.flowId, flow.type, flow.outcome, flow.failedStep ?? "", flow.reasonCode ?? "", flow.rawRef]),
      ),
    "",
  ].join("\n");
}

function failuresIndexMd(gateReport) {
  return [
    "# Failures",
    "",
    gateReport.failures.length === 0
      ? "- none"
      : markdownTable(
        ["source", "path", "line", "owner", "operation", "message"],
        gateReport.failures.map((failure) => [failure.source, failure.path ?? "", failure.line ?? "", failure.owner ?? "", failure.operation ?? "", failure.message ?? ""]),
      ),
    "",
  ].join("\n");
}

function countTextLines(text) {
  if (!text) {
    return 0;
  }
  return text.endsWith("\n") ? text.split(/\r?\n/).length - 1 : text.split(/\r?\n/).length;
}

async function analyze(options) {
  const run = await loadRun(options.runDir);
  const outDir = resolveInside(options.out ?? path.join(options.runDir, "analysis"), "out");
  const gateReport = buildGateReport(run);
  const flowConclusions = buildFlowConclusions(run.entries);
  const templates = buildNoiseTemplates(flowConclusions);
  const templatedSuccessKeys = new Set(templates.filter((template) => template.count > 10).map((template) => template.groupKey));
  const flowConclusionsForOutput = flowConclusions.filter((flow) => shouldKeepFlowConclusion(flow, templatedSuccessKeys));
  const noise = noiseSummary(run.entries);
  const requiredMissing = missingFields(run.entries);
  const semanticMissing = semanticMissingFields(run.entries);
  const digest = buildDigest(run, gateReport, flowConclusions, noise, semanticMissing, templates);
  const flowJsonlText = `${flowConclusionsForOutput.map((entry) => JSON.stringify(entry)).join("\n")}${flowConclusionsForOutput.length > 0 ? "\n" : ""}`;
  const templatesJsonlText = `${templates.map((entry) => JSON.stringify(entry)).join("\n")}${templates.length > 0 ? "\n" : ""}`;
  const flowsIndexText = flowsIndexMd(digest.flows, flowConclusions);
  const failuresIndexText = failuresIndexMd(gateReport);
  const noiseText = noiseMd(noise);
  const templatesText = templatesMd(templates);
  const missingText = missingFieldsMd(semanticMissing, requiredMissing);
  const summaryText = summaryMd(digest);
  const aiContextText = aiContext(digest);
  const readableLineCount = [
    flowJsonlText,
    templatesJsonlText,
    flowsIndexText,
    failuresIndexText,
    noiseText,
    templatesText,
    missingText,
    summaryText,
    aiContextText,
  ].reduce((sum, text) => sum + countTextLines(text), 0);
  gateReport.counts.flowConclusions = flowConclusions.length;
  gateReport.counts.outputFlowConclusions = flowConclusionsForOutput.length;
  gateReport.counts.templatedSuccessFlows = flowConclusions.length - flowConclusionsForOutput.length;
  gateReport.counts.noiseTemplates = templates.length;
  gateReport.analysisQuality = {
    rawLines: run.entries.length,
    defaultReadableLines: readableLineCount,
    defaultReadableRatio: run.entries.length === 0 ? 0 : Number((readableLineCount / run.entries.length).toFixed(3)),
    defaultReadableWithinRaw: readableLineCount < run.entries.length,
    removedDefaultCopies: true,
  };

  await mkdir(outDir, { recursive: true });
  await removeDeprecatedAnalysisCopies(outDir);
  await writeJsonl(path.join(outDir, "raw", "entries.jsonl"), run.entries);
  await writeJson(path.join(outDir, "raw", "results.json"), run.results);
  await writeJson(path.join(outDir, "raw", "artifacts.json"), run.artifacts);

  await writeText(path.join(outDir, "flows", "flows.jsonl"), flowJsonlText);
  await writeText(path.join(outDir, "flows", "index.md"), flowsIndexText);
  await writeJson(path.join(outDir, "failures", "failures.json"), gateReport.failures);
  await writeText(path.join(outDir, "failures", "index.md"), failuresIndexText);
  await writeJson(path.join(outDir, "noise", "summary.json"), noise);
  await writeText(path.join(outDir, "noise", "top-contexts.md"), noiseText);
  await writeText(path.join(outDir, "noise", "templates.jsonl"), templatesJsonlText);
  await writeText(path.join(outDir, "noise", "templates.md"), templatesText);
  await writeJson(path.join(outDir, "missing-fields", "missing-fields.json"), {
    envelope: requiredMissing,
    semantic: semanticMissing,
  });
  await writeText(path.join(outDir, "missing-fields", "index.md"), missingText);
  await writeJson(path.join(outDir, "gate-report.json"), gateReport);
  await writeText(path.join(outDir, "summary.md"), summaryText);
  await writeText(path.join(outDir, "ai-context.md"), aiContextText);

  console.log(JSON.stringify({ analysisDir: toRel(outDir), gateReport: toRel(path.join(outDir, "gate-report.json")), ...gateReport }, null, 2));
}

function semanticQueryRecord(entry, queryKind) {
  const owner = displayValue(getField(entry, "owner"));
  const operation = displayValue(getField(entry, "operation"));
  const message = queryKind === "success-template"
    ? `success template ${owner}/${operation} count=${entry.count ?? 0}`
    : `flow conclusion ${owner}/${operation} outcome=${entry.outcome ?? "unknown"}`;
  return {
    ...entry,
    queryKind,
    severity: entry.severity ?? "Info",
    validationStatus: entry.validationStatus ?? "None",
    message: entry.message ?? message,
  };
}

async function loadAnalysisQueryEntries(analysisDir) {
  const root = resolveInside(analysisDir, "analysis-dir");
  const flowConclusions = (await readJsonlIfExists(path.join(root, "flows", "flows.jsonl")))
    .map((entry) => semanticQueryRecord(entry, "flow-conclusion"));
  const successTemplates = (await readJsonlIfExists(path.join(root, "noise", "templates.jsonl")))
    .map((entry) => semanticQueryRecord(entry, "success-template"));
  return [...flowConclusions, ...successTemplates];
}

async function loadQueryEntries(options) {
  if (options.file) {
    return readJsonl(resolveInside(options.file, "file"));
  }
  if (options.analysisDir) {
    return loadAnalysisQueryEntries(options.analysisDir);
  }
  if (options.runDir) {
    return (await loadRun(options.runDir)).entries;
  }
  throw new Error("query requires --analysis-dir, --file or --run-dir.");
}

async function query(options) {
  const entries = (await loadQueryEntries(options)).filter((entry) => options.filters.every((filter) => matchesFilter(entry, filter)));
  if (options.format === "json") {
    console.log(JSON.stringify(entries, null, 2));
    return;
  }

  const lines = ["# logctl query", "", `matches: ${entries.length}`, ""];
  for (const entry of entries.slice(0, 80)) {
    lines.push(`- ${getField(entry, "severity") ?? ""} ${getField(entry, "owner") ?? ""}/${getField(entry, "context") ?? ""} operation=${getField(entry, "operation") ?? ""} validation=${getField(entry, "validationStatus") ?? ""} ${getField(entry, "message") ?? ""} (${entry.__source}:${entry.__line})`);
  }
  console.log(lines.join("\n"));
}

async function readStdin() {
  return new Promise((resolve, reject) => {
    let data = "";
    process.stdin.setEncoding("utf8");
    process.stdin.on("data", (chunk) => {
      data += chunk;
    });
    process.stdin.on("end", () => resolve(data));
    process.stdin.on("error", reject);
  });
}

async function ingest(options) {
  if (!options.stdin) {
    throw new Error("ingest currently requires --stdin.");
  }
  const outDir = resolveInside(options.out, "out");
  const rawDir = path.join(outDir, "raw");
  await mkdir(rawDir, { recursive: true });
  const input = await readStdin();
  await writeFile(path.join(rawDir, "legacy-stdout.log"), input, "utf8");
  await writeJson(path.join(outDir, "index.json"), {
    generatedAtUtc: new Date().toISOString(),
    resultSource: "legacy-stdout-fallback",
    source: options.source ?? "legacy-stdout",
    raw: toRel(path.join(rawDir, "legacy-stdout.log")),
  });
  console.log(JSON.stringify({ runDir: toRel(outDir), resultSource: "legacy-stdout-fallback" }, null, 2));
}

async function suggest(options) {
  const run = await loadRun(options.runDir);
  const suggestions = noiseSummary(run.entries)
    .filter((item) => item.count > 10)
    .map((item) => ({
      owner: item.owner,
      context: item.context,
      operation: item.operation,
      suggestion: item.recommendedAction,
      count: item.count,
      sampleMessages: item.sampleMessages,
      profilePatch: {
        rules: [
          {
            owner: item.owner === "unknown" ? undefined : item.owner,
            context: item.context === "unknown" ? undefined : item.context,
            operation: item.operation === "unknown" ? undefined : item.operation,
            budgetPerSecond: Math.max(1, Math.ceil(item.count / 10)),
            minimumSeverity: "Info",
          },
        ],
      },
    }));
  console.log(JSON.stringify({
    dryRun: options.dryRun,
    runDir: toRel(run.root),
    generatedAtUtc: new Date().toISOString(),
    suggestions,
  }, null, 2));
}

async function main() {
  const options = parseArgs(process.argv.slice(2));
  if (options.command === "profile") {
    await profile(options);
  } else if (options.command === "analyze") {
    await analyze(options);
  } else if (options.command === "query") {
    await query(options);
  } else if (options.command === "ingest") {
    await ingest(options);
  } else if (options.command === "suggest") {
    await suggest(options);
  } else {
    usage();
    process.exitCode = 1;
  }
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
