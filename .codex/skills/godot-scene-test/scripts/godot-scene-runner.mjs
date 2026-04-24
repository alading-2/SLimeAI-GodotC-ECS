#!/usr/bin/env node

import { access, readdir } from "node:fs/promises";
import { constants } from "node:fs";
import path from "node:path";
import { spawn } from "node:child_process";

const PROJECT_ROOT = process.cwd();
const DEFAULT_SCAN_ROOT = "Src/ECS/Test";
const DEFAULT_GODOT =
  "/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64";
const DEFAULT_TIMEOUT_MS = 60000;
const FAILURE_PATTERNS = [
  "C# Script Error",
  "Cannot instantiate C# script",
  "Unhandled exception",
  "Exception",
  "[FAIL]",
  "FAIL:",
  "[失败]",
  "Failed to load",
  "scene not found",
];

function printUsage() {
  console.error(`Usage:
  node scripts/godot-scene-runner.mjs list [--all]
  node scripts/godot-scene-runner.mjs run <scene-path> [--build] [--godot <path>] [--timeout <ms>]
  node scripts/godot-scene-runner.mjs run-all [--build] [--continue-on-fail] [--godot <path>] [--timeout <ms>] [--all]

Scene paths may be res:// paths or project-relative local paths.`);
}

function parseArgs(argv) {
  const [command, ...rest] = argv;
  const options = {
    command,
    scene: null,
    build: false,
    continueOnFail: false,
    includeAll: false,
    godot: null,
    timeoutMs: DEFAULT_TIMEOUT_MS,
  };

  for (let i = 0; i < rest.length; i += 1) {
    const value = rest[i];

    if (value === "--build") {
      options.build = true;
      continue;
    }

    if (value === "--continue-on-fail") {
      options.continueOnFail = true;
      continue;
    }

    if (value === "--all") {
      options.includeAll = true;
      continue;
    }

    if (value === "--godot") {
      if (!rest[i + 1] || rest[i + 1].startsWith("--")) {
        throw new Error("--godot requires an executable path.");
      }
      options.godot = rest[i + 1];
      i += 1;
      continue;
    }

    if (value === "--timeout") {
      const parsed = Number.parseInt(rest[i + 1], 10);
      if (!Number.isFinite(parsed) || parsed <= 0) {
        throw new Error("--timeout must be a positive integer in milliseconds.");
      }
      options.timeoutMs = parsed;
      i += 1;
      continue;
    }

    if (value.startsWith("--")) {
      throw new Error(`Unknown option: ${value}`);
    }

    if (options.scene === null) {
      options.scene = value;
      continue;
    }

    throw new Error(`Unexpected argument: ${value}`);
  }

  return options;
}

function normalizeSlashes(value) {
  return value.replaceAll(path.sep, "/");
}

function resToLocal(scenePath) {
  if (scenePath.startsWith("res://")) {
    return scenePath.slice("res://".length);
  }

  return scenePath;
}

function localToRes(localPath) {
  return `res://${normalizeSlashes(localPath).replace(/^\.?\//, "")}`;
}

async function pathExists(filePath, executable = false) {
  try {
    await access(filePath, executable ? constants.X_OK : constants.F_OK);
    return true;
  } catch {
    return false;
  }
}

function isExcludedScene(localPath) {
  const normalized = normalizeSlashes(localPath);
  const segments = normalized.split("/");
  const basename = path.posix.basename(normalized);

  return segments.includes("Entity") || basename.endsWith("TestEntity.tscn");
}

async function collectScenes(root = DEFAULT_SCAN_ROOT, includeAll = false) {
  if (!(await pathExists(root))) {
    throw new Error(`Scan path not found: ${root}`);
  }

  const scenes = [];

  async function walk(dir) {
    const entries = await readdir(dir, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        await walk(fullPath);
        continue;
      }

      if (!entry.isFile() || !entry.name.endsWith(".tscn")) {
        continue;
      }

      const localPath = normalizeSlashes(path.relative(PROJECT_ROOT, fullPath));
      if (!includeAll && isExcludedScene(localPath)) {
        continue;
      }

      scenes.push(localToRes(localPath));
    }
  }

  await walk(root);
  return scenes.sort((a, b) => a.localeCompare(b));
}

async function resolveScene(scenePath) {
  if (!scenePath) {
    throw new Error("Scene path is required.");
  }

  const localPath = resToLocal(scenePath);
  const absolutePath = path.resolve(PROJECT_ROOT, localPath);

  if (!absolutePath.startsWith(`${PROJECT_ROOT}${path.sep}`) && absolutePath !== PROJECT_ROOT) {
    throw new Error(`Scene path must be inside project: ${scenePath}`);
  }

  if (!(await pathExists(absolutePath))) {
    throw new Error(`Scene not found: ${scenePath}`);
  }

  if (!absolutePath.endsWith(".tscn")) {
    throw new Error(`Scene must be a .tscn file: ${scenePath}`);
  }

  return localToRes(path.relative(PROJECT_ROOT, absolutePath));
}

