# DocsAI 统一文档目录方案 v2

> 更新：2026-05-30
> 状态：current
> 范围：`DocsAI/`、`DocsOld/`、`Src/ECS/**/*.md`、`Workspace/DocsAI/` 的长期事实源归属和目录组织。
> 结论：`DocsAI/` 作为 SlimeAI 框架仓统一文档入口，AI-first 设计；`Src/ECS/` 长文档迁移为短指针；`DocsOld/` 框架和思考文档分类迁入；`Plans/` 已删除不恢复；`Workspace/DocsAI/` 保持不变仅交叉引用。

## 0. 与旧方案的关系

本文替代已废弃的 `05-DocsAI集中式ECS文档目录方案.md`（v1，2026-05-30 标记 superseded）。

v1 的核心思想保留：
- 文档主事实源集中到 `DocsAI`
- 按 ECS owner 聚合
- 默认 owner 小目录（Concept / Usage / Tests 三件套）
- `Src/ECS` 不保存长篇概念文档

v2 新增：
- 实际源码清单盘点（`DocsOld/`、`Src/ECS/`、`Workspace/DocsAI/`）
- `思考/` 独立顶级目录处理
- `DocsOld/框架/` 分类迁入映射
- `Plans/` 全部删除，相关引用清理
- `Workspace/DocsAI/` 定位明确化（保持不变，仅交叉引用）
- `Src/ECS/` 短指针规范
- 规则文档（rules.md、CLAUDE.md、AGENTS.md）更新清单
- 分阶段执行计划

## 1. 当前文档清单

### 1.1 DocsAI/（当前框架文档入口）

| 文件 | 行数 | 来源 |
| ---- | ----: | ---- |
| README.md | 49 | 原 DocsNew/README.md |
| ECS框架与AIFirst方向决策.md | 514 | 原 DocsNew/ |
| ECS/Data/Data系统说明.md | 293 | 原 DocsNew/ |

### 1.2 DocsOld/框架/（旧框架文档，待迁入）

共 ~45 个 .md 文件，约 14,000 行。按子目录分类：

**ECS 子系统文档（主体，约 35 个文件）：**

| 子目录 | 文件 | 行数范围 | 状态判断 |
| ---- | ---- | ----: | ---- |
| ECS/Entity/ | Entity架构设计理念(663), EntityManager设计说明(962), Entity受控迁移设计方案(217), 特效系统_Entity生成提示词(412) | 217-962 | 部分 current（已体现在 Src/ECS/ 规范中），部分 history |
| ECS/Component/ | Component数据驱动设计理念(171), IComponent接口说明(274) | 171-274 | 部分 current（已体现在 Component规范.md），部分 history |
| ECS/Data/ | DataSystem_Design(151), Data系统优化分析(37), DataKey静态数据类重构方案(49) | 37-151 | history（已被 DataOS 方案取代） |
| ECS/Event/ | EventBus架构设计(467), Context模式设计(198) | 198-467 | current（Event 系统尚未重写） |
| ECS/Collision/ | 碰撞系统说明(198), 碰撞层级说明(149), 对象池碰撞兼容说明(333), 碰撞问题需要注意(80) | 80-333 | current（碰撞系统文档仍有参考价值） |
| ECS/Ability/ | 技能系统架构设计理念(251), 主动技能实现(53) | 53-251 | current（Ability 系统文档仍有参考价值） |
| ECS/System/Core/ | 系统与状态分层总览(318), 系统配置与预设重构方案(739), 系统生命周期三案设计(246), 系统生命周期与项目状态设计(477) | 246-739 | mixed：总览 current，其他 history |
| ECS/System/FeatureSystem/ | FeatureSystem(389), 三段生命周期(54), 生命周期增强(120) | 54-389 | current（FeatureSystem 尚未重写） |
| ECS/System/DamageSystem/ | 伤害系统设计理念(199) | 199 | current |
| ECS/System/Movement/ | 移动系统设计说明(349), 移动碰撞语义重构(265) | 265-349 | current |
| ECS/System/AI/ | AI系统说明(569) | 569 | current |
| ECS/System/TestSystem/ | TestSystem(443), TestSystem重构方案(380), 对象池信息与视觉预览(111) | 111-443 | mixed |
| ECS/System/ | 实体状态效果系统设计(392), 实体状态管理与AI系统协调方案(188), 特效系统使用指南(166) | 166-392 | current |

