//------------------------------------------------------------------------------
//* <ResourceGenerator>
//*     ResourceGenerator 资源路径生成器工具
//*
//*     不要修改本文件，因为每次运行ResourceGenerator都会覆盖本文件。
//* </ResourceGenerator>
//------------------------------------------------------------------------------

using System.Collections.Generic;

public struct ResourceData
{
    public string Path;
    public ResourceCategory Category;
    public ResourceData(ResourceCategory category, string path)
    {
        Category = category;
        Path = path;
    }
}

public static class ResourcePaths
{
    // --- Entity ---
    public const string Entity_AbilityEntity = "AbilityEntity";
    public const string Entity_ComponentRegistrarRuntimeTest = "ComponentRegistrarRuntimeTest";
    public const string Entity_EffectEntity = "EffectEntity";
    public const string Entity_EnemyEntity = "EnemyEntity";
    public const string Entity_EntityAttributionResolverRuntimeTest = "EntityAttributionResolverRuntimeTest";
    public const string Entity_EntityDestroyPipelineRuntimeTest = "EntityDestroyPipelineRuntimeTest";
    public const string Entity_EntityIdentityRuntimeTest = "EntityIdentityRuntimeTest";
    public const string Entity_EntitySpawnPipelineRuntimeTest = "EntitySpawnPipelineRuntimeTest";
    public const string Entity_LifecycleTreeRuntimeTest = "LifecycleTreeRuntimeTest";
    public const string Entity_LightningLineEffect = "LightningLineEffect";
    public const string Entity_OwnedReferenceRegistryRuntimeTest = "OwnedReferenceRegistryRuntimeTest";
    public const string Entity_PlayerEntity = "PlayerEntity";
    public const string Entity_ProjectileEntity = "ProjectileEntity";
    public const string Entity_TargetingIndicatorEntity = "TargetingIndicatorEntity";
    public const string Entity_VisualPreviewEntity = "VisualPreviewEntity";

    // --- Component ---
    public const string Component_ActiveSkillInputComponent = "ActiveSkillInputComponent";
    public const string Component_AIComponent = "AIComponent";
    public const string Component_AttackComponent = "AttackComponent";
    public const string Component_ChargeComponent = "ChargeComponent";
    public const string Component_CollisionComponent = "CollisionComponent";
    public const string Component_ContactDamageComponent = "ContactDamageComponent";
    public const string Component_CooldownComponent = "CooldownComponent";
    public const string Component_CostComponent = "CostComponent";
    public const string Component_DataInitComponent = "DataInitComponent";
    public const string Component_EffectComponent = "EffectComponent";
    public const string Component_EntityMovementComponent = "EntityMovementComponent";
    public const string Component_EntityOrientationComponent = "EntityOrientationComponent";
    public const string Component_HealthComponent = "HealthComponent";
    public const string Component_HurtboxComponent = "HurtboxComponent";
    public const string Component_LifecycleComponent = "LifecycleComponent";
    public const string Component_PickupComponent = "PickupComponent";
    public const string Component_RecoveryComponent = "RecoveryComponent";
    public const string Component_TargetingIndicatorControlComponent = "TargetingIndicatorControlComponent";
    public const string Component_TriggerComponent = "TriggerComponent";
    public const string Component_UnitAnimationComponent = "UnitAnimationComponent";
    public const string Component_UnitStateComponent = "UnitStateComponent";

    // --- Preset ---
    public const string Preset_AbilityPreset = "AbilityPreset";
    public const string Preset_EnemyPreset = "EnemyPreset";
    public const string Preset_PlayerPreset = "PlayerPreset";
    public const string Preset_UnitCorePreset = "UnitCorePreset";

    // --- UI ---
    public const string UI_ActiveSkillBarUI = "ActiveSkillBarUI";
    public const string UI_ActiveSkillSlotUI = "ActiveSkillSlotUI";
    public const string UI_DamageNumberUI = "DamageNumberUI";
    public const string UI_HealthBarUI = "HealthBarUI";
    public const string UI_UIManager = "UIManager";

    // --- Asset ---

    // --- AssetEffect ---
    public const string AssetEffect_003 = "003";
    public const string AssetEffect_004龙卷风 = "004龙卷风";
    public const string AssetEffect_020 = "020";
    public const string AssetEffect_lrsc3 = "lrsc3";