async function resolveGodot(explicitGodot) {
  if (explicitGodot) {
    if (explicitGodot.includes("/") || explicitGodot.startsWith(".")) {
      if (await pathExists(explicitGodot, true)) {
        return explicitGodot;
      }

      throw new Error(`Godot executable not found: ${explicitGodot}`);
    }

    const result = await runProcess("which", [explicitGodot], { timeoutMs: 5000 });
    if (result.exitCode === 0 && result.stdout.trim().length > 0) {
      return explicitGodot;
    }

    throw new Error(`Godot executable not found: ${explicitGodot}`);
  }

  const candidates = [
    process.env.GODOT_BIN,
    process.env.GODOT_PATH,
    DEFAULT_GODOT,
    "godot",
  ].filter(Boolean);

  for (const candidate of candidates) {
    if (candidate.includes("/") || candidate.startsWith(".")) {
      if (await pathExists(candidate, true)) {
        return candidate;
      }
      continue;
    }

    const result = await runProcess("which", [candidate], { timeoutMs: 5000 });
    if (result.exitCode === 0 && result.stdout.trim().length > 0) {
      return candidate;
    }
  }

  throw new Error(`Godot executable not found. Tried: ${candidates.join(", ")}`);
}

function runProcess(command, args, options = {}) {
  const timeoutMs = options.timeoutMs ?? DEFAULT_TIMEOUT_MS;

  return new Promise((resolve) => {
    const child = spawn(command, args, {
      cwd: PROJECT_ROOT,
      env: process.env,
      shell: false,
    });

    let stdout = "";
    let stderr = "";
    let timedOut = false;

    const timer = setTimeout(() => {
      timedOut = true;
      child.kill("SIGTERM");
      setTimeout(() => {
        if (!child.killed) {
          child.kill("SIGKILL");
        }
      }, 2000).unref();
    }, timeoutMs);

    child.stdout.on("data", (chunk) => {
      stdout += chunk.toString();
    });

    child.stderr.on("data", (chunk) => {
      stderr += chunk.toString();
    });

    child.on("error", (error) => {
      clearTimeout(timer);
      resolve({
        exitCode: -1,
        timedOut,
        stdout,
        stderr: `${stderr}${error.message}`,
      });
    });

    child.on("close", (code) => {
      clearTimeout(timer);
      resolve({
        exitCode: code ?? -1,
        timedOut,
        stdout,
        stderr,
      });
    });
  });
}

function findFirstError(stdout, stderr) {
  const combined = `${stdout}\n${stderr}`;
  const lines = combined.split(/\r?\n/);

  for (const pattern of FAILURE_PATTERNS) {
    const line = lines.find((candidate) => candidate.includes(pattern));
    if (line) {
      return line;
    }
  }

  return null;
}

async function buildProject(timeoutMs) {
  const result = await runProcess("dotnet", ["build"], { timeoutMs });
  if (result.exitCode !== 0 || result.timedOut) {
    throw new Error(`dotnet build failed: ${JSON.stringify(result, null, 2)}`);
  }
}

async function runScene(scene, options) {
  const resolvedScene = await resolveScene(scene);
  const godot = await resolveGodot(options.godot);

  if (options.build) {
    await buildProject(options.timeoutMs);
  }

  const result = await runProcess(
    godot,
    ["--headless", "--path", ".", "--scene", resolvedScene, "--no-header"],
    { timeoutMs: options.timeoutMs },
  );

  return {
    scene: resolvedScene,
    exitCode: result.exitCode,
    timedOut: result.timedOut,
    stdout: result.stdout,
    stderr: result.stderr,
    firstError: findFirstError(result.stdout, result.stderr),
  };
}

async function main() {
  const options = parseArgs(process.argv.slice(2));

  if (options.command === "list") {
    const scenes = await collectScenes(DEFAULT_SCAN_ROOT, options.includeAll);
    console.log(JSON.stringify({ scenes }, null, 2));
    return;
  }

  if (options.command === "run") {
    const result = await runScene(options.scene, options);
    console.log(JSON.stringify(result, null, 2));
    process.exitCode = result.exitCode === 0 && !result.timedOut ? 0 : 1;
    return;
  }

  if (options.command === "run-all") {
    const scenes = await collectScenes(DEFAULT_SCAN_ROOT, options.includeAll);
    const results = [];
    const runOptions = { ...options, build: false };

    if (options.build) {
      await buildProject(options.timeoutMs);
    }

    for (const scene of scenes) {
      const result = await runScene(scene, runOptions);
      results.push(result);

      if (!options.continueOnFail && (result.exitCode !== 0 || result.timedOut)) {
        break;
      }
    }

    const failed = results.some((result) => result.exitCode !== 0 || result.timedOut);
    console.log(JSON.stringify({ results }, null, 2));
    process.exitCode = failed ? 1 : 0;
    return;
  }

  printUsage();
  process.exitCode = 1;
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
