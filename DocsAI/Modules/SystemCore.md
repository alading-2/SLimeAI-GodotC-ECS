# SystemCore 模块契约

本文是 AI 修改系统注册、启停、运行态门禁、ProjectState 和系统命令前必须阅读的执行契约。

## 职责边界

SystemCore 管理全局系统生命周期和运行态裁决。

SystemCore 负责：

- `SystemManager` 唯一 autoload 启动。
- 从 `SystemRegistry` 创建系统实例。
- 从 `runtime_snapshot.json` 的 `system.config` / `system.preset` records 投影系统配置。
- 根据 `ProjectStateSnapshot` 和 `SystemRunCondition` 判断系统是否运行。
- 通过 `SystemManager.Execute<TSystem,TRequest,TResult>` 统一执行外部命令。

SystemCore 不负责：

- 具体系统业务逻辑。
- 每个命令单独定义运行条件。
- 把系统元数据散落回代码注册描述符。

## 核心入口

- `Src/ECS/Base/System/Core/SystemManager.cs`
- `Src/ECS/Base/System/Core/SystemRegistry.cs`
- `Src/ECS/Base/System/Core/SystemDescriptor.cs`
- `Src/ECS/Base/System/Core/Lifecycle/`
- `Src/ECS/Base/System/Core/State/`
- `Data/DataOS/Snapshots/runtime_snapshot.json`
- `Src/ECS/Base/Data/RuntimeSnapshot/RuntimeDataRecordQuery.cs`

## 数据 / 事件 / 生命周期

- 代码注册只提供 `SystemId + Factory`。
- 系统元数据、标签、Required、AutoLoad、StartEnabled、依赖和运行条件都写在 DataOS authoring，并由 snapshot projection 读取。
- System preset 只负责选择装载哪些系统，配置本身同样来自 snapshot。
- 最终运行裁决是 `shouldRun = IsEnabled && IsStateAllowed`。
- 系统业务命令必须走 `SystemManager.Execute`，由 SystemCore 门禁统一裁决。
- `ProjectStateService` 使用实例级 C# event 通知 `SystemManager`，不要替换成全局事件。

## 禁止事项

- 禁止恢复旧式系统生命周期枚举和 Profile 装配表。
- 禁止业务代码直接调用系统单例绕过门禁。
- 禁止在 `SystemDescriptor` 里塞运行条件、默认启用和挂载分组。
- 禁止 Required 系统被普通 UI 或调试模块禁用 / 移除。
- 禁止单个系统启动失败拖垮整个系统装载链。

## 修改流程

1. 判断是新增系统、系统配置、运行门禁还是 ProjectState 变化。
2. 新系统先注册 `SystemId + Factory`，再在 `SystemData` 写配置。
3. 若新增运行状态，检查 `SystemRunCondition`、ProjectState 桥接和测试场景。
4. 涉及核心启停时先读 `DocsAI/Workflows/ECS核心修改门禁.md`。
5. 更新测试矩阵和项目索引。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/SystemCore/SystemCoreRuntimeTest.tscn --build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build`

## 人工审查重点

- 是否保持“代码注册轻、snapshot-backed 配置重”的模型。
- `Required / AutoLoad / StartEnabled / Dependencies` 是否符合系统职责。
- 运行态门禁是否会误挡或误放核心业务命令。
- ProjectState 是否仍是三域模型。
- 失败日志是否足够定位具体系统。
