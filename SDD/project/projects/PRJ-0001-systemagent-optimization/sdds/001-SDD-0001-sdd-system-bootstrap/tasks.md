# Tasks

## Progress

- **Status**: done
- **Completed**: 7/7
- **Current**: done

## Task List

- [x] T1.1 确认 SDD MVP 事实源和目录边界
  - **Evidence**: 项目共享设计 `design/SDD/SlimeAI-SDD-MVP设计.md` 与本 SDD `design/main.md`
  - **Validation**: 已确定 Workspace/SDD、SDD、.ai-config/skills/sdd 三层边界

- [x] T1.2 实现 SDD CLI 和测试
  - **Files**: Workspace/SDD/sdd.py, Workspace/SDD/tests/test_sdd_cli.py
  - **Validation**: `python3 -m unittest discover Workspace/SDD/tests`

- [x] T1.3 创建 SDD 根目录和首个 SDD 实例
  - **Files**: SDD/README.md, SDD/INDEX.md, SDD/catalog.json, SDD/pending/SDD-0001-sdd-system-bootstrap
  - **Validation**: `python3 Workspace/SDD/sdd.py init-root && python3 Workspace/SDD/sdd.py new ...`

- [x] T1.4 新增 SDD skills 并同步 AI 配置
  - **Files**: .ai-config/skills/sdd/*/SKILL.md
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`

- [x] T1.5 更新 SystemAgent skill catalog
  - **Files**: Workspace/SystemAgent/Catalog/systemagent-catalog.yaml
  - **Validation**: `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static changed`

- [x] T1.6 验证 SDD CLI、根目录和首个实例
  - **Validation**: `python3 Workspace/SDD/sdd.py validate --all`

- [x] T1.7 记录最终进度和交接摘要
  - **Files**: SDD/active/SDD-0001-sdd-system-bootstrap/progress.md
  - **Validation**: `python3 Workspace/SDD/sdd.py show SDD-0001`
