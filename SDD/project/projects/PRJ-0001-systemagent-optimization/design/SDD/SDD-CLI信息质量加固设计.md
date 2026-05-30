# SDD CLI 信息质量加固设计

> 本文档记录对 SDD CLI 的下一轮加固设计。重点不是让 SDD 记录更多内容，而是让 SDD 更可靠地保护高价值信息、压缩关键证据、拦住空壳完成，并主动警惕冗余膨胀。

## 1. 结论摘要

当前 SDD MVP 已经具备目录结构、状态流转、任务记录、进度记录、BDD 与 validate 能力，但还没有完全达到“执行 cockpit”的要求。核心问题不是功能少，而是信息质量边界还不够硬：

1. `README.md` 会被 CLI 整体重新生成，这是明确 BUG。它会覆盖用户手写摘要和恢复点，破坏 SDD 作为上下文胶囊的可信度。
2. `done` 命令会写入泛化结论，例如“SDD 已进入 done”，导致真正有价值的 Latest Resume 被冲淡或覆盖。
3. `validate` 目前偏结构校验，能检查文件是否存在、状态是否一致，却难以发现模板残留、弱摘要、弱验证和假完成。
4. 证据记录需要收敛。SDD 不应该记录所有命令输出、所有改动文件、所有 AI 操作细节，而应该只记录未来恢复、审查、验证真正需要的核心信息。
5. “核心文件”不能靠自动列全量 diff 判断，而应根据 SDD 实际效果判断：记录改变系统行为、设计理念、事实源入口或验证门禁的文件；不记录同步副本、机械路径替换和小修小补。

推荐下一步把 SDD CLI 加固目标定义为：

```text
保护核心信息，压缩关键证据，拦住空壳完成，提醒冗余风险。
```

这不是“让 SDD 变重”，而是“让 SDD 更会筛选”。

## 2. 事实源与参考材料

本分析基于以下事实源：

- `Workspace/SDD/sdd.py`
  - `build_readme()` 当前用 `metadata['title']` 生成 `What This SDD Is About`。
  - `save_instance()` 当前会 `write_text(instance / "README.md", build_readme(...))`，导致整个 README 被覆盖。
  - `replace_latest_resume()` 当前会整体替换 `progress.md` 中的 `Latest Resume` 区块。
  - `command_done()` 当前写入固定结论“SDD 已进入 done。”并调用 `save_instance()`。
  - `validate_instance()` 当前主要检查结构、状态、任务、BDD、validation 关键字。
- `Workspace/SDD/docs/SDDFormat.md`
  - 明确 `README.md` 是入口卡片，不写完整设计、不复制任务列表、不复制完整 progress 时间线。
- `Workspace/SDD/docs/CLI.md`
  - 定义 `new/list/show/start/note/task/block/done/validate/index/doctor` 的 MVP 行为。
