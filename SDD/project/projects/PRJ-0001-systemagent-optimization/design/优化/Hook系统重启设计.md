# Hook 使用节点说明

> 来源：用户要求“Hook 先不用，只说明哪些节点可以使用”
> 日期：2026-06-12
> 优先级：P2

## 用户原始问题

用户认为 Hook 不要乱用，现在暂时不需要用到 Hook。Hook 是工具，需要用的时候才用；需要先说明到底为了什么使用、为什么使用。

## 当前裁决

Hook 当前不启用，不作为 gate，不自动 Code Review，不自动改 SDD 状态。

它未来如果恢复，只能做**低噪音提醒器**，不能做检查器。检查职责仍属于 Reviewer、Verifier、Code Review、SDD validate 和具体测试命令。

## 为什么先不用

旧 Hook 的问题不是没有价值，而是触发点不对：

- 高频 PostToolUse 会在探索性读取后制造噪音。
- Hook 无法可靠区分“读文件了解情况”和“已经完成实质修改”。
- 如果 Hook 自己判断 gate 结果，会和 lean review mode、Code Review、Verifier 职责冲突。

在 SDD、TDD/Test、Code Review 边界还没稳定前启用 Hook，只会把不稳定规则自动化。

## 未来可用节点

| 节点 | 触发时机 | 只提醒什么 | 明确不做什么 |
| --- | --- | --- | --- |
| SessionStart | 会话开始，且存在 active/blocked SDD | 当前 SDD、State、明显下一步 | 不自动切换状态，不创建任务 |
| FirstEdit | 本轮第一次编辑文件前 | must-read、事实源边界、是否读 owner skill | 不拦截只读搜索，不判断方案对错 |
| PostValidation | build/test/validate/sync/lint 后 | 是否需要 Code Review、Verifier、progress 更新 | 不判断代码质量，不替代测试 gate |
| PreStop | 会话准备结束且有实质改动 | 是否需要 retrospective、git status、SDD 恢复点 | 不自动提交，不自动复盘，不自动 done |

这些节点的共同原则：**只在 AI 容易忘记流程动作的边界提醒一次，不在每次工具调用后输出。**

## 与 Code Review 的关系

Hook 可以提醒“验证后考虑运行 Code Review”，但不能自动审查代码。Code Review 本身是 skill，可由用户语义触发，也可由 AI 在实现完成后手动调用。

## 与 SDD 的关系

Hook 可以提醒“有 active SDD，先看 State / Next / Blocker”或“收尾前更新 progress 状态面板”，但不能替代 SDD CLI，也不能把 `validate` 结果解释成业务完成。

## 后续实现条件

只有满足以下条件后才考虑实现 Hook：

1. SDD 精简规则和模板已经稳定。
2. Code Review skill 的职责边界已经稳定。
3. TDD/Test 的 Data 试点已经证明可执行验证链路。
4. Hook 输出能保持静默优先：无明确节点不输出。
5. 用户确认需要自动提醒，而不是只靠文档和 skill 触发。
