# 计划 04：框架模块 AI 契约文档

## 目标

为核心框架模块建立 AI 可读契约，减少 AI 开发时靠猜、靠试错、靠人工审查兜底的问题。

每个模块文档必须回答：

- 模块职责是什么
- 输入是什么
- 输出是什么
- 生命周期是什么
- 允许依赖什么
- 禁止绕过什么
- 常见错误是什么
- 修改后跑哪些测试

## 输入文件

- `Docs/框架/项目索引.md`
- `Docs/框架/ECS/`
- `Src/ECS/Base/`
- `Data/DataKey/`
- `.codex/skills/ecs-*/SKILL.md`

## 修改范围

新增或更新：

- `DocsAI/Modules/Entity.md`
- `DocsAI/Modules/Component.md`
- `DocsAI/Modules/Data.md`
- `DocsAI/Modules/Event.md`
- `DocsAI/Modules/SystemCore.md`
- `DocsAI/Modules/AbilitySystem.md`
- `DocsAI/Modules/DamageSystem.md`
- `DocsAI/Modules/TestSystem.md`
- `DocsAI/Modules/UI.md`
- `DocsAI/Modules/Tools.md`

## 执行步骤

1. 从项目索引提取核心模块入口。
2. 每个模块只写 AI 开发必需契约，不复制全部设计文档。
3. 每个模块文档采用固定结构：
   - 职责边界
   - 核心入口
   - 数据 / 事件 / 生命周期
   - 禁止事项
   - 修改流程
   - 推荐测试
   - 人工审查重点
4. 对高风险模块加红线：
   - Entity 生命周期
   - Component 状态存储
   - Event 订阅清理
   - System 启停和运行门禁
   - DamageService 伤害入口
   - ResourceManagement 资源加载
5. 在相关 Skill 中指向这些模块契约。

## 验证命令

```bash
find DocsAI/Modules -maxdepth 1 -type f | sort
rg -n "职责边界|禁止事项|推荐测试|人工审查重点" DocsAI/Modules
rg -n "DocsAI/Modules" .codex/skills
```

## 验收标准

- AI 修改核心模块前能找到对应模块契约。
- 每个模块都有推荐测试入口。
- 契约文档不与人类设计文档重复长篇内容。
- 高风险架构红线被明确写入对应模块文档。

## 风险点

- 模块契约太长会变成另一个项目索引。
- 模块契约太短会失去约束价值。
- 需要和 AGENTS.md、Skill 保持一致，避免三套规则冲突。

## 完成输出

最终回复必须包含：

- 已建立的模块契约
- 哪些模块仍需要补充
- 哪些红线被写入契约
- 建议优先审查的契约文档