**非 ECS 文档（约 10 个文件）：**

| 子目录 | 文件 | 行数 | 状态判断 |
| ---- | ---- | ----: | ---- |
| 工具/对象池/ | 对象池.md | 210 | current |
| 工具/输入/ | InputMap.md | 101 | current |
| 工具/logger/ | logger.md | 139 | current |
| 工具/Data系统/ | DataForge综合架构设计(862) | 862 | history（DataOS 已取代） |
| 文档/Data数据/ | 纯CSharpData存储方案(302), 独立数据配置编辑器(242) | 242-302 | history（DataOS 已取代） |
| 优化/AI/ | ai_first_godot_csharp_ecs_program_overview.md | 780 | history/思考 |
| 优化/ | CSharp与Godot数学工具选型分析(437), 运动与目标选择重构方案(415), 通用数学工具重构执行方案(84) | 84-437 | mixed |
| UI/ | UI架构设计理念.md | 142 | current |
| 项目索引.md | 729 | history |
| README.md | 42 | 旧入口 |

### 1.3 DocsOld/思考/（旧思考文档，待迁入）

| 文件 | 行数 | 内容 |
| ---- | ----: | ---- |
| 碰撞问题/幽灵碰撞问题深度分析.md | 80 | 对象池+碰撞的 Godot 物理引擎 bug 根因分析 |
| ECS架构设计/单位分类与阵营设计.md | 114 | Identity vs Relation 维度分析，避免组合爆炸 |

### 1.4 Src/ECS/（源码旁内嵌文档）

共 35 个 .md 文件，6,870 行。按行数分类：

**长文档（>200 行，应迁移）：**

| 文件 | 行数 | 对应 owner |
| ---- | ----: | ---- |
| Base/Entity/Core/EntityManager.md | 860 | Entity |
| Base/System/TestSystem/README.md | 579 | TestSystem |
| Base/Component/Component规范.md | 394 | Component |
| Base/System/FeatureSystem/README.md | 315 | FeatureSystem |
| AI/README.md | 299 | AI |
| Base/System/Movement/Strategies/数学与物理概念详解.md | 290 | Movement |
| Base/Component/Ability/CostComponent/README.md | 280 | CostComponent |
| Base/Entity/Entity规范.md | 276 | Entity |
| Base/System/Movement/Strategies/README.md | 257 | Movement |
| Base/Component/Movement/EntityMovementComponent说明.md | 253 | Movement |
| Base/Component/Unit/Common/AttackComponent/AttackComponent.md | 249 | AttackComponent |
| Base/System/DamageSystem/README.md | 243 | DamageSystem |
| Base/System/Movement/ScalarDriver/README.md | 242 | Movement |
| Tools/ObjectPool/ObjectPool.md | 236 | ObjectPool |
| Base/System/AbilitySystem/README.md | 213 | AbilitySystem |
| Base/System/Movement/README.md | 209 | Movement |

**中等文档（50-200 行，可选迁移）：**

| 文件 | 行数 | 对应 owner |
| ---- | ----: | ---- |
| Base/System/Core/README.md | 192 | SystemCore |
| Tools/NodeLifecycle/NodeLifecycleManager.md | 172 | NodeLifecycle |
| Tools/Timer/TimerManager.md | 160 | Timer |
| Base/System/MouseSelection/README.md | 151 | MouseSelection |
| Base/System/TargetingSystem/README.md | 140 | TargetingSystem |
| Base/Component/Collision/CollisionComponent/CollisionComponent.md | 131 | Collision |
| Base/Component/Collision/ContactDamageComponent/ContactDamageComponent.md | 130 | Collision |
| UI/README.md | 129 | UI |
| Tools/TargetSelector/README.md | 82 | TargetSelector |
| Base/System/Spawn/SpawnSystem.md | 75 | Spawn |
| Tools/Math/README.md | 57 | Math |
| Tools/Logger/README.md | 54 | Logger |

**短文档（<50 行，保留在 Src/）：**

