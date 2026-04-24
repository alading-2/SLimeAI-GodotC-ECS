---
name: godot-scene-test
description: 运行 Godot C# 项目测试场景并获取打印信息。用于 AI 需要通过 CLI 列出 Src/ECS/Test 下可运行测试场景、运行指定场景、捕获 stdout/stderr、根据打印信息自动 Debug 和复验时触发。
---

# Godot Scene Test

## 规则

- 默认用 CLI，不默认用 MCP。
- 只负责：列出测试场景、运行场景、返回打印信息、根据日志定位问题。
- 不写 Godot 资源文件，不重保存 `.tscn/.tres/.uid`。
- 路径缺失、Godot 不存在、场景不存在时，停止并说明原因。
- 修改代码前后运行 `git status --short`。

## Godot 路径

优先使用：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run <scene> --godot <godot-path>
```

否则按顺序读取：

```text
GODOT_BIN
GODOT_PATH
/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64
godot
```

## 常用命令

列出测试场景：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list
```

运行指定场景：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn
```

运行并先构建：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build
```

批量运行测试场景：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-all --continue-on-fail
```

## 手动 fallback

没有 runner 时：

```bash
dotnet build
"${GODOT_BIN:-godot}" --headless --path . --scene <res://scene.tscn> --no-header
```

## 日志判断

优先看：

```text
exitCode
timedOut
stdout
stderr
firstError
```

失败优先级：

```text
C# Script Error
Cannot instantiate C# script
Unhandled exception
Exception
[FAIL]
FAIL:
[失败]
Failed to load
scene not found
```

注意：不要只因为普通 `ERROR:` 就直接判失败，先看测试最终输出和 exit code。
