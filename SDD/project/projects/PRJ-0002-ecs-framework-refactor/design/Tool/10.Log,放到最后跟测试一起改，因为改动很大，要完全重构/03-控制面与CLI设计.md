# 控制面与 CLI 设计

> 更新：2026-05-31
> 状态：current design note

## 1. 原则

控制面分三层：

1. **文件**：默认事实源。
2. **CLI**：运行时临时覆盖。
3. **AI 建议**：从最近 run 里分析后回写文件。

这三层不是重复，而是各自负责不同稳定性。

## 2. 文件事实源

建议至少有三个文件：

- `log.profile.json`：默认策略。
- `log.rules.json`：可复用规则库。
- `log.overrides.json`：当前 run 或当前会话的临时覆盖快照。

文件里要能表达：

- default level。
- rule priority。
- budget。
- sink 开关。
- test 专用规则。
- validation 专用规则。

## 3. CLI 控制

CLI 不应该只做“开关某个等级”，而应该直接面向 AI 调试任务。

建议命令：

- `logctl profile <name>`
- `logctl set context=EntityManager level=Debug`
- `logctl mute context=DamageSystem`
- `logctl enable channel=Validation`
- `logctl top --last 10s`
- `logctl suggest --run-dir <path>`
- `logctl apply-suggestions --dry-run`

## 4. CLI 的真实作用

CLI 适合处理这些场景：

- 某个模块突然刷屏，先临时压掉。
- 某个模块需要打开更细的证据。
- 需要快速确认是哪个 context 最 noisy。
- 需要基于最近一次 run 自动生成下一轮建议。

CLI 不适合单独承担永久配置，因为这会让下一次 AI 会话无法复现。

## 5. AI 建议回写

AI 不只是看日志，也要帮忙做策略优化。

建议流程：

```text
scene run
  -> 生成 log digest
  -> AI 读取热度 / 重复 / 缺字段 / 无价值日志
  -> 输出建议
  -> 人类确认或自动应用
  -> 回写 log.profile.json / log.rules.json
```

建议类型：

- 某 context 应降级到 `Warn`。
- 某 channel 应只进 JSONL，不刷 console。
- 某类重复日志应合并。
- 某个 test helper 应改成 Validation artifact。

## 6. 预算规则

必须支持：

- 每秒最大条数。
- 每 context 最大条数。
- 每 entity 最大条数。
- 每 operation 最多展开样本数。

预算超出后，不能简单丢弃，应该：

- 输出摘要。
- 记录 suppressed count。
- 保持可追踪性。

## 7. 回写边界

CLI 临时覆盖必须能落到：

- `log.overrides.json`
- scene run metadata
- gate report

否则下次还是要猜。
