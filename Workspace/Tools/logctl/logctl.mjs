#!/usr/bin/env node

import { mkdir, readFile, readdir, writeFile } from "node:fs/promises";
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
        if (entry.name === "analysis") {
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
      const parsed = JSON.parse(trimmed);
      parsed.__source = toRel(filePath);
      parsed.__line = index + 1;
      entries.push(parsed);
    } catch {
      entries.push({
        channel: "Diagnostics",
        severity: "Warn",
        message: "invalid jsonl line",
        fields: { line: index + 1, value: trimmed },
        __source: toRel(filePath),
        __line: index + 1,
      });
    }
  }
  return entries;
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
  const status = normalizeStatus(entry.validationStatus);
  if (status === "pass") {
    return "pass";
  }
  if (status === "fail") {
    return "fail";
  }
  return null;
}

function entryKey(entry, key) {
  if (Object.prototype.hasOwnProperty.call(entry, key)) {
    return entry[key];
  }
  if (entry.fields && Object.prototype.hasOwnProperty.call(entry.fields, key)) {
    return entry.fields[key];
  }
  return undefined;
}

function matchesFilter(entry, filter) {
  const comparison = filter.match(/^([^><=]+)(>=)(.+)$/);
  if (comparison) {
    const [, key, , expected] = comparison;
    if (key.trim() !== "severity") {
      return true;
    }
    const actualRank = severityRank.get(String(entry.severity ?? "").toLowerCase()) ?? -1;
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

function buildGateReport(run) {
  const entryFailures = run.entries.filter((entry) => entryStatus(entry) === "fail" || String(entry.severity ?? "").toLowerCase() === "error");
  const artifactFailures = run.artifacts.filter((artifact) => artifact.status === "fail");
  const artifactPasses = run.artifacts.filter((artifact) => artifact.status === "pass");
  const structuredValidation = run.entries.filter((entry) => normalizeStatus(entry.channel) === "validation");
  const hasStdoutFallback = run.results.some((result) => {
    const data = result.data;
    return Boolean(data?.firstError || String(data?.failureReason ?? "").includes("TestFailMarker"));
  });

  let resultSource = "exit-code";
  if (run.artifacts.length > 0) {
    resultSource = "artifact";
  } else if (structuredValidation.length > 0 || run.entries.length > 0) {
    resultSource = "structured-log";
  } else if (hasStdoutFallback || run.stdoutFiles.length > 0) {
    resultSource = "stdout-pattern-fallback";
  }

  return {
    generatedAtUtc: new Date().toISOString(),
    runDir: toRel(run.root),
    resultSource,
    status: artifactFailures.length > 0 || entryFailures.length > 0 ? "failed" : "passed",
    counts: {
      entries: run.entries.length,
      validationEntries: structuredValidation.length,
      artifacts: run.artifacts.length,
      artifactPasses: artifactPasses.length,
      artifactFailures: artifactFailures.length,
      structuredFailures: entryFailures.length,
      stdoutFallbackFiles: run.stdoutFiles.length,
    },
    failures: [
      ...artifactFailures.map((artifact) => ({ source: "artifact", path: artifact.path, status: artifact.status })),
      ...entryFailures.map((entry) => ({ source: "structured-log", path: entry.__source, line: entry.__line, owner: entry.owner, operation: entry.operation, message: entry.message })),
    ],
  };
}

function missingFields(entries) {
  const required = ["runElapsedMs", "frame", "physicsFrame", "severity", "owner", "context", "operation", "message"];
  return entries
    .map((entry) => ({
      source: entry.__source,
      line: entry.__line,
      missing: required.filter((field) => entry[field] === undefined || entry[field] === null || entry[field] === ""),
    }))
    .filter((item) => item.missing.length > 0);
}

function noiseSummary(entries) {
  const counts = new Map();
  for (const entry of entries) {
    const key = [entry.owner ?? "unknown", entry.context ?? "unknown", entry.operation ?? "unknown", entry.message ?? ""].join("|");
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  return [...counts.entries()]
    .map(([key, count]) => {
      const [owner, context, operation, message] = key.split("|");
      return { owner, context, operation, message, count };
    })
    .sort((a, b) => b.count - a.count)
    .slice(0, 50);
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

function aiContext(gateReport, entries) {
  const owners = [...new Set(entries.map((entry) => entry.owner ?? "unknown"))].sort();
  const failures = gateReport.failures.slice(0, 20);
  return [
    "# Log Analysis Context",
    "",
    `- runDir: ${gateReport.runDir}`,
    `- resultSource: ${gateReport.resultSource}`,
    `- status: ${gateReport.status}`,
    `- entries: ${gateReport.counts.entries}`,
    `- validationEntries: ${gateReport.counts.validationEntries}`,
    `- artifacts: ${gateReport.counts.artifacts}`,
    `- owners: ${owners.join(", ") || "none"}`,
    "",
    "## Failure Focus",
    "",
    failures.length === 0 ? "- none" : failures.map((failure) => `- ${failure.source}: ${failure.owner ?? ""} ${failure.operation ?? ""} ${failure.message ?? failure.path}`).join("\n"),
    "",
    "## Query Examples",
    "",
    "```bash",
    "logctl query --analysis-dir <run>/analysis owner=Ability",
    "logctl query --analysis-dir <run>/analysis severity>=Warn",
    "```",
    "",
  ].join("\n");
}

async function analyze(options) {
  const run = await loadRun(options.runDir);
  const outDir = resolveInside(options.out ?? path.join(options.runDir, "analysis"), "out");
  const gateReport = buildGateReport(run);

  await mkdir(outDir, { recursive: true });
  await writeJsonl(path.join(outDir, "raw", "entries.jsonl"), run.entries);
  await writeJson(path.join(outDir, "raw", "results.json"), run.results);
  await writeJson(path.join(outDir, "raw", "artifacts.json"), run.artifacts);

  for (const [owner, entries] of groupBy(run.entries, "owner")) {
    await writeJsonl(path.join(outDir, "by-owner", `${owner}.jsonl`), entries);
  }
  for (const [phase, entries] of groupBy(run.entries, "phase")) {
    await writeJsonl(path.join(outDir, "by-phase", `${phase}.jsonl`), entries);
  }

  const flowEntries = run.entries.filter((entry) => normalizeStatus(entry.channel) === "flow" || entry.operation);
  await writeJson(path.join(outDir, "flows", "flows.json"), flowEntries);
  await writeJson(path.join(outDir, "failures", "failures.json"), gateReport.failures);
  await writeJson(path.join(outDir, "noise", "summary.json"), noiseSummary(run.entries));
  await writeJson(path.join(outDir, "missing-fields", "missing-fields.json"), missingFields(run.entries));
  await writeJson(path.join(outDir, "gate-report.json"), gateReport);
  await writeFile(path.join(outDir, "ai-context.md"), aiContext(gateReport, run.entries), "utf8");

  console.log(JSON.stringify({ analysisDir: toRel(outDir), gateReport: toRel(path.join(outDir, "gate-report.json")), ...gateReport }, null, 2));
}

async function loadQueryEntries(options) {
  if (options.file) {
    return readJsonl(resolveInside(options.file, "file"));
  }
  if (options.analysisDir) {
    const file = path.join(resolveInside(options.analysisDir, "analysis-dir"), "raw", "entries.jsonl");
    return readJsonl(file);
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
    lines.push(`- ${entry.severity ?? ""} ${entry.owner ?? ""}/${entry.context ?? ""} operation=${entry.operation ?? ""} validation=${entry.validationStatus ?? ""} ${entry.message ?? ""} (${entry.__source}:${entry.__line})`);
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
      suggestion: "consider budget/sample or summary-only stdout",
      count: item.count,
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
