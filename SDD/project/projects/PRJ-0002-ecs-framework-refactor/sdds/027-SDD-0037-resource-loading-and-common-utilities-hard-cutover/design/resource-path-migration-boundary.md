# Resource Path Migration Boundary

## Purpose

本文件保存 SDD-0037 执行所需的资源路径迁移边界摘要，避免主设计依赖外部 skill 文档才能恢复。

## Rules

- `res://` 本身合法；它表示当前 Godot project root。
- 路径稳定性不属于运行时 `ResourceLoading` 职责。
- 资源新增、删除、移动、重命名或检查旧路径残留时，应走项目目录 / resource-path-migration workflow。
- 迁移必须先 dry-run，再 apply。
- 替换 root 必须是当前 git boundary，不能是 `/home/slime/Code/SlimeAI` 父目录。
- 框架仓只拥有框架资源 catalog；游戏仓资源后续应由游戏仓自己的 catalog/generator 拥有。

## Required Flow

1. 确认 git boundary 和 dirty 范围。
2. 明确 old/new path，优先使用 Godot `res://` 路径。
3. 对路径替换先 dry-run。
4. 确认影响范围后 apply。
5. 用 `rg` 检查 old path residue，并分类 current runtime / DataOS / DocsAI / history。
6. 重新运行 ResourceGenerator。
7. 按影响面运行 build、DataOS validator 和 SDD validate。

## Script Entry

框架仓当前脚本入口是 `.ai-config/skills/core/resource-path-migration/scripts/migrate_resource_path.py`。执行 SDD-0037 时应先读对应 skill 获取最新命令参数；本文件只冻结本 SDD 的 owner 边界和流程要求。
