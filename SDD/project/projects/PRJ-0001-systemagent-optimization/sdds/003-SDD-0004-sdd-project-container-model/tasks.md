# Tasks

## Progress

- **Status**: done
- **Completed**: 5/5
- **Current**: done

## Task List

- [x] T1.1 定义项目 CLI 与 metadata 状态模型测试
  - **Validation**: `python3 -m unittest discover Workspace/SDD/tests` 曾按 RED 失败，证明测试覆盖新行为
- [x] T1.2 实现 `SDD/project` 项目容器 CLI、索引和 catalog schema v2
  - **Validation**: `python3 -m unittest discover Workspace/SDD/tests` 通过
- [x] T1.3 改造 `start/block/done` 为 metadata-first，不再移动 SDD 目录
  - **Validation**: `test_start_block_done_update_metadata_without_moving_directory` 通过
- [x] T1.4 更新 SDD docs、skill、CHANGELOG 和项目实例资料
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh && bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all`
- [x] T1.5 运行最终验证并记录完成证据
  - **Validation**: `python3 Workspace/SDD/sdd.py validate --all`