- `Workspace/SDD/docs/ValidationRules.md`
  - 当前 validation 规则偏结构一致性。
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/SlimeAI-SDD-MVP设计.md`
  - 明确 SDD 是上下文胶囊，`tasks.md` 负责“做没做”，`progress.md` 负责“发现了什么、为什么、下次从哪里继续”。
- `Resources/Skills/superpowers/`
  - `brainstorming` 强调实现前设计确认、上下文探索、多方案比较、设计落盘、自检。
  - `writing-plans` 强调计划可交接、无占位符、精确文件和验证步骤。
  - `verification-before-completion` 强调 evidence before claims。
  - `test-driven-development` 强调行为变更前先有能失败的测试。
  - `systematic-debugging` 强调先找 root cause，再修复，不做症状补丁。

## 3. 从 superpowers 借鉴什么，不照搬什么

### 3.1 借鉴点

superpowers 对 SDD 最大的参考价值不是目录，也不是具体 skill 名称，而是“让 AI 不要跳步”的流程约束。

可吸收为 SDD 原则：

1. **设计先于实现**：中大型任务进入实现前，必须有设计、任务拆解、行为约束和验证思路。
2. **证据先于完成声明**：不能只写“完成了”，必须记录能支撑完成判断的核心验证摘要。
3. **计划必须可交接**：`tasks.md` 不能只是口号，要能指导后续会话继续执行。
4. **自检必须反空壳**：设计文档、计划、验证记录不能保留模板残句或泛化占位。
5. **YAGNI 和简化优先**：不记录对恢复无价值的信息，不把 SDD 变成第二个日志系统。
6. **根因修复**：README 被覆盖不是“摘要写得不够好”，而是 CLI 写策略错误，必须修写入边界。
7. **行为变更要有测试**：README 保护、done 继承 resume、validate 质量规则，都应有单元测试覆盖。

### 3.2 不照搬点

不应照搬以下内容：

1. **不强制所有小任务都进入 SDD**：SlimeAI 的 SDD 只覆盖中大型任务、跨模块重构、AI 配置治理和需要长期恢复的任务。
2. **不强制逐问逐答**：用户已明确偏好“深度思考、详细分析”，SDD 更适合输出结构化确认包。
3. **不使用 superpowers 的默认文档路径**：SlimeAI 已有 `Workspace/SDD`、`SDD/`、`.ai-config/skills/sdd/` 三层边界。
4. **不引入第三方依赖或 Node-first 工具链**：当前 CLI 用 Python 标准库即可。
5. **不自动 commit 计划文档**：SlimeAI 允许 AI 自动 commit，但仍需遵守 Git 边界和范围确认。
6. **不把 superpowers 的完整流程复制到 SDD**：SDD 应吸收流程精华，不制造双重制度。

## 4. 重新定义 SDD 里的“重要信息”

SDD 不是日志系统，而是重要信息压缩器。

一个信息是否应该写入 SDD，先过四个问题：

```text
1. 它能帮助未来恢复任务吗？
2. 它能解释为什么这样设计或这样执行吗？
3. 它能证明任务完成、未完成或被阻塞吗？
4. 它虽然可从 git / 对话 / 终端历史找回，但这里是否需要一个摘要入口？
```

如果四个问题都是否，就不应该写入 SDD。

### 4.1 SDD 应记录

- **设计决策**：为什么选 A，不选 B。
- **任务状态**：哪些任务完成，哪些未完成，当前从哪里继续。
- **关键发现**：实施中发现的设计缺陷、边界风险、事实源冲突。
- **验证摘要**：核心命令、结果摘要、失败原因或通过证据。
- **恢复点**：下一次接手时第一步做什么。
- **核心影响面**：改了哪个系统、事实源、工作流、公共接口或设计理念。
- **追溯入口**：commit id、关键 artifact 路径、重要对话摘要引用。

### 4.2 SDD 不应记录

- 每次读了哪些文件。
- 每个 grep、find、临时命令。
- 所有命令完整输出。
- 所有被修改文件的完整列表。
- `.claude/.codex/.windsurf` 同步副本的逐项变化。
- 机械路径替换产生的大量文件清单。
- 每个 patch 的细节。
- 与最终设计无关的中间思路噪音。

完整细节应交给：

```text
git commit / git diff / AI 对话记录 / 必要 artifact
```

SDD 只保存摘要入口和关键判断。

## 5. “核心证据”应该怎么记录

### 5.1 推荐证据模型

SDD 里的证据应分三层：

| 层级 | 内容 | 是否写入 SDD |
| --- | --- | --- |
| 核心摘要 | 验证命令、结果、关键结论、commit id | 必须或强烈建议 |
| 外部引用 | artifact 路径、场景日志、失败报告、AI 对话摘要 | 按需引用 |
| 完整细节 | 全量 diff、完整命令输出、完整终端历史、全部文件列表 | 默认不写 |

### 5.2 推荐格式

在 `progress.md` 的关键记录或最终 validation 记录中写：

```markdown
- **Evidence**:
  - `python3 Workspace/SDD/sdd.py validate --all`: 0 error / 0 warning
  - `python3 -m unittest discover Workspace/SDD/tests`: 8 tests passed
  - commit `xxxxxxx`: 保存完整 diff
```

如果涉及 Godot 场景、复杂失败或不可复现输出，再补 artifact：

```markdown
- **Artifacts**:
  - `.ai-temp/scene-tests/runs/<run-id>/summary.md`
