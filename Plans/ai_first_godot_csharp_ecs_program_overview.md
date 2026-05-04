# AI-First Godot C# ECS 程序开发总说明

## 1. 项目定位

本项目不是传统意义上只给人类程序员使用的 Godot C# ECS 框架，而是一个 **AI-First 的 Godot C# 游戏程序开发框架**。

核心目标是让 AI 能够在明确的架构约束、开发规范、测试命令、日志反馈和文档体系下，独立完成程序功能开发、调试、验证和文档更新。

人类开发者主要负责：

- 定义总体架构方向
- 审查关键设计决策
- 审查核心 ECS 管理逻辑
- 合并稳定功能
- 维护长期技术路线

AI 主要负责：

- 阅读项目文档和 Skill
- 分析现有代码结构
- 开发新 System / Component / Event / Tool
- 编写或更新测试
- 执行构建和运行验证
- 读取日志并修复问题
- 同步更新文档
- 输出变更总结和风险说明

本项目的最终程序目标不是简单地写出一个功能集合，而是建立一套 **AI 可读、AI 可改、AI 可测、AI 可维护** 的游戏程序开发底座。

---

## 2. 核心原则

### 2.1 AI 可理解

所有核心模块必须有清晰文档说明，包括：

- 模块职责
- 输入数据
- 输出行为
- 依赖关系
- 生命周期
- 常见错误
- 测试方式
- 修改注意事项

AI 不应该依赖猜测来理解项目。  
项目结构、命名、调用流程、开发约束都应该通过文档和代码结构明确表达。

### 2.2 AI 可操作

所有重要开发流程都应该可以通过命令行执行，例如：

- 构建项目
- 运行 Godot 测试场景
- 运行 ECS 回归测试
- 校验配置表
- 收集日志
- 检查代码结构

AI 不能只生成代码，还必须能够执行验证命令并根据结果修复问题。

### 2.3 AI 可验证

每个关键模块都应该有对应的验证方式。

最低要求：

- 代码能通过构建
- 核心功能有测试场景或测试脚本
- 修改 System 后能运行对应测试
- 修改 ECS 核心后必须运行回归测试
- 修改数据结构后必须验证旧功能是否受影响

没有验证的功能不算完成。

### 2.4 高内聚、低耦合

框架继续坚持现有方向：

- System 之间尽量解耦
- Component 职责明确
- Event 用于跨系统通信
- 数据和逻辑分离
- 功能通过组合实现
- 可通过配置启用或关闭系统

### 2.5 小步提交，稳定演进

AI 不应该一次性大改大量核心代码。

推荐流程：

1. 先读文档
2. 输出计划
3. 小步修改
4. 构建验证
5. 运行测试
6. 修复问题
7. 更新文档
8. 输出变更总结

---

## 3. 当前阶段目标

当前阶段只关注 **程序开发体系**，暂时不处理：

- 美术资源生成
- 策划数据生成
- 音效音乐生成
- 宣传运营内容
- 大规模玩法设计

当前阶段的核心目标是：

> 让 AI 能够稳定地开发、调试、验证和维护 Godot C# ECS 程序框架。

优先级如下：

1. 稳定 ECS 核心架构
2. 建立自动化构建和测试命令
3. 建立日志收集和调试流程
4. 建立项目级 AGENTS.md / Skill / Docs 体系
5. 建立标准 System / Component / Event 开发流程
6. 建立核心回归测试机制
7. 让 AI 能够按固定流程完成一个程序功能闭环

---

## 4. 程序框架组成

### 4.1 ECS 核心层

ECS 核心层是框架底座，负责：

- Entity 创建、销毁、查询
- Component 添加、移除、获取
- System 注册、启用、禁用、更新
- Event 分发和订阅
- 生命周期管理
- 对象池管理
- 调试信息输出
- 核心错误处理

ECS 核心层必须保持稳定，不允许随意修改。

