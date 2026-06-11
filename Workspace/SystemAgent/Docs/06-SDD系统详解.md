# SDD 系统详解

## 什么是 SDD

SDD（Software Design Document）是 SlimeAI 的**任务上下文胶囊系统**。它管理中大型工程任务的设计、执行、验证和恢复。

### 一句话定位

**让 AI 跨会话记住"做到哪了、怎么做的、下一步做什么"。**

### 解决的核心问题

AI 会话是无状态的。没有 SDD：
- 第一次会话做了设计，第二次会话不记得设计
- 改了一半的代码，下次来不知道改了什么
- 验证过的结论，下次来重新验证
- 阻塞的问题，下次来不知道为什么阻塞

SDD 通过结构化文件解决这些问题。

## SDD 文件结构

```
SDD/
├── INDEX.md                ← 全局索引（CLI 自动生成）
├── catalog.json            ← 机器可读索引
├── templates/              ← 模板目录
└── project/
    └── projects/
        ├── PRJ-0001-systemagent-optimization/
        │   ├── README.md   ← 项目入口
        │   ├── design/     ← 项目级设计（所有 SDD 共享）
        │   └── sdds/       ← 子 SDD 目录
        │       ├── 001-SDD-0001-xxx/
        │       ├── 002-SDD-0003-xxx/
        │       └── ...
        └── PRJ-0002-ecs-framework-refactor/
            ├── README.md
            ├── design/
            └── sdds/
```

### 单个 SDD 实例的文件

```
<SDD-name>/
├── README.md               ← 入口卡（状态、范围、摘要、阅读顺序、恢复点）
├── sdd.json                ← CLI 元数据（id, status, progress, links）
├── tasks.md                ← 任务清单（带验证命令的 checkbox）
├── progress.md             ← 时间线（决策、验证、阻塞、Latest Resume）
├── design/                 ← 设计文档（INDEX.md + main.md + 其他）
│   ├── INDEX.md
│   └── main.md
├── bdd.md                  ← 行为场景（Given/When/Then）
├── notes.md                ← 参考和待定问题
├── execution-prompt.md     ← 新会话执行提示词（可选）
└── artifacts/              ← 验证产物（可选）
```

## SDD CLI

```bash
# 创建
python3 Workspace/SDD/sdd.py project-new <project-name>
python3 Workspace/SDD/sdd.py new <sdd-id> <title>

# 管理
python3 Workspace/SDD/sdd.py list
python3 Workspace/SDD/sdd.py show <sdd-id>
python3 Workspace/SDD/sdd.py start <sdd-id>
python3 Workspace/SDD/sdd.py task <sdd-id> "task description"
python3 Workspace/SDD/sdd.py note <sdd-id> "note content"
python3 Workspace/SDD/sdd.py block <sdd-id> "block reason"
python3 Workspace/SDD/sdd.py done <sdd-id>

# 验证
python3 Workspace/SDD/sdd.py validate <sdd-id>
python3 Workspace/SDD/sdd.py validate --all
python3 Workspace/SDD/sdd.py index
python3 Workspace/SDD/sdd.py doctor
```

## 当前状态

- **PRJ-0001**（SystemAgent 优化）：11 个 SDD，全部 done
- **PRJ-0002**（ECS 框架重构）：28 个 SDD，26 done，2 blocked
  - SDD-0027（Timer 调度器重写）：blocked，缺 Godot runner
  - SDD-0040（Log AI-first 观测硬切）：blocked，18/19 任务完成，缺 Godot runner

## SDD 膨胀问题（重点分析）

### 问题现象

后期 SDD 的 `progress.md` 严重膨胀。以 SDD-0040（Log）为最极端案例：

| 文件 | 行数 | 问题 |
|------|------|------|
| progress.md | 266 行 | 32 个时间线条目，大量样板重复 |
| design/ | 2683 行 | 从项目级 design 导入的快照（会过时） |
| execution-prompt.md | 212 行 | 非原始模板的一部分 |
| bdd.md | 91 行 | 合理 |
| tasks.md | 61 行 | 合理（19 个任务） |
| README.md | 42 行 | 合理 |

### 膨胀原因

1. **样板任务完成条目**：P005-P010 是 6 个格式完全相同的"完成任务 T1.X"条目，没有任何有区分度的信息
2. **Conclusion 和 Resume 重复**：验证条目中 Conclusion 和 Resume 包含完全相同的文字（5-7 行），纯重复
3. **验证证据过于详细**：嵌入完整命令行和完整输出摘要，违反了 SDD 格式规范中"不推荐：完整终端输出"的指导
4. **design 快照过时**：SDD-0040 导入了项目级 design 的完整快照（2683 行），README 注明是"历史快照"，项目级 design 才是权威源

### AI-First 视角分析

从 AI-first 角度看，SDD 膨胀的本质是：**AI 不知道什么信息是"恢复上下文"必需的，什么只是过程记录。**

AI 倾向于记录一切（因为它的训练是"更多信息更好"），但实际上：
- 恢复上下文只需要：Latest Resume + 最后 2-3 个关键决策 + 当前阻塞项
- 过程记录只需要：关键转折点（不是每完成一个任务都记录）
- 验证证据只需要：通过/不通过 + 产物路径（不需要完整命令输出）

### 建议的简化方向

1. **progress.md 行数上限**：建议不超过 100 行，超过时压缩早期条目
2. **禁止 Conclusion/Resume 重复**：Resume 只写"下一步做什么"，不重复 Conclusion
3. **任务完成条目合并**：连续的任务完成可以合并为一个条目
4. **验证证据引用化**：只写产物路径，不嵌入命令输出
5. **design 不导入快照**：SDD 只引用项目级 design，不复制

---

## SDD 与 TDD 的关系

SDD 的 `bdd.md` 定义行为场景，TestDesigner 将 BDD 转化为验证场景，Implementer 将验证场景实现为测试代码。

```
SDD bdd.md (Given/When/Then)
    ↓
TestDesigner (5 字段标准答案)
    ↓
Implementer (测试代码)
    ↓
ValidationSession (artifact)
```

**当前问题**：这个链条的连接是手动的，没有自动追溯性。
