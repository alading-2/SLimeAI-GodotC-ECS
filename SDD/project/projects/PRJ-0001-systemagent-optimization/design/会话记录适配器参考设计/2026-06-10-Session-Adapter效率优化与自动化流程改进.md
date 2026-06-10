# Session Adapter 效率优化与自动化流程改进

> 日期：2026-06-10
> 来源：用户检查 6/8-6/9 对话记录后发现效率问题，要求分析并解决
> 状态：全部 7 项改动已完成 ✅

## 用户原始问题

> 你分析一下9日和8日一些对话的内容，然后找问题，我发现每次对话的git diff次数过多，有问题，应该还有很多问题

> 然后现在session-adapter改了，对应的分析对话记录的skill也要改，你看看这个skill在哪，systemagent的actor应该也要更新

> 我不用openspec了，我自己写了个sdd

> 实际上现在我只改了全局规则，其他东西没改，应该还有不少问题

## 问题分析

### 现象：git diff 次数过多

分析 6/8-6/9 共 11 个 Codex 会话后，发现每次 `apply_patch` 后 AI 都执行 `git diff --check` + `git status --short` + `sdd.py validate`，有时同一组文件修改触发 5-8 轮验证。

### 根因：不是 git diff 本身，而是验证循环

实证数据表明 git diff/status 只占总 tool calls 的 5-10%，真正的效率瓶颈是：

| 问题 | 实证数据 | 根因 |
|---|---|---|
| 验证循环 | 17:45 会话有 20 个循环，sdd.py validate 连续跑 7 次 | AI 每改一个文件就跑完整验证 |
| 文件读放大 | Log.cs 被读 22 次 | DeepThink/Retrospective/实现切换时重新读取 |
| Skill 重复加载 | 同一 SKILL.md 读 3 次 | 每次 context switch 重新加载 |

### 额外发现：OpenSpec 残留

用户已弃用 OpenSpec 改用 SDD，但 skills.yaml 仍注册 4 个 openspec skill，sdd-workflow 还引导"不要删除 OpenSpec"，共 20+ 处残留。

### 额外发现：规则副本未同步

用户修改了 `~/.claude/CLAUDE.md` 的 Git Safety 段（git status 批量化、git push 自动化、OpenSpec → SDD），但 `.ai-config/rules/rules.md` 和其他副本还是旧的。

## 解决方案

### 1. session_adapter 新增效率指标 ✅ 已完成

在 `session_adapter.py` 中新增：

- **验证循环检测**：`detect_verification_loops()` — edit 后连续 2+ 个 validation signal 算一个循环
- **文件读放大检测**：`detect_file_read_amplification()` — 统计每个文件被读取次数，标记 >3 次的
- **VALIDATION_RE 扩展**：从 `git diff --check` 扩展为 `git diff|git status|git log`，更准确统计验证行为
- **derived/efficiency.md**：新增 digest 文件，记录验证循环明细、文件读放大明细、阈值标注
- **index.json efficiency 字段**：新增摘要字段，支持 `list-digests --efficiency-loop N` 过滤

验证：`py_compile` 通过、`digest-codex` 生成 efficiency.md、`list-digests --efficiency-loop 5` 筛选出 5 个会话。

### 2. 规则同步与 OpenSpec 清理 ✅ 已完成

**规则同步**：
- `.ai-config/rules/rules.md` Git Safety 段更新（git status 批量化、git push 自动化、OpenSpec → SDD）
- 同步到 AGENTS.md、CLAUDE.md、.trae/rules/rules.md、.opencode/rules.md

**OpenSpec 清理**：
- skills.yaml 删除 4 条 openspec skill 注册
- sdd-workflow："不删除 OpenSpec" → "openspec/ 已废弃"
- openai.yaml："OpenSpec SDD" → "SDD"
- R002 白名单删除 openspec/
- Documentation.md、project-index 更新措辞

验证：规则文件中 grep "openspec|OpenSpec" 结果为 0。

### 3. 效率引导规则 ✅ 已完成

在 rules.md 新增 `## Efficiency` 段：

```
- 同一轮任务中编辑多个文件时，全部编辑完成后再统一验证；不要每改一个文件就跑一次完整验证。
- DeepThink 或 Retrospective 切换时，如果 5 分钟内刚读过同一文件，引用路径即可，不要重新读取全文。
- 已读取的 skill 文件在同一会话中不需要重新加载。
```

