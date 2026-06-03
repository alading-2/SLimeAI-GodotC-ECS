# System Core 概念

> status: current
> sourcePaths: Src/ECS/Runtime/System/
> relatedDocs: DocsAI/ECS/Runtime/System/Usage.md, DocsAI/ECS/Runtime/System/SystemManifest.md
> lastReviewed: 2026-06-03

## 1. 一句话定位

System Core 是 ECS 系统层的注册、装载、生命周期、项目状态门禁和命令执行基础设施，采用数据驱动模型：代码只注册 `SystemId + Factory`，其余系统配置来自 DataOS runtime snapshot。

## 2. 核心概念

### 四层分层

```
SystemManager（系统管理层）
  ├─ ProjectState（项目级状态：游戏暂停、菜单、战斗）
  ├─ Entity Status Effect（实体状态效果：无敌、眩晕、减速）
  └─ AI Behavior Tree（行为树：巡逻、追击、攻击）
```

### 数据驱动系统注册

系统通过 DataOS snapshot 配置，运行时由 `SystemRegistry` 和 `SystemManager` 统一管理。每个系统有：
- **SystemId**：唯一标识
- **Factory**：系统实例工厂
- **SystemData**：来自 `system.config` 的 MountGroup、Tags、Required、AutoLoad、StartEnabled、Priority、Dependencies 和三域运行条件
- **运行态**：`SystemManager` 内部的 loaded / enabled / stateAllowed / running

### 运行条件

系统运行条件来自 `system.config` 的三域状态：

- `AllowedFlowStates`
- `RequiredOverlays`
- `BlockedOverlays`
- `AllowedSimulationStates`

`SystemManager` 的裁决公式固定为：

```text
shouldRun = IsEnabled && IsStateAllowed
```

外部命令必须通过 `SystemManager.Execute<TSystem, TRequest, TResult>`，并复用同一套运行态门禁。

### AI-first Contract Layer

SDD-0029 后，System Core 增加只读合同层：

- `SystemManifest.md`：AI 路由入口，列出当前 14 个 system 的 owner、source、config、run condition、command handler、tests 和风险。
- `SystemPreflight`：检查 config / registry / preset / dependencies / cycle / descriptor-only allow-list。
- `SystemDiagnosticsSnapshot`：合并 config、registry、runtime、ProjectState 和 lifecycle trace，供 TestSystem、Godot validation 和 AI debug 共用。
- `SystemBlockedReasonCode`：稳定机器 reason code，避免只依赖中文日志判断阻断原因。
- `SystemLifecycleTrace`：轻量 ring buffer，记录 bootstrap、系统启停、状态门禁和 command events。

## 3. 职责边界

| SystemCore 做 | SystemCore 不做 |
| ---- | ---- |
| 系统注册与发现 | 具体业务逻辑（归各 System） |
| 生命周期管理 | 系统间直接调用 |
| 运行条件检查与 command gate | 手动 new System() |
| preflight / diagnostics / trace | 写回 DataOS 配置或生成第二事实源 |

## 4. 依赖关系

- **SystemManager**：系统管理层
- **SystemRegistry**：系统注册表
- **SystemConfigService / SystemPresetService**：DataOS runtime snapshot 投影
- **ProjectStateService**：三域项目状态源
- **DataOS snapshot**：系统配置事实源
