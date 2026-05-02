# Godot 场景测试

本文说明 AI 如何通过 CLI 运行 Godot C# 测试场景并读取日志。

## 入口脚本

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs <command>
```

命令：

- `list`：列出 `Src/ECS/Test` 下可运行测试场景。
- `run <scene>`：运行指定场景。
- `run-many <scene>...`：顺序运行多个显式指定的测试场景。
- `run-all`：顺序运行全部或筛选后的测试场景。

常用参数：

- `--build`：运行前先执行 `dotnet build`。
- `--filter <text>`：按路径筛选测试场景。
- `--continue-on-fail`：批量运行时失败后继续。
- `--max-log-lines <n>`：限制日志摘要行数。
- `--full-logs`：输出完整 stdout / stderr；默认只输出尾部摘要。
- `--errors-only`：`logSummary.importantLines` 只保留错误、异常和失败标记。
- `--attempts <1-3>`：失败后最多重试 3 次，成功即停止；成功时只返回最新成功结果，全部失败时返回最后一次完整结果和每次失败摘要。
- `--log-dir <path>`：把完整 stdout / stderr / combined log / result JSON 按场景落盘。
- `--log-retention-days <n>`：日志日期目录保留天数，默认 1 天。
- `--output <path>`：把 JSON 结果写入项目内文件。
- `--godot <path>`：显式指定 Godot 可执行文件。
- `--timeout <ms>`：设置单次命令超时。

## 常用命令

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter Movement
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build --attempts 2 --errors-only --log-dir Docs/测试/场景测试日志/runs
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-many res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn --build --continue-on-fail --attempts 2 --errors-only --log-dir Docs/测试/场景测试日志/runs
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-all --filter Movement --build --continue-on-fail --attempts 2 --errors-only --log-dir Docs/测试/场景测试日志/runs
```

## 执行语义

- Godot 以 `--headless` 运行，不打开编辑器窗口。
- `--no-header` 只减少 Godot 启动头信息，不负责加速。
- `run-many` / `run-all` 都是同步顺序执行，不会同时多开 Godot。
- `run / run-many / run-all --build` 只在命令开始前 `dotnet build` 一次；重试不会重复 build。
- 不建议并行多开 Godot；Godot C# 的 `.godot/mono`、资源导入缓存和运行状态更适合单进程顺序验证。
- `run-all` 只在全部场景结束后输出聚合 JSON；需要中间结果时必须使用 `--log-dir`，已完成场景会先落盘。
- 一般修改只运行测试矩阵中相关场景；不要默认全量跑所有测试。

## 输出字段

运行场景时优先看：

- `failed`：本次场景是否失败。
- `status`：`passed` / `failed` / `timed_out`。
- `attemptsUsed` / `maxAttempts`：实际尝试次数和允许尝试次数。
- `failureReason`：失败原因分类。
- `firstError`：第一条关键错误行。
- `errorContext`：关键错误附近日志。
- `logSummary`：stdout/stderr 尾部和重要日志摘要。
- `rawLogsTruncated`：原始 stdout/stderr 是否被裁剪。
- `stdoutLineCount` / `stderrLineCount`：完整日志行数。
- `exitCode`：Godot 进程退出码。
- `timedOut`：是否超时。
- `logFiles`：使用 `--log-dir` 时出现，指向该场景完整日志文件。
- `logIndex`：使用 `--log-dir` 时出现，指向本次运行的 `index.json`。
- `summary` / `skippedScenes`：批量运行时出现，说明执行、跳过、通过、失败和超时数量。

## 日志落盘与 grep

长日志不要直接塞进上下文。优先：

```bash
rg -n -m 80 -C 3 "ERROR:|\\[ERROR\\]|\\[FAIL\\]|Exception|Cannot instantiate" Docs/测试/场景测试日志/runs/<日期>/<时间>
```

日志目录说明见 `Docs/测试/Godot场景测试Runner使用说明.md` 和 `Docs/测试/场景测试日志/README.md`。

日志是临时运行产物。一次任务结束前，如果已经完成分析和复验，删除本轮生成的具体运行目录：

```bash
rm -r -- Docs/测试/场景测试日志/runs/<日期>/<时间>
```

只删除本轮 `logIndex` 指向的目录；不要清空整个 `runs/`。

## 视觉截图边界

- CLI headless 测试拿日志，不拿游戏窗口截图。
- UI 布局、游戏实际画面、编辑器报错窗口需要人工打开有界面场景后截图。
- Linux 桌面截图使用 `flameshot gui -d 3000`，执行后 3 秒内切到目标窗口并框选。
- 截图结论和日志结论分开记录。

视觉预览场景命令：

```bash
"${GODOT_BIN:-/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64}" --path . --scene res://Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn
flameshot gui -d 3000
```

## 使用规则

- 不要让 AI 要求用户打开 Godot 复制 Output；先跑 runner。
- 不要因为普通 `ERROR:` 直接判失败，先看 `failed` 和 `failureReason`。
- 默认加 `--build`；摘要默认加 `--errors-only`，完整信息用 `--log-dir` 保存后 grep。
- 不要运行会重保存 `.tscn/.tres/.uid` 的编辑器操作。
- 如果 headless 无法覆盖视觉表现，再说明需要人工打开 Godot 验证。
- 任务收尾时记录验证命令、关键失败信息；已分析完的本轮日志目录要删除。
