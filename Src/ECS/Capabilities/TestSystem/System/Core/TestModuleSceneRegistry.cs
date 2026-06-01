using System;

/// <summary>
/// TestSystem 模块场景注册表。
/// <para>
/// 这里定义调试面板支持的模块顺序、导航路径和对应场景资源键。
/// 宿主只依赖这份清单生成导航树，真正切换时再实例化当前模块。
/// </para>
/// </summary>
internal static class TestModuleSceneRegistry
{
    private static readonly TestModuleSceneDefinition[] _entries =
    {
        new(new TestModuleDefinition("attribute-test", $"{TestModuleGroupId.Attribute}.属性测试"),
            ResourcePaths.System_AttributeTestModule),
        new(new TestModuleDefinition("ability-test", $"{TestModuleGroupId.Ability}.技能测试"),
            ResourcePaths.System_AbilityTestModule),
        new(new TestModuleDefinition("spawn-enemy", $"{TestModuleGroupId.Common}.敌人生成"),
            ResourcePaths.System_SpawnTestModule),
        new(new TestModuleDefinition("system-info", $"{TestModuleGroupId.System}.系统监控"),
            ResourcePaths.System_SystemInfoTestModule),
        new(new TestModuleDefinition("object-pool-info", $"{TestModuleGroupId.Info}.对象池"),
            ResourcePaths.System_ObjectPoolInfoModule),
        new(new TestModuleDefinition("resource-catalog-test", $"{TestModuleGroupId.Resource}.资源目录"),
            ResourcePaths.System_ResourceCatalogTestModule)
    };

    /// <summary>
    /// 返回当前 TestSystem 支持的全部模块清单。
    /// </summary>
    internal static ReadOnlySpan<TestModuleSceneDefinition> Entries => _entries;
}
