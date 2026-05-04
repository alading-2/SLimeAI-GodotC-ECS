# Skill 设计规则

Skill 是 AI 的任务触发入口，不是项目百科。

## 标准结构

每个 `SKILL.md` 只保留：

- 什么时候触发
- 不属于本 Skill 的任务应该转向哪里
- 必读 DocsAI / Docs / references
- 允许修改的范围
- 禁止事项
- 推荐验证命令
- 最终输出要求

## 内容放置规则

- 任务流程、触发条件、命令入口 -> `SKILL.md`
- 模块契约、详细语义、踩坑记录 -> `DocsAI/Modules/`
- Godot 场景测试、日志判定、测试矩阵 -> `DocsAI/Tests/`
- 长任务恢复协议 -> `DocsAI/Workflows/`
- 外部源码研究、AI 自评复盘、DataOS 等跨模块流程 -> `DocsAI/Protocols/`
- 踩坑和经验沉淀 -> `DocsAI/Experience/`
- 项目级执行计划 -> `Plans/<分类>/<计划名>/`
- 计划模板示例 -> 直接放在对应计划目录内，或按分类提供示例目录
- 稳定脚本 -> `skill/scripts/`
- 领域参数细节或模板 -> `skill/references/`，后续可逐步迁到 `DocsAI/`

## 编写原则

- Skill 入口要短，避免超过必要上下文。
- 不复制 `Docs/框架/项目索引.md` 的长导航。
- 不放历史方案全文。
- 不使用机器绝对路径。
- 不把不同模块的规则混在一个 Skill。
- 修改框架接口或流程时，必须同步检查相关 Skill 和 DocsAI 是否过期。

## 推荐验证

```bash
find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l
rg -n "/mnt/[e]|file://[/]|复刻土豆兄[弟]" .codex/skills
rg -n "DocsAI/|Skill到DocsAI映射|Skill设计规则" .codex/skills DocsAI
```
