# AI 开发闭环

本文定义 AI 执行程序开发任务时的标准闭环。适用于新增或修改 System、Component、Event、Tool、DataKey、测试和文档。

## 任务开始

1. 读 `AGENTS.md`。
2. 读 `DocsAI/README.md` 和 `DocsAI/INDEX.md`。
3. 根据任务触发对应 Skill。
4. 通过 `rg` / `find` / `sed` 读取相关源码和文档。
5. 判断是否涉及 ECS 核心、高风险生命周期或公共 API。

## 计划

执行前必须明确：

- 目标是什么
- 修改哪些模块
- 是否影响 ECS 核心
- 是否新增 System / Component / Event / DataKey
- 是否需要新增或更新测试
- 是否需要更新 Docs / DocsAI / Skill
- 验证命令是什么

大任务必须拆成阶段计划，小任务可以在最终回复中说明执行步骤。

## 实现

- 小步修改，不做无关重构。
- 保持现有项目风格。
- 不直接绕过 EntityManager、DamageService、TimerManager、ResourceManagement 等统一入口。
- 不把运行时业务状态藏进 Component 私有字段；共享状态进入 Data。
- 不用 Godot Signal 承载核心业务通信；核心逻辑使用 EventBus。
- `_Process` 中禁止 new 对象和 LINQ。

## 验证

最低验证：

```bash
dotnet build
```

根据修改类型追加：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run <scene> --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run-all --continue-on-fail
```

无法验证时必须说明原因、风险和替代检查。

## 文档同步

修改完成前检查：

- `Docs/框架/项目索引.md` 是否需要更新。
- `DocsAI/INDEX.md` 是否需要更新。
- 对应 `DocsAI/Modules/*` 是否需要更新。
- 对应 `.codex/skills/*/SKILL.md` 是否需要更新。
- 代码和 DocsAI 契约是否一致（`Src/**/*.md` 已移除）。

## 最终输出

最终回复必须包含：

```text
完成内容：
修改文件：
验证命令：
验证结果：
风险点：
建议人工重点审查：
```

如果没有运行构建或测试，必须明确说明。

