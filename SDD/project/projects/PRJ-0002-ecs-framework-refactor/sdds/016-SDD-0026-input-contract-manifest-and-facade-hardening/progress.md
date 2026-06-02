# Progress

## Latest Resume

- **Updated**: 2026-06-02
- **Current Task**: done
- **Last Conclusion**: Input Contract Manifest And Facade Hardening completed; 2026-06-02 docs governance cleanup made `DocsAI/ECS/Tools/Input/README.md` the owner main entry and demoted `Concept.md / Usage.md / InputMap.md` to optional auxiliary docs.
- **Next Action**: No further work in SDD-0026. Future Input work should use a new SDD for ControllerGlyphProfile, runtime InputContext, or manifest auto-validation.
- **Open Blockers**: none
## Timeline

### P001 — 2026-06-01 19:56 — resume

- **Context**: 创建 SDD。
- **Conclusion**: 已建立任务上下文胶囊。
- **Evidence**: README、sdd.json、design、tasks、progress、bdd、notes 已生成。
- **Impact**: 后续围绕 tasks.md 和 progress.md 记录执行。
- **Resume**: 从 README 的 Current Resume 继续。

### P002 — 2026-06-01 — execution-capsule-filled

- **Context**: 用户要求基于 Input Contract 研究和文档收口生成 SDD 以及提示词。
- **Conclusion**: 本 SDD 已填充执行级设计、任务、BDD 和 `execution-prompt.md`。前置事实源包括 DocsAI Input manifest、项目级 `design/Tool/Input/` 设计包、`project.godot` 最小键盘备用绑定和 tools skill 路由。
- **Evidence**: `design/main.md` 记录 facade/调用点迁移方案；`tasks.md` 拆为 T1.1~T1.7；`bdd.md` 覆盖改键、Ability、Targeting、Controller glyph 和 manifest 对齐场景；`execution-prompt.md` 提供下一轮执行入口。
- **Impact**: SDD-0026 可直接作为后续 Input runtime 语义硬化任务的恢复入口，不再依赖聊天上下文。
- **Resume**: 下一轮先读 `execution-prompt.md`，再从 T1.1 readiness baseline 开始；注意当前工作区有大量既有 SDD-0025/目录迁移改动，不要回滚或混入无关变更。

### P003 — 2026-06-01 20:00 — resume

- **Context**: 启动或恢复 SDD。
- **Conclusion**: SDD 已进入 active。
- **Evidence**: start command
- **Impact**: 任务可以继续推进。
- **Resume**: 按 tasks.md 的 Current 继续。

### P004 — 2026-06-01 22:33 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.3。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P005 — 2026-06-01 22:33 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.2。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P006 — 2026-06-01 22:33 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.5。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P007 — 2026-06-01 22:33 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.4。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P008 — 2026-06-01 22:37 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.6。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P009 — 2026-06-01 22:37 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.7。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P010 — 2026-06-01 22:45 — validation-summary

- **Context**: 收口 Input Contract Manifest And Facade Hardening。
- **Conclusion**: 业务输入调用点已从按钮名 API 迁到语义 facade，旧 `IsX/IsLeftBumper/IsRightBumper/IsCancel/IsPause` 调用在 Ability/Targeting/UI 业务层不再出现；Debug/Test 裸输入仍按设计作为例外保留。
- **Evidence**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 0 warning / 0 error；`bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 完成；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only` 输出 39 skills Critical 0 / Advisory 0；`rg -n "InputManager\\.Is(LeftBumper|RightBumper|X|Cancel|Pause)\\(" Src/ECS/Capabilities Src/ECS/UI Src/ECS/Runtime` 无输出；`git diff --check` 目标范围无输出。
- **Impact**: AI 改键入口现在可从 `InputMap.md` 的业务 action/context 追到 `InputManager` 语义方法和消费者，不再需要从 `BtnX` 等按钮名猜业务功能。
- **Resume**: 若需要进一步硬化，下一步应做 manifest 自动校验脚本或 runtime `InputContext`，不要在本 SDD 内继续扩范围。

### P011 — 2026-06-01 22:37 — validation

- **Context**: 任务完成。
- **Conclusion**: Input Contract Manifest And Facade Hardening completed: InputManager now exposes business semantic facade methods, Ability/Targeting/PauseMenu consumers use those methods, DocsAI Input usage and tools skill source are synchronized.
- **Evidence**: dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly: 0 warning / 0 error; python3 Workspace/SDD/sdd.py validate SDD-0026: 0 error / 0 warning; python3 Workspace/SDD/sdd.py validate --all: 0 error / 0 warning; bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only: 39 skills Critical 0 / Advisory 0; grep gate for old business InputManager button APIs in Capabilities/UI/Runtime: no output; git diff --check target ranges: no output.
- **Impact**: 任务已完成并保留归档上下文。
- **Resume**: No further work in SDD-0026. Future Input work should use a new SDD for ControllerGlyphProfile, runtime InputContext, or manifest auto-validation.

### P012 — 2026-06-02 — docs-structure-governance

- **Context**: 用户确认执行 DocsAI 规则调整，要求不要再强制 `Concept.md / Usage.md / InputMap.md` 三件套，Input 文档要讲清当前用法、按键功能和后续扩展。
- **Conclusion**: Input DocsAI 主入口收口到 `DocsAI/ECS/Tools/Input/README.md`；`Concept.md`、`Usage.md`、`InputMap.md` 改为可选辅助页；DocsAI 管理规则、ECS README、思考入口、tools skill 和项目级 Input 设计包均同步为灵活 owner 文档组织。
- **Evidence**: `DocsAI/ECS/Tools/Input/README.md` 新增/更新为主入口；`DocsAI/ECS/README.md`、`DocsAI/管理/DocsAI统一管理与索引规则.md`、`.ai-config/rules/rules.md` 和 `.ai-config/skills/core/tools/SKILL.md` 已改为 README 优先、辅助文档可选。
- **Impact**: 后续 owner 不再为了模板强行拆 `Concept/Usage/Tests/InputMap`；Input manifest 可在 README 或可选表格文档中维护。
- **Resume**: 本 SDD 仍保持 done；后续 runtime InputContext、ControllerGlyphProfile、manifest 自动校验另建 SDD。
