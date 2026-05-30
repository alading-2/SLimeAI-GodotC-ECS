# Tasks

## Progress

- **Status**: done
- **Completed**: 6/6
- **Current**: done

## Task List

- [x] T1.1 确认 `Workspace/SystemAgent` 目标信息架构和本 SDD 范围
  - **Validation**: `design/main.md` 明确目标、非目标、事实源边界和后续依赖
- [x] T1.2 更新 SystemAgent 顶层入口和目录地图
  - **Validation**: `Workspace/SystemAgent/README.md` 与 `Workspace/SystemAgent/INDEX.md` 能表达最新 Workflow / Capability / Role / Artifact / Gate / Subagent 分层
- [x] T1.3 补齐目录级索引和 capability/policy 落点
  - **Validation**: 新增或更新的目录级 `INDEX.md` 能说明能力正文、wrapper policy、subagent policy 和运行配置边界
- [x] T1.4 同步 catalog 与文档治理策略
  - **Validation**: `Catalog/manifest.yaml`、必要 catalog 和 `Policies/DocumentationManagement.md` 与目录结构一致
- [x] T1.5 审计旧入口、重复事实源和不该迁入的内容
  - **Validation**: 旧路径命中分类为 current / generated / historical / migration pointer / violation，violation 已修复或记录阻塞
- [x] T1.6 运行验证并更新项目级恢复信息
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0006`
  - **Validation**: `python3 Workspace/SDD/sdd.py validate --all`
  - **Validation**: `git diff --check`
