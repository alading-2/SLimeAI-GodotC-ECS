# Data 方案用户原始问题

## 2026-06-19 深度分析请求

```text
$systemagent-deepthink
/home/slime/Code/SlimeAI/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/10.GodotOOP框架方向
/home/slime/Code/SlimeAI/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/10.GodotOOP框架方向/Data
/home/slime/Code/SlimeAI/SlimeAI/Workspace/Else/参考/Data参考
/home/slime/资料/Obsidian/Obsidian/游戏开发/资源/Resources/Engine/Engine/QFramework/Docs/03-SlimeAI采纳建议，
/home/slime/资料/Obsidian/Obsidian/游戏开发/资源/Resources/Engine/Engine/QFramework/Program/Assets/QFramework/QFramework.cs，
QFramework可以只用来参考概念
- 不用ecs是定了的，现在主要解决data的问题，07-OOP中数据定义与运行时管理方案.md给出了方向，确实有很多字段都是component内部用的，集中在一起反而出问题
- Data参考里面可以看出哪些字段是哪个component，你可以记录下来，实际上数据字段应该放在component,因为component是功能单元
- 数据定义是用之前的meta静态方式，还是用现在的数据库方式？深度分析，要不要回退成之前的meta形式？
- 定义集中没问题，数据要分开放，这样的话数据实际上就存在字段里面，这样的话只有不会加载不用的component的字段，然后就是运行时管理，如果其他地方要数据读写，怎么办？不能直接获取component然后改数据，这样会耦合。QFramework每个字段都用了Command,Query,model存数据，用command改modle,其他地方调用command就行，我认为这个有点麻烦，不过我感觉我现在分了这么多层也挺麻烦，参考它框架的概念思想，深度分析，他这个不是不好，感觉就是拆得太细，我感觉你可以单独分析这个框架跟SlimeAI的Data,对比两种方式，以及对SlimeAI有什么建议或者尝试
  - Command我感觉跟Event是有相似地方的，他不是强制调用，如果没有这个Command应该可以不调用，这跟event差不多，Event管理事件，Command管理数据，可能不止数据
  - 我认为这些概念非常重要，会对现在的方案有很大的启发，所以要重构方案
- Data系统肯定还有大量的问题没有解决，你要一一深度思考，跟我深入交流确定方案，
- /home/slime/Code/SlimeAI/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/10.GodotOOP框架方向/Data，重构这里的文档，详细说明所有问题、解决方案分析（这个还没完全定下来，需要跟我深入交流）
- 深度思考详细分析广泛搜索
```

## 2026-06-19 追加确认

```text
ok,run

- Command可以用，你自己分析
- AttackState / MoveMode / Velocity / AIState 这类运行时状态，是继续 Data authoritative，还是改为 Component/System authoritative + Data projection？这个什么意思？
```