任何修改 ECS 核心层的任务都必须：

- 先说明修改原因
- 评估对现有 System 的影响
- 运行完整回归测试
- 更新 ECS 核心文档
- 输出风险说明

### 4.2 System 层

System 是游戏行为逻辑的主要承载单位。

System 应该具备：

- 独立职责
- 清晰输入
- 清晰输出
- 可配置启停
- 可测试
- 可文档化

一个 System 不应该隐式依赖其他 System 的内部实现。  
如果需要跨系统通信，优先通过 Event、共享数据结构或明确的服务接口完成。

### 4.3 Component 层

Component 用于描述实体具备的能力、状态或数据。

本项目中的 Component 可以带有一定逻辑，但必须遵守以下原则：

- 不承担大型流程控制
- 不直接管理其他实体的生命周期
- 不绕过 ECS 核心管理器
- 不隐藏重要副作用
- 逻辑必须可被 System 理解和测试

Component 的添加和移除可以代表功能开启和关闭，但该行为必须有文档说明。

### 4.4 Event 层

Event 用于解耦系统之间的通信。

Event 设计原则：

- 命名清晰
- 数据结构简单
- 不包含复杂业务逻辑
- 不依赖具体接收方
- 可追踪、可调试

重要 Event 必须记录在文档中，说明触发时机、携带数据和典型监听者。

### 4.5 Tool 层

Tool 是为了让 AI 和人类都能更高效地维护项目。

Tool 包括但不限于：

- 构建脚本
- 测试脚本
- 日志收集脚本
- 数据校验脚本
- 项目结构检查脚本
- 代码统计脚本
- 回归测试脚本

Tool 的目标不是炫技，而是减少重复劳动，让 AI 能够自动完成验证闭环。

---

## 5. 推荐目录结构

建议逐步整理为以下结构。实际项目可以根据现有代码渐进调整。

```text
/AGENTS.md
/README.md

/Docs/
  ProjectVision.md
  Architecture.md
  ECS-Core-Contract.md
  System-Development-Guide.md
  Component-Development-Guide.md
  Event-Development-Guide.md
  Debugging-Guide.md
  Testing-Guide.md
  DecisionLog.md
  KnownIssues.md
  TestMatrix.md

/Plans/
  CurrentPlan.md
  Backlog.md
  Done.md

/Skills/
  develop-new-system/
    SKILL.md
  add-new-component/
    SKILL.md
  add-new-event/
    SKILL.md
  debug-godot-runtime/
    SKILL.md
  refactor-ecs-core/
    SKILL.md
  run-regression-test/
    SKILL.md
  update-docs-and-tests/
    SKILL.md

/Tools/
  build.sh
  run_godot_test.sh
  run_scene_test.sh
  run_regression.sh
  collect_logs.sh
  validate_project_structure.sh

/Src/
  Core/
  Components/
  Systems/
  Events/
  Tools/
  Debug/

/Tests/
  Unit/
  Integration/
  Scenes/
  Regression/
```

---

## 6. AGENTS.md 职责

`AGENTS.md` 是 AI 进入项目后的第一入口。

它应该告诉 AI：

- 项目是什么
- 当前阶段目标是什么
- 必须先读哪些文档
- 修改代码前必须做什么
- 修改代码后必须跑什么命令
- 哪些地方不能随便改
- 输出结果必须包含什么

建议 AGENTS.md 核心规则：

```text
1. 开始任务前，必须先阅读 AGENTS.md 和相关 Docs。
2. 涉及 ECS 核心修改时，必须阅读 Docs/ECS-Core-Contract.md。
3. 涉及 System 开发时，必须阅读 Docs/System-Development-Guide.md。
4. 涉及 Component 开发时，必须阅读 Docs/Component-Development-Guide.md。
5. 涉及 Event 开发时，必须阅读 Docs/Event-Development-Guide.md。
6. 修改代码后必须运行构建命令。
7. 修改 System 后必须运行对应测试。
8. 修改 ECS 核心后必须运行回归测试。
9. 任务完成前必须更新相关文档。
10. 最终回复必须包含修改文件、验证命令、测试结果、风险点。
```