    // --- AssetUnit ---

    // --- AssetUnitEnemy ---
    public const string AssetUnitEnemy_chailangren = "chailangren";
    public const string AssetUnitEnemy_yuren = "yuren";

    // --- AssetUnitPlayer ---
    public const string AssetUnitPlayer_bubing = "bubing";
    public const string AssetUnitPlayer_deluyi = "deluyi";
    public const string AssetUnitPlayer_guangfa = "guangfa";

    // --- AssetProjectile ---
    public const string AssetProjectile_ArrowNeedle = "ArrowNeedle";
    public const string AssetProjectile_BoomerangChevron = "BoomerangChevron";
    public const string AssetProjectile_BulletDiamond = "BulletDiamond";
    public const string AssetProjectile_LaserBolt = "LaserBolt";

    // --- System ---
    public const string System_AbilityCatalogItem = "AbilityCatalogItem";
    public const string System_AbilityGroupSection = "AbilityGroupSection";
    public const string System_AbilityOwnedItem = "AbilityOwnedItem";
    public const string System_AbilityTestModule = "AbilityTestModule";
    public const string System_AttributeCheckEditor = "AttributeCheckEditor";
    public const string System_AttributeEditorRow = "AttributeEditorRow";
    public const string System_AttributeModifierEditor = "AttributeModifierEditor";
    public const string System_AttributeNumericEditor = "AttributeNumericEditor";
    public const string System_AttributeOptionEditor = "AttributeOptionEditor";
    public const string System_AttributeTestModule = "AttributeTestModule";
    public const string System_AttributeTextEditor = "AttributeTextEditor";
    public const string System_DamageService = "DamageService";
    public const string System_DamageStatisticsSystem = "DamageStatisticsSystem";
    public const string System_MouseSelectionSystem = "MouseSelectionSystem";
    public const string System_ObjectPoolInfoModule = "ObjectPoolInfoModule";
    public const string System_PauseMenuSystem = "PauseMenuSystem";
    public const string System_RecoverySystem = "RecoverySystem";
    public const string System_ResourceCatalogTestModule = "ResourceCatalogTestModule";
    public const string System_ResourcePickerControl = "ResourcePickerControl";
    public const string System_SpawnSystem = "SpawnSystem";
    public const string System_SpawnTestModule = "SpawnTestModule";
    public const string System_SystemCoreRuntimeTest = "SystemCoreRuntimeTest";
    public const string System_SystemInfoTestModule = "SystemInfoTestModule";
    public const string System_TestSystem = "TestSystem";

    // --- Tools ---
    public const string Tools_InputTest = "InputTest";
    public const string Tools_LogTest = "LogTest";
    public const string Tools_MyMathTest = "MyMathTest";
    public const string Tools_ObjectPoolInit = "ObjectPoolInit";
    public const string Tools_ObjectPoolManagerTest = "ObjectPoolManagerTest";
    public const string Tools_ObjectPoolVisualTest = "ObjectPoolVisualTest";
    public const string Tools_TargetSelectorTest = "TargetSelectorTest";
    public const string Tools_TimerManager = "TimerManager";

    // --- Data ---

    // --- DataAbility ---

    // --- DataUnit ---

    // --- Test ---
    public const string Test_AbilityInventoryServiceRuntimeTest = "AbilityInventoryServiceRuntimeTest";
    public const string Test_AbilitySystemPipelineTest = "AbilitySystemPipelineTest";
    public const string Test_ActiveSkillInputTest = "ActiveSkillInputTest";
    public const string Test_DamageSystemTest = "DamageSystemTest";
    public const string Test_MainTest = "MainTest";
    public const string Test_MovementCollisionRuntimeTest = "MovementCollisionRuntimeTest";
    public const string Test_MovementComponentTestScene = "MovementComponentTestScene";
    public const string Test_MovementTestEntity = "MovementTestEntity";
    public const string Test_SpawnTestScene = "SpawnTestScene";
    public const string Test_VisualPreviewScene = "VisualPreviewScene";

