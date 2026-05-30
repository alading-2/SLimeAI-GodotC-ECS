# SystemAgent Skill Test

对 `.ai-config/skills/` 的 SKILL.md 提供分层质量检查，参考 CCGS Skill Testing Framework 设计。

## 三层测试体系

| 层 | 成熟度 | 命令或执行方式 | 检查内容 | 工具 |
| -- | ------ | -------------- | -------- | ---- |
| **Static Lint** | implemented automation | `lint.sh static [all\|changed]` | 结构规则 R001-R006 | Python 规则脚本 |
| **Behavioral Spec** | pilot / manual | AI 读取试点 spec 文件评估 | 5 个测试用例（happy/failure/mode/edge/context） | 参考 `templates/skill-test-spec.md` |
| **Category Rubric** | manual review | AI 按分类 rubric 检查 | 按 function_category 的专属 2-3 指标 | 参考下方 Rubric 表 |

Catalog 在 `Workspace/SystemAgent/Catalog/systemagent-catalog.yaml` 可记录每层的 `last_*` 人工快照；Static Lint 的权威证据是命令输出和 `.ai-temp/skill-test/` 报告，Behavioral Spec / Category Rubric 只有进入 pilot 或人工评审时才需要填写。

---

## Static Lint 用法

```bash
# 扫描全部 skill
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all

# 仅扫描 git 修改过的 skill
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static changed

# advisory 模式（lint 失败退出码仍为 0）
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail

# 仅输出 1-3 行 summary
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static changed --no-fail --summary-only
```

## 退出码

| 退出码 | 含义 |
| ------ | ---- |
| 0 | 全部通过，或 `--no-fail` 模式 |
| 1 | 存在 critical failure（R001 / R002 / R003） |

## 规则索引（Static Lint）

| 规则 ID | 名称 | 严重度 | 说明 |
| ------- | ---- | ------ | ---- |
| R001 | frontmatter-required-fields | critical | SKILL.md 必须含 `name` + `description` frontmatter |
| R002 | references-exist | critical | SKILL.md 内路径引用必须真实存在 |
| R003 | source-of-truth-alignment | critical | SKILL.md 只能引用 `.ai-config/` 源，不能引用副本路径 |
| R004 | sync-targets-up-to-date | advisory | `.ai-config/` 源与三 IDE 副本 MD5 必须一致 |
| R005 | catalog-coverage | advisory | `systemagent-catalog.yaml` 登记数量必须等于实际 skill 数 |
| R006 | no-direct-copy-edits | advisory | 副本修改时间不得晚于 `.last-sync` 时间戳 |

---

## Behavioral Spec 测试（AI 执行）

当前状态：pilot / manual。没有自动 runner，也没有要求所有 skill 都提供 spec 文件。

首批 pilot scope 限定为 5 个高风险或高杠杆 skill：`ai-feature-development`、`sdd-workflow`、`sdd-management`、`ai-config-management`、`systemagent-retrospective`。本治理任务只记录 pilot scope，不创建 behavioral spec 文件；spec 文件创建和执行规则需要后续 SDD。

1. 在 `Workspace/SystemAgent/Catalog/systemagent-catalog.yaml` 的 skill 条目中找到 `spec:` 字段
2. 读取对应的 spec 文件（参考模板 `templates/skill-test-spec.md`）
3. 逐项评估：Static Assertions → Review Mode Checks → 5 个 Test Cases → Protocol Compliance
4. 输出 PASS / FAIL / PARTIAL，更新 catalog 中的 `last_spec / last_spec_result`

新建 spec 文件：复制 `templates/skill-test-spec.md` 到对应目录，填写内容，更新 catalog `spec` 字段。

---

## Category Rubric 检查（AI 执行）

当前状态：manual review。`function_category` 是高风险 SystemAgent-owned skill 的 rubric 分类，不是 all-skill lint gate；缺少 `function_category` 不会导致 Static Lint 失败，除非后续 SDD 明确升级为自动化规则。

按 `function_category` 字段选择对应 rubric：

| function_category | Rubric 指标 | 核心检查 |
| --- | --- | --- |
| `gate` | GR1-GR3 | review-mode read、gate ID 引用、no auto-advance |
| `review` | RV-R1..R3 | read-only、structured findings table、verdict compliance |
| `authoring` | AU-A1..A3 | may-I-write、skeleton-first、single-source |
| `readiness` | RD-RD1..RD3 | multi-dimensional、3 verdict levels、evidence citation |
| `pipeline` | PL-P1..P3 | output schema、reads-before-writes、no cross-domain |
| `analysis` | AN-AN1..AN3 | evidence-first、structured findings、no auto-write |
| `sprint` | SP-SP1..SP3 | structured output、no auto-commit、verdict consistency |
| `utility` | 通过 static lint 即可 | R001-R006 全通过 |

---

## CI 接入

```bash
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all
# 退出码 1 = critical failure，可作为 CI gate
```

## 报告

每次 Static Lint 运行输出 JSON 到 `.ai-temp/skill-test/static-{timestamp}.json`。
