# 日志判定与 Debug

本文说明 AI 如何根据 Godot CLI 测试输出定位问题。

## 失败优先级

runner 按以下优先级给出 `failureReason`：

1. `TimedOut`
2. `ExitCodeNonZero:<code>`
3. `CSharpScriptError`
4. `CannotInstantiateCSharpScript`
5. `UnhandledException`
6. `Exception`
7. `TestFailMarker`
8. `FailedToLoad`
9. `SceneNotFound`

普通 Godot `ERROR:` 只进入日志摘要，不直接让 `failed=true`。

## 读取顺序

1. 看 `failed`。
2. 看 `failureReason`。
3. 看 `firstError`。
4. 看 `errorContext`。
5. 看 `logSummary.importantLines`。
6. 需要更多上下文时优先用 `--log-dir .ai-temp/scene-tests/runs` 保存完整日志后 `rg` 检索；只在确实需要把完整日志放进 JSON 时使用 `--full-logs`。

## 定位规则

- `CannotInstantiateCSharpScript`：优先查脚本类名、partial、命名空间、编译错误。
- `CSharpScriptError`：优先看 `firstError` 和其后的 C# 堆栈。
- `UnhandledException` / `Exception`：优先查堆栈顶端的项目源码路径。
- `FailedToLoad`：优先查 `res://` 路径、ResourceManagement、资源是否存在。
- `SceneNotFound`：优先查场景路径是否拼错，是否在项目根目录内。
- `TestFailMarker`：优先查测试场景里主动输出的失败原因。
- `TimedOut`：优先查死循环、等待条件、场景退出逻辑、异步计时器。

## Debug 闭环

1. 复现失败：
   ```bash
   node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run <scene> --build --attempts 2 --errors-only --log-dir .ai-temp/scene-tests/runs
   ```
2. 根据 `failureReason` 和 `errorContext` 定位源码。
3. 长日志用 `rg` 检索：
   ```bash
   rg -n -m 80 -C 3 "ERROR:|\\[ERROR\\]|\\[FAIL\\]|Exception|Cannot instantiate" .ai-temp/scene-tests/runs/<日期>/<时间>
   ```
4. 小步修复。
5. 重新运行同一个场景。
6. 如果涉及共享模块，再用 `run-many` 运行测试矩阵里的相关场景。
7. 如果问题属于 UI、游戏画面、动画、摄像机或编辑器窗口，执行视觉截图流程：
   ```bash
   "${GODOT_BIN:-/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64}" --path . --scene res://Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn
   flameshot gui -d 3000
   ```
8. 最终回复记录失败原因、修复点和复验命令；如果保留日志，说明 `logIndex`。
9. 分析完成且无需保留证据时，删除本轮日志目录：
   ```bash
   rm -r -- .ai-temp/scene-tests/runs/<日期>/<时间>
   ```
