# QFramework 源码证据

## 搜索范围

本轮读取和核对：

- 本地源码：
  - `/home/slime/资料/Obsidian/Obsidian/游戏开发/资源/Resources/Engine/Engine/QFramework/Program/Assets/QFramework/QFramework.cs`
  - `/home/slime/资料/Obsidian/Obsidian/游戏开发/资源/Resources/Engine/Engine/QFramework/Program/Assets/QFramework/Framework/Scripts/QFramework.cs`
  - `/home/slime/资料/Obsidian/Obsidian/游戏开发/资源/Resources/Engine/Engine/QFramework/Program/Assets/QFramework/Toolkits`
- 本地分析：
  - `/home/slime/资料/Obsidian/Obsidian/游戏开发/资源/Resources/Engine/Engine/QFramework/Docs/01-架构核心模式分析/`
  - `/home/slime/资料/Obsidian/Obsidian/游戏开发/资源/Resources/Engine/Engine/QFramework/Docs/03-SlimeAI采纳建议/`
  - `/home/slime/资料/Obsidian/Obsidian/游戏开发/资源/Resources/Engine/Engine/QFramework/Docs/04-官方使用指南/`
- Context7：
  - `/liangxiegame/qframework`
  - `/websites/qf_readthedocs_io_zh-cn`
- Web：
  - QFramework GitHub / docs 搜索
  - Godot Nodes and Scenes / C# exports / Resources 官方文档核对

未覆盖：

- 未运行 QFramework Unity 示例或 Godot 示例。
- 未全量阅读 Toolkits 所有源码。
- 未评估 QFramework 大型商业项目长期维护数据。

## Evidence

### 核心规模

本地源码行数：

```text
Program/Assets/QFramework/Framework/Scripts/QFramework.cs: 956 lines
Program/Assets/QFramework/QFramework.cs: 1449 lines
```

这支持用户判断：QFramework 核心底层确实不多。

但这个规模只覆盖应用层架构，不覆盖 SlimeAI 当前已有的 DataOS、System diagnostics、GodotBridge、对象池、碰撞、目标查询、日志、验证和 SDD 工作流。

### Architecture

QFramework 的 `Architecture<T>` 是每个架构子类一个静态实例，内部持有一个 `IOCContainer`。`Init()` 先注册 Model/System/Utility，然后依次初始化 Model，再初始化 System。`Deinit()` 清理 System、Model、容器并重置静态实例。

本地分析总结它的定位不是完整 DI，而是“应用配置入口”：`Init()` 像架构图，一眼能看到有哪些 Model / System / Utility。

对 SlimeAI 的直接影响：

- `Init()` 清单可读性值得学。
- static singleton 和 Type-key IOC 不能直接作为 SlimeAI runtime 根。
- SlimeAI 已有 `SystemRegistry + SystemManager + DataOS system.config + SystemManifest`，比 QFramework Architecture 多了运行态门禁、依赖、diagnostics 和阻断原因。

### Command / Query

QFramework 的 `Command` 是独立对象，执行时由 Architecture 注入架构引用。它能访问 Model/System/Utility，能发 Event，也能发其他 Command。`Query` 有返回值，表达只读查询。

本地分析指出 QFramework 的 Command 成本：

- 每次 `SendCommand(new XxxCommand())` 都分配对象。
- Command 既是消息又是 handler，没有 handler 分离。
- 默认同步执行，没有内置 async、验证、history 或 tracing。
- Command 可以调 Command，容易形成深层调用链。

Context7 官方库示例也显示典型路径是 Controller 调 `SendCommand`，Command 修改 Model 的 `BindableProperty<T>`。

对 SlimeAI 的直接影响：

- Command/Query/Event 的语义要采纳。
- `AbstractCommand` 对象体系不应采纳。
- SlimeAI 已有更接近目标的形态：`DamageProcessRequest` / `DamageProcessResult` 是 typed request/result，`DamageService` 是 handler/pipeline。

### Event