---

## 7. Skill 体系

Skill 是 AI 执行重复工程任务的标准流程。

每个 Skill 应该包含：

- 适用场景
- 输入要求
- 必须阅读的文件
- 执行步骤
- 必须运行的命令
- 输出格式
- 禁止事项
- 验收标准

### 7.1 develop-new-system

用途：开发新的 System。

流程：

1. 阅读 System 开发规范
2. 查找类似 System
3. 明确输入 Component / Event / Config
4. 明确输出行为
5. 编写 System
6. 编写测试或测试场景
7. 运行构建
8. 运行对应测试
9. 更新文档
10. 输出总结

### 7.2 add-new-component

用途：新增 Component。

流程：

1. 阅读 Component 开发规范
2. 判断 Component 是数据型、状态型还是能力型
3. 检查是否已有类似 Component
4. 编写 Component
5. 更新相关 System
6. 编写测试
7. 更新文档

### 7.3 add-new-event

用途：新增 Event。

流程：

1. 阅读 Event 开发规范
2. 判断是否真的需要 Event
3. 定义事件数据结构
4. 明确触发者和监听者
5. 添加日志追踪点
6. 编写测试
7. 更新事件文档

### 7.4 debug-godot-runtime

用途：根据 Godot 运行日志定位问题。

流程：

1. 运行测试场景
2. 收集日志
3. 提取错误堆栈
4. 定位相关文件
5. 分析原因
6. 小步修复
7. 再次运行验证
8. 输出问题原因和修复说明

### 7.5 refactor-ecs-core

用途：修改 ECS 核心。

这是高风险 Skill。

流程：

1. 阅读 ECS-Core-Contract
2. 说明为什么必须改核心
3. 列出影响范围
4. 制定最小改动方案
5. 修改代码
6. 运行完整回归测试
7. 更新核心文档
8. 输出风险说明

### 7.6 run-regression-test

用途：执行回归测试。

流程：

1. 构建项目
2. 运行核心单元测试
3. 运行关键测试场景
4. 检查日志错误
5. 输出测试结果
6. 如果失败，定位失败模块

### 7.7 update-docs-and-tests

用途：代码完成后同步文档和测试。

流程：

1. 检查本次修改涉及哪些模块
2. 检查对应文档是否过期
3. 检查对应测试是否缺失
4. 补充文档
5. 补充测试
6. 输出文档变更说明

---

## 8. 标准程序开发流程

AI 执行任何程序任务时，都应该遵循以下流程。

### 8.1 任务开始

AI 需要先确认：

- 任务目标
- 涉及模块
- 是否影响 ECS 核心
- 是否需要新增 System
- 是否需要新增 Component
- 是否需要新增 Event
- 是否需要新增测试
- 是否需要更新文档

### 8.2 任务计划

AI 需要输出简短计划：

```text
目标：
影响范围：
实现步骤：
验证方式：
风险点：
```

### 8.3 实现阶段

实现时遵守：

- 小步修改
- 不做无关重构
- 不随意改变公共 API
- 不删除旧功能，除非任务明确要求
- 不隐藏错误
- 不绕过 ECS 核心流程

### 8.4 验证阶段

至少运行：

```bash
./Tools/build.sh
```

根据修改类型追加：

```bash
./Tools/run_scene_test.sh <scene>
./Tools/run_regression.sh
./Tools/collect_logs.sh
```

### 8.5 收尾阶段

任务完成前必须检查：

- 构建是否通过
- 测试是否通过
- 日志是否有错误
- 文档是否更新
- Skill 是否需要更新
- 是否有未完成 TODO
- 是否有风险需要人类审查

---

## 9. 验收标准

一个程序任务只有满足以下条件，才算完成：

