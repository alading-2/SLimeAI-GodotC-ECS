---
name: "OPSX: Archive"
description: 完成 OpenSpec 变更的收尾工作：将 delta 规格合并回基线，并清理执行历史，避免遗留目录成为 AI 的长期入口。
---

归档已完成的变更，不让旧执行历史成为 AI 的入口。

**输入**：可指定变更名称。若未提供，从对话上下文中推断；若仍然模糊，必须让用户从可用变更中选择。

**步骤**

1. **未提供名称时，提示用户选择**

   运行 `openspec list --json` 获取可用变更列表，使用 **AskUserQuestion 工具** 让用户选择。

   仅展示活跃变更，并附带各变更使用的 schema。

   **重要**：不要猜测或自动选择，始终由用户决定。

2. **检查 artifact 完成状态**

   运行 `openspec status --change "<name>" --json` 检查 artifact 完成情况。

   解析 JSON，关注：
   - `schemaName`：使用的工作流模式
   - `artifacts`：各 artifact 的状态（`done` 或其他）

   **若存在未完成的 artifact：**
   - 列出未完成的 artifact 并发出警告
   - 使用 **AskUserQuestion 工具** 确认是否继续归档
   - 用户确认后再继续

3. **检查任务完成状态**

   读取 tasks 文件（通常为 `tasks.md`），统计 `- [ ]`（未完成）与 `- [x]`（已完成）的数量。

   **若存在未完成任务：**
   - 显示警告，告知未完成任务数
   - 使用 **AskUserQuestion 工具** 确认是否继续归档
   - 用户确认后再继续

   **若不存在 tasks 文件**：跳过任务相关警告，直接继续。

4. **评估 delta spec 同步状态**

   检查 `openspec/changes/<name>/specs/` 下是否存在 delta spec。若无，直接跳过同步提示。

   **若存在 delta spec：**
   - 将每个 delta spec 与对应的基线 spec `openspec/specs/<capability>/spec.md` 对比
   - 判断将要应用的变更（新增、修改、删除、重命名）
   - 汇总后向用户展示

   **提示选项：**
   - 若有变更待同步：「立即同步（推荐）」、「不同步直接归档」
   - 若已同步：「立即归档」、「强制再同步」、「取消」

   若用户选择同步，使用项目 OpenSpec CLI 或直接编辑 spec 将 delta 合并到 `openspec/specs/`。仅在基线 spec 校验通过后才继续。

5. **收尾并清理执行历史**

   优先使用 CLI 归档（若其支持合并 spec）：

   ```bash
   openspec archive <name> -y
   ```

   若 CLI 创建了带日期的归档目录，确认基线 spec 已包含目标需求后，删除该执行历史目录：

   ```bash
   rm -r <dated completed change directory>
   ```

   若因 delta 标题不匹配导致 CLI 无法合并，则手动将 delta 合并到 `openspec/specs/`，校验通过后删除 `openspec/changes/<name>`。

6. **展示汇总**

   输出归档完成摘要：
   - 变更名称
   - 使用的 schema
   - 执行目录是否已删除
   - spec 是否已同步（如适用）
   - 关于警告的说明（未完成的 artifact / 任务）

**成功时的输出格式**

```
## 归档完成

**变更：** <change-name>
**模式：** <schema-name>
**执行历史：** 已删除（spec 同步后）
**Specs：** ✓ 已同步至主 spec（或「无 delta spec」或「跳过同步」）

所有 artifact 完成，所有任务完成。
```

**约束**

- 未提供变更名称时，始终提示用户选择
- 使用 artifact 图（`openspec status --json`）检查完成状态
- 警告不阻塞归档，仅告知并确认即可
- 不要将已完成的执行历史保留为 AI 的长期入口
- 清晰汇总归档结果
- 同步时使用 agent-driven 的 openspec-sync-specs 方式
- 存在 delta spec 时，必须执行同步评估并在提示前展示汇总摘要