| 文件 | 行数 | 说明 |
| ---- | ----: | ---- |
| Base/Component/Unit/Common/DataInitComponent/DataInitComponent.md | 46 | 短说明 |
| Base/Component/Collision/PickupComponent/PickupComponent.md | 42 | 短说明 |
| Base/Component/Ability/TriggerComponent/README.md | 31 | 短说明 |
| Base/Component/Ability/CooldownComponent/README.md | 31 | 短说明 |
| Base/Component/Ability/ChargeComponent/README.md | 30 | 短说明 |
| Tools/ParentManager/README.md | 17 | 短说明 |
| Tools/Input/InputManager.md | 5 | 自动生成桩 |

### 1.5 Workspace/DocsAI/（工作区级文档，不迁入）

| 文件 | 作用 |
| ---- | ---- |
| INDEX.md | 工作区文档路由入口 |
| OpenWorkspace.md | 工作区打开方式 |
| MultiGameLayout.md | 多游戏架构 |
| GitSubmoduleWorkflow.md | Git submodule 操作指南 |
| BrotatoLikeOpenSpecExecutionPrompts.md | BrotatoLike 执行提示词 |
| ChatHistory/ | 聊天记录 |
| Reviews/ | 审阅记录 |
| Temp/ | 临时文档 |

定位：工作区级 AI 流程文档，与框架文档分离，保持不变。

## 2. DocsAI 目标目录结构