```text
1. 代码实现完成。
2. 构建通过。
3. 相关测试通过。
4. 运行日志无新增错误。
5. 文档已更新。
6. 变更范围清晰。
7. 风险点已说明。
8. 人类可以快速审查关键 diff。
```

AI 最终输出必须包含：

```text
完成内容：
修改文件：
验证命令：
验证结果：
风险点：
建议人工重点审查：
```

---

## 10. ECS 核心修改规则

ECS 核心是项目最重要的部分，不能随意修改。

以下内容属于核心高风险区域：

- Entity 管理
- Component 存储
- Component 生命周期
- System 调度
- Event 分发
- 对象池
- 全局状态管理
- 初始化顺序
- 销毁顺序

修改核心前必须回答：

```text
为什么必须改？
有没有不改核心的替代方案？
会影响哪些 System？
会影响哪些 Component？
会影响已有测试吗？
怎么回滚？
```

核心修改完成后必须运行完整回归测试。

---

## 11. 日志和调试规范

日志是 AI 调试项目的主要输入。

日志必须满足：

- 能定位模块
- 能定位 Entity
- 能定位 System
- 能显示关键状态
- 能区分 Info / Warning / Error
- 错误日志要包含足够上下文

推荐日志格式：

```text
[Level][Module][System][EntityId] Message
```

示例：

```text
[Error][ECS][MovementSystem][Entity:1024] Missing PositionComponent
```

AI 调试时必须优先读取日志，不应该凭空猜测。

---

## 12. 测试体系

测试分为四层。

### 12.1 Unit Test

用于测试单个类或小模块，例如：

- Component 行为
- Event 数据结构
- 工具函数
- 配置解析

### 12.2 Integration Test

用于测试多个模块组合，例如：

- Entity + Component + System
- Event 触发和监听
- System 启停
- 数据加载到运行时

### 12.3 Scene Test

用于运行 Godot 场景，验证真实运行效果，例如：

- 移动测试场景
- 碰撞测试场景
- 攻击测试场景
- AI 行为测试场景

### 12.4 Regression Test

用于保护核心功能不被破坏。

至少应该覆盖：

- Entity 创建销毁
- Component 添加移除
- System 注册启停
- Event 分发
- 数据加载
- 对象池复用
- 典型 gameplay loop

---

## 13. 计划文件机制

长任务不能依赖聊天上下文，必须依赖仓库内计划文件。

建议使用：

```text
/Plans/CurrentPlan.md
/Plans/Backlog.md
/Plans/Done.md
/Docs/DecisionLog.md
/Docs/KnownIssues.md
```

`CurrentPlan.md` 应该记录：

```text
当前目标：
当前阶段：
已完成：
未完成：
阻塞问题：
下一步：
验证方式：
```

当上下文清空后，新的 AI 只要读取这些文件，就能继续任务。

---

## 14. 人类审查重点

人类不应该继续逐行审查所有 AI 代码，而应该重点审查：

- ECS 核心逻辑
- 生命周期顺序
- System 耦合关系
- 公共 API 变化
- 隐式副作用
- 是否破坏数据驱动原则
- 是否产生重复架构
- 是否影响长期扩展

AI 最终必须告诉人类：

```text
建议重点审查哪些文件，为什么。
```

这样人类从“代码检查员”升级为“架构负责人”。

---

## 15. 当前最小落地目标

当前阶段不要追求完整自动化，先完成最小闭环。

最小闭环定义：

```text
AI 能开发一个小功能
AI 能构建项目
AI 能运行测试场景
AI 能读取日志
AI 能修复错误
AI 能更新文档
AI 能输出风险点
```

建议第一个实验任务：

```text
让 AI 按 develop-new-system Skill 开发一个小型 System。
例如：HealthSystem、LifetimeSystem、SimpleDamageSystem 或 MovementDebugSystem。
```