验证：sync 后 AGENTS.md、CLAUDE.md、.trae 副本均包含 Efficiency 段。

### 4. SDD 自动化流程补全 ✅ 已完成

rules.md SDD 完成流程更新：

```
- 完成 SDD task 时，更新 tasks.md / progress.md + commit + push 是默认动作。
- 完成后按影响面运行验证；验证通过后再 commit + push。
- 如涉及框架仓改动，push 后提醒用户更新游戏仓 submodule。
```

验证：sync 后副本均包含新流程。

### 5. Retrospective 集成 ✅ 已完成

- `Actors/Retrospective.md`：新增 `efficiencyInsights` 输出和 Efficiency Analysis 段落
- `systemagent-retrospective/SKILL.md`：新增 efficiency 必读和输出要求

验证：sync 后 .claude/skills、.codex/skills 副本均包含 efficiency 内容。

### 6. 历史 digest 重建 ✅ 已完成

对 6/8-6/9 全部 13 个 Codex session 重新运行 `digest-codex-month`，生成 `derived/efficiency.md`。

验证：`list-digests --efficiency-loop 5` 筛选出 5 个高循环会话。

### 7. 设计文档 ✅ 已完成

- `效率指标设计.md`：Phase 1-4 实施方案（status: implemented）
- `2026-06-10-会话效率与自动化流程问题分析.md`：问题全景分析（status: superseded）
- `2026-06-10-Session-Adapter效率优化与自动化流程改进.md`：本文档（status: current）
- `INDEX.md`：更新状态

## 修改文件清单（全部已完成 ✅）

| 文件 | 改动 | 状态 |
|---|---|---|
| `Workspace/SystemAgent/Tools/session-adapter/session_adapter.py` | VALIDATION_RE 扩展、效率检测函数、render_efficiency、index efficiency 字段 | ✅ |
| `.ai-config/rules/rules.md` | Git Safety 更新、Efficiency 段、SDD 流程补全、删除 OpenSpec 引用 | ✅ |
| `Workspace/SystemAgent/Actors/Retrospective.md` | Efficiency Analysis 段落 | ✅ |
| `.ai-config/skills/systemagent-skill/systemagent-retrospective/SKILL.md` | efficiency 必读和输出要求 | ✅ |
| `Workspace/SystemAgent/Registry/skills.yaml` | 删除 4 条 openspec 注册 | ✅ |
| `.ai-config/skills/sdd/sdd-workflow/SKILL.md` | OpenSpec → 已废弃 | ✅ |
| `.ai-config/skills/ai/ai-feature-development/agents/openai.yaml` | OpenSpec SDD → SDD | ✅ |
| `Workspace/SystemAgent/Tools/skill-test/rules/R002-references-exist.py` | 删除 openspec 白名单 | ✅ |
| `Workspace/SystemAgent/Rules/Documentation.md` | 更新措辞 | ✅ |
| `.ai-config/skills/core/project-index/SKILL.md` | 删除 openspec 行 | ✅ |
| `AGENTS.md`、`CLAUDE.md`、`.trae/rules/rules.md`、`.opencode/rules.md` | 同步副本 | ✅ |
| `Workspace/DocsAI/ChatHistory/2026/06/08-09/` 全部 digest | 重建，新增 derived/efficiency.md | ✅ |
| SDD design 文档（3 个） | 效率指标设计、问题分析、本文档 | ✅ |

## 效率数据（重建后）

验证循环 >5 的会话：

| Session | Loops | Avg Val/Edit | 说明 |
|---|---|---|---|
| 019eaab6-bfe7 | 16 | 1.0 | 游戏开发流程 |
| 019eabea-e8ff | 9 | 2.1 | session-adapter |
| 019eaab6-604d | 9 | 1.0 | Log 设计 |
| 019ea4d9-5ae7 | 6 | 2.5 | SDD-0022 |
| 019eab1a-2051 | 5 | 0.9 | session-adapter |

## 待观察

- 效率规则写入后，下次新会话的验证循环次数是否下降
- Retrospective 是否能自动检查效率并报告
- SDD 完成流程的自动验证 + push 是否顺畅
