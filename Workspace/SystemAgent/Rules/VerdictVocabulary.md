# Verdict 词表

所有 review gate、reviewer subagent 和 retrospective 输出的 verdict 必须是以下三档之一。verdict 行区分大小写，必须全大写。

## APPROVE

**含义**：全部检查通过；下游可继续。

**格式**：单独一行 `APPROVE`。不允许 `approve`、`Approve`、`APPROVED` 等变体。

## CONCERNS

**含义**：存在非阻塞问题；下游可继续但需跟进。

**格式**：第一行以 `CONCERNS` 开头，可附分号分隔的问题列表，例如：

```text
CONCERNS: 缺 BDD 试点; ProjectState.md 未同步
```

## REJECT

**含义**：存在阻塞问题；下游必须先修。

**格式**：第一行以 `REJECT` 开头，后续列出阻塞项。

## Grep rule

```bash
grep -E '^(APPROVE|CONCERNS|REJECT)' <reviewer_output>
```

## Additive metadata compatibility

Reviewer 输出可以包含 `remediation_phase: plan|implement|test|docs` 等附加元数据，但这些字段不能替代、重命名或包裹聚合 verdict 行。外部项目的 `verdict: APPROVE`、`NEEDS_CHANGES` 或类似格式只能作为参考，必须适配为本文件的 grep-compatible 聚合 verdict。

Reviewer 多 gate 时输出末尾必须有 1 行聚合 verdict。聚合按最严原则取：`REJECT > CONCERNS > APPROVE`。

## Aggregation rule

- 任一 gate 为 `REJECT`，聚合 verdict 为 `REJECT`。
- 否则任一 gate 为 `CONCERNS`，聚合 verdict 为 `CONCERNS`。
- 全部 gate 为 `APPROVE`，聚合 verdict 为 `APPROVE`。

## Retrospective mapping

`conclusion` 与 `verdict` 必须双向一致：

| conclusion | verdict |
| --- | --- |
| `pass` | `APPROVE` |
| `needs-followup` | `CONCERNS` |
| `blocked` | `REJECT` |

同一份 retrospective 输出中，`conclusion` 与 `verdict` 不一致即视为 retrospective 不完整。
