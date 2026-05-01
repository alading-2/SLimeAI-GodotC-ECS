# 计划 01：Docs / DocsAI 文档体系迁移

## 目标

建立面向人类和面向 AI 的双文档体系：

- `Docs/`：给人看，讲愿景、架构、模块设计、使用指南和决策记录。
- `DocsAI/`：给 AI 看，讲任务流程、模块契约、测试矩阵、调试协议和长任务恢复。
- `Src/**/*.md`：不再作为长期主文档目录，只保留短 README 或源码附近的极简说明。

本阶段不要求迁移全部文档，只建立结构、入口、规则和首批核心文档。

## 输入文件

- `Docs/框架/优化/AI/ai_first_godot_csharp_ecs_program_overview.md`
- `Docs/框架/项目索引.md`
- `Src/ECS/AI/README.md`
- `Src/ECS/UI/README.md`
- `.codex/skills/project-index/SKILL.md`

## 修改范围

- 新增 `DocsAI/README.md`
- 新增 `DocsAI/INDEX.md`
- 新增 `DocsAI/ProjectState.md`
- 新增 `DocsAI/Workflows/AI开发闭环.md`
- 新增 `DocsAI/Workflows/文档迁移协议.md`
- 新增或调整 `Docs/README.md`
- 更新 `Docs/框架/项目索引.md`

## 执行步骤

1. 统计现有 Markdown 文档分布：
   - `find Docs Src Data .codex/skills -name '*.md' | sort`
2. 将文档分为四类：
   - 人类概念文档
   - AI 执行文档
   - 源码附近 API 说明
   - 历史方案 / 归档记录
3. 创建 `DocsAI/` 基础目录和入口文档。
4. 把 AI-First 总览中的项目定位、核心原则、最小闭环拆入 `DocsAI/README.md` 和 `DocsAI/Workflows/AI开发闭环.md`。
5. 在 `Docs/README.md` 中只保留人类阅读入口，不堆执行细节。
6. 更新 `Docs/框架/项目索引.md`，新增 AI-First 文档体系入口。
7. 暂不删除旧文档；旧文档只补“新入口请看...”跳转。

## 验证命令

```bash
find DocsAI -maxdepth 3 -type f | sort
rg -n "DocsAI|AI开发闭环|文档迁移协议" Docs DocsAI .codex/skills
rg -n "/mnt/e|file:///|复刻土豆兄弟" DocsAI Docs/README.md Docs/框架/项目索引.md
```

## 验收标准

- 新 AI 只读 `AGENTS.md` + `DocsAI/README.md` + `DocsAI/INDEX.md` 就能知道项目 AI 开发入口。
- 人类从 `Docs/README.md` 能找到项目愿景、架构和模块说明。
- `DocsAI/ProjectState.md` 能记录当前迁移阶段、下一步和阻塞项。
- 没有把大段模块细节复制进 Skill 或 AGENTS。

## 风险点

- 历史链接多，第一阶段不要追求全量改链。
- `Src/**/*.md` 里有大量实际 API 信息，不能直接删除。
- `DocsAI` 是 AI 主入口，但不能和 `.codex/skills` 职责重复。

## 完成输出

最终回复必须包含：

- 新增的 DocsAI 文件
- 哪些旧文档仍未迁移
- 后续需要迁移的模块优先级
- 验证命令和结果

