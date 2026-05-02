# Progress

## 当前目标

完成 AI-First 文档与 Skill 按当前代码对齐。

## 当前阶段

继续执行源码旁文档分模块校准。第三批优先处理 Ability / Data / Entity / 测试 README 中会误导 AI 写错代码的条目。

## 已完成

- 建立本计划目录和 01-05 计划文件。
- 审计 DocsAI、Skill、Src 文档和旧计划状态。
- 新增 `DocsAI/Modules/FeatureSystem.md` 与 `DocsAI/Modules/DataAuthoring.md`。
- 更新 `DocsAI/INDEX.md`、`DocsAI/ProjectState.md`、Skill 映射和项目索引。
- 压缩长 Skill，总行数从 1927 降到 715。
- 清理本轮范围内明显旧绝对链接、旧项目名和旧 Skill 入口。
- 第二批按代码修正 Ability 参考示例、Data 测试 README、TestSystem README 和旧迁移计划状态分叉。
- 第三批按代码修正 CostComponent、Data README、Component 规范、EntityManager 文档和两个测试 README。
- 第四批按代码修正 Tools 族 ObjectPool、TargetSelector、TimerManager 源码旁文档。
- 第五批按代码修正 Component / Attack / Collision / UI 源码旁文档和 DocsAI 契约。

## 未完成

- `Src/**/*.md` 的长文档迁移仍需后续分模块处理。
- Movement 长设计文档、AI 行为树细节、Collision 设计总览等仍可继续做深层迁移评估。
- 旧 `MainTest` 失败不是本轮范围，需独立 Debug。
- 需要用真实 AI 功能任务继续验证每个 Skill 是否足够可执行。

## 验证方式

见 `README.md` 与 `05_Final_Verification_And_Handoff.md`。
