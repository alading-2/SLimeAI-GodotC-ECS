#!/usr/bin/env python3
"""SystemAgent skill-test static lint runner.

输出 stdout summary + JSON 到 .ai-temp/skill-test/static-{ts}.json
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

# ---------------------------------------------------------------------------
# 数据结构
# ---------------------------------------------------------------------------


@dataclass
class Violation:
    file: str
    kind: str
    detail: str
    severity: str  # "critical" | "advisory"


@dataclass
class RuleResult:
    rule_id: str
    name: str
    passed: int = 0
    failed: int = 0
    violations: list[Violation] = field(default_factory=list)


# ---------------------------------------------------------------------------
# 工具函数
# ---------------------------------------------------------------------------


def _parse_frontmatter(text: str) -> dict[str, str]:
    """从 SKILL.md 解析 YAML frontmatter（仅取 name/description）。"""
    m = re.match(r"^---\s*\n(.*?)\n---", text, re.DOTALL)
    if not m:
        return {}
    fm: dict[str, str] = {}
    for line in m.group(1).splitlines():
        kv = re.match(r"^(\w[\w-]*):\s*(.*)", line)
        if kv:
            fm[kv.group(1)] = kv.group(2).strip()
    return fm


def _md5(path: Path) -> str:
    return hashlib.md5(path.read_bytes()).hexdigest()


def _collect_skills(root: Path) -> list[Path]:
    skills_dir = root / ".ai-config" / "skills"
    return sorted(skills_dir.rglob("SKILL.md"))


def _collect_changed_skills(root: Path) -> list[Path]:
    """返回 git 修改过的 SKILL.md（包含新增和 staged）。"""
    try:
        result = subprocess.run(["git", "diff", "--name-only", "HEAD"],
                                capture_output=True,
                                text=True,
                                cwd=root,
                                timeout=10)
        changed = set(result.stdout.splitlines())
        result2 = subprocess.run(
            ["git", "ls-files", "--others", "--exclude-standard"],
            capture_output=True,
            text=True,
            cwd=root,
            timeout=10)
        changed.update(result2.stdout.splitlines())
        return sorted(root / p for p in changed
                      if p.startswith(".ai-config/skills/")
                      and p.endswith("SKILL.md") and (root / p).exists())
    except Exception:
        return _collect_skills(root)


# ---------------------------------------------------------------------------
# 规则加载
# ---------------------------------------------------------------------------


def _load_rules(rules_dir: Path) -> list[Any]:
    rules = []
    for py_file in sorted(rules_dir.glob("R*.py")):
        spec = importlib.util.spec_from_file_location(py_file.stem, py_file)
        mod = importlib.util.module_from_spec(spec)  # type: ignore
        spec.loader.exec_module(mod)  # type: ignore
        rules.append(mod)
    return rules


# ---------------------------------------------------------------------------
# Runner
# ---------------------------------------------------------------------------


def run_lint(root: Path, scope: str, summary_only: bool) -> int:
    rules_dir = Path(__file__).parent / "rules"
    rules = _load_rules(rules_dir)

    # 收集 skill 文件
    if scope == "changed":
        skill_files = _collect_changed_skills(root)
        if not skill_files:
            if not summary_only:
                print("SystemAgent Skill Test — static (changed)")
                print("No changed skills found. Skipping.")
            return 0
    elif scope == "all":
        skill_files = _collect_skills(root)
    else:
        target = (root / scope).resolve()
        if target.is_dir():
            skill_files = sorted(target.rglob("SKILL.md"))
        elif target.is_file() and target.name == "SKILL.md":
            skill_files = [target]
        else:
            skill_files = []

    # 加载辅助数据
    manifest_path = root / "Workspace" / "SystemAgent" / "Catalog" / "manifest.yaml"
    catalog_path = root / "Workspace" / "SystemAgent" / "Registry" / "skills.yaml"

    try:
        import yaml  # type: ignore
        manifest = yaml.safe_load(
            manifest_path.read_text()) if manifest_path.exists() else {}
        catalog = yaml.safe_load(
            catalog_path.read_text()) if catalog_path.exists() else {}
    except Exception as e:
        manifest, catalog = {}, {}
        print(f"WARN: 加载 manifest/catalog 失败: {e}", file=sys.stderr)

    # 执行规则
    results: list[RuleResult] = []
    for rule_mod in rules:
        rule_id = Path(rule_mod.__file__).stem.split("-")[0]
        rule_name = rule_mod.name()
        violations: list[Violation] = rule_mod.check(skill_files, manifest,
                                                     catalog, root)
        passed = len(skill_files) - len(violations)
        r = RuleResult(
            rule_id=rule_id,
            name=rule_name,
            passed=max(passed, 0),
            failed=len(violations),
            violations=violations,
        )
        results.append(r)

    # 汇总
    critical_failures = sum(r.failed for r in results if any(
        v.severity == "critical" for v in r.violations))
    advisory_warnings = sum(r.failed for r in results if all(
        v.severity == "advisory" for v in r.violations))
    exit_code = 1 if critical_failures > 0 else 0

    # 输出
    if summary_only:
        cf_str = f"Critical:{critical_failures}" if critical_failures else "Critical:0"
        aw_str = f"Advisory:{advisory_warnings}" if advisory_warnings else "Advisory:0"
        print(f"skill-lint: {len(skill_files)} skills | {cf_str} | {aw_str}")
    else:
        print(f"SystemAgent Skill Test — static")
        print(f"Rules: {' '.join(r.rule_id for r in results)}")
        print(f"Skills scanned: {len(skill_files)}")
        print()
        for r in results:
            line = f"{r.rule_id} {r.name}: {r.passed} PASS, {r.failed} FAIL"
            print(line)
            for v in r.violations:
                print(f"  - {v.file}: {v.detail}")
        print()
        print(f"Critical failures: {critical_failures}")
        print(f"Advisory warnings: {advisory_warnings}")
        print(f"Exit code: {exit_code}")

    # 写 JSON 报告
    ts = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    report_dir = root / ".ai-temp" / "skill-test"
    report_dir.mkdir(parents=True, exist_ok=True)
    report = {
        "scanned": len(skill_files),
        "rules": {
            r.rule_id: {
                "name":
                r.name,
                "pass":
                r.passed,
                "fail":
                r.failed,
                "violations": [{
                    "file": v.file,
                    "kind": v.kind,
                    "detail": v.detail,
                    "severity": v.severity
                } for v in r.violations],
            }
            for r in results
        },
        "criticalFailures": critical_failures,
        "advisoryWarnings": advisory_warnings,
        "exitCode": exit_code,
    }
    report_path = report_dir / f"static-{ts}.json"
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2))

    return exit_code


# ---------------------------------------------------------------------------
# Entry
# ---------------------------------------------------------------------------


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    parser.add_argument("--scope", default="all")
    parser.add_argument("--summary-only", action="store_true")
    args = parser.parse_args()
    return run_lint(Path(args.root), args.scope, args.summary_only)


if __name__ == "__main__":
    raise SystemExit(main())
