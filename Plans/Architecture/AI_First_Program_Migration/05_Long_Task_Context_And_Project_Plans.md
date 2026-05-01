# 计划 05：长任务上下文与项目计划机制

## 目标

解决项目级长任务中 AI 上下文清空、任务拆分不清、阶段交接困难的问题。

建立一种仓库内可恢复的任务机制，让新的 AI 会话能通过文件继续任务，而不是依赖聊天历史。

## 输入文件

- `Docs/框架/优化/AI/ai_first_godot_csharp_ecs_program_overview.md`
- `DocsAI/ProjectState.md`
- `DocsAI/Workflows/AI开发闭环.md`
- `Docs/superpowers/plans/`

## 修改范围

- 新增 `DocsAI/Workflows/长任务上下文协议.md`
- 新增当前计划目录内的 `Progress.md`
- 新增当前计划目录内的 `Backlog.md`
- 新增当前计划目录内的 `Done.md`
- 新增当前计划目录内的 `PlanTemplate_Example.md`
- 更新 `DocsAI/README.md`

## 执行步骤

1. 定义长任务文件结构：
   - 当前目标
   - 当前阶段
   - 已完成
   - 未完成
   - 阻塞问题
   - 下一步
   - 验证方式
   - 人工审查重点
2. 把 `Docs/superpowers/plans/` 中的历史计划归类为参考，不直接作为新机制主入口。
3. 制作计划目录模板，让每个新任务包都能复用。
4. 在 `ProjectState.md` 中记录当前迁移处于第几阶段。
5. 规定上下文恢复流程：
   - 读 `AGENTS.md`
   - 读 `DocsAI/README.md`
   - 读 `DocsAI/ProjectState.md`
   - 读当前计划文件
   - 再执行任务
6. 规定阶段完成后必须更新 `Progress / Done / ProjectState`。

## 验证命令

```bash
find Plans DocsAI/Workflows -maxdepth 2 -type f | sort
rg -n "当前目标|当前阶段|已完成|未完成|阻塞问题|下一步|验证方式" DocsAI Plans
```

## 验收标准

- 任意新 AI 会话能通过计划文件恢复当前任务。
- 每个长任务能拆成多个可独立执行的子任务。
- 阶段完成后有明确记录，不依赖聊天上下文。
- 人类可以快速知道当前任务进度和下一步。

## 风险点

- 如果每个小改动都写长任务计划，会增加维护负担。
- 只对跨多文件、多阶段、会持续多轮的任务启用这个机制。
- `ProjectState.md` 必须保持短，不要写成日志流水账。

## 完成输出

最终回复必须包含：

- 新增长任务文件
- 当前迁移阶段状态
- 下一阶段建议执行哪个 plan
- 旧 `Docs/superpowers/plans` 的处理建议
