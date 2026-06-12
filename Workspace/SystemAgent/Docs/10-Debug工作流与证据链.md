# Debug 工作流与证据链

> 状态：current
> 作用：说明 SystemAgent 的 Debug workflow 如何与 Log / Validation / Test / Gate / SDD 协作，形成“不要让 AI 猜”的证据链。

## 一句话定位

SystemAgent 的 DebugFix 解决的是 **怎么调、怎么判、怎么复盘**；Log / Validation / Test 提供的是 **拿什么调、拿什么判、拿什么复盘**。

## Control Plane vs Evidence Plane

```text
SystemAgent Control Plane
  - DebugFix route
  - Debugger actor
  - Review Gates
  - Verifier / Retrospective
  - SDD 恢复点与完成门

Evidence Plane
  - Log / OperationTrace / ValidationSession
  - artifact
  - logctl analyze/query
  - Test scene / runtime test / BDD 映射
```

### 这两个平面分别负责什么

| 平面 | 负责内容 |
| --- | --- |
| Control Plane | 工作流、角色、门禁、裁决、复盘 |
| Evidence Plane | 运行事实、验证结果、分析入口、失败定位 |

这就是为什么：
- **Log 不是 SystemAgent 本体**
- 但 **没有 Log / Validation / Test，SystemAgent Debug 会退化成猜**

## DebugFix 的真正输入

`Workspace/SystemAgent/Routes/DebugFix.md` 已经说明，DebugFix 不是“看一眼报错然后猜修”，而是：

1. Route：确认失败类型与最小读取范围
2. Reproduce：先获得失败证据
3. Diagnose：形成假设树，定位根因
4. Fix：做最小修复
5. Verify：运行回归验证
6. Close：同步证据、风险和恢复点

也就是说，DebugFix 的关键不是“修复动作”，而是 **证据链**。

## 一条标准的 Debug 证据链

```text
用户报告异常 / 测试失败 / 场景异常
  -> DebugFix 进入 Reproduce
  -> 运行场景 / 测试 / 构建
  -> Log / ValidationSession 产生日志与 artifact
  -> logctl analyze 产出 summary / ai-context / flows / failures / missing-fields
  -> Debugger 形成问题陈述 + 假设树 + 根因
  -> Reviewer / Verifier 读取 artifact oracle 做判定
  -> Retrospective 记录缺口与恢复点
```

## Reviewer / Verifier 为什么这么严格

`Workspace/SystemAgent/Rules/ReviewGates.md` 的 evidence contract 已经明确：

- `无 error`
- `exit code 0`
- stdout PASS marker
- clean log

**都只能算诊断信号，不能单独作为正确性证明。**

### 为什么？

因为这些信号都不能回答：
- 预期观察是什么？
- 关键 check 是否真的覆盖了行为？
- 没有失败，究竟是通过了，还是只是没看到失败？
- 当前 run 是 `passed` 还是 `no-failure-observed`？

## 四类关键证据

### 1. Reproduce evidence

必须先证明问题真的存在：
- 错误输出
- scene run 失败
- artifact fail
- structured failure
- 或明确说明“当前无法复现”

### 2. Validation artifact

这是最强证据。应该至少包含：
- `expectedInputs`
- `expectedObservations`
- `passCriteria`
- `failCriteria`
- `artifactPath`
- `checks[]`

### 3. Structured observation

如果没有 artifact，至少应该有：
- `OperationTrace` flow
- `summary.md`
- `ai-context.md`
- `flows/flows.jsonl`
- `failures/`

但这类证据通常只能支撑：
- `failed`
- 或 `no-failure-observed`

不能自动升级成 `passed`。

### 4. SDD 状态证据

对于中大型问题，还需要：
- `tasks.md`
- `progress.md` 的 State / Next / Blocker
- `bdd.md` 或设计旁行为验收引用
- validation artifact / logctl analysis 引用

否则下次会话不知道：
- 已经验证到哪一步
- 哪个行为场景已覆盖
- 为什么当前状态算 blocked / concerns / resolved

## Log / Validation / Test 在 Debug 中分别扮演什么角色

| 组件 | 在 Debug 中的角色 |
| --- | --- |
| Log | 记录运行事实，产出 flow / summary / structured failure |
| ValidationSession | 把测试/场景断言钉成 artifact oracle |
| Test | 负责提供可复现、可重复的触发入口 |
| logctl analyze | 把 raw 证据提炼成 AI 默认可读入口 |
| Review Gates | 规定什么证据才允许判定通过 |

## 为什么“同一流程的信息要记录在一起”

如果一个流程被拆成十几条散乱 `_log.Info`：
- AI 需要自己猜哪几条属于同一次动作
- AI 不知道哪一步最关键
- AI 难以判断“成功了但部分降级”还是“完全成功”
- Debugger 只能用文本串联，而不是直接读取一个 flow conclusion

所以 AI-first 的目标不是“减少日志条数”，而是：

> **让一次流程有一个稳定结论，让失败、跳过、成功、缺字段都有固定语义位置。**

## 调试时的默认阅读顺序

### 场景 / 运行问题

1. 先看 `index.json` / `result.json` / scene artifact（如果有）
2. 再看 `logctl analyze` 产出的 `summary.md`
3. 再看 `ai-context.md`
4. 再看 `flows/flows.jsonl` / `failures/` / `missing-fields/`
5. 最后才 raw JSONL 下钻

### 代码 / 流程问题

1. 先看相关 owner README `## Log`
2. 再看 flow contract / summary
3. 失败时再下钻到 raw entry 和源码调用点

## 当前 Debug 文档缺口已经补在哪里

本文件补的是之前缺失的一层：
- Log 侧文档会说明 **Observation substrate 是什么**
- 本文件说明 **Debug workflow 怎样消费这个 substrate**

对应 Logger 侧入口：
- `DocsAI/ECS/Tools/Logger/Log与AI-first Observation.md`

## 禁止事项

- 不把“没有报错”写成“验证通过”。
- 不把 clean analyzer summary 写成“功能正确”。
- 不跳过 Reproduce 直接猜根因。
- 不用 stdout PASS marker 替代 artifact oracle。
- 不把 Log 当作只给测试用的打印工具。