```

### 5.3 不推荐格式

不建议把完整输出粘贴进 SDD：

```markdown
- **Evidence**: 这里贴了几百行命令输出……
```

也不建议写空泛证据：

```markdown
- **Evidence**: done
- **Evidence**: ok
- **Evidence**: passed
```

这些信息既冗余又不可审查。

## 6. “核心文件”怎么判断

核心文件不是“所有改动文件”。核心文件应根据 SDD 的实际效果判断。

### 6.1 应记录的核心文件

满足以下任一条件，通常应记录：

1. **改变系统行为**
   - 例如 `Workspace/SDD/sdd.py` 改变 CLI 写入、状态流转或 validate 规则。
2. **改变设计理念或长期事实源**
   - 例如 `Workspace/SDD/docs/SDDFormat.md`、`Workspace/SDD/docs/ValidationRules.md`。
3. **改变 AI 工作流入口或门禁**
   - 例如 `.ai-config/skills/sdd/sdd-workflow/SKILL.md`。
4. **改变公共接口或跨模块契约**
   - 例如 GameOS Capability Contract、DataOS schema、SystemAgent workflow/gate。
5. **改变验证方式**
   - 例如新增测试文件、runner、analyzer、CI 门禁。

### 6.2 不应记录为核心文件

以下通常不应列入核心文件，只在必要时概括：

1. **同步副本**
   - `.claude/skills/*`
   - `.codex/skills/*`
   - `.windsurf/skills/*`
2. **机械路径替换文件**
   - 例如旧路径到新路径的大批量替换。
3. **格式、小修和 lint 修正**
   - 空白、换行、标题层级、拼写修复。
4. **自动生成文件**
   - `INDEX.md`、`catalog.json` 等可由 CLI 再生成的内容。

### 6.3 推荐写法

优先记录核心区域，而不是长文件列表：

```markdown
- **Key Areas**:
  - `Workspace/SDD`：CLI 写入边界与 validate 规则。
  - `.ai-config/skills/sdd`：SDD 使用门禁和信息记录规则。
- **Trace**: commit `xxxxxxx`
```

如果必须列文件，控制在 3-8 个：

```markdown
- **Key Files**:
  - `Workspace/SDD/sdd.py` — README 不再整体覆盖，done 继承恢复点。
  - `Workspace/SDD/docs/ValidationRules.md` — 增加质量与冗余检查规则。
  - `.ai-config/skills/sdd/sdd-workflow/SKILL.md` — 明确实现前 readiness check。
```

超过 8 个时，应改为“核心区域 + commit id”。

## 7. README 覆盖是 BUG，不是格式偏好

### 7.1 当前问题

当前 `save_instance()` 每次保存实例都会执行：

```text
write README.md = build_readme(metadata, latest)
```

这导致：

- 用户手写的 `What This SDD Is About` 被标题替换。
- 用户整理过的 `Current Resume` 被泛化结论替换。
- README 作为入口卡片的可信度下降。
- AI 越使用 CLI，越可能破坏高价值人类上下文。

这与 SDD MVP 设计冲突。README 是入口卡片，不是每次状态变化都可完全重建的临时视图。

### 7.2 正确原则

`new` 可以生成 README 初始内容；后续 CLI 写操作只能做“字段级、区块级、可控更新”。

推荐边界：

| 操作 | README 行为 |
| --- | --- |
| `new` | 创建 README 初始模板 |
| `start` | 只更新 Status、Updated、Current Task 等机器字段 |
| `block` | 只更新 Status、Updated、Open Blockers |
| `task` | 默认不改 README 正文，只同步 Current Task |
| `note` | 默认不改 README 正文，只在需要时同步 Latest Resume 摘要 |
| `done` | 只更新 Status、Updated、Current Task，保留人工摘要 |
| `index` | 只重建根 `SDD/INDEX.md` 和 `catalog.json` |

### 7.3 实现方向

不再让 `save_instance()` 负责整体生成 README。拆成三个职责：

1. `save_metadata()`：只写 `sdd.json`。
2. `sync_tasks_header()`：只更新 `tasks.md` 顶部机器字段。
3. `patch_readme_fields()`：只 patch README 中明确可由 CLI 管理的字段。

如果 README 缺失或结构严重损坏，CLI 可以 warning，并提供显式修复命令，例如：

```bash
python3 Workspace/SDD/sdd.py repair-readme SDD-0003 --preview
python3 Workspace/SDD/sdd.py repair-readme SDD-0003 --apply
```

但 MVP 加固阶段不一定要实现 repair 命令。先修复“不覆盖”这个根因。

## 8. done 命令应该保留结论，而不是制造泛化结论

### 8.1 当前问题

`command_done()` 当前会写入固定结论：

```text
SDD 已进入 done。
```

这句话只有状态信息，没有任务价值。它不能回答：

- 实际完成了什么？
- 关键验证是什么？
- 有没有后续风险？
- 如果未来要追溯，应看哪里？

### 8.2 正确行为

`done` 应该：

1. 继承当前 `Latest Resume` 的核心结论。
2. 追加 validation 记录，而不是覆盖历史上下文。
3. 允许用户传入更具体的完成结论。
4. 只把 `current_task` 改为 `done`，不抹掉 `last_conclusion`。

推荐参数：

```bash
python3 Workspace/SDD/sdd.py done SDD-0003 \
  --validation "python3 Workspace/SDD/sdd.py validate SDD-0003: 0 error / 0 warning" \
  --conclusion "CLI README 覆盖 BUG 已修复，validate 增加信息质量检查。" \
  --next-action "无需继续；后续问题创建新 SDD 引用本任务。"
```

如果没有 `--conclusion`：

- 默认使用当前 Latest Resume 的 `Last Conclusion`。
- validation entry 里记录进入 done 的事实。
- 不把结论降级成“SDD 已进入 done”。

## 9. validate 应增强“质量检查”，但不能鼓励冗余

### 9.1 设计原则

`validate` 的目标不是替用户审稿，也不是强迫写长文档，而是发现明显低质量和明显失真：

```text
结构错 -> error
状态错 -> error
假完成 -> error
模板残留 -> done 时 error，active 时 warning
弱摘要/弱证据 -> warning
冗余风险 -> warning
```

### 9.2 反空壳检查

建议新增规则：

| Rule | Severity | 检查 |
| --- | --- | --- |
| `SDD015` | warn/error | 模板残留。done 状态仍有模板句应为 error。 |
| `SDD016` | warn | README 摘要过弱，例如等于标题、过短或仍是一句话模板。 |
| `SDD017` | warn | Latest Resume 过弱，例如只有“done/ok/完成/无”。 |
| `SDD018` | warn | validation 记录过弱，例如只有 passed/ok/done，没有命令和结果摘要。 |
| `SDD019` | warn/error | done SDD 缺少可追溯入口。没有 validation 摘要、commit、artifact 或核心影响面时 warning；完全无 validation 已由 `SDD013` error。 |

### 9.3 反冗余检查

为了避免 validate 变成“要求记录更多”的工具，还应增加冗余风险提醒：

| Rule | Severity | 检查 |
| --- | --- | --- |
| `SDD020` | warn | README 过长，继续保留当前 100 行提醒。 |
| `SDD021` | warn | progress 记录很多，但 Latest Resume 仍然弱，说明没有压缩恢复点。 |
| `SDD022` | warn | artifacts 里有多个文件，但 progress/notes 没有引用。 |
| `SDD023` | warn | Key Files 列表过长，疑似复制 git diff。 |
| `SDD024` | warn | notes 过长且无小标题索引，疑似变成第二个设计文档。 |

### 9.4 为什么这些先做 warning

除结构错误、状态错误、done 保留模板外，大部分质量问题应先 warning，不应一开始强制 error。

原因：

- SDD 的任务类型差异大。
- 有些 SDD 是纯设计、纯研究或纯流程治理。
- 过度严格会迫使 AI 写冗余内容来“过校验”。
- warning 更适合引导信息质量，而不是制造形式主义。

## 10. Workflow readiness gate

SDD 加固不只改 CLI，还要改 AI 使用方式。

中大型任务实现前应满足 readiness：

```text
1. 有明确目标和边界。
2. design/ 有非模板设计，且 design/INDEX.md 指向 main/current。
3. tasks.md 有可执行拆分，不是空 checkbox。
4. bdd.md 有场景，或明确说明不适用原因。
5. progress.md 的 Latest Resume 能说明当前从哪里继续。
6. validate 目标 SDD 没有 error。
```

注意：readiness 不是要求文档很长，而是要求文档有恢复价值。

`sdd-workflow` skill 应强化：

- 实现前检查 readiness。
- 不把 SDD 写成流水账。
- 每个任务组后只记录关键结论和验证摘要。
- 完成前必须有新鲜验证证据。

`sdd-management` skill 应强化：

- CLI 写操作不得覆盖用户维护的高价值区块。
- `README.md` 是人工可维护入口卡片。
- `progress.md` 是核心结论与恢复点，不是完整命令日志。
- `artifacts/` 只保存必要证据，不做默认输出垃圾桶。

## 11. 推荐实施范围

### 11.1 必做

1. **修复 README 整体覆盖 BUG**
   - 修改 `save_instance()` 职责。
   - 新增 README 字段级 patch。
   - 单元测试证明手写摘要不会被 `start/task/note/block/done` 覆盖。

2. **修复 done 泛化结论**
   - `done` 默认继承现有 Latest Resume。
   - 增加 `--conclusion`、`--next-action` 参数。
   - validation entry 记录验证摘要，不覆盖核心结论。

3. **增强 validate 信息质量检查**
   - 模板残留。
   - README 摘要过弱。
   - Latest Resume 过弱。
   - validation 过弱。
   - done 缺少追溯入口。

4. **增强 validate 冗余风险检查**
   - Key Files 过长。
   - artifacts 未引用。
   - notes 过长。
   - progress 记录多但 Latest Resume 弱。

5. **更新 SDD 文档和 skill**
   - `Workspace/SDD/docs/SDDFormat.md`
   - `Workspace/SDD/docs/CLI.md`
   - `Workspace/SDD/docs/ValidationRules.md`
   - `.ai-config/skills/sdd/sdd-workflow/SKILL.md`
   - `.ai-config/skills/sdd/sdd-management/SKILL.md`

6. **补测试**
   - README 保留测试。
   - done 继承 resume 测试。
   - validate 质量 warning/error 测试。
   - validate 冗余 warning 测试。

### 11.2 可选但不优先

1. `repair-readme` 命令。
2. `validate --strict-quality` 模式。
3. `sdd evidence add` 子命令。
4. 自动从 git 读取 commit id。
5. artifact 引用索引。

这些可以以后再做，不应阻塞本轮加固。

### 11.3 不做

1. 不自动保存所有命令输出。
2. 不自动列出所有 git diff 文件。
3. 不自动抓取 AI 对话全文。
4. 不把 artifacts 变成默认日志目录。
5. 不要求所有 SDD 都必须有完整 Key Files 列表。
6. 不引入第三方依赖。
7. 不大改目录结构。

## 12. 建议任务拆分

后续可创建一个正式 SDD，例如：

```text
SDD CLI Information Quality Hardening
```

建议任务：

```markdown
- [ ] T1.1 为 README 覆盖 BUG 写失败测试。
- [ ] T1.2 拆分 save_instance，改为字段级 patch README。
- [ ] T1.3 验证 start/task/note/block/done 不覆盖手写 README 摘要。
- [ ] T2.1 为 done 继承 Latest Resume 写失败测试。
- [ ] T2.2 增加 done --conclusion 和 --next-action。
- [ ] T2.3 验证 done 记录 validation 但保留核心结论。
- [ ] T3.1 增加模板残留、弱 README、弱 resume、弱 validation 检查。
- [ ] T3.2 增加冗余风险 warning。
- [ ] T3.3 更新 ValidationRules、CLI、SDDFormat 文档。
- [ ] T4.1 更新 .ai-config/skills/sdd 源 skill。
- [ ] T4.2 运行 ai-config sync 和 skill-test。
- [ ] T5.1 运行 SDD CLI 单元测试、validate --all、py_compile。
- [ ] T5.2 更新当前 SDD progress，记录核心验证摘要和追溯入口。
```

## 13. 验收标准

本轮加固完成后，应满足：

1. 用户手写 `README.md` 摘要不会因 CLI 状态变化被覆盖。
2. `done` 不再把高价值结论替换为“SDD 已进入 done”。
3. `validate` 能识别明显模板残留和假完成。
4. `validate` 能提醒弱摘要、弱验证和冗余风险。
5. SDD 文档明确“核心证据”和“核心文件”的判断标准。
6. SDD skill 明确实现前 readiness gate。
7. 所有行为变更有单元测试覆盖。
8. 验证命令至少包括：

```bash
python3 -m unittest discover Workspace/SDD/tests
python3 Workspace/SDD/sdd.py validate --all
python3 -m py_compile Workspace/SDD/sdd.py Workspace/SDD/tests/test_sdd_cli.py
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all
```

如修改 `.ai-config/skills/sdd/`，还必须运行：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
```

## 14. 最终判断

SDD 现在最需要的不是“记录更多”，而是“记录得更准”。

真正有价值的 SDD 应该让未来接手者快速知道：

```text
为什么做、怎么设计、做到哪、关键发现是什么、怎么验证、从哪里追溯完整细节。
```

如果 SDD 记录了大量命令输出、全部文件列表和琐碎过程，却覆盖了 README 摘要和 Latest Resume，那它反而降低了恢复价值。

因此下一轮加固的核心原则是：

```text
CLI 不覆盖人类高价值信息；
validate 拦住空壳和假完成；
progress 记录关键结论，不记录流水账；
证据只写核心摘要，完整细节交给 git 和 artifact；
核心文件按影响判断，不按 diff 数量判断。
```