```text
DocsAI/
  README.md                              # 框架文档统一入口
  INDEX.md                               # AI 路由索引（自动生成或手动维护）
  ECS框架与AIFirst方向决策.md              # [已存在] 方向决策事实源

  ECS/                                    # 框架核心文档
    README.md                             # ECS 文档总览 + 阅读顺序

    Base/                                 # ECS 基础概念
      README.md
      Entity/
        Concept.md                        # 实体概念（来源：Entity规范.md + Entity架构设计理念.md）
        Usage.md                          # 实体使用（来源：EntityManager.md + Entity受控迁移设计方案.md）
        Tests.md                          # [按需] 实体测试
        Archive/                          # [按需] 历史设计文档
      Component/
        Concept.md                        # 组件概念（来源：Component规范.md + Component数据驱动设计理念.md）
        Usage.md                          # 组件使用（来源：IComponent接口说明.md）
      Data/
        Concept.md                        # 数据概念（来源：Data系统说明.md）
        Usage.md                          # 数据使用
      Event/
        Concept.md                        # 事件概念（来源：EventBus架构设计.md）
        Usage.md                          # 事件使用（来源：Context模式设计.md）
      Collision/
        Concept.md                        # 碰撞概念（来源：碰撞系统说明.md + 碰撞层级说明.md）
        Usage.md                          # 碰撞使用（来源：对象池碰撞兼容说明.md + 碰撞问题需要注意.md）
      Relationship/                       # [未来] 关系系统

    System/                               # 系统层文档
      README.md
      Core/
        Concept.md                        # 系统核心概念（来源：系统与状态分层总览.md + System Core README）
        Usage.md                          # 系统核心使用
        Archive/                          # 历史：系统生命周期三案、系统配置与预设
      AbilitySystem/
        Concept.md                        # 技能系统概念（来源：技能系统架构设计理念.md + AbilitySystem README）
        Usage.md                          # 技能系统使用
      FeatureSystem/
        Concept.md                        # 特性系统概念（来源：FeatureSystem.md + FeatureSystem README）
        Usage.md                          # 特性系统使用
        Archive/                          # 历史：三段生命周期、生命周期增强
      DamageSystem/
        Concept.md                        # 伤害系统概念（来源：伤害系统设计理念.md + DamageSystem README）
        Usage.md
      Movement/
        Concept.md                        # 移动系统概念（来源：移动系统设计说明.md + Movement README）
        Usage.md                          # 移动系统使用（来源：Strategies README + ScalarDriver README）
        Archive/                          # 历史：移动碰撞语义重构
      AI/
        Concept.md                        # AI 系统概念（来源：AI系统说明.md + AI README）
        Usage.md
      Collision/                          # 碰撞系统（如与 Base/Collision/ 有重叠可合并）
      Spawn/
        Concept.md                        # 生成系统概念（来源：SpawnSystem.md）
        Usage.md
      TargetingSystem/
        Concept.md                        # 目标选择概念（来源：TargetingSystem README）
        Usage.md
      MouseSelection/
        Concept.md                        # 鼠标选择概念（来源：MouseSelection README）
        Usage.md
      TestSystem/
        Concept.md                        # 测试系统概念（来源：TestSystem.md）
        Usage.md                          # 测试系统使用（来源：TestSystem重构方案.md）
        Archive/                          # 历史：对象池信息与视觉预览
      Status/                             # 状态效果系统（来源：实体状态效果系统设计.md）
        Concept.md
        Usage.md
      Effect/                             # 特效系统（来源：特效系统使用指南.md）
        Concept.md
        Usage.md
      Projectile/                         # [按需] 投射物系统

    Tools/                                # 工具层文档
      README.md
      ObjectPool/
        Concept.md                        # 对象池概念（来源：对象池.md + ObjectPool.md）
        Usage.md
      Timer/
        Concept.md                        # 定时器概念（来源：TimerManager.md）
        Usage.md
      Math/
        Concept.md                        # 数学工具概念（来源：Math README + 数学与物理概念详解.md）
        Usage.md
      TargetSelector/
        Concept.md                        # 目标查询概念（来源：TargetSelector README）
        Usage.md
      Input/
        Concept.md                        # 输入管理概念（来源：InputMap.md）
        Usage.md
      Logger/
        Concept.md                        # 日志概念（来源：logger.md + Logger README）
        Usage.md
      NodeLifecycle/
        Concept.md                        # 节点生命周期概念（来源：NodeLifecycleManager.md）
        Usage.md
      ResourceManagement/                 # [按需] 资源管理
      DataForge/                          # [按需] DataForge（来源：DataForge综合架构设计.md，history）

    UI/                                   # UI 层文档
      Concept.md                          # UI 概念（来源：UI架构设计理念.md + UI README）
      Usage.md
      Archive/

    Component/                            # 通用组件文档
      README.md
      AttackComponent/
        Concept.md                        # 攻击组件（来源：AttackComponent.md）
        Usage.md
      EntityMovementComponent/
        Concept.md                        # 移动组件（来源：EntityMovementComponent说明.md）
        Usage.md
      CostComponent/
        Concept.md                        # 消耗组件（来源：CostComponent README）
        Usage.md
      CooldownComponent/                  # [按需] 冷却组件
      ChargeComponent/                    # [按需] 充能组件
      TriggerComponent/                   # [按需] 触发组件
      DataInitComponent/                  # [按需] 数据初始化组件
      CollisionComponent/                 # [按需] 碰撞组件
      ContactDamageComponent/             # [按需] 接触伤害组件
      PickupComponent/                    # [按需] 拾取组件

  思考/                                   # 设计思考与深度分析
    README.md                             # 思考文档索引
    碰撞问题/
      幽灵碰撞问题深度分析.md              # [从 DocsOld/迁入]
    ECS架构设计/
      单位分类与阵营设计.md                # [从 DocsOld/迁入]
    AI开发范式/
      ai_first_godot_csharp_ecs_program_overview.md  # [从 DocsOld/迁入]
    数学工具/
      CSharp与Godot数学工具选型分析.md     # [从 DocsOld/迁入]
      运动与目标选择通用数学工具重构方案.md # [从 DocsOld/迁入]

  Archive/                                # 历史归档（不作为执行依据）
    README.md                             # 归档说明
    Data历史/                             # 旧 Data 方案
      DataSystem_Design.md                # [从 DocsOld/迁入]
      Data系统优化分析.md
      DataKey静态数据类重构方案.md
      纯CSharpData存储方案.md
      独立数据配置编辑器.md
    Entity历史/                           # 旧 Entity 方案
      特效系统_Entity生成提示词.md         # [从 DocsOld/迁入]
    System历史/                           # 旧 System 方案
      实体状态管理与AI系统协调方案.md      # [从 DocsOld/迁入]
      系统生命周期三案设计.md
      系统生命周期与项目状态设计.md
      系统配置与预设重构方案.md
    通用数学工具重构执行方案.md             # [从 DocsOld/迁入]
    项目索引.md                           # [从 DocsOld/迁入] 旧项目总索引
    Spine说明.md                          # [从 DocsOld/迁入]
    DataForge (数据锻造台) 综合架构设计与实施文档.md  # [从 DocsOld/迁入]
```

### 2.1 目录设计原则

