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
- 本轮任务使用 `--log-dir` 生成的日志，分析结束后删除对应 `.ai-temp/scene-tests/runs/<日期>/<时间>` 目录；只在需要复查或给用户留证据时保留路径。

## 必读

- `DocsAI/Tests/Godot场景测试.md`
- `DocsAI/Tests/测试矩阵.md`
- `DocsAI/Tests/日志判定与Debug.md`

## 常用命令

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter Movement
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build --attempts 2 --errors-only --log-dir .ai-temp/scene-tests/runs
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-many res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn --build --continue-on-fail --attempts 2 --errors-only --log-dir .ai-temp/scene-tests/runs
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-all --filter Movement --build --continue-on-fail --attempts 2 --errors-only --log-dir .ai-temp/scene-tests/runs
```

默认 `stdout` / `stderr` 只保留尾部摘要，避免长日志刷屏。需要完整打印信息时优先加 `--log-dir .ai-temp/scene-tests/runs`，然后用 `rg` 检索落盘日志；只有确实需要 JSON 内含完整日志时才加 `--full-logs`。

使用 `--log-dir` 时，runner 会为每个场景 attempt 创建 `screenshots/` 和 `artifacts/` 子目录，并注入环境变量：

```text
GODOT_SCENE_TEST_SCREENSHOT_DIR
GODOT_SCENE_TEST_ARTIFACT_DIR
GODOT_SCENE_TEST_SCENE_DIR
GODOT_SCENE_TEST_RUN_DIR
```

场景内主动保存的 PNG 截图写入 `GODOT_SCENE_TEST_SCREENSHOT_DIR`；这类截图属于测试打印产物，会出现在 `artifactDirs` / `artifacts.screenshots`，和 stdout/stderr 同目录管理。

`run` / `run-many` / `run-all` 都支持 `--attempts <1-3>`：失败才重试，成功即停止。`run-many` / `run-all` 都是同步顺序执行：一个 Godot headless 进程退出后才启动下一个。Godot 不会打开编辑器窗口，也不会同时多开。

`--no-header` 只减少 Godot 启动头信息，不负责加速。UI、游戏画面、编辑器报错窗口等视觉问题不能只靠 headless CLI，需要打开有界面 Godot 场景并用 `flameshot gui -d 3000` 截图。

## Visual Screenshot

需要验证 UI 布局、游戏实际画面、动画、摄像机、编辑器弹窗或报错窗口时，先启动有界面 Godot：

```bash
"${GODOT_BIN:-/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64}" --path . --scene res://Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn
```

再截图：

```bash
flameshot gui -d 3000
```

截图结论和 CLI 日志结论分开写。不要要求用户复制 Godot Output；日志仍优先用 runner 获取。

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
logFiles
artifactDirs
artifacts
logIndex
summary
skippedScenes
```

普通 Godot `ERROR:` 不直接等于测试失败，先看 `failed`、`failureReason`、测试最终输出和 exit code。

长日志检索：

```bash
rg -n -m 80 -C 3 "ERROR:|\\[ERROR\\]|\\[FAIL\\]|Exception|Cannot instantiate" .ai-temp/scene-tests/runs/<日期>/<时间>
```

本轮日志分析完成后清理：

```bash
rm -r -- .ai-temp/scene-tests/runs/<日期>/<时间>
```

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
