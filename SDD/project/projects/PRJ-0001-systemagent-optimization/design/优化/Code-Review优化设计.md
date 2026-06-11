# Code Review 优化设计

> 来源：SystemAgent 深度分析
> 日期：2026-06-11
> 优先级：P1

## 当前状态

systemagent-code-review skill（121 行）定义了完整的三阶段审查流程：

1. **Phase 1**：从 SDD 设计文档提取需求清单
2. **Phase 2**：逐项审查（DONE/PARTIAL/MISSING/WRONG + 根因分析）
3. **Phase 3**：结构化报告

**做得好的部分**：
- 根因分类（需求误解/接受标准低/概念边界模糊/技术限制/遗漏）
- 明确禁止行为（不写代码、不接受"应该能通过"、不把编译通过当完成）
- 与 test-designer 和 verifier 的职责边界清晰

## 问题分析

### 核心问题：Code Review 与 TDD 脱节

Code Review 的 Phase 2 做的是"需求 vs 实现"对比，但它不读取测试结果。这意味着：

- 不知道哪些功能有测试覆盖
- 不知道测试是否通过
- 不知道 ValidationSession 产出了什么 artifact
- 只能做代码级别的静态审查，不能做运行时验证审查

### 次要问题

1. **没有自动化执行**：全靠 AI 手动触发 skill，没有 hook 自动在 Execute 阶段后触发
2. **没有与 Retrospective 联动**：Retrospective 做效率分析，Code Review 做需求分析，但两者不共享信息
3. **根因分析只是分类**：有分类（需求误解等）但没有量化（哪个根因最常见、需要系统性改进）

## 设计方案

### 方案 1：Code Review + TDD Artifact 联动（P1）

在 Phase 2 中增加"测试证据"维度：

```
Phase 2 逐项审查：
  对每个需求项：
    1. 定位实现代码 → file:line
    2. 定位测试代码 → file:line ← 新增
    3. 读取测试 artifact → ValidationSession JSON ← 新增
    4. 判定状态：
       - DONE + 有测试 + 测试通过 → DONE
       - DONE + 无测试 → PARTIAL（缺少测试覆盖）
       - DONE + 测试失败 → WRONG（实现可能有问题）
       - MISSING → MISSING
```

新增判定维度：

| 实现状态 | 测试状态 | 最终判定 |
|----------|----------|----------|
| DONE | 有测试，通过 | DONE |
| DONE | 无测试 | PARTIAL（缺测试） |
| DONE | 有测试，失败 | WRONG |
| PARTIAL | 任意 | PARTIAL |
| MISSING | 任意 | MISSING |

### 方案 2：根因统计（P2）

在报告中增加根因分布统计：

```
## Root Cause Distribution

| 根因 | 数量 | 占比 | 趋势 |
|------|------|------|------|
| 需求理解偏差 | 2 | 40% | ↑ 与上次 review 相比 |
| 接受标准太低 | 1 | 20% | → |
| 概念边界模糊 | 1 | 20% | ↓ |
| 技术限制 | 0 | 0% | — |
| 遗漏 | 1 | 20% | → |
```

**价值**：如果"需求理解偏差"持续是最高根因，说明 needs-read 入口链不够清晰，需要系统性改进。

### 方案 3：自动触发（P2，依赖 Hook 重启）

在 NewFeature 工作流的 Validate 阶段自动触发 Code Review：

```
Execute (Implementer) 完成
  ↓
自动触发 Code Review (Phase 2)
  ↓
如果全部 DONE → 继续到 Verifier
如果有 PARTIAL/MISSING/WRONG → 返回 Implementer
```

### 方案 4：与 Retrospective 共享数据（P3）

Code Review 的根因数据和 Retrospective 的效率数据应该合并分析：

```
Code Review 输出:
  - 需求完成率: DONE/Total
  - 根因分布: {误解: 2, 遗漏: 1, ...}
  
Retrospective 输出:
  - 效率指标: {验证循环: 3次, 文件放大: 2x, ...}
  - sessionEvidence: {tool_failures: [...]}
  
合并分析:
  - "需求误解"高 + "验证循环"多 → 需求入口链需要改进
  - "遗漏"高 + "文件放大"多 → must-read 列表不完整
```

## 实施路径

| 阶段 | 内容 | 工作量 |
|------|------|--------|
| Phase 1 | Code Review + TDD Artifact 联动 | Skill 更新 |
| Phase 2 | 根因统计 | Skill 更新 |
| Phase 3 | 自动触发（依赖 Hook 重启） | Hook + Route 更新 |
| Phase 4 | 与 Retrospective 数据共享 | 跨 Skill 设计 |
