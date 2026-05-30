# SDD CLI Source Modularization

## Goal

将过长的 `Workspace/SDD/sdd.py` 拆分为 `Workspace/SDD/Src/` 下的功能模块，让 `sdd.py` 只保留命令行参数定义、命令绑定和 `main()`，同时保持现有 CLI 行为、命令名称、输出格式和测试入口不变。

## Context

`Workspace/SDD/sdd.py` 已承载模板、仓储、索引、校验、任务、进度、项目容器和命令处理等多类职责。随着项目容器和信息质量规则增加，继续把所有逻辑放在单文件中会降低可维护性，也会让后续 SDD CLI 扩展更容易误改无关区域。

本任务仍遵循现有 SDD 约束：

- 不引入第三方依赖，继续使用 Python 标准库。
- `python3 Workspace/SDD/sdd.py <command>` 和 `bash Workspace/SDD/sdd.sh <command>` 保持公开入口不变。
- 本轮只做模块边界拆分，不改变 metadata schema、目录结构、命令语义和用户可见输出。
- 项目级共享设计仍在 `PRJ-0001/design/`，本文件只记录本任务特定设计。

## Design

采用按职责拆分的 `Src/` package：

- `sdd.py`：保留 `argparse`、`build_parser()`、`main()` 和命令函数绑定。
- `Src/config.py`：路径、状态列表、校验常量和 `resolve_root()`。
- `Src/errors.py`：CLI 专用异常。
- `Src/io.py`：时间、slug、JSON 和文本写入辅助。
- `Src/templates.py`：Root、Project、SDD 实例模板与 README / progress / tasks 构造。
- `Src/repository.py`：实例和项目扫描、定位、ID 分配、目录名计算。
- `Src/root_ops.py`：根目录初始化。
- `Src/instance_ops.py`：实例创建、metadata 保存、README 字段 patch、状态写入辅助。
- `Src/project_ops.py`：项目创建。
- `Src/progress.py`：Latest Resume 与 timeline 追加。
- `Src/tasks.py`：任务计数、任务 header 和 checkbox 更新。
- `Src/catalog.py`：catalog / INDEX 构建与写入。
- `Src/validation.py`：结构、metadata、项目和信息质量校验。
- `Src/commands.py`：所有 `command_*` 函数，作为 CLI 参数到业务模块的薄适配层。

取舍：本轮不引入类模型或 Repository 对象，避免在“拆文件”同时改变行为模型。模块之间先沿用原函数签名和原数据结构，后续若需要对象化或单元测试增强，另建 SDD。

## Verification

- `python3 -m py_compile Workspace/SDD/sdd.py Workspace/SDD/Src/*.py`
- `python3 -m unittest discover Workspace/SDD/tests`
- `python3 Workspace/SDD/sdd.py doctor`
- `python3 Workspace/SDD/sdd.py validate --all`
- `git diff --check`