    // --- Other ---
    public const string Other_DataCatalogTestScene = "DataCatalogTestScene";
    public const string Other_DataFeatureBridgeTestScene = "DataFeatureBridgeTestScene";
    public const string Other_DataRuntimeTestScene = "DataRuntimeTestScene";
    public const string Other_DataSnapshotApplyTestScene = "DataSnapshotApplyTestScene";
    public const string Other_ECSTestScene = "ECSTestScene";
    public const string Other_TestEntity = "TestEntity";

    public static readonly Dictionary<ResourceCategory, Dictionary<string, ResourceData>> Resources = new()
    {
        { ResourceCategory.Entity, new Dictionary<string, ResourceData>
            {
                { Entity_AbilityEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Capabilities/Ability/Entity/AbilityEntity.tscn") },
                { Entity_ComponentRegistrarRuntimeTest, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Runtime/Entity/Tests/ComponentRegistrarRuntimeTest.tscn") },
                { Entity_EffectEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Capabilities/Effect/Entity/EffectEntity.tscn") },
                { Entity_EnemyEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Capabilities/Unit/Entity/Enemy/EnemyEntity.tscn") },
                { Entity_EntityAttributionResolverRuntimeTest, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Runtime/Entity/Tests/EntityAttributionResolverRuntimeTest.tscn") },
                { Entity_EntityDestroyPipelineRuntimeTest, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Runtime/Entity/Tests/EntityDestroyPipelineRuntimeTest.tscn") },
                { Entity_EntityIdentityRuntimeTest, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Runtime/Entity/Tests/EntityIdentityRuntimeTest.tscn") },
                { Entity_EntitySpawnPipelineRuntimeTest, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Runtime/Entity/Tests/EntitySpawnPipelineRuntimeTest.tscn") },
                { Entity_LifecycleTreeRuntimeTest, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Runtime/Entity/Tests/LifecycleTreeRuntimeTest.tscn") },
                { Entity_LightningLineEffect, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Capabilities/Effect/Entity/LightningLineEffect/LightningLineEffect.tscn") },
                { Entity_OwnedReferenceRegistryRuntimeTest, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Runtime/Entity/Tests/OwnedReferenceRegistryRuntimeTest.tscn") },
                { Entity_PlayerEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Capabilities/Unit/Entity/Player/PlayerEntity.tscn") },
                { Entity_ProjectileEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Capabilities/Projectile/Entity/ProjectileEntity.tscn") },
                { Entity_TargetingIndicatorEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Capabilities/Unit/Entity/TargetingIndicator/TargetingIndicatorEntity.tscn") },
                { Entity_VisualPreviewEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Test/GlobalTest/VisualPreview/Entity/VisualPreviewEntity.tscn") },
            }
        },
        { ResourceCategory.Component, new Dictionary<string, ResourceData>
            {
                { Component_ActiveSkillInputComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Ability/Component/ActiveSkillInputComponent/ActiveSkillInputComponent.tscn") },
                { Component_AIComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/AI/Component/AIComponent/AIComponent.tscn") },
                { Component_AttackComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Unit/Component/Common/AttackComponent/AttackComponent.tscn") },
                { Component_ChargeComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Ability/Component/ChargeComponent/ChargeComponent.tscn") },
                { Component_CollisionComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Collision/Component/CollisionComponent/CollisionComponent.tscn") },
                { Component_ContactDamageComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Collision/Component/ContactDamageComponent/ContactDamageComponent.tscn") },
                { Component_CooldownComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Ability/Component/CooldownComponent/CooldownComponent.tscn") },
                { Component_CostComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Ability/Component/CostComponent/CostComponent.tscn") },
                { Component_DataInitComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Unit/Component/Common/DataInitComponent/DataInitComponent.tscn") },
                { Component_EffectComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Effect/Component/EffectComponent/EffectComponent.tscn") },
                { Component_EntityMovementComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Movement/Component/EntityMovementComponent.tscn") },
                { Component_EntityOrientationComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Movement/Component/EntityOrientationComponent.tscn") },
                { Component_HealthComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Unit/Component/Common/HealthComponent/HealthComponent.tscn") },
                { Component_HurtboxComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Collision/Component/HurtboxComponent/HurtboxComponent.tscn") },
                { Component_LifecycleComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Unit/Component/Common/LifecycleComponent/LifecycleComponent.tscn") },
                { Component_PickupComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Collision/Component/PickupComponent/PickupComponent.tscn") },
                { Component_RecoveryComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Unit/Component/Common/RecoveryComponent/RecoveryComponent.tscn") },
                { Component_TargetingIndicatorControlComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Unit/Component/TargetingIndicatorControlComponent/TargetingIndicatorControlComponent.tscn") },
                { Component_TriggerComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Ability/Component/TriggerComponent/TriggerComponent.tscn") },
                { Component_UnitAnimationComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Unit/Component/Common/UnitAnimationComponent/UnitAnimationComponent.tscn") },
                { Component_UnitStateComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Capabilities/Unit/Component/Common/UnitStateComponent/UnitStateComponent.tscn") },
            }
        },
        { ResourceCategory.Preset, new Dictionary<string, ResourceData>
            {
                { Preset_AbilityPreset, new ResourceData(ResourceCategory.Preset, "res://Src/ECS/Capabilities/Ability/Presets/AbilityPreset.tscn") },
                { Preset_EnemyPreset, new ResourceData(ResourceCategory.Preset, "res://Src/ECS/Capabilities/Unit/Presets/EnemyPreset.tscn") },
                { Preset_PlayerPreset, new ResourceData(ResourceCategory.Preset, "res://Src/ECS/Capabilities/Unit/Presets/PlayerPreset.tscn") },
                { Preset_UnitCorePreset, new ResourceData(ResourceCategory.Preset, "res://Src/ECS/Capabilities/Unit/Presets/UnitCorePreset.tscn") },
            }
        },
        { ResourceCategory.UI, new Dictionary<string, ResourceData>
            {
                { UI_ActiveSkillBarUI, new ResourceData(ResourceCategory.UI, "res://Src/ECS/UI/UI/SkillUI/ActiveSkillBarUI.tscn") },
                { UI_ActiveSkillSlotUI, new ResourceData(ResourceCategory.UI, "res://Src/ECS/UI/UI/SkillUI/ActiveSkillSlotUI.tscn") },
                { UI_DamageNumberUI, new ResourceData(ResourceCategory.UI, "res://Src/ECS/UI/UI/DamageNumberUI/DamageNumberUI.tscn") },
                { UI_HealthBarUI, new ResourceData(ResourceCategory.UI, "res://Src/ECS/UI/UI/HealthBarUI/HealthBarUI.tscn") },
                { UI_UIManager, new ResourceData(ResourceCategory.UI, "res://Src/ECS/UI/Core/UIManager.tscn") },
            }
        },
        { ResourceCategory.Asset, new Dictionary<string, ResourceData>
            {
            }
        },
        { ResourceCategory.AssetEffect, new Dictionary<string, ResourceData>
            {
                { AssetEffect_003, new ResourceData(ResourceCategory.AssetEffect, "res://assets/Effect/003/AnimatedSprite2D/003.tscn") },
                { AssetEffect_004龙卷风, new ResourceData(ResourceCategory.AssetEffect, "res://assets/Effect/004龙卷风/AnimatedSprite2D/004龙卷风.tscn") },
                { AssetEffect_020, new ResourceData(ResourceCategory.AssetEffect, "res://assets/Effect/020/AnimatedSprite2D/020.tscn") },
                { AssetEffect_lrsc3, new ResourceData(ResourceCategory.AssetEffect, "res://assets/Effect/lrsc3/AnimatedSprite2D/lrsc3.tscn") },
            }
        },
        { ResourceCategory.AssetUnit, new Dictionary<string, ResourceData>
            {
            }
        },
        { ResourceCategory.AssetUnitEnemy, new Dictionary<string, ResourceData>
            {
                { AssetUnitEnemy_chailangren, new ResourceData(ResourceCategory.AssetUnitEnemy, "res://assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn") },
                { AssetUnitEnemy_yuren, new ResourceData(ResourceCategory.AssetUnitEnemy, "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn") },
            }
        },
        { ResourceCategory.AssetUnitPlayer, new Dictionary<string, ResourceData>
            {
                { AssetUnitPlayer_bubing, new ResourceData(ResourceCategory.AssetUnitPlayer, "res://assets/Unit/Player/bubing/AnimatedSprite2D/bubing.tscn") },
                { AssetUnitPlayer_deluyi, new ResourceData(ResourceCategory.AssetUnitPlayer, "res://assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn") },
                { AssetUnitPlayer_guangfa, new ResourceData(ResourceCategory.AssetUnitPlayer, "res://assets/Unit/Player/guangfa/AnimatedSprite2D/guangfa.tscn") },
            }
        },
        { ResourceCategory.AssetProjectile, new Dictionary<string, ResourceData>
            {
                { AssetProjectile_ArrowNeedle, new ResourceData(ResourceCategory.AssetProjectile, "res://assets/Projectile/Projectile/Polygon2D/ArrowNeedle.tscn") },
                { AssetProjectile_BoomerangChevron, new ResourceData(ResourceCategory.AssetProjectile, "res://assets/Projectile/Projectile/Polygon2D/BoomerangChevron.tscn") },
                { AssetProjectile_BulletDiamond, new ResourceData(ResourceCategory.AssetProjectile, "res://assets/Projectile/Projectile/Polygon2D/BulletDiamond.tscn") },
                { AssetProjectile_LaserBolt, new ResourceData(ResourceCategory.AssetProjectile, "res://assets/Projectile/Projectile/Line2D/LaserBolt.tscn") },
            }
        },
        { ResourceCategory.System, new Dictionary<string, ResourceData>
            {
                { System_AbilityCatalogItem, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Ability/AbilityCatalogItem.tscn") },
                { System_AbilityGroupSection, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Ability/AbilityGroupSection.tscn") },
                { System_AbilityOwnedItem, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Ability/AbilityOwnedItem.tscn") },
                { System_AbilityTestModule, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Ability/AbilityTestModule.tscn") },
                { System_AttributeCheckEditor, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Attribute/AttributeCheckEditor.tscn") },
                { System_AttributeEditorRow, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Attribute/AttributeEditorRow.tscn") },
                { System_AttributeModifierEditor, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Attribute/AttributeModifierEditor.tscn") },
                { System_AttributeNumericEditor, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Attribute/AttributeNumericEditor.tscn") },
                { System_AttributeOptionEditor, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Attribute/AttributeOptionEditor.tscn") },
                { System_AttributeTestModule, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Attribute/AttributeTestModule.tscn") },
                { System_AttributeTextEditor, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Attribute/AttributeTextEditor.tscn") },
                { System_DamageService, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/Damage/System/DamageService.tscn") },
                { System_DamageStatisticsSystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/Damage/System/DamageStatisticsSystem.tscn") },
                { System_MouseSelectionSystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/Tools/Input/MouseSelection/MouseSelectionSystem.tscn") },
                { System_ObjectPoolInfoModule, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Info/ObjectPoolInfoModule.tscn") },
                { System_PauseMenuSystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/UI/PauseMenu/PauseMenuSystem.tscn") },
                { System_RecoverySystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/Unit/System/RecoverySystem/RecoverySystem.tscn") },
                { System_ResourceCatalogTestModule, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/ResourceCatalog/ResourceCatalogTestModule.tscn") },
                { System_ResourcePickerControl, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/ResourceCatalog/ResourcePickerControl.tscn") },
                { System_SpawnSystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/Spawn/System/SpawnSystem.tscn") },
                { System_SpawnTestModule, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/Spawn/SpawnTestModule.tscn") },
                { System_SystemCoreRuntimeTest, new ResourceData(ResourceCategory.System, "res://Src/ECS/Runtime/System/Tests/SystemCore/SystemCoreRuntimeTest.tscn") },
                { System_SystemInfoTestModule, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/System/SystemInfoTestModule.tscn") },
                { System_TestSystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/Capabilities/TestSystem/System/TestSystem.tscn") },
            }
        },
        { ResourceCategory.Tools, new Dictionary<string, ResourceData>
            {
                { Tools_InputTest, new ResourceData(ResourceCategory.Tools, "res://Src/ECS/Tools/Input/Tests/InputTest.tscn") },
                { Tools_LogTest, new ResourceData(ResourceCategory.Tools, "res://Src/ECS/Tools/Logger/Tests/LogTest.tscn") },
                { Tools_MyMathTest, new ResourceData(ResourceCategory.Tools, "res://Src/ECS/Tools/Math/Tests/MyMathTest.tscn") },
                { Tools_ObjectPoolInit, new ResourceData(ResourceCategory.Tools, "res://Src/ECS/Tools/ObjectPool/ObjectPoolInit.tscn") },
                { Tools_ObjectPoolManagerTest, new ResourceData(ResourceCategory.Tools, "res://Src/ECS/Tools/ObjectPool/Tests/ObjectPoolManagerTest.tscn") },
                { Tools_ObjectPoolVisualTest, new ResourceData(ResourceCategory.Tools, "res://Src/ECS/Tools/ObjectPool/Tests/ObjectPoolVisualTest.tscn") },
                { Tools_TargetSelectorTest, new ResourceData(ResourceCategory.Tools, "res://Src/ECS/Tools/TargetSelector/Tests/TargetSelectorTest.tscn") },
                { Tools_TimerManager, new ResourceData(ResourceCategory.Tools, "res://Src/ECS/Tools/Timer/TimerManager.tscn") },
            }
        },
        { ResourceCategory.Data, new Dictionary<string, ResourceData>
            {
            }
        },
        { ResourceCategory.DataAbility, new Dictionary<string, ResourceData>
            {
            }
        },
        { ResourceCategory.DataUnit, new Dictionary<string, ResourceData>
            {
            }
        },
        { ResourceCategory.Test, new Dictionary<string, ResourceData>
            {
                { Test_AbilityInventoryServiceRuntimeTest, new ResourceData(ResourceCategory.Test, "res://Src/ECS/Capabilities/Ability/Tests/AbilityInventoryServiceRuntimeTest.tscn") },
                { Test_AbilitySystemPipelineTest, new ResourceData(ResourceCategory.Test, "res://Src/ECS/Capabilities/Ability/Tests/AbilitySystemPipelineTest.tscn") },
                { Test_ActiveSkillInputTest, new ResourceData(ResourceCategory.Test, "res://Src/ECS/Capabilities/Ability/Tests/ActiveSkillInputTest/ActiveSkillInputTest.tscn") },
                { Test_DamageSystemTest, new ResourceData(ResourceCategory.Test, "res://Src/ECS/Capabilities/Damage/Tests/DamageSystemTest.tscn") },
                { Test_MainTest, new ResourceData(ResourceCategory.Test, "res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn") },
                { Test_MovementCollisionRuntimeTest, new ResourceData(ResourceCategory.Test, "res://Src/ECS/Capabilities/Movement/Tests/MovementCollisionRuntimeTest.tscn") },
                { Test_MovementComponentTestScene, new ResourceData(ResourceCategory.Test, "res://Src/ECS/Capabilities/Movement/Tests/MovementComponentTestScene.tscn") },
                { Test_MovementTestEntity, new ResourceData(ResourceCategory.Test, "res://Src/ECS/Capabilities/Movement/Tests/MovementTestEntity.tscn") },
                { Test_SpawnTestScene, new ResourceData(ResourceCategory.Test, "res://Src/ECS/Capabilities/Spawn/Tests/SpawnTestScene.tscn") },
                { Test_VisualPreviewScene, new ResourceData(ResourceCategory.Test, "res://Src/ECS/Test/GlobalTest/VisualPreview/VisualPreviewScene.tscn") },
            }
        },
        { ResourceCategory.Other, new Dictionary<string, ResourceData>
            {
                { Other_DataCatalogTestScene, new ResourceData(ResourceCategory.Other, "res://Src/ECS/Runtime/Data/Tests/DataOS/DataCatalogTestScene.tscn") },
                { Other_DataFeatureBridgeTestScene, new ResourceData(ResourceCategory.Other, "res://Src/ECS/Runtime/Data/Tests/DataOS/DataFeatureBridgeTestScene.tscn") },
                { Other_DataRuntimeTestScene, new ResourceData(ResourceCategory.Other, "res://Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.tscn") },
                { Other_DataSnapshotApplyTestScene, new ResourceData(ResourceCategory.Other, "res://Src/ECS/Runtime/Data/Tests/DataOS/DataSnapshotApplyTestScene.tscn") },
                { Other_ECSTestScene, new ResourceData(ResourceCategory.Other, "res://Src/ECS/Runtime/Tests/ECSTest/ECSTestScene.tscn") },
                { Other_TestEntity, new ResourceData(ResourceCategory.Other, "res://Src/ECS/Runtime/Tests/ECSTest/Entity/TestEntity.tscn") },
            }
        },
    };
}
