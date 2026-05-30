# 工作区设置指南

## 克隆工作区

```bash
git clone --recurse-submodules https://github.com/alading-2/BrotatoLike.git
cd BrotatoLike
```

如果已克隆但缺少 submodule：

```bash
git submodule update --init
```

## 目录结构

```
/home/slime/Code/SlimeAI/          (工作区根)
├── SlimeAI/                       (框架仓，独立 git)
├── Games/BrotatoLike/              (游戏仓，独立 git)
│   └── SlimeAI/                   (submodule → 框架仓)
├── Resources/Engine/                         (引擎参考)
└── openspec/                       (框架规格)
```

## 更新框架

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
git submodule update --remote SlimeAI
git add SlimeAI
git commit -m "Update SlimeAI submodule"
```

或使用 VSCode Task：`update: BrotatoLike SlimeAI Submodule`

## 开发原则

- 框架改动只在 `SlimeAI/` 仓提交
- 游戏改动只在 `Games/BrotatoLike/` 仓提交
- `Games/BrotatoLike/SlimeAI/` 是只读镜像，禁止直接修改