验收目标不是功能多复杂，而是验证 AI 是否能完整跑通工程流程。

---

## 16. 后续演进方向

当前只做程序体系，后续可以逐步扩展到：

- 数据驱动玩法生成
- 自动数值验证
- AI 策划表生成
- AI 资源生成
- AI 自动导入资源
- AI 自动生成 Demo
- AI 自动打包测试版本

但这些都应该建立在当前程序闭环稳定之后。

程序闭环没打通之前，不应该急着扩展资源和策划。

---

## 17. 总结

本项目下一阶段的核心不是继续无边界地增加功能，而是建立 **AI 工程化开发能力**。

短期重点：

```text
1. 稳定 ECS 核心。
2. 建立自动构建和测试命令。
3. 建立日志读取和调试流程。
4. 建立 AGENTS.md / Docs / Skills 体系。
5. 让 AI 能独立完成一个程序功能闭环。
```

项目真正的价值不只是 Godot C# ECS，而是：

> 一个让 AI 能够持续开发、验证和维护游戏程序框架的工程体系。

当这个体系跑通后，后续再扩展策划、数据、资源和完整游戏生成，才会真正高效。

---

## 18. 2026-05 深层校准：从 AI-First ECS 到 Godot AI Game OS

当前判断需要继续上提一层：本项目不应只停留在 “AI 辅助开发 Godot C# ECS”，而应迁移成 **Godot AI Game OS**。

核心变化：

- ECS 只是 Runtime 内核，不是项目最高目录语义。
- AI 高频工作对象应是 `Capability / DataOS / Validation / Observation / Protocol / Skill`。
- 项目结构可以彻底重构，不保留旧兼容层。
- 外部成熟框架和 Godot 底层源码要成为项目自我迭代机制的一部分。

## 19. 数据层新判断

`.tres` 和纯 C# DataNew 都不是最终 AI-first 数据形态。

目标应改为：

```text
SQLite Authoring DB -> validate -> generate snapshot -> runtime load
```

理由：

- AI 更擅长查询和批量修改结构化表，而不是维护大段 C# 初始化器或 `.tres` 资源文本。
- 数据不应存运行时对象，应存标量、enum 文本、资源路径、稳定 id 和关系。
- C# 类型安全应来自生成器输出，而不是让 AI 手写所有配置类。
- 运行时仍应读取生成快照，避免在游戏热路径上直接依赖任意 SQL 查询。

对应协议：`DocsAI/Protocols/AI原生数据层协议.md`。

## 20. 外部源码研究机制

框架完善不能只查文档。本轮已确认需要固定化源码研究流程：

- Bevy：学习 World、Schedule、Relationship、Observer、Message、Bundle 的源码组织。
- Flecs：学习 pair relationship、query、observer、REST/stats 对关系和查询的统一消费。
- Arch：学习 C# ECS 的 archetype/chunk、CommandBuffer、Query、事件和测试组织。
- Godot 4.6.2 本地源码：用于分析 PhysicsServer2D、CollisionObject2D、AreaPair、query flush 时序。

对应协议和 Skill：

- `DocsAI/Protocols/外部资料与源码研究协议.md`
- `.codex/skills/research-reference-framework/SKILL.md`

## 21. Observation 与经验库

Godot 对象池碰撞问题说明：业务层日志不够时，AI 需要看到引擎后端状态。

后续 Observation 目标：

- Entity dump。
- Component dump。
- Data snapshot。
- Event trace。
- Relationship graph dump。
- ObjectPool stats。
- PhysicsServer2D trace：RID、ObjectID、shape、space、pair、query flush。

踩坑经验不再散落到聊天记录，统一进入：

- `DocsAI/Experience/踩坑与经验索引.md`
- `DocsAI/Experience/Godot物理与对象池碰撞经验.md`

当 AI 多轮修不好时，不只继续猜代码，必须执行：

- `DocsAI/Protocols/AI表现复盘协议.md`
