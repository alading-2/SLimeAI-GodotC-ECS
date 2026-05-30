#!/usr/bin/env python3
"""
从 .ai-config/skills/openspec/ 生成 .claude/commands/opsx/*.md
保持 command 的 frontmatter（name/category/tags），正文与统一源 skill 一致。
"""
import os

ROOT = os.path.dirname(os.path.dirname(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

MAPPINGS = [
    ("apply", "openspec-apply-change"),
    ("explore", "openspec-explore"),
    ("propose", "openspec-propose"),
    ("archive", "openspec-archive-change"),
]


def extract_frontmatter_and_body(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()

    if not content.startswith("---"):
        return {}, content

    parts = content.split("---", 2)
    if len(parts) < 3:
        return {}, content

    fm_text = parts[1].strip()
    body = parts[2].strip()

    fm = {}
    for line in fm_text.split("\n"):
        line = line.strip()
        if ":" in line:
            key, val = line.split(":", 1)
            fm[key.strip()] = val.strip()

    return fm, body


def main():
    for cmd_name, skill_name in MAPPINGS:
        skill_path = os.path.join(ROOT, ".ai-config", "skills", "openspec",
                                  skill_name, "SKILL.md")
        cmd_path = os.path.join(ROOT, ".claude", "commands", "opsx",
                                f"{cmd_name}.md")

        if not os.path.exists(skill_path):
            print(f"SKIP: skill not found: {skill_path}")
            continue

        skill_fm, skill_body = extract_frontmatter_and_body(skill_path)
        cmd_fm, _ = extract_frontmatter_and_body(cmd_path)

        # 保留 command 的 name/category/tags，只更新 description
        desc = skill_fm.get("description", "OpenSpec workflow command")
        raw_name = cmd_fm.get("name", f"OPSX: {cmd_name.capitalize()}")
        # cmd_fm["name"] 可能已带引号，去重
        clean_name = raw_name.strip('"')
        new_fm_lines = [f'name: "{clean_name}"', f"description: {desc}"]

        for key in ["category", "tags"]:
            if key in cmd_fm:
                new_fm_lines.append(f"{key}: {cmd_fm[key]}")

        new_content = "---\n" + "\n".join(
            new_fm_lines) + "\n---\n\n" + skill_body + "\n"

        with open(cmd_path, "w", encoding="utf-8") as f:
            f.write(new_content)

        print(f"UPDATED: {cmd_path}")

    print("Done.")


if __name__ == "__main__":
    main()
