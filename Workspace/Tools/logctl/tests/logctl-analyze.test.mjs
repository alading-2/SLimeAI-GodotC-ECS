import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";

const repoRoot = process.cwd();
const logctl = path.join(repoRoot, "Workspace", "Tools", "logctl", "logctl.mjs");

function makeEntry(overrides = {}) {
  return {
    runElapsedMs: 100,
    frame: 10,
    physicsFrame: 20,
    severity: "Info",
    outcome: "None",
    validationStatus: "None",
    channel: "Runtime",
    owner: "Runtime",
    context: "Fixture",
    operation: "Fixture",
    phase: "Runtime",
    message: "fixture",
    fields: {},
    ...overrides,
  };
}

function writeRun(entries) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "slimeai-logctl-"));
  const rawDir = path.join(root, "raw");
  fs.mkdirSync(rawDir, { recursive: true });
  fs.writeFileSync(
    path.join(rawDir, "scene-log.jsonl"),
    `${entries.map((entry) => JSON.stringify(entry)).join("\n")}\n`,
    "utf8",
  );
  return root;
}

function readJsonl(filePath) {
  return fs.readFileSync(filePath, "utf8")
    .trim()
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => JSON.parse(line));
}

test("analyze outputs flow conclusions and semantic templates instead of raw bucket copies", () => {
  const entries = [
    makeEntry({
      channel: "Flow",
      owner: "Ability",
      context: "AbilitySystem",
      operation: "AbilityTryTrigger",
      phase: "Ability",
      outcome: "Started",
      correlationId: "ability-flow-1",
      message: "flow started",
      fields: { entryType: "flow_start", entityId: "player_001", abilityId: "dash" },
    }),
    makeEntry({
      channel: "Flow",
      owner: "Ability",
      context: "AbilitySystem",
      operation: "AbilityTryTrigger",
      phase: "Ability",
      outcome: "None",
      correlationId: "ability-flow-1",
      message: "cooldown check passed",
      fields: { entryType: "flow_step", stepIndex: 1, stepName: "CooldownCheck", durationMs: 3 },
    }),
    makeEntry({
      channel: "Flow",
      owner: "Ability",
      context: "AbilitySystem",
      operation: "AbilityTryTrigger",
      phase: "Ability",
      severity: "Warn",
      outcome: "Failed",
      correlationId: "ability-flow-1",
      message: "target query failed",
      fields: {
        entryType: "flow_step",
        stepIndex: 2,
        stepName: "TargetQuery",
        durationMs: 7,
        reasonCode: "NoCandidateInRange",
      },
    }),
    makeEntry({
      channel: "Flow",
      owner: "Ability",
      context: "AbilitySystem",
      operation: "AbilityTryTrigger",
      phase: "Ability",
      severity: "Error",
      outcome: "Failed",
      correlationId: "ability-flow-1",
      message: "AbilityTryTrigger failed",
      fields: {
        entryType: "flow_summary",
        durationMs: 9,
        stepCount: 2,
        entityId: "player_001",
        abilityId: "dash",
        reasonCode: "NoCandidateInRange",
      },
    }),
  ];

  for (let index = 0; index < 420; index += 1) {
    entries.push(makeEntry({
      runElapsedMs: 200 + index,
      frame: 20 + index,
      physicsFrame: 40 + index,
      channel: "Flow",
      owner: "TargetSelector",
      context: "TargetQueryEngine",
      operation: "TargetQueryEntities",
      phase: "Targeting",
      outcome: "Completed",
      correlationId: `target-query-${index}`,
      message: "TargetQueryEntities completed",
      fields: {
        entryType: "flow_summary",
        durationMs: index % 5,
        stepCount: 0,
        candidateCount: 10 + index,
        returnedCount: index % 4,
        reasonCode: "ok",
      },
    }));
  }

  const runDir = writeRun(entries);
  const outDir = path.join(runDir, "analysis");
  execFileSync(process.execPath, [logctl, "analyze", "--run-dir", runDir, "--out", outDir], {
    cwd: repoRoot,
    stdio: "pipe",
    encoding: "utf8",
  });

  assert.equal(fs.existsSync(path.join(outDir, "by-owner")), false);
  assert.equal(fs.existsSync(path.join(outDir, "by-phase")), false);
  assert.equal(fs.existsSync(path.join(outDir, "flows", "flows.json")), false);

  const flowConclusions = readJsonl(path.join(outDir, "flows", "flows.jsonl"));
  const ability = flowConclusions.find((flow) => flow.flowId === "ability-flow-1");
  assert.ok(ability);
  assert.equal(ability.outcome, "Failed");
  assert.equal(ability.failedStep, "TargetQuery");
  assert.equal(ability.reasonCode, "NoCandidateInRange");
  assert.equal(ability.entryCount, 4);
  assert.match(ability.rawRef, /scene-log\.jsonl:1-4$/);

  const templates = readJsonl(path.join(outDir, "noise", "templates.jsonl"));
  const targetTemplate = templates.find((template) => template.owner === "TargetSelector" && template.operation === "TargetQueryEntities");
  assert.ok(targetTemplate);
  assert.equal(targetTemplate.count, 420);
  assert.equal(targetTemplate.outcome, "Completed");
  assert.deepEqual(targetTemplate.metrics.candidateCount, { min: 10, avg: 219.5, max: 429 });

  const summary = fs.readFileSync(path.join(outDir, "summary.md"), "utf8");
  assert.match(summary, /## Flow Outcomes/);
  assert.match(summary, /AbilityTryTrigger/);
  assert.match(summary, /NoCandidateInRange/);

  const gateReport = JSON.parse(fs.readFileSync(path.join(outDir, "gate-report.json"), "utf8"));
  assert.ok(gateReport.analysisQuality.defaultReadableLines < gateReport.analysisQuality.rawLines);

  const targetQuery = JSON.parse(execFileSync(
    process.execPath,
    [logctl, "query", "--analysis-dir", outDir, "owner=TargetSelector", "operation=TargetQueryEntities", "--format", "json"],
    { cwd: repoRoot, stdio: "pipe", encoding: "utf8" },
  ));
  assert.equal(targetQuery.length, 1);
  assert.equal(targetQuery[0].queryKind, "success-template");
  assert.equal(targetQuery[0].count, 420);

  const abilityQuery = JSON.parse(execFileSync(
    process.execPath,
    [logctl, "query", "--analysis-dir", outDir, "owner=Ability", "operation=AbilityTryTrigger", "--format", "json"],
    { cwd: repoRoot, stdio: "pipe", encoding: "utf8" },
  ));
  assert.equal(abilityQuery.length, 1);
  assert.equal(abilityQuery[0].queryKind, "flow-conclusion");
  assert.equal(abilityQuery[0].outcome, "Failed");
  assert.equal(abilityQuery[0].entryCount, 4);
});

test("analyze removes stale raw bucket copies from an existing output directory", () => {
  const runDir = writeRun([
    makeEntry({
      channel: "Flow",
      owner: "Ability",
      context: "AbilitySystem",
      operation: "AbilityTryTrigger",
      phase: "Ability",
      outcome: "Completed",
      correlationId: "ability-flow-stale-cleanup",
      fields: { entryType: "flow_summary", durationMs: 1, stepCount: 1 },
    }),
  ]);
  const outDir = path.join(runDir, "analysis");
  fs.mkdirSync(path.join(outDir, "by-owner"), { recursive: true });
  fs.mkdirSync(path.join(outDir, "by-phase"), { recursive: true });
  fs.mkdirSync(path.join(outDir, "flows"), { recursive: true });
  fs.writeFileSync(path.join(outDir, "by-owner", "Ability.jsonl"), "{}\n", "utf8");
  fs.writeFileSync(path.join(outDir, "by-phase", "Ability.jsonl"), "{}\n", "utf8");
  fs.writeFileSync(path.join(outDir, "flows", "flows.json"), "[]\n", "utf8");

  execFileSync(process.execPath, [logctl, "analyze", "--run-dir", runDir, "--out", outDir], {
    cwd: repoRoot,
    stdio: "pipe",
    encoding: "utf8",
  });

  assert.equal(fs.existsSync(path.join(outDir, "by-owner")), false);
  assert.equal(fs.existsSync(path.join(outDir, "by-phase")), false);
  assert.equal(fs.existsSync(path.join(outDir, "flows", "flows.json")), false);
});

test("query analysis dir does not fall back to raw entries when semantic index is empty", () => {
  const runDir = writeRun([
    makeEntry({
      channel: "Runtime",
      owner: "Runtime",
      context: "Fixture",
      operation: "RuntimeOnly",
      message: "runtime-only entry",
    }),
  ]);
  const outDir = path.join(runDir, "analysis");
  execFileSync(process.execPath, [logctl, "analyze", "--run-dir", runDir, "--out", outDir], {
    cwd: repoRoot,
    stdio: "pipe",
    encoding: "utf8",
  });

  const semanticQuery = JSON.parse(execFileSync(
    process.execPath,
    [logctl, "query", "--analysis-dir", outDir, "owner=Runtime", "operation=RuntimeOnly", "--format", "json"],
    { cwd: repoRoot, stdio: "pipe", encoding: "utf8" },
  ));
  assert.equal(semanticQuery.length, 0);

  const rawQuery = JSON.parse(execFileSync(
    process.execPath,
    [logctl, "query", "--file", path.join(outDir, "raw", "entries.jsonl"), "owner=Runtime", "operation=RuntimeOnly", "--format", "json"],
    { cwd: repoRoot, stdio: "pipe", encoding: "utf8" },
  ));
  assert.equal(rawQuery.length, 1);
});
