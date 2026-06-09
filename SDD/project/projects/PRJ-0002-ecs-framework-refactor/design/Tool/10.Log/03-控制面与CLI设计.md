# 控制面与 CLI 设计

> 更新：2026-06-09
> 状态：current design note

## 1. 原则

控制面分三层：

1. **文件**：默认事实源。
2. **CLI**：运行时临时覆盖。
3. **AI 建议**：从最近 run 里分析后回写文件。

这三层不是重复，而是各自负责不同稳定性。

## 2. 文件事实源

建议至少有三个文件：

- `Config/Log/log.profile.json`：默认策略。
- `Config/Log/log.rules.json`：可复用规则库。
- `Config/Log/log.overrides.json`：当前 run 或当前会话的临时覆盖快照。

文件里要能表达：

- default level。
- rule priority。
- budget。
- sink 开关。
- test 专用规则。
- validation 专用规则。
- flow 展开规则。
- phase 默认映射。
- analyzer 输出目录规则。

推荐形态：

```json
{
  "profile": "ai-default",
  "defaultSeverity": "Warn",
  "console": {
    "enabled": true,
    "showWallClockUtc": false,
    "showFlowSummary": true,
    "sink": "stdout-summary"
  },
  "jsonl": {
    "enabled": true,
    "sink": "buffered-file",
    "flush": "batch"
  },
  "godotEditor": {
    "enabled": false,
    "richText": false,
    "pushWarningsAndErrors": false
  },
  "rules": [
    {
      "owner": "Ability",
      "operation": "Cast",
      "minSeverity": "Info",
      "console": "summary",
      "jsonl": "full",
      "budgetPerSecond": 20
    }
  ]
}
```

`log.overrides.json` 必须带 `createdBy`、`createdAtRunElapsedMs`、`reason`、`expires`，否则临时覆盖会变成隐藏事实源。

## 3. CLI 控制

CLI 不应该只做“开关某个等级”，而应该直接面向 AI 调试任务。

建议命令：

- `logctl profile use <name>`
- `logctl set owner=Ability operation=Cast severity=Info console=summary jsonl=full`
- `logctl mute context=DamageSystem --console-only`
- `logctl enable channel=Validation`
- `logctl enable sink=godot-editor --profile editor-debug`
- `logctl flow expand operation=AbilityCast --max-steps 50`
- `logctl top --last 10s`
- `logctl analyze --run-dir <path> --out <path>`
- `logctl suggest --run-dir <path>`
- `logctl apply-suggestions --dry-run`
- `logctl snapshot --write-overrides`

## 4. CLI 的真实作用

CLI 适合处理这些场景：

- 某个模块突然刷屏，先临时压掉。
- 某个模块需要打开更细的证据。
- 需要快速确认是哪个 context 最 noisy。
- 需要基于最近一次 run 自动生成下一轮建议。
- 需要把某个 flow 从 console summary 临时展开为 JSONL full。
- 需要把某个 Validation failure 的相关 owner 打开到 Debug。
- 需要在人工 editor debug 时临时打开 Godot editor sink。

CLI 不适合单独承担永久配置，因为这会让下一次 AI 会话无法复现。

## 5. AI 建议回写

AI 不只是看日志，也要帮忙做策略优化。

建议流程：

```text
scene run
  -> runner 收集 stdout / JSONL / artifact
  -> logctl analyze 拆分 raw/by-owner/by-phase/flows/failures/noise
  -> 生成 ai-context.md
  -> AI 按 owner Log.md 读取热度 / 重复 / 缺字段 / 无价值日志
  -> 输出建议
  -> 人类确认或自动应用
  -> 回写 log.profile.json / log.rules.json
```

建议类型：

- 某 context 应降级到 `Warn`。
- 某 channel 应只进 JSONL，不刷 console。
- 某类重复日志应合并。
- 某个 test helper 应改成 Validation artifact。
- 某个 owner 缺 `phase` / `operation` / `reasonCode`。
- 某类 flow 应从逐条输出改为 summary。
- 某个 context 应只在失败 run 中展开。

## 6. 预算规则

必须支持：

- 每秒最大条数。
- 每 context 最大条数。
- 每 entity 最大条数。
- 每 operation 最多展开样本数。
- 每 phase 最大 console 条数。
- 每 flow 最大 step 展开数。
- 每 Validation check 最大 sample 数。

预算超出后，不能简单丢弃，应该：

- 输出摘要。
- 记录 suppressed count。
- 保持可追踪性。

预算不等于静默丢弃。预算超出后至少要写：

```text
[FLOW:SuppressedSummary] owner=Movement operation=CollisionCheck suppressed=382 windowMs=1000 reason=budget_exceeded
```

这样 AI 知道“有很多被压掉”，而不是误以为没有发生。

## 7. 回写边界

CLI 临时覆盖必须能落到：

- `log.overrides.json`
- scene run metadata
- gate report

否则下次还是要猜。

## 8. 开关策略裁决

用户关心“代码里的 Log 到底打开还是关闭，level 提高就是不打印了，能不能 CLI 控制”。裁决如下：

| 控制方式 | 适合 | 不适合 |
| --- | --- | --- |
| 代码 `if` / `IsEnabled` | 高频热路径避免构造昂贵字段；保护不该默认收集的数据 | 做长期策略和现场调试开关 |
| profile 文件 | 默认、可复现、可审查的策略 | 临场快速试错 |
| CLI override | 单次 run / 当前会话的快速调试 | 长期事实源 |
| AI suggestion | 根据 run digest 建议降噪、补字段、调预算 | 自动无审查大范围改策略 |

结论：**默认策略必须在文件，实时调整用 CLI，代码只负责廉价地判断是否需要构造日志。**

## 9. Sink 控制裁决

AI-first 默认 sink 不是 Godot Output 面板：

| Sink | 默认 | CLI/profile 控制 | 说明 |
| --- | --- | --- | --- |
| `stdout-summary` | 开 | `console=summary/off` | C# stdout，只输出摘要和关键错误。 |
| `jsonl-buffered-file` | 开 | `jsonl=full/summary/off` | AI 主输入，buffered file 写入。 |
| `memory` | 开 | Validation 内部控制 | 测试和 artifact 汇总使用。 |
| `artifact` | 开 | runner/Validation 控制 | gate report 和 analyzer 使用。 |
| `godot-editor` | 关 | `sink=godot-editor` | 仅人工 editor debug；可选择是否 `GD.PushWarning/Error`。 |

`GD.PrintRich` / `GD.PushError` 不再是默认打印实现。它们只能由 `GodotEditorSink` 调用，并且必须在 profile 和 run metadata 中留下痕迹。这样 AI 下次恢复时能知道当时是否启用了 Godot editor 输出。

## 10. 运行时接入方式

CLI 对 Godot headless run 的控制可以通过环境变量和 override 文件进入：

```text
SLIMEAI_LOG_PROFILE=ai-default
SLIMEAI_LOG_OVERRIDES=/path/to/log.overrides.json
SLIMEAI_LOG_RUN_DIR=/path/to/run
```

runner 负责把这些注入进 Godot 进程，Log runtime 负责读取并记录到 run metadata。这样 CLI 不需要直接连 Godot 运行时，也能保持可复现。

未来如果需要 live toggling，再考虑 Godot Debug panel / local socket / file watcher；第一版不把 live socket 作为必需项。
