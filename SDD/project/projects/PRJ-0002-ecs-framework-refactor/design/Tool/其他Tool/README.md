# 其他 Tool 设计包

> 更新：2026-06-03
> 状态：current research decision
> 范围：`CommonTool` / `ResourceManagement` 接入、`Math`、`NodeLifecycle`、`ParentManager`、`TargetSelector`
> 排除：`Input`、`ObjectPool`、`Timer` 已有独立设计；`Logger/Log` 按用户要求跳过。

## 0. 本设计包回答什么

这份设计包用于检查 Tools 中剩余工具是否还需要保留、是否符合 AI-first ECS 原则、当前缺陷是什么，以及后续应如何完善。

结论先写清楚：

```text
TargetSelector 必须保留，并应优先做 AI-first 查询契约硬化。
Math 必须保留，但要区分纯数学、游戏公式、概率和目标选择几何 ownership。
NodeLifecycle 必须保留为底层过渡注册表，但不应继续作为业务查询公共入口。
ParentManager 必须保留为运行时挂载点管理，但应升级为 manifest 化的 RuntimeMountRegistry 语义。
CommonTool 不应继续扩展为杂项工具；现有 LoadPackedScene 应收口到 ResourceManagement/ResourceLoading 契约。
```

## 1. 工具裁决总览

| Tool | 是否必要 | AI-first 裁决 | 优先级 |
| --- | --- | --- | --- |
| `TargetSelector` | 必要 | 从静态全局扫描工具升级为可诊断、可复现、可替换候选源的目标查询引擎；第一阶段先补 query validation、origin/provider、RNG、diagnostics。 | P0 |
| `Math` | 必要 | 保留轻量数学层；拆清纯几何/曲线、游戏公式、概率随机和 TargetSelector 领域边界，不引入第三方数学运行时。 | P1 |
| `NodeLifecycle` | 必要但降级 | 只作为 Node 注册表和 transitional bridge；Entity/UI/Component/TargetSelector 不应把它当最终业务查询事实源。 | P1 |
| `ParentManager` | 必要但改名语义 | 保留统一挂载点能力；后续演进为 manifest 化 mount registry，避免自由字符串和隐式 root 生命周期。 | P1 |
| `CommonTool` | 不作为独立 owner 必要 | 冻结为临时兼容 facade；禁止继续往里塞杂项；资源加载契约归 `ResourceManagement`。 | P2 |
| `ResourceManagement` | 必要 | 作为资源 manifest 与加载入口继续保留；补 strict lookup、load diagnostics、path-source policy 和 catalog validation。 | P2 |

## 2. 阅读顺序

| 文档 | 用途 |
| --- | --- |
| [01-现状证据与AI-first裁决.md](01-现状证据与AI-first裁决.md) | DeepThink 确认包、整体问题形态、风险、方案和总裁决 |
| [02-CommonTool与ResourceManagement裁决.md](02-CommonTool与ResourceManagement裁决.md) | 杂项工具与资源加载边界 |
| [03-Math目标架构与验证.md](03-Math目标架构与验证.md) | 数学工具层、曲线、几何、概率和公式边界 |
| [04-NodeLifecycle与ParentManager边界.md](04-NodeLifecycle与ParentManager边界.md) | Node 注册、运行时挂载点和场景树生命周期边界 |
| [05-TargetSelector查询契约.md](05-TargetSelector查询契约.md) | 目标查询引擎、候选源、过滤、排序、随机和 diagnostics |
| [06-实施路线与验证门禁.md](06-实施路线与验证门禁.md) | 后续 SDD 拆分、调用点迁移、BDD 和验证命令 |

## 3. 当前事实源

- 方向：`DocsAI/ECS框架与AIFirst方向决策.md`
- Tools 文档：`DocsAI/ECS/Tools/`
- 源码：`Src/ECS/Tools/`
- 资源加载：`Data/ResourceManagement/`
- Data 资源路径约束：`DocsAI/ECS/Runtime/Data/Data系统说明.md`
- System 初始化挂载：`Src/ECS/Runtime/System/SystemManager.cs`
- 当前设计索引：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md`

## 4. 完成定义

本设计包完成后，不代表开始改源码。后续进入实现型 SDD 时至少要满足：

- 每个 Tool 有明确 owner、使用入口、禁止项和验证入口。
- `TargetSelector` 能输出结构化 query diagnostics，AI 不需要读日志猜为什么选不到目标。
- `Math` 测试能稳定复现概率、曲线、几何边界，不依赖不可控随机。
- `NodeLifecycle` 只在底层 manager 使用，业务查询默认走 Entity/UI/TargetSelector 的 typed facade。
- `ParentManager` 的挂载点来自 manifest，不靠散落字符串。
- `CommonTool` 不再作为新增杂项入口。
- 资源加载能说明加载来源、分类、key/path、失败原因和 catalog 覆盖情况。
