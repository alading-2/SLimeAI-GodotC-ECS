# Godot 场景测试

本文说明 AI 如何通过 CLI 运行 Godot C# 测试场景并读取日志。

## 入口脚本

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs <command>
```

命令：

- `list`：列出 `Src/ECS/Test` 下可运行测试场景。
- `run <scene>`：运行指定场景。
- `run-all`：批量运行测试场景。

常用参数：

- `--build`：运行前先执行 `dotnet build`。
- `--filter <text>`：按路径筛选测试场景。
- `--continue-on-fail`：批量运行时失败后继续。
- `--max-log-lines <n>`：限制日志摘要行数。
- `--full-logs`：输出完整 stdout / stderr；默认只输出尾部摘要。
- `--output <path>`：把 JSON 结果写入项目内文件。
- `--godot <path>`：显式指定 Godot 可执行文件。
- `--timeout <ms>`：设置单次命令超时。

## 常用命令

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter Movement
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-all --continue-on-fail
```

## 输出字段

运行场景时优先看：

- `failed`：本次场景是否失败。
- `failureReason`：失败原因分类。
- `firstError`：第一条关键错误行。
- `errorContext`：关键错误附近日志。
- `logSummary`：stdout/stderr 尾部和重要日志摘要。
- `rawLogsTruncated`：原始 stdout/stderr 是否被裁剪。
- `stdoutLineCount` / `stderrLineCount`：完整日志行数。
- `exitCode`：Godot 进程退出码。
- `timedOut`：是否超时。

## 使用规则

- 不要让 AI 要求用户打开 Godot 复制 Output；先跑 runner。
- 不要因为普通 `ERROR:` 直接判失败，先看 `failed` 和 `failureReason`。
- 不要运行会重保存 `.tscn/.tres/.uid` 的编辑器操作。
- 如果 headless 无法覆盖视觉表现，再说明需要人工打开 Godot 验证。
