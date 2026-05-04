# DocsAI ProjectState

本文记录 AI-First 文档体系当前状态。保持短，只写能帮助下一个 AI 会话继续工作的内容。

## 当前目标

按当前代码重新校准：

- `DocsAI/Modules/` 模块契约。
- `.claude/skills/*` / `.codex/skills/*` 短入口。
- `Docs/` 人类文档入口和项目索引。
- `Src/**/*.md` 已全部移除，源码入口统一由项目索引指向 .cs 文件。
- 新增 Godot AI Game OS 彻底迁移方向：DataOS、外部源码研究机制、Observation、经验库和 AI 表现复盘。
- 新增 SkilmeAI 多仓库彻底迁移方向：当前 `brotato-my` 降级为迁移输入仓库；长期建设迁入 `SkilmeAI` AI 框架主仓库、独立 Godot 引擎仓库和独立游戏仓库。

## 当前阶段

`AI_First_Docs_Code_Alignment`（二轮收敛）已完成。后续执行：
- `Plans/Architecture/AI_First_Test_Infra_Deep_Docs/`（5 阶段完成）：GodotSkill 测试基础设施 + Movement/AI/Collision 文档深层审计
- `Plans/Architecture/AI_First_Src_Docs_Deep_Audit/`（完成）：剩余 Src .md 全量删除，唯一真相源为 DocsAI + Docs/ + 项目索引
- `Plans/Architecture/框架整体迁移/迁移.md`：当前新的彻底重构总计划，目标是不兼容迁移到 Godot AI Game OS。
- `Plans/Architecture/SkilmeAI_多仓库彻底迁移/README.md`：当前更高层的多仓库迁移计划，规定工作区、仓库边界、NuGet/DLL 阶段、游戏 Skill 和旧仓库归档策略。
- `Plans/Architecture/Godot_AI_Game_OS_Migration/README.md`：当前正在执行的 Godot AI Game OS 迁移控制面计划，负责阶段状态、资产盘点和从当前仓库恢复执行。

## 已完成