1. **AI-first**：AI 从 `DocsAI/README.md` 进入，按 ECS 分类快速定位到 owner 目录的 `Concept.md` 或 `Usage.md`。
2. **Owner 聚合**：一个功能 owner 一个目录，目录内默认只有 `Concept.md`、`Usage.md`，有测试再加 `Tests.md`。
3. **思考独立**：`思考/` 目录存放设计分析、深度研究、范式探讨等非执行文档，按主题分子目录。
4. **历史归档**：`Archive/` 存放已被取代的设计文档，默认索引不指向它，但 AI 可以查阅以理解历史决策。
5. **Src 精简**：`Src/ECS/` 中的长文档迁移为短指针（10-20 行 README），保留一句话说明 + DocsAI/ 链接 + 关键 API 列表。
6. **Workspace 不变**：`Workspace/DocsAI/` 保持现状，仅在 `DocsAI/README.md` 中交叉引用。

### 2.2 Owner 目录内文档规范

每个 owner 目录默认最多三类文档：

**Concept.md**：回答"这是什么，为什么这样做，边界在哪里"。

```text
# <Owner 名称>

> status: current | history
> sourcePaths: Src/ECS/...
> relatedDocs: DocsAI/ECS/...

## 1. 一句话定位
## 2. 核心概念
## 3. 职责边界
## 4. 依赖关系
## 5. 历史备注 [可选]
```

**Usage.md**：回答"怎么用，改哪里，不能怎么改"。

```text
# <Owner 名称> 使用说明

## 1. 源码入口
## 2. 常见调用流程
## 3. 数据和事件
## 4. 修改边界
## 5. Debug 入口
```

**Tests.md**：只有存在测试或测试场景时才创建。

```text
# <Owner 名称> 测试说明

## 1. 测试代码 / 场景路径
## 2. 运行命令
## 3. PASS 标准
## 4. 常见失败和排查
```

各文件可以很短。没有内容就不写，不需要为了模板凑齐段落。

额外拆分条件：

| 条件 | 处理 |
| ---- | ---- |
| Concept.md 或 Usage.md 超过约 400 行且仍在增长 | 先压缩；压缩后仍复杂再拆 |
| Debug 日志和排查流程独立成体系 | 可拆 Debug.md |
| 历史迁移说明很长但仍需保留 | 可拆 Migration.md 或移入 Archive/ |

## 3. AI 路由规则

### 3.1 默认入口链

```text
AGENTS.md / CLAUDE.md
  -> DocsAI/README.md（框架文档统一入口）
  -> DocsAI/ECS/README.md（ECS 文档总览 + 阅读顺序）
  -> DocsAI/ECS/<分类>/<owner>/Concept.md 或 Usage.md
  -> [按需] DocsAI/思考/<主题>/...
  -> [按需] SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/...
  -> [执行前] 对应 Src/ECS/** 短指针确认源码入口
```

### 3.2 AI 读取规则

| 场景 | 读取目标 |
| ---- | ---- |
| 不懂概念或边界 | `DocsAI/ECS/<owner>/Concept.md` |
| 要改代码或接入功能 | `DocsAI/ECS/<owner>/Usage.md` |
| 要验证或 Debug | `DocsAI/ECS/<owner>/Tests.md`；没有则看 Usage.md 的 Debug 入口 |
| 需要理解设计思考 | `DocsAI/思考/<主题>/` |
| 需要理解历史决策 | `DocsAI/Archive/` 或 Concept.md 的"历史备注"节 |
| 中大型任务设计 | `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/` |
| 方向决策 | `DocsAI/ECS框架与AIFirst方向决策.md` |
| 工作区操作 | `Workspace/DocsAI/` |

### 3.3 不允许的路由

- `history` / `Archive/` 文档不得作为实现依据
- `思考/` 文档不作为代码修改的直接依据，仅作为设计参考
- `Src/ECS/` 短指针不作为详细文档阅读，仅确认源码路径
- 不恢复 `Plans/` 目录或引用

## 4. 文档迁移映射

### 4.1 DocsAI/ 现有文件保留

| 当前文件 | 处理 |
| ---- | ---- |
| README.md | 更新为新的统一入口（当前内容需大幅更新） |
| ECS框架与AIFirst方向决策.md | 保留不动 |
| ECS/Data/Data系统说明.md | 迁移到 `ECS/Base/Data/Concept.md`（或保留原位作为 Concept.md） |

### 4.2 DocsOld/框架/ 迁入映射

**直接迁入（current 文档）：**

