# 用户原始问题

```text
$systemagent-deepthink 
SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/9.ECS框架优化/4.弃用ECS框架
DocsAI/思考/框架/ECS框架
- 框架名：SlimeAIFramework
- 在SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/10.GodotOOP框架方向，新建设计文档，这个我认为没问题，先确定方向是对的，Entity改成Object应该没问题，Component,System语义我认为可以直接保留
- Data我认为可以单独一个文件夹存放说明文档，Data的方案还没定下来，Event取驱动、Data驱动实际上也是框架的基础，填表格就能传数据这个要保留，这个是很重要的功能，也就是数据库到Data这条路要实现，你深度思考怎么解决，这里文档的意思是指Component内部的字段放在Component里面吗，其实也可以，但是要考虑Data怎么传过去，维护其实也可以，也就是从对象池拿对象出来更新Data时应该要更新Component的数据字段，那现在Data的Description数值限制之类的功能怎么实现，还有DataModifier，怎么实现要不要保留DataModifier？所以Data要考虑的地方很多，你要深度思考广泛搜索Data有什么方案
- Data名字保持不需要改名，SDD-0044我删了
- 深度思考详细分析，可以广泛搜索web,ctx7相关内容，更新/新建相关文档
```

