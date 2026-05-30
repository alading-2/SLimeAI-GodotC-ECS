# 工作区打开方式

## 推荐打开目录

```
/home/slime/Code/SlimeAI
```

理由：
- 可同时看到框架代码、游戏代码、引擎扩展
- 适合同时修改框架和验证游戏侧
- VSCode 工作区任务（build、submodule update）基于此目录

## 目录结构速查

```text
/home/slime/Code/SlimeAI/
├── AGENTS.md                          ← AI 入口
├── .ai-config/                        ← Skill/Rule 唯一源
├── .vscode/tasks.json                 ← VSCode Task
├── Workspace/DocsAI/                  ← 工作区级别 AI 文档
├── Docs/                              ← 人类可读文档
├── SlimeAI/                          ← 框架仓（Git 仓库）
│   ├── GameOS/ DataOS/ Tests/
│   └── DocsAI/                        ← 框架文档
├── Games/BrotatoLike/                 ← 游戏仓（Git 仓库）
│   ├── project.godot
│   ├── SlimeAI/                      ← submodule（只读镜像）
│   └── DocsAI/                        ← 游戏文档
├── Games/Game2/ ...                   ← 未来
├── Resources/Engine/                            ← 引擎源码分析（参考）
│   └── Docs/
├── openspec/                          ← OpenSpec 变更
└── Tools/                             ← 同步脚本
```

## 各目录角色

| 目录 | 类型 | 可写？ | Git？ |
|------|------|--------|-------|
| `SlimeAI/` | 框架仓 | ✅ 唯一可写 | ✅ |
| `Games/BrotatoLike/` | 游戏仓 | ✅ 只写游戏 | ✅ |
| `Games/BrotatoLike/SlimeAI/` | submodule | ❌ 只读镜像 | ✅（独立仓库） |
| `Resources/Engine/` | 参考材料 | ✅ | ❌ |
| `openspec/` | 框架规格 | ✅ | 部分在框架仓 |
| `Workspace/` | 工作区文档 | ✅ | ❌（工作区级别） |

## AI 视野约束

- 日常开发关注：`SlimeAI/`、`Games/BrotatoLike/`、`.ai-config/`、`openspec/`
- `Resources/Engine/Docs/` 仅在研究参考框架时使用，不纳入日常框架开发上下文
- `Resources/Else/` 等是外部参考，禁止作为事实源引用