| 源文件 | 目标位置 | 处理方式 |
| ---- | ---- | ---- |
| ECS/Entity/Entity架构设计理念.md | ECS/Base/Entity/Concept.md | 合并提炼 |
| ECS/Entity/EntityManager设计说明.md | ECS/Base/Entity/Usage.md | 合并提炼 |
| ECS/Entity/Entity受控迁移设计方案.md | ECS/Base/Entity/Archive/ 或 Concept.md 历史备注 | 视长度决定 |
| ECS/Component/Component数据驱动设计理念.md | ECS/Base/Component/Concept.md | 合并提炼 |
| ECS/Component/IComponent接口说明.md | ECS/Base/Component/Usage.md | 合并提炼 |
| ECS/Event/EventBus架构设计.md | ECS/Base/Event/Concept.md | 合并提炼 |
| ECS/Event/Context模式设计.md | ECS/Base/Event/Usage.md | 合并提炼 |
| ECS/Collision/碰撞系统说明.md | ECS/Base/Collision/Concept.md | 合并提炼 |
| ECS/Collision/碰撞层级说明.md | ECS/Base/Collision/Concept.md | 合并 |
| ECS/Collision/对象池碰撞兼容说明.md | ECS/Base/Collision/Usage.md | 合并提炼 |
| ECS/Collision/碰撞问题需要注意.md | ECS/Base/Collision/Usage.md | 合并 |
| ECS/Ability/技能系统架构设计理念.md | ECS/System/AbilitySystem/Concept.md | 合并提炼 |
| ECS/Ability/其他/主动技能实现.md | ECS/System/AbilitySystem/Usage.md | 合并提炼 |
| ECS/System/Core/系统与状态分层总览.md | ECS/System/Core/Concept.md | 合并提炼 |
| ECS/System/FeatureSystem/FeatureSystem.md | ECS/System/FeatureSystem/Concept.md | 合并提炼 |
| ECS/System/伤害系统设计理念.md | ECS/System/DamageSystem/Concept.md | 合并提炼 |
| ECS/System/Movement/移动系统设计说明.md | ECS/System/Movement/Concept.md | 合并提炼 |
| ECS/System/Movement/移动碰撞语义重构与ArcShot修正方案.md | ECS/System/Movement/Archive/ | 历史归档 |
| ECS/System/AI/AI系统说明.md | ECS/System/AI/Concept.md | 合并提炼 |
| ECS/System/实体状态效果系统设计.md | ECS/System/Status/Concept.md | 合并提炼 |
| ECS/System/特效系统使用指南.md | ECS/System/Effect/Concept.md | 合并提炼 |
| ECS/System/TestSystem/TestSystem.md | ECS/System/TestSystem/Concept.md | 合并提炼 |
| UI/UI架构设计理念.md | ECS/UI/Concept.md | 合并提炼 |
| 工具/对象池/对象池.md | ECS/Tools/ObjectPool/Concept.md | 合并提炼 |
| 工具/输入/InputMap.md | ECS/Tools/Input/Concept.md | 合并提炼 |
| 工具/logger/logger.md | ECS/Tools/Logger/Concept.md | 合并提炼 |

**移入 Archive/（history 文档）：**

