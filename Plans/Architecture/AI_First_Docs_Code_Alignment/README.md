# AI-First 文档与 Skill 按代码对齐计划

本文是旧 `AI_First_Program_Migration` 完成后的二轮文档收敛计划。旧迁移计划保持历史完成状态，本计划不追加旧计划阶段、不修改旧计划当前阶段，只把当前代码、`DocsAI/`、`.codex/skills/` 和 `Src/**/*.md` 重新校准到一致状态。

## 目标

- 让 AI 通过短 Skill 进入任务。
- 让模块规则集中在 `DocsAI/Modules/`。
- 让 `Docs/` 保持人类架构说明和历史背景。
- 让 `Src/**/*.md` 逐步变成源码旁短 README / API 入口。
- 清理旧机器绝对路径、`.windsurf` 旧入口和过期计划指向。

## 执行顺序

1. `01_Code_And_Docs_Audit.md`
   - 审计代码、DocsAI、Skill、Src 文档现状，只记录事实。
2. `02_DocsAI_Module_Contracts_Update.md`
   - 按当前代码补齐和修正 AI 模块契约。
3. `03_Skill_Short_Entry_Refactor.md`
   - 把长 Skill 压缩为触发条件、边界、必读文档、流程、禁止事项和验证命令。
4. `04_Src_Docs_Consolidation.md`
   - 清理源码旁文档的明显过期链接，补充新入口，不删除历史说明。
5. `05_Final_Verification_And_Handoff.md`
   - 跑一致性检查，更新本计划状态文件，并让 `DocsAI/ProjectState.md` 指向本计划。

## 状态文件

- `Progress.md`
- `Done.md`
- `Backlog.md`

## 验收标准

- `DocsAI/Modules/FeatureSystem.md` 与 `DocsAI/Modules/DataAuthoring.md` 存在。
- `DocsAI/INDEX.md`、`DocsAI/ProjectState.md`、`DocsAI/Skills/Skill到DocsAI映射.md` 指向本轮计划和新增模块。
- `.codex/skills/*/SKILL.md` 仍保留原 Skill 名称，但长 Skill 已明显压缩。
- 明显旧绝对路径、`file:///`、`复刻土豆兄弟`、`.windsurf` 入口不再出现在当前 AI 入口和本轮清理范围。
- 构建命令能执行并记录结果。

## 验证命令

```bash
git status --short
find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l
find DocsAI -maxdepth 3 -type f | sort
rg -n "/mnt/e|file:///|复刻土豆兄弟|\\.windsurf" DocsAI .codex/skills Docs/框架/项目索引.md Src -g "*.md"
rg -n "DocsAI/Modules|DocsAI/Tests|DocsAI/Workflows" .codex/skills DocsAI
dotnet build
```
