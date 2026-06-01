# DocsAI 统一管理与索引规则

> 状态：current
> 定位：DocsAI 统一事实源、分层、索引和迁移规则。
> 更新：2026-06-01

## 1. 总原则

`DocsAI/` 是 SlimeAI 框架文档统一入口。框架功能文档、使用说明、测试说明、调试说明和深度背景都应能从这里稳定路由。

核心原则：

- `DocsAI/ECS/` 是 ECS 框架功能文档事实源。
- `Src/ECS/` 是源码目录，不承载 SlimeAI 框架文档；框架 Markdown 文档统一迁入 `DocsAI/ECS/`。
- 迁移文档必须优先来自原 `Src/ECS/**.md` 长文档，不允许用重新生成的薄文档替代原文，也不能因为拆分丢失概念、示例、参数或历史约束。
- 文档拆分是管理手段，不是硬性格式；需要拆才拆，不适合拆时保留原文结构或原文件名。
- `DocsAI/管理/` 保存已经确定的 DocsAI 治理规则。
- `DocsAI/思考/` 保存深度思考、设计推导、ADR 和仍可演进的判断。
- `DocsAI/Archive/` 保存历史归档，不作为当前执行依据。

## 2. 目录职责

| 目录 | 职责 |
| ---- | ---- |
| `DocsAI/管理/` | 已确定的 DocsAI 管理、索引、迁移、维护规则 |
| `DocsAI/ECS/` | ECS 框架功能文档事实源 |
| `DocsAI/ECS/Runtime/` | Entity / Data / Event / System Core 等共享 ECS 内核文档 |
| `DocsAI/ECS/Capabilities/` | Ability / Damage / Movement 等功能 owner 文档 |
| `DocsAI/ECS/Tools/` | Timer / ObjectPool / Input / TargetSelector / Logger 等通用工具文档 |
| `DocsAI/ECS/UI/` | UI binding、HUD、调试 UI 与界面层规则 |
| `DocsAI/思考/` | 框架功能背后的深度分析、设计推导、ADR、复盘 |
| `DocsAI/Archive/` | 历史文档和失效资料归档 |
| `Workspace/DocsAI/` | 工作区级文档，例如跨仓库、submodule、AI 流程 |

## 3. ECS 文档组织

ECS owner 文档优先无损保存原文。`Concept.md`、`Usage.md`、`Tests.md`、`Debug.md` 是推荐命名，不是强制结构。

| 文件 | 内容 |
| ---- | ---- |
| `Concept.md` | 设计定位、职责边界、契约、依赖、红线、关键数据流 |
| `Usage.md` | 调用方式、参数说明、典型代码、扩展步骤、常见错误 |
| `Tests.md` | 验证入口、测试场景、覆盖范围、期望输出 |
| `Debug.md` | 日志、排查流程、失败模式、定位顺序 |
| 原文件名 | 原文结构完整、拆分会降低可读性或增加维护成本时使用 |

拆分规则：

- 原 `Src/ECS` 文档内容必须完整迁入 `DocsAI/ECS`，不得因为拆分丢失信息。
- 如果原文同时包含设计和使用信息，优先整体迁入一个完整文档；只有在内容过长或职责明显分离时才拆分。
- 拆分后的文档必须互相链接，并保留原文迁移来源标记。
- 生成式摘要只能作为导航或辅助说明，不能替代原文事实源。
- 需要解释“为什么”时链接到 `DocsAI/思考/`，不要把长篇推导硬塞进执行入口。

功能归属规则：

- 新任务默认从 `Runtime/` 或 `Capabilities/` 进入，不从旧技术层分类进入。
- `Runtime/` 只放跨功能共享的 ECS 内核：Entity identity/lifecycle、Data runtime、EventBus、System lifecycle。
- `Capabilities/<Owner>/` 放用户会用功能词描述的 owner，例如 Ability、Damage、Movement、Collision、Feature、Effect、Projectile、AI、Spawn、Unit。
- Capability 内部可以继续保留 Component / System / Events / Tests / DataKeys 语义；这些语义不再作为 DocsAI 顶层路由。
- `Tools/` 和 `UI/` 保留顶层，不强行迁入 Capabilities。
- 旧 `DocsAI/ECS/System/`、`DocsAI/ECS/Component/`、`DocsAI/ECS/Entity/`、`DocsAI/ECS/Data/`、`DocsAI/ECS/Event/` 不作为当前入口；需要保留的内容迁入对应 owner 的 `Concepts/` 或原文件名下。

