---
name: "OPSX: Propose"
description: 提出新变更，一步生成全部 artifact。当用户想快速描述要构建的内容，并获得一份包含设计、规格和任务的完整提案时使用。
---

提出新变更 —— 创建变更并一步生成所有 artifact。

将创建以下 artifact：

- proposal.md（做什么 & 为什么）
- design.md（怎么做）
- tasks.md（实现步骤）

准备就绪后，运行 /opsx:apply 开始执行

---

**输入**：用户请求中应包含变更名称（kebab-case）或对要构建内容的描述。

**步骤**

1. **若输入不明确，询问用户想构建什么**

   使用 **AskUserQuestion 工具**（开放式，无预设选项）询问：

   > 「你想做什么变更？描述一下你要构建或修复的内容。」

   从用户的描述中推导出 kebab-case 名称（例如「添加用户认证」→ `add-user-auth`）。

   **重要**：在明确用户要构建什么之前，不要继续。

2. **创建变更目录**

   ```bash
   openspec new change "<name>"
   ```

   这会在 `openspec/changes/<name>/` 下创建带有 `.openspec.yaml` 的脚手架。

3. **获取 artifact 构建顺序**

   ```bash
   openspec status --change "<name>" --json
   ```

   解析 JSON，获取：
   - `applyRequires`：实现前需要完成的 artifact ID 数组（例如 `["tasks"]`）
   - `artifacts`：所有 artifact 的列表及其状态与依赖关系

4. **按顺序创建 artifact，直至达到可执行状态**

   使用 **TodoWrite 工具** 跟踪 artifact 创建进度。

   按依赖顺序循环（先处理无未决依赖的 artifact）：

   a. **对每个 `ready` 状态的 artifact（依赖已满足）：**
   - 获取指令：
     ```bash
     openspec instructions <artifact-id> --change "<name>" --json
     ```
   - 指令 JSON 包含：
     - `context`：项目背景（仅作为你的约束，**不要**写入输出）
     - `rules`：artifact 专用规则（仅作为你的约束，**不要**写入输出）
     - `template`：输出文件的结构模板
     - `instruction`：该 artifact 类型的 schema 级指导
     - `outputPath`：artifact 写入路径
     - `dependencies`：已完成的相关 artifact，读取以获取上下文
   - 读取已完成的依赖文件获取上下文
   - 使用 `template` 作为输出文件的结构
   - 将 `context` 和 `rules` 作为约束应用 —— 但不要把它们写入文件
   - 简要提示进度：「已创建 <artifact-id>」

   b. **持续创建直到所有 `applyRequires` artifact 完成**
   - 每创建完一个 artifact，重新运行 `openspec status --change "<name>" --json`
   - 检查 `applyRequires` 中的每个 artifact ID 是否都已在 artifacts 数组中标记为 `status: "done"`
   - 全部完成后停止

   c. **若某个 artifact 需要用户输入**（上下文不清）：
   - 使用 **AskUserQuestion 工具** 澄清
   - 然后继续创建

5. **展示最终状态**

   ```bash
   openspec status --change "<name>"
   ```

**输出**

所有 artifact 创建完成后，汇总：

- 变更名称与位置
- 已创建 artifact 的列表及简要说明
- 就绪提示：「所有 artifact 已创建！可以开始实现。」
- 引导：「运行 `/opsx:apply` 或让我来实现，开始处理任务。」

**Artifact 创建指南**

- 遵循 `openspec instructions` 返回的每个 artifact 类型的 `instruction` 字段
- schema 定义了每个 artifact 应包含什么 —— 照做
- 创建新 artifact 前先读取依赖 artifact 获取上下文
- 使用 `template` 作为输出文件结构 —— 填充各章节
- **重要**：`context` 和 `rules` 是给你的约束，不是文件内容
  - 不要把 `<context>`、`<rules>`、`<project_context>` 块复制到 artifact 中
  - 它们指导你写什么，但绝不应出现在输出中

**约束**

- 创建实现所需的**全部** artifact（由 schema 的 `apply.requires` 定义）
- 创建新 artifact 前务必先读取依赖 artifact
- 若上下文严重不足，询问用户 —— 但优先做出合理决策以保持推进
- 若同名变更已存在，询问用户是想继续它还是新建一个
- 写入后验证每个 artifact 文件存在，再进入下一个
