# 其他 Tool 设计包

> 更新：2026-06-04
> 状态：current research decision, 2026-06-04 hard cutover override
> 范围：`CommonTool` / `ResourceManagement` 接入、`Math`、`NodeLifecycle`、`ParentManager`、`TargetSelector`
> 排除：`Input`、`ObjectPool`、`Timer` 已有独立设计；`Logger/Log` 按用户要求跳过。

## 0. 本设计包回答什么

这份设计包用于检查 Tools 中剩余工具是否还需要保留、是否符合 AI-first ECS 原则、当前缺陷是什么，以及后续应如何完善。

结论先写清楚：

```text
2026-06-04 override：功能优先，代码可丢弃；执行型 SDD 不为旧 API 长期兼容让路。
旧类名、旧方法、旧文件如果阻碍 AI 判断功能、owner、scope 和验证，可以 hard cutover 删除。

TargetSelector 必须保留，并应优先做 AI-first 查询契约硬化。
Math 必须保留，但要区分纯数学、游戏公式、概率和目标选择几何 ownership。
NodeLifecycle 功能必须保留为底层 Node registry，但业务查询入口应 hard cutover 到 Entity/UI/TargetSelector typed facade。
ParentManager 功能必须保留并升级为 RuntimeMountRegistry / SceneMountRegistry：统一管理大量 Entity / Pool / UI / Tool 节点在 SceneTree 中的挂载路径。
CommonTool 不应继续存在为杂项 owner；现有 LoadPackedScene 应迁入 ResourceManagement/ResourceLoading 契约，执行切片完成前删除旧入口。
```

## 1. 工具裁决总览

| Tool | 是否必要 | AI-first 裁决 | 优先级 |
| --- | --- | --- | --- |
| `RuntimeMountRegistry` / `ParentManager` | 必要 | P0 功能能力；统一 SceneTree mount manifest、scope、pending/in-tree diagnostics。执行时可直接替换 `ParentManager` API，不要求兼容旧自由字符串接口。 | P0 |
| `TargetQueryEngine` / `TargetSelector` | 必要 | P0 功能能力；从静态全局扫描工具升级为可诊断、可复现、可替换候选源的目标查询引擎。第一切片默认先补 contract / candidate source / diagnostics / deterministic RNG；空间索引需确认或证据触发。 | P0 |
| `ResourceLoading` / `ResourceManagement` | 必要 | P1 功能能力；作为资源 manifest 与加载入口继续保留，执行时删除 contains fallback 和无 source `LoadPath`，补 structured result、source policy、catalog diagnostics。 | P1 |
| `MathFormula` / `Geometry2D` / deterministic random | 必要 | P1 功能能力；拆清纯几何/曲线、游戏公式、概率随机和 TargetSelector 领域边界，不引入第三方数学运行时。 | P1 |
| `NodeLifecycleRegistry` / `NodeLifecycle` | 必要但低层化 | P1 底层能力；保留 Node registry、owner metadata、snapshot diagnostics、invalid cleanup，业务查询 hard cutover 到 Entity/UI/TargetSelector。 | P1 |
| `CommonTool` | 不保留为 owner | 删除目标；不作为独立 owner 或长期 facade。现有加载功能迁入 `ResourceManagement` / `ResourceLoading`。 | P1 |

## 2. 阅读顺序

| 文档 | 用途 |
| --- | --- |
| [01-现状证据与AI-first裁决.md](01-现状证据与AI-first裁决.md) | DeepThink 确认包、整体问题形态、风险、方案和总裁决 |
| [02-CommonTool与ResourceManagement裁决.md](02-CommonTool与ResourceManagement裁决.md) | 杂项工具与资源加载边界 |
| [03-Math目标架构与验证.md](03-Math目标架构与验证.md) | 数学工具层、曲线、几何、概率和公式边界 |
| [04-NodeLifecycle与ParentManager边界.md](04-NodeLifecycle与ParentManager边界.md) | Node 注册、运行时挂载点和场景树生命周期边界 |
| [05-TargetSelector查询契约.md](05-TargetSelector查询契约.md) | 目标查询引擎、候选源、过滤、排序、随机和 diagnostics |
| [06-实施路线与验证门禁.md](06-实施路线与验证门禁.md) | 后续 SDD 拆分、调用点迁移、BDD 和验证命令 |
| [07-2026-06-04-AI-first完全重构校准.md](07-2026-06-04-AI-first完全重构校准.md) | 用户最新裁决：功能优先、可 hard cutover、不为旧 API 长期兼容；进入执行前必须先读 |

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
- `ParentManager` / `RuntimeMountRegistry` 的挂载点来自 manifest，不靠散落字符串；能输出 pending / in-tree / invalid diagnostics。
- `CommonTool` 删除或迁入资源加载 owner，不再作为新增杂项入口。
- 资源加载能说明加载来源、分类、key/path、失败原因和 catalog 覆盖情况。
