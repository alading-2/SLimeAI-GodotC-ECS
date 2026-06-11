# SDD 精简设计

> 来源：SystemAgent 深度分析
> 日期：2026-06-11
> 优先级：P1

## 问题陈述

后期 SDD 的 `progress.md` 严重膨胀。SDD-0040（Log）是极端案例：266 行 progress，其中大量样板重复。

**从 AI-first 角度分析**：AI 不知道什么信息是"恢复上下文"必需的，什么只是过程记录。AI 倾向于记录一切，但实际恢复只需要关键信息。

## 膨胀根因

### 1. 样板任务完成条目

```
### P005 - 2026-06-09T10:00
- **Context**: 正在执行 SDD-0040 任务 T1.2
- **Conclusion**: 完成任务 T1.2
- **Evidence**: 修改了 xxx 文件
- **Impact**: 无
- **Resume**: 继续处理下一个未完成任务
```

P005-P010 是 6 个格式完全相同的条目，唯一不同的是任务编号。没有任何有区分度的信息。

### 2. Conclusion 和 Resume 重复

验证条目中：
```
- **Conclusion**: 构建通过，lint 通过，xxx 功能已实现，yyy 文件已更新...
- **Resume**: 构建通过，lint 通过，xxx 功能已实现，yyy 文件已更新...
```

Conclusion 和 Resume 包含完全相同的文字。

### 3. 验证证据过于详细

```
- **Evidence**: 运行 dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly，
  输出 0 Error(s) 0 Warning(s)。运行 bash Workspace/SystemAgent/Tools/skill-test/lint.sh
  static all --no-fail --summary-only，输出 R001: PASS, R002: PASS, ...
```

嵌入完整命令行和完整输出摘要。

## AI-First 分析

### 恢复上下文真正需要什么

AI 在新会话中恢复 SDD 上下文，真正需要的信息：

1. **Latest Resume**（最新恢复点）：最后做了什么、下一步做什么
2. **当前阻塞项**：有什么阻止继续
3. **关键决策**（只保留转折点）：重要的设计选择
4. **当前任务状态**：哪些任务完成了、哪些没完成（tasks.md 已有）

**不需要的**：
- 每个任务完成的时间线（tasks.md 的 checkbox 已经记录了）
- 重复的 Conclusion/Resume
- 完整的命令输出（只需要产物路径）

### 类比

progress.md 应该像 **Git log --oneline**，而不是 **Git log --stat**。

## 设计方案

### 方案 1：progress.md 行数上限

新增验证规则 SDD025：

```
SDD025: 如果 progress.md 超过 100 行，发出警告。
超过 150 行，发出错误。
```

### 方案 2：条目格式精简

**任务完成条目**（合并连续完成）：

```
### P003 - 2026-06-09
- **Batch**: T1.1-T1.5 完成（5 个任务）
- **Key Change**: Logger 重构为 5 Sink 架构
- **Resume**: 继续 T1.6
```

**验证条目**（不重复 Conclusion/Resume）：

```
### P004 - 2026-06-09
- **Type**: Validation
- **Result**: PASS（构建通过，lint 通过）
- **Artifact**: .ai-temp/scene-tests/artifacts/log-0609.json
- **Resume**: 继续 T1.7
```

### 方案 3：禁止 Conclusion/Resume 重复

规则：Resume 字段只能包含"下一步做什么"，不能重复 Conclusion 的内容。

### 方案 4：验证证据引用化

规则：Evidence 字段只写产物路径（artifact path、log path），不嵌入命令输出。

### 方案 5：design 不导入快照

规则：SDD 只引用项目级 design，不复制。在 README.md 中写：
```
Design: 引用 SDD/project/projects/PRJ-0002/design/Tool/10.Log/（权威源）
```

## 新的 progress.md 模板

```markdown
# Progress

## Latest Resume

> 当前状态：[一句话描述]
> 下一步：[具体行动]
> 阻塞：[无 / 具体阻塞项]

## Timeline

### P001 - 2026-06-09
- **Event**: SDD 启动
- **Decision**: 采用 5 Sink 架构替代旧 Logger

### P002 - 2026-06-09
- **Batch**: T1.1-T1.5 完成
- **Key Change**: Logger 核心实现
- **Artifact**: .ai-temp/scene-tests/artifacts/log-batch1.json

### P003 - 2026-06-09
- **Type**: Validation
- **Result**: PASS
- **Artifact**: .ai-temp/scene-tests/artifacts/log-validation.json

### P004 - 2026-06-09
- **Event**: 阻塞
- **Blocker**: Godot runner 不可用，无法执行场景测试
- **Impact**: T2.6 无法验证
```

## 实施路径

| 阶段 | 内容 | 工作量 |
|------|------|--------|
| Phase 1 | 新增 SDD025 验证规则（行数上限） | 规则更新 |
| Phase 2 | 更新 progress.md 模板 | 模板更新 |
| Phase 3 | 更新 SDD Format 文档 | 文档更新 |
| Phase 4 | 迁移历史 SDD（可选，只迁最膨胀的） | 按需 |
