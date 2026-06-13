import json
import ast
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
SDD_CLI = REPO_ROOT / "Workspace" / "SDD" / "sdd.py"


class SDDCliTests(unittest.TestCase):

    def setUp(self):
        self.temp_dir = Path(tempfile.mkdtemp(prefix="sdd-cli-test-"))
        self.root = self.temp_dir / "SDD"

    def tearDown(self):
        shutil.rmtree(self.temp_dir)

    def run_sdd(self, *args, expect_success=True):
        result = subprocess.run(
            [sys.executable,
             str(SDD_CLI), *args, "--root",
             str(self.root)],
            cwd=REPO_ROOT,
            text=True,
            capture_output=True,
        )
        if expect_success and result.returncode != 0:
            self.fail(
                f"命令失败: {result.args}\nSTDOUT:\n{result.stdout}\nSTDERR:\n{result.stderr}"
            )
        if not expect_success and result.returncode == 0:
            self.fail(
                f"命令应失败但成功: {result.args}\nSTDOUT:\n{result.stdout}\nSTDERR:\n{result.stderr}"
            )
        return result

    def assert_progress_is_state_panel(self, text):
        self.assertIn("## State", text)
        self.assertIn("## Decisions", text)
        self.assertIn("## Validation", text)
        self.assertNotIn("## Timeline", text)
        self.assertNotRegex(text, r"(?m)^### P\d{3}\b")
        self.assertNotIn("task command", text)
        self.assertNotIn("继续处理下一个未完成任务", text)

    def test_init_root_creates_sdd_root_structure(self):
        self.run_sdd("init-root")

        for name in ["README.md", "INDEX.md", "catalog.json"]:
            self.assertTrue((self.root / name).exists(), name)
        for name in ["templates", "pending", "active", "blocked", "done"]:
            self.assertTrue((self.root / name).exists(), name)
        for name in ["project", "project/projects", "project/archived"]:
            self.assertTrue((self.root / name).exists(), name)

        catalog = json.loads(
            (self.root / "catalog.json").read_text(encoding="utf-8"))
        self.assertEqual(catalog["schema_version"], 2)
        self.assertEqual(catalog["items"], [])
        self.assertEqual(catalog["projects"], [])

    def test_sdd_entrypoint_only_keeps_cli_parser_and_main(self):
        tree = ast.parse(SDD_CLI.read_text(encoding="utf-8"))
        function_names = {
            node.name
            for node in tree.body if isinstance(node, ast.FunctionDef)
        }
        class_names = {
            node.name
            for node in tree.body if isinstance(node, ast.ClassDef)
        }

        self.assertEqual(function_names, {"build_parser", "main"})
        self.assertEqual(class_names, set())
        for name in [
                "__init__.py",
                "commands.py",
                "config.py",
                "repository.py",
                "templates.py",
                "validation.py",
        ]:
            self.assertTrue((SDD_CLI.parent / "Src" / name).exists(), name)

    def test_new_creates_pending_sdd_and_index_lists_it(self):
        self.run_sdd("init-root")
        result = self.run_sdd(
            "new",
            "SDD System Bootstrap",
            "--type",
            "workflow",
            "--scope",
            "Workspace/SDD",
            "--area",
            "Workspace/SDD",
            "--tag",
            "sdd",
        )

        self.assertIn("SDD-0001-sdd-system-bootstrap", result.stdout)
        instance = self.root / "pending" / "SDD-0001-sdd-system-bootstrap"
        self.assertTrue((instance / "README.md").exists())
        self.assertTrue((instance / "design" / "INDEX.md").exists())
        metadata = json.loads(
            (instance / "sdd.json").read_text(encoding="utf-8"))
        self.assertEqual(metadata["id"], "SDD-0001")
        self.assertEqual(metadata["status"], "pending")
        self.assertEqual(metadata["title"], "SDD System Bootstrap")

        self.run_sdd("index")
        list_result = self.run_sdd("list")
        self.assertIn("SDD-0001", list_result.stdout)
        self.assertIn("pending", list_result.stdout)

    def test_new_progress_defaults_to_state_panel(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "State Panel", "--scope", "Workspace/SDD")
        instance = self.root / "pending" / "SDD-0001-state-panel"

        progress = (instance / "progress.md").read_text(encoding="utf-8")

        self.assert_progress_is_state_panel(progress)
        self.assertIn("- **Status**: pending", progress)
        self.assertIn("- **Current**: T1.1", progress)
        self.assertIn("- **Blocker**: none", progress)

    def test_start_block_done_update_metadata_without_moving_directory(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "State Flow", "--scope", "Workspace/SDD")
        instance = self.root / "pending" / "SDD-0001-state-flow"

        self.run_sdd("start", "SDD-0001")
        self.assertTrue(instance.exists())
        metadata = json.loads(
            (instance / "sdd.json").read_text(encoding="utf-8"))
        self.assertEqual(metadata["status"], "active")

        self.run_sdd("block", "SDD-0001", "Need user decision")
        self.assertTrue(instance.exists())
        metadata = json.loads(
            (instance / "sdd.json").read_text(encoding="utf-8"))
        self.assertEqual(metadata["status"], "blocked")
        self.assertIn("Need user decision",
                      (instance / "progress.md").read_text(encoding="utf-8"))

        self.run_sdd("start", "SDD-0001")
        self.assertTrue(instance.exists())
        self.run_sdd("task", "SDD-0001", "done", "T1.1")
        self.run_sdd("done", "SDD-0001", "--validation",
                     "python3 Workspace/SDD/sdd.py validate --all passed")
        self.assertTrue(instance.exists())
        self.assertFalse((self.root / "done" / "SDD-0001-state-flow").exists())
        metadata = json.loads(
            (instance / "sdd.json").read_text(encoding="utf-8"))
        self.assertEqual(metadata["status"], "done")

    def test_state_commands_update_panel_without_timeline_entries(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "Panel Commands", "--scope", "Workspace/SDD")
        instance = self.root / "pending" / "SDD-0001-panel-commands"

        self.run_sdd("start", "SDD-0001")
        self.run_sdd("note", "SDD-0001", "--type", "decision",
                     "使用状态面板记录关键裁决。")
        self.run_sdd("block", "SDD-0001", "Need user decision")
        self.run_sdd("start", "SDD-0001")
        self.run_sdd("task", "SDD-0001", "done", "T1.1")
        self.run_sdd(
            "done",
            "SDD-0001",
            "--validation",
            "python3 Workspace/SDD/sdd.py validate SDD-0001: 0 error / 0 warning",
        )

        progress = (instance / "progress.md").read_text(encoding="utf-8")
        self.assert_progress_is_state_panel(progress)
        self.assertIn("使用状态面板记录关键裁决。", progress)
        self.assertIn("Need user decision", progress)
        self.assertIn("python3 Workspace/SDD/sdd.py validate SDD-0001", progress)

    def test_note_records_decision_and_show_exposes_state(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "Resume Context", "--scope", "Workspace/SDD")
        self.run_sdd("note", "SDD-0001", "--type", "decision",
                     "Use README as entry card")

        show = self.run_sdd("show", "SDD-0001")
        self.assertIn("Resume Context", show.stdout)
        self.assertIn("## State", show.stdout)
        self.assertIn("T1.1", show.stdout)
        progress = (self.root / "pending" / "SDD-0001-resume-context" /
                    "progress.md").read_text(encoding="utf-8")
        self.assertIn("Use README as entry card", progress)

    def test_validate_accepts_metadata_status_independent_from_directory(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "Metadata Status", "--scope", "Workspace/SDD")
        instance = self.root / "pending" / "SDD-0001-metadata-status"
        metadata_path = instance / "sdd.json"
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        metadata["status"] = "active"
        metadata_path.write_text(
            json.dumps(metadata, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8")

        result = self.run_sdd("validate", "SDD-0001")
        self.assertNotIn("SDD004", result.stdout)

    def test_validate_fails_on_invalid_metadata_status(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "Invalid Status", "--scope", "Workspace/SDD")
        instance = self.root / "pending" / "SDD-0001-invalid-status"
        metadata_path = instance / "sdd.json"
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        metadata["status"] = "running"
        metadata_path.write_text(
            json.dumps(metadata, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8")

        result = self.run_sdd("validate", "SDD-0001", expect_success=False)
        self.assertIn("SDD004", result.stdout)
        self.assertIn("invalid-status", result.stdout)

    def test_done_requires_all_tasks_completed(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "Incomplete Done", "--scope", "Workspace/SDD")
        self.run_sdd("start", "SDD-0001")

        result = self.run_sdd(
            "done",
            "SDD-0001",
            "--validation",
            "validation was attempted",
            expect_success=False,
        )
        self.assertIn("未完成任务", result.stdout)
        self.assertTrue(
            (self.root / "pending" / "SDD-0001-incomplete-done").exists())

    def test_readme_summary_survives_state_changes(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "Preserve README", "--scope", "Workspace/SDD")
        pending = self.root / "pending" / "SDD-0001-preserve-readme"
        readme_path = pending / "README.md"
        readme_text = readme_path.read_text(encoding="utf-8")
        readme_text = readme_text.replace(
            "\nPreserve README\n\n## Reading Order",
            "\n人工摘要：这个 SDD 用来证明 README 不会被 CLI 状态变化覆盖。\n\n## Reading Order",
            1,
        )
        readme_text = readme_text.replace(
            "- **Last Conclusion**: SDD-0001 已创建，用于跟踪 Preserve README。",
            "- **Last Conclusion**: 人工恢复点：继续验证 README 写入边界。",
        )
        readme_text = readme_text.replace(
            "- **Next Action**: 阅读 README、design/INDEX.md 和 tasks.md 后继续推进。",
            "- **Next Action**: 人工下一步：先运行状态流转命令。",
        )
        readme_path.write_text(readme_text, encoding="utf-8")

        self.run_sdd("start", "SDD-0001")
        self.run_sdd("note", "SDD-0001", "--type", "decision", "追加一条进度记录")
        self.run_sdd("block", "SDD-0001", "等待验证证据")
        self.run_sdd("start", "SDD-0001")
        self.run_sdd("task", "SDD-0001", "done", "T1.1")
        self.run_sdd(
            "done",
            "SDD-0001",
            "--validation",
            "python3 Workspace/SDD/sdd.py validate SDD-0001: 0 error / 0 warning",
            "--conclusion",
            "README 写入边界已验证。",
            "--next-action",
            "无需继续。",
        )
        final_readme = readme_path.read_text(encoding="utf-8")

        self.assertIn("人工摘要：这个 SDD 用来证明 README 不会被 CLI 状态变化覆盖。", final_readme)
        self.assertIn("- **Last Conclusion**: 人工恢复点：继续验证 README 写入边界。",
                      final_readme)
        self.assertIn("- **Next Action**: 人工下一步：先运行状态流转命令。", final_readme)
        self.assertIn("- **Status**: done", final_readme)
        self.assertIn("- **Current Task**: done", final_readme)

    def test_done_inherits_latest_resume_when_no_conclusion_is_given(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "Done Resume", "--scope", "Workspace/SDD")
        self.run_sdd("start", "SDD-0001")
        self.run_sdd("task", "SDD-0001", "done", "T1.1")
        self.run_sdd("note", "SDD-0001", "--type", "decision",
                     "核心结论：CLI 已保护 README 并增强 validate。")

        self.run_sdd(
            "done",
            "SDD-0001",
            "--validation",
            "python3 Workspace/SDD/sdd.py validate SDD-0001: 0 error / 0 warning",
        )
        instance = self.root / "pending" / "SDD-0001-done-resume"
        progress = (instance / "progress.md").read_text(encoding="utf-8")

        self.assertIn("核心结论：CLI 已保护 README 并增强 validate。", progress)
        self.assertIn(
            "python3 Workspace/SDD/sdd.py validate SDD-0001: 0 error / 0 warning",
            progress)
        self.assertIn("- **Status**: done", progress)

    def test_validate_reports_quality_warnings_and_template_errors(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "Weak Done", "--scope", "Workspace/SDD")
        self.run_sdd("start", "SDD-0001")
        self.run_sdd("task", "SDD-0001", "done", "T1.1")
        self.run_sdd("done", "SDD-0001", "--validation", "ok")
        instance = self.root / "pending" / "SDD-0001-weak-done"
        (instance / "notes.md").write_text(
            "# Notes\n\n一句话说明这个 SDD 要解决什么问题\n",
            encoding="utf-8")
        progress_path = instance / "progress.md"
        progress_text = progress_path.read_text(encoding="utf-8")
        progress_path.write_text(
            progress_text.replace("- **Next**: 无需继续；如有新问题创建新 SDD 引用本任务。",
                                  "- **Next**: ok"),
            encoding="utf-8")

        result = self.run_sdd("validate", "SDD-0001", expect_success=False)

        self.assertIn("SDD015", result.stdout)
        self.assertIn("SDD016", result.stdout)
        self.assertIn("SDD017", result.stdout)
        self.assertIn("SDD018", result.stdout)
        self.assertIn("SDD019", result.stdout)

    def test_validate_reports_redundancy_warnings(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "Redundant Progress", "--scope", "Workspace/SDD")
        self.run_sdd("start", "SDD-0001")
        instance = self.root / "pending" / "SDD-0001-redundant-progress"
        progress_path = instance / "progress.md"
        weak_latest = """# Progress

## Latest Resume

- **Updated**: 2026-05-24 00:00
- **Current Task**: T1.1
- **Last Conclusion**: ok
- **Next Action**: done
- **Open Blockers**: none

## Timeline
"""
        entries = []
        for index in range(1, 8):
            entries.append(
                f"""### P{index:03d} — 2026-05-24 00:0{index} — change

- **Context**: 记录 {index}
- **Conclusion**: ok
- **Evidence**: done
- **Impact**: none
- **Resume**: continue
""")
        key_files = "\n".join(f"  - `file-{index}.md`"
                              for index in range(1, 10))
        progress_path.write_text(
            weak_latest + "\n".join(entries) +
            f"\n- **Key Files**:\n{key_files}\n",
            encoding="utf-8",
        )
        artifacts = instance / "artifacts"
        (artifacts / "one.log").write_text("artifact one", encoding="utf-8")
        (artifacts / "two.log").write_text("artifact two", encoding="utf-8")
        notes_lines = "\n".join(f"无结构记录 {index}" for index in range(130))
        (instance / "notes.md").write_text(f"# Notes\n\n{notes_lines}\n",
                                           encoding="utf-8")

        result = self.run_sdd("validate", "SDD-0001")

        self.assertIn("SDD021", result.stdout)
        self.assertIn("SDD022", result.stdout)
        self.assertIn("SDD023", result.stdout)
        self.assertIn("SDD024", result.stdout)

    def test_design_import_copies_file_and_updates_index(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "Design Import Test", "--scope", "Workspace/SDD")
        instance = self.root / "pending" / "SDD-0001-design-import-test"

        # 创建外部设计文档
        ext_dir = self.temp_dir / "external"
        ext_dir.mkdir()
        ext_file = ext_dir / "external-design.md"
        ext_file.write_text("# External Design\n\n完整设计内容。\n", encoding="utf-8")

        self.run_sdd("design-import", "SDD-0001", str(ext_file), "--role",
                     "reference", "--notes", "外部设计")

        dest = instance / "design" / "external-design.md"
        self.assertTrue(dest.exists())
        self.assertIn("完整设计内容", dest.read_text(encoding="utf-8"))

        index = (instance / "design" / "INDEX.md").read_text(encoding="utf-8")
        self.assertIn("external-design.md", index)
        self.assertIn("reference", index)

    def test_validate_reports_sdd025_thin_design_and_external_refs(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "Thin Design", "--scope", "Workspace/SDD")
        instance = self.root / "pending" / "SDD-0001-thin-design"

        # main.md 只有模板占位（行数 < 20），触发 thin-design
        (instance / "design" / "main.md").write_text(
            "# Thin\n\n## Goal\n\n说明问题。\n\n## Design\n\n描述设计。\n",
            encoding="utf-8")

        result = self.run_sdd("validate", "SDD-0001")
        self.assertIn("SDD025", result.stdout)
        self.assertIn("thin-design", result.stdout)

        # main.md 引用外部路径但 design/ 下只有 main.md
        (instance / "design" / "main.md").write_text(
            "# External Ref\n\n## Goal\n\n主设计来自 Workspace/DocsAI/Idea/xxx.md。\n\n"
            "## Design\n\n描述设计。\n\n## Verification\n\n列出验证。\n",
            encoding="utf-8")

        result = self.run_sdd("validate", "SDD-0001")
        self.assertIn("SDD025", result.stdout)
        self.assertIn("design-refs-external", result.stdout)

    def test_validate_accepts_featurespec_source_without_scenario_copy(self):
        self.run_sdd("init-root")
        self.run_sdd("new", "FeatureSpec BDD", "--scope", "Workspace/SDD")
        instance = self.root / "pending" / "SDD-0001-featurespec-bdd"
        (instance / "design" / "FeatureSpec示例.FeatureSpec.md").write_text(
            "# FeatureSpec\n\n## FS-1\n\n行为规格。\n",
            encoding="utf-8")
        (instance / "bdd.md").write_text(
            "# BDD\n\n"
            "## Applicability\n\n"
            "- **Required**: true\n"
            "- **Reason**: 测试 FeatureSpec 引用。\n"
            "- **Source**: `design/FeatureSpec示例.FeatureSpec.md`\n"
            "- **Executed features**: FS-1\n",
            encoding="utf-8")

        result = self.run_sdd("validate", "SDD-0001")

        self.assertNotIn("SDD011", result.stdout)

    def test_validate_project_child_shared_design_refs_are_not_thin_design(self):
        self.run_sdd("init-root")
        self.run_sdd("project-new", "Shared Design Project", "--scope",
                     "Workspace/SystemAgent")
        self.run_sdd("new", "Shared Ref Child", "--project", "PRJ-0001",
                     "--scope", "Workspace/SDD")
        child = (self.root / "project" / "projects" /
                 "PRJ-0001-shared-design-project" / "sdds" /
                 "001-SDD-0001-shared-ref-child")
        (child / "design" / "main.md").write_text(
            "# Shared Ref Child\n\n## Goal\n\n只记录当前 SDD 的局部目标。\n",
            encoding="utf-8")

        result = self.run_sdd("validate", "SDD-0001")

        self.assertNotIn("thin-design", result.stdout)
        self.assertNotIn("design-refs-external", result.stdout)

    def test_project_cli_creates_lists_shows_and_archives_project(self):
        self.run_sdd("init-root")

        result = self.run_sdd("project-new", "SystemAgent Optimization",
                              "--scope", "Workspace/SystemAgent", "--tag",
                              "systemagent")

        self.assertIn("PRJ-0001-systemagent-optimization", result.stdout)
        project = self.root / "project" / "projects" / "PRJ-0001-systemagent-optimization"
        self.assertTrue((project / "README.md").exists())
        self.assertTrue((project / "project.json").exists())
        self.assertTrue((project / "design" / "INDEX.md").exists())
        self.assertTrue((project / "Core" / "roadmap.md").exists())
        roadmap = (project / "Core" / "roadmap.md").read_text(encoding="utf-8")
        self.assertIn("## Design Progress", roadmap)
        self.assertIn("## Next SDDs", roadmap)
        self.assertIn("Done", roadmap)
        progress = (project / "Core" / "progress.md").read_text(encoding="utf-8")
        self.assertIn("## Project Status Board", progress)
        self.assertIn("Design Docs", progress)
        metadata = json.loads(
            (project / "project.json").read_text(encoding="utf-8"))
        self.assertEqual(metadata["id"], "PRJ-0001")
        self.assertEqual(metadata["status"], "active")

        list_result = self.run_sdd("project-list")
        self.assertIn("PRJ-0001", list_result.stdout)
        self.assertIn("projects", list_result.stdout)

        show_result = self.run_sdd("project-show", "PRJ-0001")
        self.assertIn("SystemAgent Optimization", show_result.stdout)

        self.run_sdd("project-archive", "PRJ-0001")
        archived = self.root / "project" / "archived" / "PRJ-0001-systemagent-optimization"
        self.assertTrue(archived.exists())
        archived_metadata = json.loads(
            (archived / "project.json").read_text(encoding="utf-8"))
        self.assertEqual(archived_metadata["status"], "done")

    def test_new_can_create_project_child_sdd_and_index_lists_it(self):
        self.run_sdd("init-root")
        self.run_sdd("project-new", "SystemAgent Optimization", "--scope",
                     "Workspace/SystemAgent")

        result = self.run_sdd("new", "SDD Project Container Model",
                              "--project", "PRJ-0001", "--scope",
                              "Workspace/SDD")

        self.assertIn("001-SDD", result.stdout)
        child = (self.root / "project" / "projects" /
                 "PRJ-0001-systemagent-optimization" / "sdds" /
                 "001-SDD-0001-sdd-project-container-model")
        self.assertTrue(child.exists())
        metadata = json.loads((child / "sdd.json").read_text(encoding="utf-8"))
        self.assertEqual(metadata["project_id"], "PRJ-0001")
        self.assertEqual(metadata["project_order"], 1)

        self.run_sdd("start", "SDD-0001")
        self.run_sdd("task", "SDD-0001", "done", "T1.1")
        self.run_sdd(
            "done", "SDD-0001", "--validation",
            "python3 Workspace/SDD/sdd.py validate SDD-0001: 0 error / 0 warning"
        )
        self.assertTrue(child.exists())
        metadata = json.loads((child / "sdd.json").read_text(encoding="utf-8"))
        self.assertEqual(metadata["status"], "done")

        list_result = self.run_sdd("list")
        self.assertIn("SDD-0001", list_result.stdout)
        self.assertIn("PRJ-0001", list_result.stdout)

    def test_validate_reports_invalid_project_metadata(self):
        self.run_sdd("init-root")
        self.run_sdd("project-new", "Broken Project", "--scope",
                     "Workspace/SDD")
        project = self.root / "project" / "projects" / "PRJ-0001-broken-project"
        metadata_path = project / "project.json"
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        metadata["status"] = "running"
        metadata_path.write_text(
            json.dumps(metadata, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8")

        result = self.run_sdd("validate", "--all", expect_success=False)

        self.assertIn("SDD026", result.stdout)
        self.assertIn("invalid-project-status", result.stdout)


if __name__ == "__main__":
    unittest.main()
