# Godot 场景测试 Runner 使用说明

本文面向人类阅读，说明 `.codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs` 已实现什么、会输出什么、如何拿 Godot 打印信息，以及长日志应该如何保存和检索。

AI 执行流程仍以 `DocsAI/Tests/` 为准；本文解释“为什么这样用”和“日志在哪里看”。

## 1. 这个 Runner 做什么

Runner 通过命令行启动 Godot C# 项目的测试场景，捕获 Godot 进程的 `stdout` / `stderr`，再整理成 JSON。

它不会打开 Godot 编辑器窗口，因为运行命令使用了：

```bash
--headless --path . --scene <scene> --no-header
```

`--headless` 表示无界面模式，适合 VM、CI、AI 调试和终端验证。因此在 VMware Ubuntu 桌面里看不到 Godot 窗口是正常的；场景仍然会被 Godot 进程加载和运行，打印信息从进程输出里捕获。

`--no-header` 只是不输出 Godot 启动头信息，主要减少日志噪音，基本不影响运行速度。真正能减少耗时的是：一次命令里 `--build` 只构建一次，然后用 `run-many` 顺序跑多个相关场景。

不建议为了加快速度并行多开 Godot。Godot C# 项目会使用 `.godot/mono` 临时编译产物、资源导入缓存和项目级运行状态；并行多进程更容易制造偶发冲突，测试结论也更难解释。默认策略是一个 Godot headless 进程结束后，再启动下一个。

## 2. 已实现功能

### 2.1 列出测试场景

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list
```

默认扫描 `Src/ECS/Test` 下的 `.tscn`，排除 Entity 模板类场景。

按路径筛选：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter Movement
```

### 2.2 运行单个场景

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build
```

建议默认加 `--build`，因为 Godot C# 场景经常依赖最新编译结果。

### 2.3 顺序运行多个指定场景

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-many \
  res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn \
  res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn \
  --build --continue-on-fail
```

`run-many` 用于“改了某个系统后，跑这个系统相关的几个测试”。它不是全量回归。

执行方式是同步顺序执行：第一个 Godot 进程退出后，才启动下一个场景。不会同时多开 Godot。

如果传 `--build`，`run-many` 会在批次开始前执行一次 `dotnet build`，然后逐个运行场景；不会每个场景都重新 build。

### 2.4 批量运行全部或筛选后的场景

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-all --filter Movement --build --continue-on-fail
```

`run-all` 也不是并发执行。它会先收集场景列表，再按顺序逐个运行。只有在所有场景跑完后，终端才输出聚合 JSON；这容易让人误以为“没有同步”或“卡住了”。如果要观察每个场景的完整打印，使用 `--log-dir`。

## 3. 打印信息怎么拿

Godot 的 `GD.Print`、`GD.PrintRich`、`GD.PushWarning`、`GD.PushError` 以及 C# 异常堆栈，都会进入 Godot 进程的 `stdout` 或 `stderr`。

Runner 默认不会把完整日志都塞进最终 JSON，只保留尾部和摘要，避免上下文爆炸。完整打印建议写入日志目录：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-many \
  res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn \
  res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn \
  --build --continue-on-fail --errors-only \
  --log-dir .ai-temp/scene-tests/runs
```

本项目常用 `Src/ECS/Tools/Logger/Log.cs` 打印，而不是直接使用原版 `GD.Print`。Runner 可以捕获这些输出：

- `Log.Info / Debug / Success / Trace` 通过 `GD.PrintRich` 输出，通常进入 `stdout`。
- `Log.Warn` 会同时调用 `GD.PushWarning` 和 `GD.PrintRich`，因此 warning 可能同时出现在 Godot 调试输出和普通打印中。
- `Log.Error` 会同时调用 `GD.PushError` 和 `GD.PrintRich`，因此 error 可能同时出现在 `stderr` 和 `stdout`。
- 日志上下文形如 `[LogTest]`、`[MovementCollisionRuntimeTest]`，可以按组件/系统名检索。

生成结构：

```text
.ai-temp/scene-tests/runs/<日期>/<时间>/
├── index.json
├── 001_<场景路径>_attempt1/
│   ├── screenshots/
│   ├── artifacts/
│   ├── stdout.log
│   ├── stderr.log
│   ├── combined.log
│   └── result.json
└── 002_<场景路径>_attempt2/
    ├── screenshots/
    ├── artifacts/
    ├── stdout.log
    ├── stderr.log
    ├── combined.log
    └── result.json
```