| 源文件 | 目标位置 |
| ---- | ---- |
| ECS/Data/DataSystem_Design.md | Archive/Data历史/ |
| ECS/Data/Data系统优化分析.md | Archive/Data历史/ |
| ECS/Data/DataKey静态数据类重构方案.md | Archive/Data历史/ |
| ECS/Entity/特效系统_Entity生成提示词.md | Archive/Entity历史/ |
| ECS/System/实体状态管理与AI系统协调方案.md | Archive/System历史/ |
| ECS/System/Core/其他/系统生命周期三案设计.md | Archive/System历史/ |
| ECS/System/Core/其他/系统生命周期与项目状态设计.md | Archive/System历史/ |
| ECS/System/Core/其他/系统配置与预设重构方案.md | Archive/System历史/ |
| ECS/System/FeatureSystem/优化/*.md | Archive/System历史/ |
| ECS/System/TestSystem/优化/*.md | Archive/System历史/ |
| 文档/Data数据/纯CSharpData存储方案.md | Archive/Data历史/ |
| 文档/Data数据/独立数据配置编辑器.md | Archive/Data历史/ |
| 工具/Data系统/DataForge综合架构设计.md | Archive/ |

**移入思考/（分析/研究文档）：**

| 源文件 | 目标位置 |
| ---- | ---- |
| 优化/AI/ai_first_godot_csharp_ecs_program_overview.md | 思考/AI开发范式/ |
| 优化/CSharp与Godot数学工具选型分析.md | 思考/数学工具/ |
| 优化/运动与目标选择通用数学工具重构方案.md | 思考/数学工具/ |
| 优化/通用数学工具重构执行方案.md | Archive/（已执行完毕） |

**删除或合并：**

| 源文件 | 处理 |
| ---- | ---- |
| 项目索引.md | 删除（729 行旧索引，已被 DocsAI/README.md 取代） |
| README.md | 删除（旧入口） |
| Spine说明.md | Archive/ |
| ECS/System/Data/DataSystem_Design.md | 删除（19 行重定向文件） |

### 4.3 DocsOld/思考/ 迁入映射

| 源文件 | 目标位置 |
| ---- | ---- |
| 碰撞问题/幽灵碰撞问题深度分析.md | 思考/碰撞问题/ |
| ECS架构设计/单位分类与阵营设计.md | 思考/ECS架构设计/ |

### 4.4 Src/ECS/ 迁移规则

**迁移为短指针（长文档，>50 行）：**

所有 >50 行的 Src/ECS/ 文档迁移后，原位替换为 10-20 行的短 README：

```markdown
# <模块名>

> 详细文档见 `DocsAI/ECS/<分类>/<owner>/Concept.md` 和 `Usage.md`。

## 源码入口

- `Src/ECS/<路径>/<文件>.cs`

## 关键 API

- `<API 1>`：...
- `<API 2>`：...
```

**保留在 Src/（短文档，<=50 行）：**

以下文件保留原位不动（足够短，不需要迁移）：

- DataInitComponent.md (46)
- PickupComponent.md (42)
- TriggerComponent/README.md (31)
- CooldownComponent/README.md (31)
- ChargeComponent/README.md (30)
- ParentManager/README.md (17)
- InputManager.md (5)

### 4.5 Workspace/DocsAI/ 处理

保持不变，仅在 `DocsAI/README.md` 中添加交叉引用：

```text
## 工作区文档

工作区级文档（Git submodule、多游戏架构、AI 流程）见 `Workspace/DocsAI/INDEX.md`。
```

## 5. 规则文档更新清单

以下文件中的文档路径引用需要更新：

| 文件 | 需更新的引用 |
| ---- | ---- |
| `.ai-config/rules/rules.md` | `DocsNew/README.md` -> `DocsAI/README.md`；删除 `Plans/` 引用；更新事实源边界 |
| `CLAUDE.md`（同步副本） | 同上（由同步脚本自动处理） |
| `AGENTS.md`（同步副本） | 同上（由同步脚本自动处理） |
| `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md` | 更新 `05-DocsAI` 条目状态 |
| `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md` | 更新阅读顺序中的文档路径 |
| `Workspace/DocsAI/INDEX.md` | 添加框架 DocsAI/ 交叉引用 |
| `Workspace/DocsAI/OpenWorkspace.md` | 更新目录结构描述 |
| `Workspace/SystemAgent/README.md` | 检查是否有 DocsNew/ 或 Plans/ 引用 |
| `DocsAI/README.md` | 完全重写为统一入口 |

## 6. 分阶段执行计划

### Phase 1：建立骨架 + 迁移核心文档

**目标**：DocsAI/ 目录骨架建立，核心规范文档迁移完成，AI 路由可用。

1. 创建 `DocsAI/INDEX.md`（AI 路由索引）
2. 创建 `DocsAI/ECS/README.md`（ECS 文档总览）
3. 创建核心 owner 目录和 Concept.md / Usage.md：
   - `ECS/Base/Entity/`（来源：Entity规范.md + Entity架构设计理念.md + EntityManager设计说明.md）
   - `ECS/Base/Component/`（来源：Component规范.md + Component数据驱动设计理念.md + IComponent接口说明.md）
   - `ECS/Base/Data/`（来源：Data系统说明.md）
   - `ECS/Base/Event/`（来源：EventBus架构设计.md + Context模式设计.md）
   - `ECS/Base/Collision/`（来源：碰撞系统说明.md + 碰撞层级说明.md + 对象池碰撞兼容说明.md）
4. 创建 `DocsAI/思考/README.md` + 迁移思考文档
5. 创建 `DocsAI/Archive/README.md`
6. 更新 `DocsAI/README.md` 为统一入口

### Phase 2：迁移 System / Tools / UI 文档

**目标**：所有 System、Tools、UI 层文档迁移完成。

1. 创建 System 层 owner 目录和 Concept.md / Usage.md：
   - Core、AbilitySystem、FeatureSystem、DamageSystem、Movement、AI、Status、Effect、TestSystem、Spawn、TargetingSystem、MouseSelection
2. 创建 Tools 层 owner 目录和 Concept.md / Usage.md：
   - ObjectPool、Timer、Math、TargetSelector、Input、Logger、NodeLifecycle
3. 创建 UI 层 Concept.md / Usage.md
4. 创建 Component 层 owner 目录（AttackComponent、EntityMovementComponent 等）
5. 迁移 Archive/ 文档

### Phase 3：精简 Src/ECS/ + 更新引用

**目标**：Src/ECS/ 长文档替换为短指针，所有规则文档引用更新。

1. Src/ECS/ 中 >50 行的文档替换为短指针 README
2. 更新 `.ai-config/rules/rules.md` 中的路径引用
3. 运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 同步副本
4. 更新 SDD design INDEX.md 中的条目
5. 更新 Workspace/DocsAI/ 交叉引用

### Phase 4：验证

1. 验证所有 DocsAI/ 内部链接有效
2. 验证 Src/ECS/ 短指针指向正确
3. 验证规则文档引用一致
4. 验证 AI 路由可达

```bash
# 验证 DocsAI/ 内部链接
rg -n '\[.*\]\(.*\)' DocsAI/ --include="*.md" | rg '\]\(\.\./|\]\(\./' | head -50

# 验证 Src/ECS/ 短指针指向
rg -n "DocsAI/" Src/ECS/ --include="*.md"

# 验证规则文档引用
rg -n "DocsNew|Plans/" .ai-config/rules/rules.md CLAUDE.md AGENTS.md 2>/dev/null

# 验证无残留旧引用
rg -n "Docs/框架|DocsNew/" . -g "*.md" --max-depth 3
```

## 7. 风险和护栏

| 风险 | 护栏 |
| ---- | ---- |
| DocsAI 变成大杂烩 | 必须按 ECS owner 聚合，并维护 README.md / INDEX.md |
| 目录拆得太碎 | 默认每个 owner 只放 Concept / Usage / Tests |
| 同一事实重复维护 | Src/ 只能保留短指针，Archive/ 只存放不维护 |
| 旧概念文档误导 AI | 每篇迁移文档必须标 status，Archive/ 从默认索引移除 |
| 合并提炼丢失细节 | 合并时保留"历史备注"节，Archive/ 保留原始文件 |
| 思考文档和执行文档混淆 | 思考/ 目录明确标记为非执行依据 |
| Plans/ 残留引用 | Phase 3 全局 grep 验证 |
| Workspace/DocsAI/ 和 DocsAI/ 定位混淆 | README.md 交叉引用时明确说明边界 |

## 8. 决策表

| 问题 | 决策 |
| ---- | ---- |
| 长期文档主事实源 | `DocsAI/` |
| DocsAI/ 怎么组织 | 按 ECS 分类 + owner 聚合，默认 owner 小目录 |
| 概念文档放哪里 | `DocsAI/ECS/<分类>/<owner>/Concept.md` |
| 使用文档放哪里 | `DocsAI/ECS/<分类>/<owner>/Usage.md` |
| 测试说明放哪里 | 有测试时放 `DocsAI/ECS/<分类>/<owner>/Tests.md` |
| 思考/研究文档放哪里 | `DocsAI/思考/<主题>/` |
| 历史归档放哪里 | `DocsAI/Archive/<分类>/` |
| Src/**/*.md 如何处理 | >50 行迁移为短指针；<=50 行保留原位 |
| DocsOld/框架/ 如何处理 | current 文档提炼迁入 DocsAI/，history 文档移入 Archive/ |
| DocsOld/思考/ 如何处理 | 迁入 DocsAI/思考/ |
| Workspace/DocsAI/ 如何处理 | 保持不变，交叉引用 |
| Plans/ 如何处理 | 已删除，不恢复，清理所有引用 |
| 是否照搬 SlimeAI-AiFirst | 吸收集中事实源和 owner 聚合，不照搬 GameOS 术语 |
