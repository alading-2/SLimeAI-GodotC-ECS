# Progress

## Latest Resume

- **Updated**: 2026-06-08 01:53
- **Current Task**: done
- **Last Conclusion**: Math hard cutover completed: ProbabilityTool/DeterministicRandom provide seeded RNG, MyMath and GeometryCalculator old current APIs are removed, DamageFormula and AbilityFormula own capability formulas, Geometry2D remains pure Math.
- **Next Action**: No remaining SDD-0038 work; future formula balance or RNG service work should start from DocsAI/ECS/Tools/Math and the owning capability docs, not from deleted legacy APIs.
- **Open Blockers**: none
## Timeline

### P001 — 2026-06-07 22:39 — resume

- **Context**: 创建 SDD。
- **Conclusion**: 已建立 Math formula + deterministic random hard cutover 执行上下文胶囊。
- **Evidence**: README、sdd.json、design/main.md、tasks.md、progress.md、bdd.md、notes.md、execution-prompt.md 已生成。
- **Impact**: 后续不再重复确认 Math 是否保留或是否保 `MyMath` 聚合类；默认保功能、删杂项形状。
- **Resume**: 从 `execution-prompt.md` 的 T1.1 Readiness Baseline 继续。

### P002 — 2026-06-08 01:16 — resume

- **Context**: 启动或恢复 SDD。
- **Conclusion**: SDD 已进入 active。
- **Evidence**: start command
- **Impact**: 任务可以继续推进。
- **Resume**: 按 tasks.md 的 Current 继续。

### P003 — 2026-06-08 01:21 — decision

- **Context**: 用户或 AI 追加记录。
- **Conclusion**: T1.1 baseline：MyMath 当前仍聚合属性倍率、冷却缩减、护甲倍率和概率；CheckProbability 直接使用 GD.Randf()，Math 测试依赖 10000 次统计波动；Damage Crit/Lifesteal/Dodge/Defense 与 Ability Cooldown/Charge/Trigger 仍直接调用 MyMath；Geometry2D 已承接纯几何，但 TargetSelector 仍通过 GeometryCalculator 公开门面，DocsAI/TargetSelector 和 Math 文档仍把 GeometryCalculator 写成 current/兼容入口；validate SDD-0038 当前 0 error / 1 warning（weak latest resume）。
- **Evidence**: note command
- **Impact**: 作为后续恢复上下文。
- **Resume**: T1.1 baseline：MyMath 当前仍聚合属性倍率、冷却缩减、护甲倍率和概率；CheckProbability 直接使用 GD.Randf()，Math 测试依赖 10000 次统计波动；Damage Crit/Lifesteal/Dodge/Defense 与 Ability Cooldown/Charge/Trigger 仍直接调用 MyMath；Geometry2D 已承接纯几何，但 TargetSelector 仍通过 GeometryCalculator 公开门面，DocsAI/TargetSelector 和 Math 文档仍把 GeometryCalculator 写成 current/兼容入口；validate SDD-0038 当前 0 error / 1 warning（weak latest resume）。

### P004 — 2026-06-08 01:21 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.1。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P005 — 2026-06-08 01:47 — decision

- **Context**: 用户或 AI 追加记录。
- **Conclusion**: T1.2-T1.6 实施：先写 MathRuntimeTest RED 测试并确认 build 因缺失 ProbabilityTool/DeterministicRandom 失败；随后新增 ProbabilityTool/DeterministicRandom，迁移 Geometry2D/SpawnPositionCalculator 随机采样到可注入 RNG，删除 MyMath 聚合类；Damage 公式迁到 DamageFormula，Ability 冷却/充能迁到 AbilityFormula，Damage/Ability 概率调用迁到 ProbabilityTool；删除 GeometryCalculator，TargetQueryEngine 和测试直接调用 Geometry2D；BezierTemplateBuilder 文档标注为 Movement/Ability 模板 helper。build 已通过 0 error。
- **Evidence**: note command
- **Impact**: 作为后续恢复上下文。
- **Resume**: T1.2-T1.6 实施：先写 MathRuntimeTest RED 测试并确认 build 因缺失 ProbabilityTool/DeterministicRandom 失败；随后新增 ProbabilityTool/DeterministicRandom，迁移 Geometry2D/SpawnPositionCalculator 随机采样到可注入 RNG，删除 MyMath 聚合类；Damage 公式迁到 DamageFormula，Ability 冷却/充能迁到 AbilityFormula，Damage/Ability 概率调用迁到 ProbabilityTool；删除 GeometryCalculator，TargetQueryEngine 和测试直接调用 Geometry2D；BezierTemplateBuilder 文档标注为 Movement/Ability 模板 helper。build 已通过 0 error。

