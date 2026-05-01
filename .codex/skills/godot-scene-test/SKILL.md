---
name: godot-scene-test
description: 运行 Godot C# 项目测试场景并获取打印信息。用于 AI 需要通过 CLI 列出 Src/ECS/Test 下可运行测试场景、运行指定场景、捕获 stdout/stderr、根据日志自动 Debug 和复验时触发。
---

# Godot Scene Test

## 职责

- 只负责 CLI 场景测试、日志捕获、失败摘要和复验。
- 不写 Godot 资源文件，不重保存 `.tscn/.tres/.uid`。
- 路径缺失、Godot 不存在、场景不存在时，停止并说明原因。
- 修改代码前后运行 `git status --short`。

## 必读

- `DocsAI/Tests/Godot场景测试.md`
- `DocsAI/Tests/测试矩阵.md`
- `DocsAI/Tests/日志判定与Debug.md`

## 常用命令

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter Movement
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-all --continue-on-fail
```

默认 `stdout` / `stderr` 只保留尾部摘要，避免长日志刷屏。需要完整原始日志时加 `--full-logs`，或用 `--output <path>` 保存 JSON 后再检索。

## 输出优先看

```text
failed
failureReason
firstError
errorContext
logSummary
rawLogsTruncated
exitCode
timedOut
```

普通 Godot `ERROR:` 不直接等于测试失败，先看 `failed`、`failureReason`、测试最终输出和 exit code。

## Godot 路径

runner 按顺序读取：

```text
--godot <path>
GODOT_BIN
GODOT_PATH
/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64
godot
```

## 手动 fallback

```bash
dotnet build
"${GODOT_BIN:-godot}" --headless --path . --scene <res://scene.tscn> --no-header
```
