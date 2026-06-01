/// <summary>
/// TestSystem 模块场景定义。
/// <para>
/// 宿主需要在不预先实例化模块的情况下构建导航树，因此把模块元数据和场景资源键放到独立定义里。
/// </para>
/// </summary>
internal readonly record struct TestModuleSceneDefinition(
    TestModuleDefinition Definition, // 模块稳定 Id 与导航路径
    string SceneResourceKey // 对应 ResourcePaths.System_* 资源键
);
