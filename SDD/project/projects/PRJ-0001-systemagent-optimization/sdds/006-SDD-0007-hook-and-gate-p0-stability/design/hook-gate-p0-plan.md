# Hook and Gate P0 Task Plan

## Target Layers

| Layer | Target |
| --- | --- |
| Hook runtime | Stop event 总是合法 JSON，异常时 fallback 到最小合法输出 |
| Hook state | 只保存轻量计数、最近提示和敏感路径标记，不保存任务正文 |
| PostToolUse | 同类提示去重，验证命令和敏感路径触发一次性提示 |
| Gate evidence | 优先从当前 SDD artifact 读取 selected workflow、must-read、tasks、progress、bdd、validation |
| Documentation | README、workflow、gate、policy 和 catalog 指向稳定事实源 |

## Execution Order

1. 先定位现有 hook script、配置入口和 smoke 方式。
2. 先写或补齐 smoke，再修改 Stop fallback。
3. 再做 PostToolUse 降噪，避免把 P1 噪声与 P0 稳定性混在一个不可验证改动中。
4. 最后同步 Gate 输入契约和文档。
