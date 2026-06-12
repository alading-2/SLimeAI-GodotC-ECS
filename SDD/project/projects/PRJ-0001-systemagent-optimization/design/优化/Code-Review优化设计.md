# Code Review 优化设计

> 来源：用户指出 Code Review 方向偏向 TDD 后的 SystemAgent 深度分析
> 日期：2026-06-12
> 优先级：P1

## 用户原始问题

用户指出当前 Code Review 方向可能错了：Code Review 不应只是 TDD 或测试覆盖检查，最重要的是看代码是否按要求实现功能；测试结果可以参考，但审查核心是代码本身、功能实现和必要代码质量。

## 真实问题

用户判断成立。当前 `systemagent-code-review` 的主流程是从 SDD 提取需求清单，再逐条判断 `DONE/PARTIAL/MISSING/WRONG`。这个方向保留了“功能是否实现”的优点，但缺两个边界：

1. **它容易被误解成 TDD gate**：如果把“有测试且通过”当作 DONE 的必要条件，Code Review 就会退化为 `RV-TEST-COVERAGE` 的重复。
2. **它没有明确代码质量审查维度**：只看需求条目是否出现，可能漏掉明显低质量实现、架构红线、热路径性能问题和错误处理问题。

Code Review 的第一责任不是证明测试通过，而是回答：**代码是否按设计实现了用户要的功能，并且没有用明显不合格的方式实现。**

## 职责边界

| 能力 | 主要问题 | 不负责什么 |
| --- | --- | --- |
| TDD / RV-TEST-COVERAGE | 测试是否先 RED 后 GREEN、标准答案和 artifact 是否可复验 | 不评价整体代码设计好坏 |
| Verifier | 完成声明是否有可复查证据 | 不逐行审查实现质量 |
| Reviewer / Review Gates | 流程、边界、集成、文档、配置是否合规 | 不替代功能实现审查 |
| Code Review | 功能是否实现、代码是否明显偏离设计或质量底线 | 不把缺测试直接等同于功能未实现 |

测试结果、ValidationSession artifact、构建结果都是 Code Review 的 evidenceRef。它们能帮助发现风险，但不能替代读代码。

## 审查维度

规则必须够用，不做复杂评分。Code Review 只检查 6 类问题：

1. **功能/设计实现度（最高优先级）**：用户需求、SDD 设计、BDD 场景、完成定义是否真的在代码中实现；实现方向是否和设计一致。
2. **ECS 架构红线**：是否违反 Entity 生命周期、DataKey、EventBus、ResourceManagement、TimerManager、TargetSelector、DamageService、对象池等项目硬约束。
3. **修改范围和依赖边界**：是否越界改无关模块、引入不必要依赖、绕过 owner skill 或跨 git 边界混改。
4. **可维护性底线**：命名是否误导、重复是否明显、函数是否过长或嵌套过深；只抓会影响后续维护的明显问题，不做风格洁癖。
5. **热路径性能和日志噪音**：`_Process`、高频循环、调度器、查询器中是否新增 `new`、LINQ、过量日志或不必要分配。
6. **失败语义和错误处理**：失败是否有明确返回、异常、日志或 artifact；是否吞错、伪造成功、用默认值掩盖错误。

安全问题不作为默认主轴，只在本轮涉及外部输入、脚本执行、路径处理、网络、数据库、密钥或用户数据时进入审查。

## 判定方式

继续使用现有状态词，不新增复杂等级：

| 状态 | 含义 |
| --- | --- |
| `DONE` | 功能实现完整，代码质量和架构边界没有阻塞问题。 |
| `PARTIAL` | 有实现但缺行为、边界或质量问题；也可用于“功能实现了但证据不足/测试缺口明显”。 |
| `MISSING` | 需求或关键行为没有实现。 |
| `WRONG` | 实现方向错误、违反明确设计或触碰架构红线。 |

测试缺失通常是 evidence/test gap，默认不直接把功能判为 `MISSING`。只有当缺少可复验标准导致无法判断功能是否实现，或测试失败暴露实现错误时，才影响功能状态。

## Skill 触发

Code Review 应作为 skill 存在，可手动也可自动触发：

- 用户说“Code Review”“代码审查”“审查实现”“检查代码质量”“看实现是否符合需求”时触发。
- SDD task 勾选完成前、实现完成后最终汇报前可以手动触发。
- 未来 Hook 重启后，只能在验证或准备收尾节点提醒运行 Code Review；Hook 不直接做审查，也不强制高频触发。

## 实施路径

| 阶段 | 内容 | 说明 |
| --- | --- | --- |
| Phase 1 | 更新 `systemagent-code-review` skill | 明确“功能实现度优先 + 6 个质量维度 + 测试证据辅助”。 |
| Phase 2 | 更新 Review Gate / Docs 边界 | 避免把 Code Review 写成 TDD gate 的重复。 |
| Phase 3 | Data 试点后再接入 artifact | 等 Runtime/Data 测试试点稳定后，Code Review 再引用 Data 测试 artifact。 |
| Phase 4 | Hook 仅做提醒 | Hook 系统未重启前不做自动触发实现。 |
