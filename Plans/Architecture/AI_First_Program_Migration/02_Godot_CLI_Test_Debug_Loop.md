# 计划 02：Godot CLI 测试与日志 Debug 闭环

## 目标

让 AI 可以通过 CLI 独立完成以下动作：

- 列出可运行测试场景
- 构建 Godot C# 项目
- 运行指定场景
- 捕获 stdout / stderr
- 提取关键错误上下文
- 根据日志定位问题并复验

目标不是替代全部人工体验测试，而是先消除“人工打开 Godot 复制日志”的重复劳动。

## 输入文件

- `.codex/skills/godot-scene-test/SKILL.md`
- `.codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs`
- `Src/ECS/Test/`
- `Docs/框架/优化/AI/ai_first_godot_csharp_ecs_program_overview.md`

## 修改范围

- 更新 `godot-scene-runner.mjs`
- 更新 `godot-scene-test/SKILL.md`
- 新增 `DocsAI/Tests/Godot场景测试.md`
- 新增 `DocsAI/Tests/测试矩阵.md`
- 新增 `DocsAI/Tests/日志判定与Debug.md`
- 更新 `Docs/框架/项目索引.md`

## 执行步骤

1. 保留现有 `list / run / run-all` 命令，不破坏当前用法。
2. 为 runner 输出增加稳定字段：
   - `failed`
   - `failureReason`
   - `firstError`
   - `errorContext`
   - `logSummary`
3. 增加可选参数：
   - `--filter <text>`：按路径筛选测试场景
   - `--max-log-lines <n>`：限制日志摘要长度
   - `--output <path>`：把 JSON 结果写入文件
4. 失败判定优先级：
   - 超时
   - 非 0 exit code
   - C# Script Error
   - Cannot instantiate C# script
   - Unhandled exception
   - `[FAIL]` / `FAIL:` / `[失败]`
   - Failed to load / scene not found
5. 注意：普通 Godot `ERROR:` 不直接等于测试失败，需要结合最终测试输出和 exit code。
6. 在 DocsAI 测试文档中记录常用场景、适用模块和推荐运行命令。
7. 将 Skill 缩短为入口文档，只指向 DocsAI 测试文档和 runner 命令。

## 验证命令

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter Movement
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build
```

## 验收标准

- `list` 能稳定列出当前测试场景。
- `run` 返回结构化 JSON，包含失败判断和日志摘要字段。
- `run-all --continue-on-fail` 能保留所有场景结果。
- Skill 明确告诉 AI 失败后如何读日志、如何定位、如何复验。
- AI 不需要用户手动复制 Godot Output。

## 风险点

- Godot headless 有时会输出非致命 `ERROR:`，不能过度判失败。
- 不要让 runner 自动修改 `.tscn/.tres/.uid`。
- 不要把测试 runner 做成复杂测试框架，先保证日志闭环稳定。

## 完成输出

最终回复必须包含：

- runner 新增字段和参数
- 成功运行的测试场景
- 若测试失败，给出 `failureReason` 和关键日志
- 仍需人工打开 Godot 验证的场景类型