## 4. Src 文档规则

`Src/ECS/` 不再保存框架 Markdown 文档。框架文档统一进入 `DocsAI/ECS/`，源码目录只保留代码、场景、资源和必要的非 Markdown 工程文件。

删除规则：

- 完成迁移后删除对应 `Src/ECS/**/*.md`。
- 不保留短指针，避免 `Src` 被误认为文档入口。
- 源码注释中可以引用 `DocsAI/ECS/...`，但不要嵌入长篇文档。
- 后续新增框架文档默认写入 `DocsAI/ECS/`，不要在 `Src/ECS/` 新建 Markdown 文档。

允许例外：

- 当前不设置例外。
- 如果未来确实需要在 `Src` 放置源码近场说明，必须先在 `DocsAI/管理/` 登记规则和索引理由。

## 5. 索引维护规则

修改 DocsAI 时必须检查对应索引：

| 修改内容 | 必查索引 |
| ---- | ---- |
| 新增或移动 DocsAI 顶层目录 | `DocsAI/README.md`、`DocsAI/INDEX.md` |
| 新增或移动 ECS owner | `DocsAI/ECS/README.md` |
| 新增或移动 Runtime / Capability owner | `DocsAI/ECS/README.md`、`DocsAI/ECS/Runtime/README.md`、`DocsAI/ECS/Capabilities/README.md`、`DocsAI/管理/目录架构迁移清单.md` |
| 新增管理规则 | `DocsAI/管理/README.md` |
| 新增深度思考 | `DocsAI/思考/README.md` |
| 从 Src 迁移文档 | `DocsAI/管理/Src文档迁移清单.md`、`DocsAI/ECS/README.md` |

AI 读取顺序：

1. `DocsAI/README.md`
2. `DocsAI/INDEX.md`
3. `DocsAI/管理/DocsAI统一管理与索引规则.md`
4. `DocsAI/ECS/README.md`
5. 目标 `Runtime/<owner>` 或 `Capabilities/<owner>` 下的完整迁移文档；迁移未完成时按 `DocsAI/管理/目录架构迁移清单.md` 追溯旧路径
6. 需要验证时读取 `Tests.md`、迁移文档中的测试章节或测试脚本
7. 需要理解背景时读取相关 `DocsAI/思考/`

## 6. 迁移规则

从 `Src/ECS` 迁移到 `DocsAI/ECS` 时遵循：

- 先读取原文，不凭空生成替代文档。
- 保留原文的事实、示例、参数、警告和测试入口。
- 修正相对链接，确保从新位置仍能跳回源码。
- 原 `Src` Markdown 文件在确认迁移后删除。
- 更新 `DocsAI/ECS/README.md` 中的 owner 索引。
- 更新 `DocsAI/管理/Src文档迁移清单.md`，记录原路径和 DocsAI 目标路径。
- 如果原文包含已经过时的路径或旧术语，优先保留内容并加迁移说明，后续再单独清理。

## 7. 思考文档边界

`DocsAI/思考/` 是重要背景资料，但不是直接执行入口。

适合放入 `DocsAI/思考/` 的内容：

- 架构取舍和争议分析。
- 框架功能背后的设计推导。
- ADR 和长期判断。
- 问题复盘和专题研究。

不适合放入 `DocsAI/思考/` 的内容：

- 已确定的 DocsAI 管理规则。
- 模块的直接使用说明。
- 测试命令和验证标准答案。
- 源码旁短指针。

## 8. 验证要求

文档治理类改动至少检查：

```bash
find DocsAI -type f -name '*.md' | sort
find Src/ECS -type f -name '*.md' | sort
git status --short
```

预期：`find Src/ECS -type f -name '*.md'` 没有输出。

涉及 SDD 或 AI 配置时再运行对应验证脚本；纯 DocsAI 迁移不强制运行框架构建。