日期目录示例：`.ai-temp/scene-tests/runs/2026-05-01/21-30-00/`。如果同一秒内创建多个运行目录，会自动追加序号：`21-30-00-002`。

设计选择：

- 一个场景一个目录：定位问题最快，不会把多个场景的输出混在一起。
- 每个场景保留 `stdout.log` / `stderr.log`：方便判断打印来源。
- 每个场景保留 `combined.log`：方便一次 `rg` 搜索。
- 每个场景预创建 `screenshots/` / `artifacts/`：场景运行中主动保存的截图和辅助产物与打印信息同目录归档。
- 每次运行保留 `index.json`：保存本次运行的场景、失败原因、首个错误和日志路径。
- 默认只保留最近 1 天的日期目录；每次使用 `--log-dir` 时会清理更旧日期，避免日志长期膨胀。可用 `--log-retention-days <n>` 调整。
- `runs/` 默认不提交到 Git：日志是运行产物，可能很大。

使用 `--log-dir` 时，runner 会在启动 Godot 前注入这些环境变量，测试场景可按需读取：

```text
GODOT_SCENE_TEST_RUN_DIR
GODOT_SCENE_TEST_RUN_DIR_REL
GODOT_SCENE_TEST_SCENE_DIR
GODOT_SCENE_TEST_SCENE_DIR_REL
GODOT_SCENE_TEST_SCREENSHOT_DIR
GODOT_SCENE_TEST_SCREENSHOT_DIR_REL
GODOT_SCENE_TEST_ARTIFACT_DIR
GODOT_SCENE_TEST_ARTIFACT_DIR_REL
```

如果场景需要保存 headless 截图，应把 PNG 写到 `GODOT_SCENE_TEST_SCREENSHOT_DIR`。runner 会在 `result.json` 和 `index.json` 中写入 `artifactDirs.screenshots` 与 `artifacts.screenshots`。这类截图是测试打印产物，和 stdout/stderr 一样用于分析和复验；它不替代人工视觉判断。

任务收尾规则：

- 一次 plan / 一轮修复结束时，如果日志已经分析完且不需要留证据，删除本轮具体运行目录。
- 删除目标从返回 JSON 的 `logIndex` 得到，例如 `.ai-temp/scene-tests/runs/2026-05-02/08-56-30/index.json` 对应删除 `.ai-temp/scene-tests/runs/2026-05-02/08-56-30`。
- `--log-retention-days` 只是兜底清理旧日期，不替代本轮任务收尾删除。

```bash
rm -r -- .ai-temp/scene-tests/runs/<日期>/<时间>
```

## 4. 长日志怎么 grep

不要把完整日志直接贴进对话上下文。先落盘，再用 `rg` 搜。

常用命令，先看本次索引：

```bash
rg -n "failed|status|failureReason|firstError|logFiles" .ai-temp/scene-tests/runs/<日期>/<时间>/index.json
```

只看错误，限制最多 80 条，带前后 3 行上下文：

```bash
rg -n -m 80 -C 3 "ERROR:|\\[ERROR\\]|\\[FAIL\\]|Exception|Cannot instantiate" .ai-temp/scene-tests/runs/<日期>/<时间>
```

按 Log 上下文过滤，例如只看某个组件/测试类：

```bash
rg -n -m 120 -C 2 "\\[MovementCollisionRuntimeTest\\]|\\[MovementCollisionPolicy\\]" .ai-temp/scene-tests/runs/<日期>/<时间>/**/combined.log
```

只看某个场景目录：

```bash
rg -n -m 80 -C 3 "ERROR:|\\[FAIL\\]" .ai-temp/scene-tests/runs/<日期>/<时间>/001_*/combined.log
```

如果某个上下文仍然太多，继续叠加关键词：

```bash
rg -n -m 80 -C 3 "\\[MovementCollisionRuntimeTest\\].*owner|TeamFilter|友军|敌方" .ai-temp/scene-tests/runs/<日期>/<时间>/**/combined.log
```

`--errors-only` 只影响 JSON 的 `logSummary.importantLines` 摘要，不会删除日志文件里的原始打印。

AI 分析长日志时，不应该把几万行日志放入主上下文。推荐让子任务读取日志文件、用 `rg` 分批提取、最后只返回“失败原因、证据行、相关源码路径、下一步建议”。

## 5. JSON 输出字段

单场景 `run` 输出：