QFramework 的事件系统是 `TypeEventSystem`，内部是 `Dictionary<Type, IEasyEvent>`。它有 Architecture 级事件和 `TypeEventSystem.Global` 全局事件。`EasyEvent` 的关键价值是返回 `IUnRegister`，便于生命周期解绑。

本地分析列出局限：

- 无 per-object / per-feature scope。
- 全局事件没有作用域隔离。
- 无内置事件 trace。
- 无过滤，优先级能力有限或没有。

对 SlimeAI 的直接影响：

- `IUnRegister` / 生命周期解绑思路可采纳。
- `TypeEventSystem.Global` 不可替代 SlimeAI scoped EventBus。
- SlimeAI 当前 `EventBus` 已有 typed payload、优先级、once、重入保护；缺的是更系统的生命周期 subscription token 和 scope 文档。

### BindableProperty

QFramework 的 `BindableProperty<T>` 是强类型值 + 变化通知：

- `Value` 变化时触发订阅。
- `RegisterWithInitValue` 注册时立即给当前值。
- `SetValueWithoutEvent` 支持静默设置。
- `Comparer` 控制去重。
- 只读接口和可写接口分离。

这不是完整 Data 系统。它没有 DataOS descriptor、snapshot、generated `DataKey<T>`、range/allowed values、modifier、computed、authority、projection 或 validation artifact。

对 SlimeAI 的直接影响：

- `RegisterWithInitValue` 语义必须采纳到 Data / UI / Debug binding。
- 只读可观察 view 值得采纳。
- 不用 `BindableProperty<T>` 替代 Data。

### ICanXxx 权限接口

QFramework 最值得认真采纳的是 `ICanXxx` 编译期能力接口。角色实现不同接口，从而只获得对应扩展方法。例如 Model 没有 `ICanGetSystem`，在 Model 内调用 `GetSystem<T>()` 会编译失败。

这比“文档约定不要调用”更强。

对 SlimeAI 的直接影响：

- 后续 SlimeAI Command / Query / Feature handler 可以引入能力接口限制上下文能力。
- 不需要复制 QFramework 所有接口名，但应复制“能力接口 + 扩展方法 + 编译期限制”的方法。

### 什么是共享数据

QFramework 官方指南中，把需要共享的数据归为三类：

- 需要持久化的数据。
- 需要跨多个界面或 MonoBehaviour 使用的数据。
- 配置表数据。

非共享数据放在脚本自身；示例说明敌人当前生命通常由敌人脚本管理，只有需要持久化或跨处访问时才进入共享数据路径。

这和 SlimeAI 当前 Data 方向高度一致：

```text
QFramework Model:
  持久化 / 跨界面 / 配置表 -> Model

SlimeAI Data:
  共享 / 表格驱动 / 约束 / modifier / computed / UI/debug/test/AI 观察 -> Data
  其他行为状态 -> Component / Profile / System
```

差异是：SlimeAI 还要处理 per-object Data、DataOS authoring、对象池重置、DataBinding projection 和 AI 验证。

## Inference

- QFramework 的成熟感来自少规则和编译期权限接口，不来自复杂 runtime。
- QFramework 能帮 SlimeAI 减少“字段到底放哪、写操作到底走哪”的解释成本。
- QFramework 不足以替代 SlimeAI 已有底层；如果硬替代，会丢掉 SlimeAI 最关键的 DataOS / System diagnostics / scoped lifecycle 价值。
- SlimeAI 最适合采纳的是 QFramework 的方法论，不是源码依赖。

## Unknown

- QFramework Toolkits 中 ActionKit/UIKit/CodeGenKit 对 SlimeAI 的价值需要单独研究，不应混入本轮核心架构裁决。
- 如果 SlimeAI 未来完全放弃 DataOS 和 SystemManager，QFramework 的直接采用价值会提高；但这与当前项目事实源冲突。
- 当前没有 profiler 证明 Command object 分配在 SlimeAI 一定不可接受；但 SlimeAI 已经有零分配 request/result 方向，没必要主动退回引用对象。

