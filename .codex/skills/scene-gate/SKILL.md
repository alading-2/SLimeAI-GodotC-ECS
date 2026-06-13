---
name: scene-gate
description: Godot 验证场景门禁。检查新/改动的场景是否有完整标准答案（README 5 字段 + PASS artifact），输出 gate report。
---

# Scene Gate

## 触发条件

- SDD verify 阶段（任务涉及 `.tscn` 文件时）
- 独立调用：`scene-gate <scene-path-1> <scene-path-2> ...`

## 输入

- `scenePaths`：要检查的场景路径列表（`res://` 格式）
- `changeName`：关联的 SDD 或兼容任务名（如有）
- `manifestPath`：验证 manifest，默认使用当前游戏项目 `DocsAI/ValidationManifest.json`，不存在时以项目约定 manifest 为准
- `runDir`：结构化场景运行目录，默认读取 `.ai-temp/scene-tests/runs` 最新 run
- `gateReport`：analyzer 生成的 durable gate report，默认 `<runDir>/gate-report.json`

## 检查项

对每个单场景或 batch manifest 场景：

1. **README 存在**：场景同目录是否有 `README.md`
2. **5 个标准答案字段**：README 是否包含 `expectedInputs`、`expectedObservations`、`passCriteria`、`failCriteria`、`artifactPath`
3. **index.json 存在**：最近一次 run 的 `.ai-temp/scene-tests/runs/<date>/<time>/index.json` 是否包含该 scene，且 entry `status=passed`、`exitCode=0`
4. **result.json 存在**：per-scene `result.json` 是否为 `status=passed`、`exitCode=0`、`firstError=null`
5. **PASS artifact**：最近一次 run 是否有 PASS 状态的 scene artifact，且 artifact 中 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath` 非空
6. **checks[] 覆盖**：artifact `checks[].name` 是否包含 manifest、FeatureSpec 或 BDD mapping 声明的 check
7. **manifest 同步**：scene 是否存在于 manifest，且包含 owner、scope、tags、relatedBddScenarios、checks、artifactFilename、catalogOwner、timeoutSeconds、releaseBatch
8. **artifact 时效**：artifact 时间是否晚于场景、README、manifest 和对应 catalog 的最后修改时间；旧证据至少 `warn`
9. **Catalog 同步**：框架场景在框架测试 catalog，游戏场景在游戏侧 `DocsAI/ValidationCatalog.md` 或项目约定索引中包含该场景条目

Batch evidence 还必须检查：

- `index.json.metadata.manifest` 记录了 manifest path、筛选条件和 selected scene 数量。
- manifest-required scenes 均出现在 `index.json.entries[]`，缺失则 `block`。
- analyzer `gate-report.json` 包含 requested/passed/failed/skipped/missing、evidence paths、五字段 completeness、missingChecks、freshness warnings 和最终 `pass|warn|block`。
- `gate-report.json.verdict=block` 时不得用 stdout PASS marker、exit code 0 或 analyzer compact summary 覆盖。

## 输出格式

```markdown
# Scene Gate Report: <changeName>

## Summary

<pass | warn | block>

## Per-Scene Results

| Scene     | README | 5 Fields | index.json | result.json | PASS Artifact | Fresh | Catalog | Status |
| --------- | ------ | -------- | ---------- | ----------- | ------------- | ----- | ------- | ------ |
| res://... | ✓      | ✓        | ✓          | ✓           | ✓             | ✓     | ✓       | pass   |
| res://... | ✓      | ✗        | -          | -           | -             | -     | ✗       | block  |

## Action Items

- [ ] <action>
```

## 门禁规则

- 任一场景缺 README 或 5 字段不全 → `block`
- 缺 `index.json`、`result.json` 或 PASS artifact → `block`（没有 artifact oracle，不能接受验证 claim）
- artifact 中标准答案五字段为空 → `block`
- manifest-required scene 未跑 → `block`
- artifact 缺 manifest / FeatureSpec / BDD 要求的 check → `block`
- artifact 过期但 README 完整 → `warn`
- 缺 catalog 条目 → `warn`
- 缺 manifest 条目 → `warn`；release-batch 场景缺 manifest 条目 → `block`
- 全部通过 → `pass`

## 推荐命令

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/analyze-godot-scene-logs.sh --run-dir .ai-temp/scene-tests/runs/<date>/<time> --manifest DocsAI/ValidationManifest.json
jq '.verdict, .summary, .scenes[] | {scene, status, evidence, artifact, freshness}' .ai-temp/scene-tests/runs/<date>/<time>/gate-report.json
```

单场景 claim 仍必须引用同一 run 的 `index.json`、per-scene `result.json` 和 scene artifact；batch claim 优先引用 `gate-report.json`，再下钻到具体 scene evidence。