- 旧 AI-First 迁移计划已建立 Docs / DocsAI / Skill / 测试 / 门禁基础。
- 本轮已新建：
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/README.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/01_Code_And_Docs_Audit.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/02_DocsAI_Module_Contracts_Update.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/03_Skill_Short_Entry_Refactor.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/04_Src_Docs_Consolidation.md`
  - `Plans/Architecture/AI_First_Docs_Code_Alignment/05_Final_Verification_And_Handoff.md`
- 本轮已补齐 `DocsAI/Modules/FeatureSystem.md` 与 `DocsAI/Modules/DataAuthoring.md`。
- 第二批已修正 Ability 参考示例、Data 测试 README、TestSystem README 和旧迁移计划状态分叉。
- 第三批已修正 CostComponent、Data README、Component 规范、EntityManager 文档和两个测试 README。
- 第四批已修正 Tools 族 ObjectPool、TargetSelector、TimerManager 源码旁文档。
- 第五批已修正 Component / Attack / Collision / UI 源码旁文档和 DocsAI 契约。
- 第 11 批已修正 Movement / AI / Collision 源码旁入口，新增三个 DocsAI 契约和三个短 Skill。
- Skill 当前为 16 个 `SKILL.md`，总计 906 行。
- 新对话交接见 `Plans/Architecture/AI_First_Docs_Code_Alignment/10_Handoff_For_New_Conversation.md`。

## 已完成（本轮）

- 压缩长 Skill 为短入口。
- 清理旧机器绝对路径、旧 Windsurf Skill 入口和过期计划指向。
- 更新本轮计划状态文件和验证结果。
- 新增 `DocsAI/Protocols/AI原生数据层协议.md`，明确 SQLite DataOS + 生成快照为目标。
- 新增 `DocsAI/Protocols/外部资料与源码研究协议.md` 和 `.codex/skills/research-reference-framework/SKILL.md`。
- 新增 `DocsAI/Protocols/AI表现复盘协议.md` 与 `DocsAI/Experience/` 经验库入口。
- 新增 Godot 物理与对象池碰撞经验文档，沉淀 PhysicsServer2D 时序和底层 trace 方向。
- 新增 `Plans/Architecture/SkilmeAI_多仓库彻底迁移/README.md`，固化 SkilmeAI 顶层工作区、多仓库、项目引用 / 本地 NuGet / DLL 三阶段和里程碑。
- 新增 `DocsAI/Protocols/SkilmeAI多仓库AI工作流协议.md`，规定 AI CLI 在框架仓库、游戏仓库和 Godot 引擎仓库之间如何读取上下文与处理跨仓库修改。
- 启动 Godot AI Game OS 迁移控制面：新增 `Plans/Architecture/Godot_AI_Game_OS_Migration/`。
- 创建 `/home/slime/Code/SkilmeAI/` 工作区骨架、`SkilmeAI` 框架仓库骨架和 `Games/BrotatoLike` 游戏仓库骨架。
- 在 `/home/slime/Code/SkilmeAI/SkilmeAI` 创建 `SkilmeAI.GameOS` 最小可构建包。
- `Tools/run-build.sh` 通过，`Tools/run-pack.sh` 已生成本地 NuGet 包。
- 在 `/home/slime/Code/SkilmeAI/Games/BrotatoLike` 创建最小 Godot C# 项目。
- `BrotatoLike` 已通过本地项目引用接入 `SkilmeAI.GameOS`，游戏仓库 build 通过。
- Runtime Data 最小内核已迁入 `SkilmeAI.GameOS`，并同步 Contracts / ApiIndex。
- Runtime Event / Entity / Relationship / Schedule / Resource / Pool / Timer 最小内核已迁入 `SkilmeAI.GameOS`。
- `Data` 变更通知已通过 `EventDataChangeSink` 接入 `RuntimeEntity.Events`。
- 新增 `Tests/SkilmeAI.GameOS.Tests` 和 `Tools/run-tests.sh`，Runtime 行为测试覆盖 Event/Data/Entity/Relationship/Schedule/Pool/Timer/Resource。
- `Games/BrotatoLike` 已建立 `Scenes/Main.tscn`、`Src/Game/Main.cs` 和 `GameBootstrap.RunFrameworkSmokeProbe()`，用于最小框架接入验证，并新增 `Plans/README.md` 作为游戏仓库整体迁移计划。
- Godot 引擎源码权威入口已更新为 `/home/slime/Code/SkilmeAI/Engine/godot-4.6.2-stable`。

## 未完成 / 风险

- `Src/**/*.md` 历史长文档尚未系统迁移，目前只按模块族清理已确认会误导 AI 的内容。
- Movement、AI、Collision 已完成第一轮深层模块族对齐；后续重点转向其它剩余模块和 `Docs/` 长设计归档。
- 旧 `MainTest` 失败不属于本轮文档收敛，需独立 Debug。
- 已有用户工作区改动集中在 Godot 场景测试 Skill、测试文档、Docs README 和项目索引，继续修改时必须合并而不是覆盖。
- DataOS 目前只是目标协议，尚未实现 SQLite schema、生成器和验证命令。
- Godot 底层 trace 目前是方案，尚未修改引擎 fork 和 GodotSkill。
- SkilmeAI 新工作区和多仓库骨架已创建，Runtime 最小内核已迁移；当前仓库继续作为迁移计划和旧资产输入。
- Godot Node Entity / Component 生命周期、Capability、DataOS 尚未迁移到新仓库。

## 推荐入口

- AI 索引：`DocsAI/INDEX.md`
- 当前计划：`Plans/Architecture/Godot_AI_Game_OS_Migration/README.md`
- 彻底迁移计划：`Plans/Architecture/框架整体迁移/迁移.md`
- SkilmeAI 多仓库迁移：`Plans/Architecture/SkilmeAI_多仓库彻底迁移/README.md`
- Godot AI Game OS 执行计划：`Plans/Architecture/Godot_AI_Game_OS_Migration/README.md`
- 多仓库 AI 工作流：`DocsAI/Protocols/SkilmeAI多仓库AI工作流协议.md`
- Skill 映射：`DocsAI/Skills/Skill到DocsAI映射.md`
- 测试矩阵：`DocsAI/Tests/测试矩阵.md`

## 验证方式

```bash
git status --short
find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l
find DocsAI -maxdepth 3 -type f | sort
rg -n "/mnt/[e]|file://[/]|复刻土豆兄[弟]|[.]windsurf" DocsAI .codex/skills Docs/框架/项目索引.md Src -g "*.md"
rg -n "DocsAI/Modules|DocsAI/Tests|DocsAI/Workflows" .codex/skills DocsAI
dotnet build
```
