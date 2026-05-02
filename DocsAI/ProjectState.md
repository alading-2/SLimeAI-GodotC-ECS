# DocsAI ProjectState

本文记录 AI-First 文档体系当前状态。保持短，只写能帮助下一个 AI 会话继续工作的内容。

## 当前目标

按当前代码重新校准：

- `DocsAI/Modules/` 模块契约。
- `.codex/skills/*` 短入口。
- `Docs/` 人类文档入口和项目索引。
- `Src/**/*.md` 源码旁文档中的明显过期链接。

## 当前阶段

执行 `Plans/Architecture/AI_First_Docs_Code_Alignment/`。

这是旧 `Plans/Architecture/AI_First_Program_Migration/` 完成后的二轮收敛计划。旧计划作为历史记录保留，不再追加新阶段。

## 已完成

- 旧 AI-First 迁移计划已建立 Docs / DocsAI / Skill / 测试 / 门禁基础。
- 本轮已新建：
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/README.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/01_Code_And_Docs_Audit.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/02_DocsAI_Module_Contracts_Update.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/03_Skill_Short_Entry_Refactor.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/04_Src_Docs_Consolidation.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/05_Final_Verification_And_Handoff.md`
- 本轮已补齐 `DocsAI/Modules/FeatureSystem.md` 与 `DocsAI/Modules/DataAuthoring.md`。
- 第二批已修正 Ability 参考示例、Data 测试 README、TestSystem README 和旧迁移计划状态分叉。
- 第三批已修正 CostComponent、Data README、Component 规范、EntityManager 文档和两个测试 README。
- 第四批已修正 Tools 族 ObjectPool、TargetSelector、TimerManager 源码旁文档。
- 第五批已修正 Component / Attack / Collision / UI 源码旁文档和 DocsAI 契约。

## 已完成（本轮）

- 压缩长 Skill 为短入口。
- 清理旧机器绝对路径、旧 Windsurf Skill 入口和过期计划指向。
- 更新本轮计划状态文件和验证结果。

## 未完成 / 风险

- `Src/**/*.md` 历史长文档尚未系统迁移，目前只按模块族清理已确认会误导 AI 的内容。
- 旧 `MainTest` 失败不属于本轮文档收敛，需独立 Debug。
- 已有用户工作区改动集中在 Godot 场景测试 Skill、测试文档、Docs README 和项目索引，继续修改时必须合并而不是覆盖。

## 推荐入口

- AI 索引：`DocsAI/INDEX.md`
- 当前计划：`Plans/Architecture/AI_First_Docs_Code_Alignment/README.md`
- Skill 映射：`DocsAI/Skills/Skill到DocsAI映射.md`
- 测试矩阵：`DocsAI/Tests/测试矩阵.md`

## 验证方式

```bash
git status --short
find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l
find DocsAI -maxdepth 3 -type f | sort
rg -n "/mnt/[e]|file://[/]|复刻土豆兄[弟]|[.]windsurf" DocsAI .codex/skills Docs/框架/项目索引.md Src -g "*.md"
rg -n "DocsAI/Modules|DocsAI/Tests|DocsAI/Workflows" .codex/skills DocsAI
dotnet build
```