- `scene`：实际运行的 `res://` 场景路径。
- `status`：场景状态，`passed` / `failed` / `timed_out`。
- `attempt`：当前返回结果是第几次尝试。
- `attemptsUsed` / `maxAttempts`：实际尝试次数和允许尝试次数。
- `exitCode`：Godot 进程退出码。
- `timedOut`：是否超时。
- `failed`：runner 是否判定失败。
- `failureReason`：失败分类，例如 `TimedOut`、`ExitCodeNonZero:1`、`CannotInstantiateCSharpScript`、`TestFailMarker`。
- `stdout` / `stderr`：默认只保留尾部；`--full-logs` 才输出完整内容。
- `stdoutLineCount` / `stderrLineCount`：完整日志行数。
- `rawLogsTruncated`：JSON 里的原始日志是否被裁剪。
- `firstError`：第一条命中的关键错误。
- `errorContext`：关键错误附近几行上下文。
- `logSummary`：尾部日志和重要行摘要。
- `logFiles`：使用 `--log-dir` 时出现，指向落盘日志文件。
- `artifactDirs`：使用 `--log-dir` 时出现，指向本场景的截图和辅助产物目录。
- `artifacts`：使用 `--log-dir` 时出现，列出本场景已生成的截图和辅助产物文件。
- `logIndex`：使用 `--log-dir` 时出现，指向本次运行索引。
- `attempts`：只有最终仍失败时出现，列出每次尝试的简要结果和日志路径。成功时只返回最新一次结果。

多场景 `run-many` / `run-all` 输出：

- `failed`：任一场景失败则为 `true`。
- `results`：每个场景的单场景结果数组。
- `logIndex`：使用 `--log-dir` 时出现，指向本次运行索引。

## 6. 怎么判断失败

优先级：

1. `timedOut=true`：场景超时，常见原因是测试没有自动退出、等待条件没满足、死循环。
2. `exitCode != 0`：Godot 进程非零退出，通常测试主动失败或运行异常。
3. `firstError` 命中关键模式：例如 C# 脚本无法实例化、异常、`[FAIL]`。

普通 `WARNING:` 一般不当作失败。普通 Godot `ERROR:` 会进入摘要和日志，但是否失败要结合 `failed / failureReason / exitCode / timedOut` 判断。

可以用 `--attempts <1-3>` 重试不稳定场景。最多 3 次；如果某次成功，最终 JSON 只返回成功那次的最新结果。如果全部失败，最终 JSON 返回最后一次完整结果，并在 `attempts` 里列出每次失败摘要。

## 7. 什么时候不用命令行

headless CLI 适合验证逻辑、生命周期、数据、碰撞语义、日志和异常。以下情况不能只依赖命令行：

- UI 布局是否重叠、是否好看。
- 游戏实际运行画面、动画、材质、粒子、摄像机效果。
- Godot 编辑器内弹窗、报错窗口、插件界面。
- 需要截图给人判断的问题。

headless 场景可以主动把视口截图保存到 `GODOT_SCENE_TEST_SCREENSHOT_DIR`，这种截图会作为测试产物进入 `.ai-temp/scene-tests/runs`。需要截图 Linux 桌面、游戏运行画面、UI 或报错窗口时，仍然先打开有界面 Godot 场景：

```bash
"${GODOT_BIN:-/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64}" --path . --scene res://Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn
```

再使用 Flameshot：

```bash
flameshot gui -d 3000
```

执行后 3 秒内切到目标窗口并框选截图。人工截图是视觉验证流程，不替代 CLI 日志测试；两者结论要分开记录。

## 8. 当前没实现什么

- 不打开 Godot 编辑器窗口；这是 headless CLI 测试，不是人工视觉调试。
- 不代替人工视觉判断；headless 只能收集场景主动保存到 `GODOT_SCENE_TEST_SCREENSHOT_DIR` 的截图产物。
- 不实时流式输出每一行 Godot 打印；当前是场景进程结束后整理输出。完整日志通过 `--log-dir` 持久化。
- 不自动修复代码；runner 只负责运行、捕获、摘要和落盘。
- 默认不写日志文件；只有显式传 `--log-dir` 才落盘，避免每次测试都产生大量文件。

## 9. 推荐工作流

改动代码后：

```bash
git status --short
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-many <相关场景1> <相关场景2> --build --continue-on-fail --attempts 2 --errors-only --log-dir .ai-temp/scene-tests/runs
rg -n -m 80 -C 3 "ERROR:|\\[ERROR\\]|\\[FAIL\\]|Exception|Cannot instantiate" .ai-temp/scene-tests/runs/<日期>/<时间>
rm -r -- .ai-temp/scene-tests/runs/<日期>/<时间>
git status --short
```

只有核心模块大改或发布前，才考虑：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-all --build --continue-on-fail --errors-only --log-dir .ai-temp/scene-tests/runs
```
