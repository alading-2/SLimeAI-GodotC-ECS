# AI-First 测试基础设施与文档深层对齐

本文是 `AI_First_Docs_Code_Alignment` 完成第 11 批后的下一阶段计划。目标：
- 完善 `.claude/skills/GodotSkill` 的 CLI 测试基础设施（脚本、Skill、AI 自主 Debug 闭环）
- 按 Handoff 推荐顺序深层审计 Movement / AI / Collision 源码旁文档并迁移
- 整理 `Docs/` 人类文档归档

## 前置状态

- `AI_First_Docs_Code_Alignment` 第 1-11 批已完成（含 Movement/AI/Collision DocsAI 契约和短 Skill）
- `.codex/skills/*` 已压缩为短入口（总计 ~906 行）
- `dotnet build` 通过，0 Error
- 旧路径扫描无命中
- `.claude/skills/GodotSkill` 重命名和 SKILL.md 更新进行中（未提交）

## 执行阶段

### 阶段 1：GodotSkill 测试基础设施完善
**状态：进行中**

- 完成 `.claude/skills/GodotSkill` 重命名（godot-scene-test → GodotSkill）
- 新增 Shell 快捷脚本 `run-test.sh`、`analyze-logs.sh`
- 完善 SKILL.md：Shell 快速命令、AI 自主 Debug 循环、Visual Screenshot 等
- 更新 `.claude/settings.local.json` 权限
- 提交 `.claude` 侧改动（不碰 `.codex`）

### 阶段 2：Movement 文档深层审计与迁移
**状态：待开始**

- 审计 `Src/ECS/Base/System/Movement/**/*.md` 长设计内容
- 判断哪些迁入 `Docs/`（人类设计说明），哪些下沉到 `DocsAI/Modules/Movement.md`
- 压缩源码旁 README 为 API 入口
- 更新项目索引

### 阶段 3：AI 文档深层审计与迁移
**状态：待开始**

- 审计 `Src/ECS/AI/README.md` 与行为树源码
- 确认 AI 节点是否需要独立 `DocsAI/Modules/AI.md` 扩展
- 压缩源码旁长 README

### 阶段 4：Collision 文档深层审计与迁移
**状态：待开始**

- 审计 Collision 文档族和 `Docs/框架/ECS/Collision/*`
- 统一 CollisionComponent / Hurtbox / MovementCollision / ContactDamage 分工
- 压缩源码旁文档

### 阶段 5：Docs/ 人类文档归档整理
**状态：待开始**

- 系统归档 `Docs/` 中散落设计文档
- 建立清晰的 Docs/ 目录索引
- 与 DocsAI/ 建立明确边界

## 约束

- 不修改 `.codex/skills/`（用户自行处理）
- 不修改运行时代码
- 不修旧 `MainTest` 历史失败
- 每个阶段完成后更新 Progress.md / Done.md / Backlog.md
- 修改代码前后运行 `git status --short`

## 状态文件

- `Progress.md` — 当前进度和阶段状态
- `Done.md` — 已完成内容记录
- `Backlog.md` — 剩余和后续事项
