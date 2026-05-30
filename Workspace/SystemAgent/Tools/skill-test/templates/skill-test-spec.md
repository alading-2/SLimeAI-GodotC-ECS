# Skill Spec: /[skill-name]

> **function_category**: [gate | review | authoring | readiness | pipeline | analysis | sprint | utility]
> **priority**: [critical | high | medium | low]
> **spec written**: [YYYY-MM-DD]
> **review_mode**: [full | lean | solo] （触发哪个模式下运行此 spec）

## Skill Summary

[一段说明：该 skill 做什么、接受什么输入、产出什么输出。]

---

## Static Assertions

运行 `skill-test static [name]` 时检查，此处记录期望结果：

- [ ] frontmatter 有 `name` + `description` 字段（R001）
- [ ] SKILL.md 内路径引用均存在（R002）
- [ ] 只引用 `.ai-config/` 源路径，不引用副本（R003）
- [ ] 有 ≥2 个 `## 步骤 / Phase / 阶段` 标题
- [ ] 末尾有"下一步"或"完成条件"段落

---

## Review Mode Checks

（参考 `Workspace/SystemAgent/Config/review-mode.txt`）

- **full mode**：触发的 gate ID 列表：[RV-PLAN-FEASIBILITY, RV-RETROSPECTIVE, ...]
- **lean mode**：只触发 phase_gates 中的 gate
- **solo mode**：不触发任何 gate；skill 直接运行

---

## Test Cases

### Case 1: Happy Path — [brief name]

**Fixture**（假设项目状态）：

- [条件 1，例如：SDD 处于 active，tasks.md 有未完成 task]
- [条件 2]

**Expected behavior**：

1. [步骤 1]
2. [步骤 2]
3. [末尾输出 APPROVE]

**Assertions**：

- [ ] 末尾出现 `APPROVE`（可 grep）
- [ ] 引用了 ReviewGates 中的 gate ID
- [ ] 没有修改实现文件

**Case Verdict**: PASS / FAIL / PARTIAL

---

### Case 2: Failure / REJECT path — [brief name]

**Fixture**：

- [缺失或无效条件，例如：tasks.md 未读、artifactPath 为空]

**Expected behavior**：

1. Skill 检测到问题
2. 输出 REJECT 或 CONCERNS
3. 未继续执行后续步骤

**Assertions**：

- [ ] 输出包含 `REJECT` 或 `CONCERNS`
- [ ] 未写入任何文件
- [ ] 给出明确的阻塞原因

**Case Verdict**: PASS / FAIL / PARTIAL

---

### Case 3: Mode Variant — lean vs full

**Fixture**：

- review-mode.txt = lean

**Expected behavior**：

1. 只触发 phase_gates 列表中的 gate，不触发全量 gate
2. 输出说明"lean 模式，跳过 [gate ID]"

**Assertions**：

- [ ] 只看到 phase_gates 中的 gate 被引用
- [ ] 输出与 full 模式下不同

**Case Verdict**: PASS / FAIL / PARTIAL

---

### Case 4: Edge Case — [brief name]

**Fixture**：

- [边界条件，例如：tasks.md 为空、git status 有未追踪文件]

**Expected behavior**：

1. Skill 不崩溃
2. 输出 CONCERNS 并说明边界情况

**Assertions**：

- [ ] 无 uncaught error
- [ ] 输出包含可理解的说明

**Case Verdict**: PASS / FAIL / PARTIAL

---

### Case 5: Context Assembly — required context 齐备

**Fixture**：

- contextSources 中所有文件都存在且可读

**Expected behavior**：

1. Skill 开始时汇报"已读 N 项 must_read"
2. 没有因为缺少上下文导致的错误

**Assertions**：

- [ ] 汇报中提到 must_read 列表
- [ ] 未出现"文件不存在"错误

**Case Verdict**: PASS / FAIL / PARTIAL

---

## Protocol Compliance

- [ ] 写文件前明确标注"将写入 [filepath]"（May-I-write 语义）
- [ ] 末尾有推荐的下一步行动
- [ ] 不自行越过用户审批继续执行
- [ ] verdict 符合 `VerdictVocabulary.md`（APPROVE / CONCERNS / REJECT）

---

## Coverage Notes

[已知未覆盖的边界条件；需要真实运行才能验证的行为；已知豁免场景。]
