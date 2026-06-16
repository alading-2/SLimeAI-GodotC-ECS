# 用户原始问题

> `$systemagent-deepthink`
>
> `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/2.Data系统优化/5.Data类型系统重构`
>
> `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/QFramework/07-QFramework数据结构与SlimeAIData对比深化.md`
>
> `https://github.com/liangxiegame/QFramework`
>
> `https://github.com/fanqie404/FrameworkDesign`
>
> `Resources/Engine/Docs`、`Resources/Engine/Engine`，这里是clone下来的引擎框架作为参考
>
> - 因为我不懂框架，现在看回去才知道框架原来这么难搞，框架是大量经验才能做的，尤其需要非常高深的C#代码经验，所以现在做框架感觉很难，处处碰壁，就算有AI我认为还是很难
> - 我是否应该直接用成熟的框架，先学习别人怎么做的，后面再自己做，比如QFramework，这个我很关注，你去仔细查看它的介绍
> - 这个框架可能有点不足够完成我的框架，但是我感觉可以仔细阅读它的设计，深度思考它是怎么做的，学习它的架构模式
> - 也可以参考其他成熟的框架看看能不能直接用，或者你告诉我应该去看哪些源码，怎么看，学习什么模式，也就是你告诉我应该去学什么？
> - 在 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/2.Data系统优化/6.架构学习`，生成相关文档，个数不限
> - 深度思考详细分析，可以广泛搜索web,ctx7相关内容，更新相关文档

## 2026-06-15 运行时解耦补充问题

> `$systemagent-deepthink`
>
> - 不建议直接替换为某个成熟框架。这个我能理解，因为我的需求似乎没有人做出来，是不是真的没人做？太难了？还是说这个是AI时代新的需求，以前没有这个需求？我感觉不是，框架其实跟AI是分开的，就算没有AI这个框架也能用，所谓AI就是框架加上一堆文档、约束、规则、skill等等来告诉AI怎么用这个框架，AI框架实际上是成熟框架+AI,框架最好适配AI，不可以太难用。所以你说不要用其他框架我可以理解，因为是新需求
> - 我最核心的需求是高度解耦，不是一般的解耦，component+system解耦，也就是任何功能随时可以拿掉，这样就能高度灵活组合。
>   - system解耦这分为游戏开始前的是否使用某个系统，还有游戏运行过程中的打开/关闭某个系统，这样的话就能够在同一个框架下面的不同游戏自由组合系统，通过git submodule设置游戏版本，从而做成一个大框架，这个框架是大量游戏功能的集合，游戏只是拼接这些功能
>   - 我看SlimeAI连这个目标都没确定，所谓事件、数据驱动也是为了完成大框架解耦，这样的解耦才是最核心的需求，为了完成解耦，entity的component解耦，system解耦也是框架的核心实现，框架最核心的地方在这里，也就是runtime。要更新相关文档，包括DocsAI里面的文档，现在框架连做什么都没搞清楚，还有SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/2.Data系统优化/5.Data类型系统重构、SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/2.Data系统优化/6.架构学习，这里的文档
>   - component,system解耦我认为应该保留，你怎么看
> - 首要目标是底层框架，我认为现在框架最大的问题在Data,当然其他地方应该也要大改，没问题，允许重大重构，因为这是一个大框架，所以底层必须做好，也必须简单直接，概念清晰，现在就是各种各样的功能全部杂在一起，不是说不能做，而是最底层最基本的框架没固定之前这些东西就不应该做，越是底层的东西越要简单直接
>   - 现在上层很多东西做得太复杂，导致难用，比如feature等，要做大框架上层也要尽量简单直接，当然这是后话，先写个TODO
>   - 通过简单的形式组合成复杂的功能才是正解
> - 现在Data集合在一起其实很方便，我需要什么功能直接填表格就行，不是说这样不好，但是Data方便的需求是上层，而且应该也有其他方式能够实现这个需求，现在的实现方法有很大问题，这个需求也就是表格驱动，这是框架后面要做的，不是现在就要做，顺序错了，现在框架底层都没做出来就想着做上层的东西了，所以现在出了大问题
> - 至于框架与AI结合是后话，也是上层的东西，先不管，实现了底层再说
> - 有问题跟我交流
> - 深度思考详细分析，可以广泛搜索web,ctx7相关内容，更新/新建相关文档

## 2026-06-15 QFramework 之后看什么

> - 其实用ECS框架是因为Component,System这些概念对解耦比较好，ECS也确实有组件解耦的概念，其实不用ECS也可以的，不过现在都实现了用着也行，ECS的概念也比较好接受好理解
> - 架构规则越少越好，能通过文档 / validator / test / code review 控制的，不要都塞进 runtime Set。这句话我很认同，也是SlimeAI的Runtime修改方向和原则，越是底层越要简单直接
> - 现在方向已经指明了，要学框架设计，要看成熟框架怎么写，看懂含义，不过我只会C#，Qframework应该没问题，比较短，然后看什么？

## 2026-06-15 是否需要查教程学习框架理论

> 我是否需要现在网上查教程学习框架搭建的理论
>
> 生成文档
