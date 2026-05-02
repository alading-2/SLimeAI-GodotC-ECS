# 06 Second Pass Code Alignment

## 目标

继续执行本计划第一轮遗留项，对已能从代码确认的过期文档和 Skill 参考内容做第二批修正。

## 本轮处理

- 修正 `ability-system` 参考文档中的旧 `FeatureId.Ability_Movement_ArcShot` 示例。
- 修正 Ability 参考文档中 `AbilityTargetSelection` 的旧 `int` 强转示例。
- 修正 Ability 参考文档中部分 DataKey 读取写法。
- 修正 Data 测试 README 的旧 `Src/Test/ECS/Data` 路径和运行时 Data 文档链接。
- 修正 TestSystem README 中旧 `.tres` 系统配置说明，改为 `Data/DataNew/System/SystemData.cs` 与 `SystemPresetData.cs`。
- 修正旧 `AI_First_Program_Migration/Progress.md` 的状态分叉，让旧计划明确归档并指向本计划。

## 不做

- 不恢复已删除的 `.claude/skills`、`.opencode/skills`、`.windsurf/skills`。
- 不迁移全部 41 个 `Src/**/*.md`，只处理本轮已明确和代码不一致的点。
- 不修复运行时代码和历史 `MainTest` 失败。

## 验证命令

```bash
rg -n "Ability[_]Movement|Src/Test/ECS/Data|Data/Config/System/System/Resource/TestSystem[.]tres|[.][.]/[.][.]/[.][.]/ECS/Data" .codex Src DocsAI Docs -g "*.md"
dotnet build
```