### P006 — 2026-06-08 01:47 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.2。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P007 — 2026-06-08 01:47 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.3。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P008 — 2026-06-08 01:47 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.4。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P009 — 2026-06-08 01:47 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.5。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P010 — 2026-06-08 01:47 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.6。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P011 — 2026-06-08 01:48 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.7。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P012 — 2026-06-08 01:48 — decision

- **Context**: 用户或 AI 追加记录。
- **Conclusion**: T1.7/T1.8 验证：dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly 已通过 0 error；ResourceGenerator 已更新 MathRuntimeTest catalog，输出 108 resources 且仅有 5 个既有 duplicate-name warnings；grep gate 显示 MyMath/GeometryCalculator 只剩禁止恢复或已删除说明，代码主链路无 CheckProbability/GD.Randf/new Random residue；sync-ai-config 已执行；skill-test static all 为 Critical:0 / Advisory:0。
- **Evidence**: note command
- **Impact**: 作为后续恢复上下文。
- **Resume**: T1.7/T1.8 验证：dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly 已通过 0 error；ResourceGenerator 已更新 MathRuntimeTest catalog，输出 108 resources 且仅有 5 个既有 duplicate-name warnings；grep gate 显示 MyMath/GeometryCalculator 只剩禁止恢复或已删除说明，代码主链路无 CheckProbability/GD.Randf/new Random residue；sync-ai-config 已执行；skill-test static all 为 Critical:0 / Advisory:0。

### P013 — 2026-06-08 01:48 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.8。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P014 — 2026-06-08 01:49 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.9。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P015 — 2026-06-08 01:49 — decision

- **Context**: 用户或 AI 追加记录。
- **Conclusion**: T1.9 build 与 Godot 证据：dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly 通过 0 error；Godot scene 未运行，原因是 /home/slime/Code/SlimeAI/Games/BrotatoLike 执行 git rev-parse 报 not a git repository，Tools/run-godot-scene.sh 缺失，当前环境 command -v godot 无输出。
- **Evidence**: note command
- **Impact**: 作为后续恢复上下文。
- **Resume**: T1.9 build 与 Godot 证据：dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly 通过 0 error；Godot scene 未运行，原因是 /home/slime/Code/SlimeAI/Games/BrotatoLike 执行 git rev-parse 报 not a git repository，Tools/run-godot-scene.sh 缺失，当前环境 command -v godot 无输出。

### P016 — 2026-06-08 01:53 — decision

- **Context**: 用户或 AI 追加记录。
- **Conclusion**: T1.10 closeout：项目级 README、Core/progress.md、Core/roadmap.md 已同步 SDD-0035~SDD-0038 全部完成状态；准备运行 SDD-0038 validate、git diff --check 和四个目标 SDD 最终复验。
- **Evidence**: note command
- **Impact**: 作为后续恢复上下文。
- **Resume**: T1.10 closeout：项目级 README、Core/progress.md、Core/roadmap.md 已同步 SDD-0035~SDD-0038 全部完成状态；准备运行 SDD-0038 validate、git diff --check 和四个目标 SDD 最终复验。

### P017 — 2026-06-08 01:53 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.10。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P018 — 2026-06-08 01:53 — validation

- **Context**: 任务完成。
- **Conclusion**: Math hard cutover completed: ProbabilityTool/DeterministicRandom provide seeded RNG, MyMath and GeometryCalculator old current APIs are removed, DamageFormula and AbilityFormula own capability formulas, Geometry2D remains pure Math.
- **Evidence**: dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly: 0 error; dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj: 108 resources, 5 existing duplicate-name warnings; sync-ai-config: exit 0; skill-test static all: Critical 0 / Advisory 0; Godot scene blocked because BrotatoLike is not a git repo, runner missing, and godot CLI unavailable.
- **Impact**: 任务已完成并保留归档上下文。
- **Resume**: No remaining SDD-0038 work; future formula balance or RNG service work should start from DocsAI/ECS/Tools/Math and the owning capability docs, not from deleted legacy APIs.
