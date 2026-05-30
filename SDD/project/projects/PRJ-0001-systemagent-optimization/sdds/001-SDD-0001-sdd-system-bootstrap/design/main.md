# SDD System Bootstrap

## Goal

建立 SlimeAI 工作区的 SDD MVP：提供标准目录、任务实例结构、Python 标准库 CLI、基础验证、索引生成、单元测试和 AI skill 入口。

## Shared Design References

完整设计不在本子 SDD 重复保存，统一引用项目级共享设计：

- `../../design/SDD/SlimeAI-SDD-MVP设计.md`
- `../../design/SDD/SDD重构与CLI详细执行计划.md`

## Task Design Summary

本任务负责把 SDD 从想法落成可运行的 MVP：工具实现放在工作区 SDD 工具目录，任务实例放在工作区 SDD 根目录，AI 使用说明放在 SDD skill 统一源。MVP 命令覆盖初始化、创建、查看、状态流转、任务更新、校验和索引生成。

## Outcome

完成后，SDD 具备可恢复任务胶囊、机器可读 catalog、人类可读 index、基础校验和单元测试。后续信息质量加固和项目级容器均以本任务产物为基础。

## Verification

- `python3 -m unittest discover Workspace/SDD/tests`
- `python3 Workspace/SDD/sdd.py validate --all`
- `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all`
- `python3 -m py_compile Workspace/SDD/sdd.py Workspace/SDD/tests/test_sdd_cli.py`
