# 其他 Tool 设计包

> 更新：2026-06-07
> 状态：current consolidated design
> 范围：`CommonTool` / `ResourceManagement` 接入、`Math`、`NodeLifecycle`、`ParentManager`、`TargetSelector`
> 排除：`Input`、`ObjectPool`、`Timer` 已有独立设计；`Logger/Log` 按用户要求跳过。

## 0. 本设计包回答什么

这份设计包用于检查 Tools 中剩余工具是否还需要保留、是否符合 AI-first ECS 原则、当前缺陷是什么，以及后续应如何完善。

结论先写清楚：

```text
功能优先，代码可丢弃；执行型 SDD 不为旧 API 长期兼容让路。
旧类名、旧方法、旧文件如果阻碍 AI 判断功能、owner、scope 和验证，可以 hard cutover 删除。

TargetSelector 必须保留，并应优先做 AI-first 查询契约硬化。
Math 必须保留，但要区分纯数学、游戏公式、概率和目标选择几何 ownership。
NodeLifecycle 功能必须保留为底层 Node registry，但业务查询入口应 hard cutover 到 Entity/UI/TargetSelector typed facade。
ParentManager 功能必须保留并升级为 RuntimeMountRegistry / SceneMountRegistry：统一管理大量 Entity / Pool / UI / Tool 节点在 SceneTree 中的挂载路径。
CommonTool 不应继续存在为杂项 owner；现有 LoadPackedScene 应迁入 ResourceManagement/ResourceLoading 契约，执行切片完成前删除旧入口。
保留“通用工具区域”概念，但不保留无约束 `CommonTool` 杂物箱。
Common Utilities 仍属于 Tools，最终位置采用 `Src/ECS/Tools/CommonUtilities/` + `DocsAI/ECS/Tools/CommonUtilities/`；NodeLifecycle 按建议迁到 Runtime；TargetSelector 不做兼容桥，完全重构。
ResourceManagement 不应继续作为“资源管理器”概念被保留；后续 hard cutover 应重命名或收敛为极薄 `ResourceLoading` 统一加载工具，职责只有加载、source/owner/usage diagnostics 和错误报告。`ResourceCatalog` / `ResourceGenerator` 是旁路 catalog / 生成器，不是运行时管理器。`res://` 本身不是问题。移动目录后的真实闭环交给 project directory / resource-path migration skill、ResourceGenerator 和 diagnostics；未来框架/游戏仓分离后，框架 catalog 不默认拥有游戏资源。
```

## 1. 工具裁决总览

| Tool | 是否必要 | AI-first 裁决 | 优先级 |
| --- | --- | --- | --- |
| `RuntimeMountRegistry` / `ParentManager` | 必要 | P0 功能能力；统一 SceneTree mount manifest、scope、pending/in-tree diagnostics。执行时可直接替换 `ParentManager` API，不要求兼容旧自由字符串接口。 | P0 |
| `TargetQueryEngine` / `TargetSelector` | 必要 | P0 功能能力；从静态全局扫描工具升级为可诊断、可复现、可替换候选源的目标查询引擎。第一切片默认先补 contract / candidate source / diagnostics / deterministic RNG；空间索引需确认或证据触发。 | P0 |
| `ResourceLoading` / current `ResourceManagement` | 必要，但不保留“管理器”职责 | P1 功能能力；保留统一加载工具，删除或重命名当前 `ResourceManagement` 管理器心智。执行时删除 contains fallback 和无 source `LoadPath`，补 structured result、source policy、catalog diagnostics。路径移动由 project directory / resource-path migration skill 和 generator/diagnostics 处理。 | P1 |
| `MathFormula` / `Geometry2D` / deterministic random | 必要 | P1 功能能力；拆清纯几何/曲线、游戏公式、概率随机和 TargetSelector 领域边界，不引入第三方数学运行时。 | P1 |
| `NodeLifecycleRegistry` / `NodeLifecycle` | 必要但低层化 | P1 底层能力；保留 Node registry、owner metadata、snapshot diagnostics、invalid cleanup，业务查询 hard cutover 到 Entity/UI/TargetSelector。 | P1 |
| `Common Utilities` / `CommonTool` | 通用工具概念保留，当前 `CommonTool` 形态不保留 | `CommonTool.LoadPackedScene` 迁入 `ResourceManagement` / `ResourceLoading`；通用工具最终放在 `Src/ECS/Tools/CommonUtilities/`，不直接堆在 `Src/ECS/Tools/` 根目录，每个 helper 必须有 owner、用途、禁止项和测试。 | P1 |

## 2. 阅读顺序

| 文档 | 用途 |
| --- | --- |
| [01-现状证据与AI-first裁决.md](01-现状证据与AI-first裁决.md) | 总体分析：目标、证据、已确认决策、未确认问题、默认假设和总体路线 |
| [02-CommonTool与ResourceManagement裁决.md](02-CommonTool与ResourceManagement裁决.md) | 功能说明：Common Utilities、ResourceLoading / ResourceManagement、ResourceGenerator、路径迁移 workflow |
| [03-Math目标架构与验证.md](03-Math目标架构与验证.md) | 功能说明：数学工具层、曲线、几何、概率和公式边界 |
| [04-NodeLifecycle与ParentManager边界.md](04-NodeLifecycle与ParentManager边界.md) | 功能说明：Node 注册、Runtime mount、SceneTree 生命周期边界 |
| [05-TargetSelector查询契约.md](05-TargetSelector查询契约.md) | 功能说明：目标查询引擎、候选源、过滤、排序、随机和 diagnostics |
| [06-实施路线与验证门禁.md](06-实施路线与验证门禁.md) | 实施说明：后续 SDD 拆分、调用点迁移、BDD、grep gate 和验证命令 |

说明：原 `07~11` 按时间追加的确认文档已合并到上述 current 文档中，不再作为事实源入口。

## 3. 当前事实源

- 方向：`DocsAI/ECS框架与AIFirst方向决策.md`
- Tools 文档：`DocsAI/ECS/Tools/`
- 源码：`Src/ECS/Tools/`
- 资源加载：`Data/ResourceManagement/`
- 资源加载 DocsAI 入口：`DocsAI/ECS/Tools/ResourceManagement/README.md`
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
- `CommonTool.LoadPackedScene` 迁入资源加载 owner；Common Utilities 固定为 `Src/ECS/Tools/CommonUtilities/` + `DocsAI/ECS/Tools/CommonUtilities/`，必须有独立 manifest、禁止项和测试，不再作为新增杂项入口。
- 资源加载能说明加载来源、分类、key/path、失败原因和 catalog 覆盖情况。
- 目录和资源移动不靠人工全仓搜索；必须使用 project directory / `project-filesystem` skill 或等价脚本替换旧路径，并用 `rg` / diagnostics 证明旧路径残留已分类或清零。
