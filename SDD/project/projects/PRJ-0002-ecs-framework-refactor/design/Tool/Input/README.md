# Input 工具设计包

> 更新：2026-06-02
> 状态：current design package
> 入口：`README.md`
> 裁决：Input 需要优化，但第一阶段不是重写输入系统，而是把 Godot action、业务语义、输入上下文、调用点和验证面显式化，让 AI 不再从散落字符串和静态包装方法里猜输入协议。

## 0. 本设计包回答什么

这份设计包回答当前 Input 相关的核心问题：

- `Src/ECS/Tools/Input/InputManager.cs` 是否够用。
- `project.godot` 里的 action 是否应该继续作为唯一事实源。
- 当前输入调用点是否过于分散。
- AI-first ECS 下，Input 应该给 AI 暴露什么契约。
- 后续如果优化，哪些是文档/审计先行，哪些才是代码改造。

结论先写清楚：**Input 现在能给人用，但不够 AI-first。它缺少显式 action manifest、输入上下文、业务语义层和验证清单。**

2026-06-01 补充裁决：手柄不只有 Xbox 一类。SlimeAI 当前保留 `BtnA/B/X/Y` 作为兼容 action 名，但文档和后续 UI 必须按“业务 action -> Godot action -> 中性物理布局 -> ControllerGlyphProfile”分层理解。

2026-06-02 补充裁决：DocsAI owner 文档不强制拆成 `Concept.md / Usage.md / InputMap.md`。Input 当前主入口改为 `DocsAI/ECS/Tools/Input/README.md`；`Concept.md`、`Usage.md`、`InputMap.md` 只是辅助页，内容少时可合并，其他 owner 不需要照这个三件套建模板。SDD-0026 的实际范围是 facade hardening 和调用点收口，不是完整重写 Input runtime。

## 1. 文件结构

| File | Role | 说明 |
| --- | --- | --- |
| `README.md` | design-index | 本文件。给出总裁决、阅读顺序、边界和完成定义。 |
| `01-现状证据与AI-first裁决.md` | research-decision | 当前 `InputManager`、`project.godot`、调用点和外部资料对照。 |
| `02-目标架构与优化路线.md` | architecture-roadmap | 定义 `InputActionId`、`InputContext`、typed facade、manifest 和测试路线。 |
| `03-调用点迁移与验证计划.md` | migration-test-plan | 调用点分层、迁移顺序、grep gate、Godot 场景验证和文档同步。 |

## 2. 总裁决

采用 **AI-first Input Contract**：

```text
project.godot InputMap
  -> 仍然承载 Godot 层物理按键/手柄绑定

InputActionId / manifest 表
  -> 显式列出 action id、业务语义、设备、默认绑定、上下文、owner；第一阶段可以放在 README 或可选 manifest 文档中

InputManager / InputFacade
  -> 提供 typed API，不让业务散落字符串

InputContext
  -> 区分 Gameplay / UI / Debug / Test 等输入模式

Validation / DocsAI
  -> 校验 project.godot 与 Input 文档、调用点、测试场景一致
```

不采用：

- 不复制 Unity Input System 或 Unreal Enhanced Input 的完整 asset/context/trigger/editor 体系。
- 不把输入直接上提为 Entity Data 状态源。
- 不让业务代码继续直接写自由字符串 action。
- 不把 `project.godot` 当成 AI 唯一可读契约。

## 3. AI-first 原则

| 旧问题 | AI-first 规则 |
| --- | --- |
| action 名散在 `project.godot` 和 C# 字符串里 | action 必须有稳定 id、中文语义、owner、默认绑定和上下文说明。 |
| `InputManager` 只是静态 wrapper | wrapper 必须表达业务动作，不只是转发 `Godot.Input`。 |
| Gameplay / UI / Debug 输入混用 | 输入上下文必须显式，避免暂停菜单、测试场景和战斗输入互相抢键。 |
| 测试场景直接调用 `Input.IsActionPressed("ui_right")` | Debug/Test 输入可以保留，但必须标注为测试上下文，不混入框架默认输入契约。 |
| 文档写“自动生成/多人隔离”，源码没有对应能力 | 文档必须反映当前实现，未来能力进入路线图，不伪装为 current。 |

## 4. 目标边界

| 模块 | 目标职责 | 禁止职责 |
| --- | --- | --- |
| `project.godot` | Godot InputMap 绑定事实源 | 不独自承担业务语义和 AI 路由。 |
| `InputActionId` | 稳定 action 常量或生成型 typed id | 不承载设备细节和业务执行。 |
| `Input manifest` | action 语义、上下文、owner、默认绑定和验证输入；可以是 README 表格、`InputMap.md` 或后续 JSON/DataOS descriptor | 不在 runtime 热路径扫描工程，也不强制所有 owner 都有独立 manifest 文件。 |
| `InputManager` | typed 查询、向量读取、设备状态、震动 | 不直接执行技能、移动、UI 行为。 |
| `InputContext` | 控制当前输入模式和屏蔽规则 | 不替代 SystemManager 的项目状态。 |
| Gameplay components | 消费 typed 输入结果，发起业务请求 | 不拼 Godot action 字符串。 |

## 5. 完成定义

Input 优化设计完成不是“再包一层方法”。

必须同时满足：

- `project.godot` 里的 action 有对应 manifest 或文档清单。
- 业务调用点不再依赖裸字符串 action。
- Gameplay / UI / Debug / Test 输入上下文被分开说明。
- DocsAI 中的 Input 文档与源码事实一致，不再声称未实现的多人隔离或自动生成。
- 后续实现有清晰验证入口：构建、grep gate、Input 测试场景、关键游戏切片 smoke。

## 6. 阅读顺序

1. 先读 `01-现状证据与AI-first裁决.md`，确认为什么要优化。
2. 再读 `02-目标架构与优化路线.md`，确认要优化到什么形状。
3. 最后读 `03-调用点迁移与验证计划.md`，确认怎么小步迁移和验证。
