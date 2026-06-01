using Godot;
using System;
using System.Collections.Generic;
using slime.data.Features;

namespace Slime.Test.DataOS;

/// <summary>
/// 验证 descriptor-first Data runtime policy。
/// </summary>
public partial class DataRuntimeTestScene : DataSceneTestBase
{
    protected override void RunTests()
    {
        Data_Get_ShouldReturnDescriptorDefault();
        Data_Set_ShouldWriteTypedValue();
        Data_Set_ShouldRejectUnknownKey();
        Data_Set_ShouldRejectWrongType();
        Data_WriteDiagnostics_ShouldReportFailureCodes();
        Data_ReferenceAndArrayContracts_ShouldNormalizeRuntimeTypes();
        Data_Set_ShouldRespectWritePolicy();
        Data_Set_ShouldApplyRangePolicy();
        Data_Set_ShouldRespectAllowedValues();
        Data_RemoveAndClear_ShouldReturnDescriptorDefault();
        Data_Set_ShouldPublishPropertyChanged();
        Data_AddModifier_ShouldRespectModifierPolicy();
        Data_AddModifier_ShouldRejectUnknownTarget();
        Data_AddModifier_ShouldApplyModifierPipeline();
        Data_RemoveModifiersBySource_ShouldOnlyRemoveMatchingSource();
        Data_ModifierChange_ShouldPublishChangeAndDirtyDependents();
        Data_GetComputed_ShouldUseResolverDependenciesAndComputeParams();
        Data_GetComputed_ShouldCacheUntilDependencyChanges();
        Data_ComputedDirty_ShouldPropagateTransitively();
    }

    private void Data_Get_ShouldReturnDescriptorDefault()
    {
        var data = new Data(Bootstrap.Catalog);
        AssertEqual("descriptor default", 10f, data.Get<float>(GeneratedDataKey.BaseHp));
    }

    private void Data_Set_ShouldWriteTypedValue()
    {
        var data = new Data(Bootstrap.Catalog);
        AssertTrue("typed write accepted", data.Set(GeneratedDataKey.BaseHp, 20f));
        AssertEqual("typed read after write", 20f, data.Get<float>(GeneratedDataKey.BaseHp));
    }

    private void Data_Set_ShouldRejectUnknownKey()
    {
        var data = new Data(Bootstrap.Catalog);
        AssertThrows<KeyNotFoundException>("unknown key rejected", () => data.Set("MissingDataKey", 1));
        AssertFalse("unknown key not created", data.Has("MissingDataKey"));
    }

    private void Data_Set_ShouldRejectWrongType()
    {
        var data = new Data(Bootstrap.Catalog);
        AssertFalse("wrong type rejected", data.SetUntyped("BaseHp", "not-float", DataWriteSource.Runtime));
        AssertEqual("default after wrong type", 10f, data.Get<float>(GeneratedDataKey.BaseHp));
    }

    private void Data_WriteDiagnostics_ShouldReportFailureCodes()
    {
        var storage = CreateRuntimeStorage(
            Definition("Attribute.BaseHp", DataValueType.Float, 10f),
            Definition("Attribute.LoaderOnly", DataValueType.Float, 0f, writePolicy: DataWritePolicy.LoaderOnly),
            Definition("Attribute.RejectRange", DataValueType.Float, 0f, minValue: 0f, maxValue: 100f, rangePolicy: DataRangePolicy.RejectRuntime),
            Definition("Attribute.Computed", DataValueType.Float, 0f, writePolicy: DataWritePolicy.ComputedReadonly),
            Definition("Unit.Name", DataValueType.String, "Slime"));

        AssertFalse("diagnostic unknown key rejected", storage.TrySetUntyped("Attribute.Missing", 1f, DataWriteSource.Runtime, out var unknown));
        AssertEqual("diagnostic unknown code", "unknown_key", unknown.Errors[0].Code);
        AssertEqual("diagnostic unknown key", "Attribute.Missing", unknown.Errors[0].StableKey);

        AssertFalse("diagnostic wrong type rejected", storage.TrySetUntyped("Attribute.BaseHp", "not-float", DataWriteSource.Runtime, out var wrongType));
        AssertEqual("diagnostic wrong type code", "wrong_clr_type", wrongType.Errors[0].Code);
        AssertEqual("diagnostic wrong expected", "Float", wrongType.Errors[0].ExpectedType);
        AssertEqual("diagnostic wrong actual", "String", wrongType.Errors[0].ActualType);

        AssertFalse("diagnostic loader policy rejected", storage.TrySetUntyped("Attribute.LoaderOnly", 1f, DataWriteSource.Runtime, out var writePolicy));
        AssertEqual("diagnostic write policy code", "write_policy_rejected", writePolicy.Errors[0].Code);
        AssertEqual("diagnostic write policy", "LoaderOnly", writePolicy.Errors[0].Policy);

        AssertFalse("diagnostic range rejected", storage.TrySetUntyped("Attribute.RejectRange", 120f, DataWriteSource.Runtime, out var rangePolicy));
        AssertEqual("diagnostic range code", "range_policy_rejected", rangePolicy.Errors[0].Code);
        AssertEqual("diagnostic range raw", "120", rangePolicy.Errors[0].RawValue);

        AssertFalse("diagnostic computed rejected", storage.TrySetUntyped("Attribute.Computed", 1f, DataWriteSource.Runtime, out var computedPolicy));
        AssertEqual("diagnostic computed policy code", "write_policy_rejected", computedPolicy.Errors[0].Code);
        AssertEqual("diagnostic computed policy", "ComputedReadonly", computedPolicy.Errors[0].Policy);

        AssertFalse("diagnostic modifier rejected", storage.TryAddModifier("Unit.Name", new DataModifier(ModifierType.Additive, 1f, id: "bad"), DataWriteSource.Runtime, out var modifierPolicy));
        AssertEqual("diagnostic modifier code", "modifier_policy_rejected", modifierPolicy.Errors[0].Code);
        AssertEqual("diagnostic modifier policy", "None", modifierPolicy.Errors[0].Policy);
    }

    private void Data_ReferenceAndArrayContracts_ShouldNormalizeRuntimeTypes()
    {
        var storage = CreateRuntimeStorage(
            Definition("AvailableAnimations", DataValueType.StringArray, Array.Empty<string>()),
            new DataDefinition
            {
                StableKey = "Feature.Modifiers",
                ValueType = DataValueType.ModifierList,
                RuntimeTypeId = "slime.data.Features.FeatureModifierEntryData[]",
                DefaultValue = Array.Empty<FeatureModifierEntryData>(),
                StoragePolicy = DataStoragePolicy.AuthoringBlob,
                WritePolicy = DataWritePolicy.LoaderOnly
            },
            Definition("AbilityIcon", DataValueType.ObjectRef, null),
            new DataDefinition
            {
                StableKey = "TargetNode",
                ValueType = DataValueType.ObjectRef,
                RuntimeTypeId = "Godot.Node2D",
                DefaultValue = null,
                StoragePolicy = DataStoragePolicy.RuntimeOnly,
                WritePolicy = DataWritePolicy.SystemOnly
            });

        AssertTrue("json string_array accepted", storage.SetUntyped("AvailableAnimations", "[\"idle\",\"run\"]", DataWriteSource.Loader));
        var animations = storage.Get<string[]>("AvailableAnimations");
        AssertEqual("json string_array count", 2, animations.Length);
        AssertEqual("json string_array value", "run", animations[1]);

        AssertTrue("json modifier_list accepted", storage.SetUntyped("Feature.Modifiers", "[{\"DataKeyName\":\"BaseHp\",\"ModifierType\":0,\"Value\":5,\"Priority\":2}]", DataWriteSource.Loader));
        var modifiers = storage.Get<FeatureModifierEntryData[]>("Feature.Modifiers");
        AssertEqual("json modifier_list count", 1, modifiers.Length);
        AssertEqual("json modifier key", "BaseHp", modifiers[0].DataKeyName);
        AssertEqual("json modifier type", ModifierType.Additive, modifiers[0].ModifierType);

        AssertTrue("resource ref string accepted", storage.SetUntyped("AbilityIcon", "res://icon.png", DataWriteSource.Loader));
        AssertEqual("resource ref path", "res://icon.png", storage.Get<ResourceRef>("AbilityIcon").Path);

        using var node = new Node2D();
        AssertFalse("node ref rejects string", storage.TrySetUntyped("TargetNode", "res://bad.tscn", DataWriteSource.System, out var nodeString));
        AssertEqual("node ref string code", "wrong_clr_type", nodeString.Errors[0].Code);
        AssertTrue("node ref accepts node", storage.SetUntyped("TargetNode", node, DataWriteSource.System));
        AssertEqual("node ref value", node, storage.Get<Node2D>("TargetNode"));
    }

    private void Data_Set_ShouldRespectWritePolicy()
    {
        var storage = CreateRuntimeStorage(
            Definition("Attribute.LoaderOnly", DataValueType.Float, 0f, writePolicy: DataWritePolicy.LoaderOnly),
            Definition("Attribute.SystemOnly", DataValueType.Float, 0f, writePolicy: DataWritePolicy.SystemOnly),
            Definition("Attribute.Computed", DataValueType.Float, 0f, writePolicy: DataWritePolicy.ComputedReadonly),
            Definition("Attribute.DebugOnly", DataValueType.Float, 0f, writePolicy: DataWritePolicy.DebugOnly));

        AssertFalse("loader only rejects runtime", storage.SetUntyped("Attribute.LoaderOnly", 10f, DataWriteSource.Runtime));
        AssertTrue("loader only accepts loader", storage.SetUntyped("Attribute.LoaderOnly", 10f, DataWriteSource.Loader));
        AssertFalse("system only rejects runtime", storage.SetUntyped("Attribute.SystemOnly", 20f, DataWriteSource.Runtime));
        AssertTrue("system only accepts system", storage.SetUntyped("Attribute.SystemOnly", 20f, DataWriteSource.System));
        AssertFalse("computed rejects write", storage.SetUntyped("Attribute.Computed", 30f, DataWriteSource.System));
        AssertFalse("debug only rejects runtime", storage.SetUntyped("Attribute.DebugOnly", 40f, DataWriteSource.Runtime));
        AssertTrue("debug only accepts debug", storage.SetUntyped("Attribute.DebugOnly", 40f, DataWriteSource.Debug));
    }

    private void Data_Set_ShouldApplyRangePolicy()
    {
        var storage = CreateRuntimeStorage(
            Definition("Attribute.Clamp", DataValueType.Float, 0f, minValue: 0f, maxValue: 100f, rangePolicy: DataRangePolicy.ClampRuntime),
            Definition("Attribute.Reject", DataValueType.Float, 0f, minValue: 0f, maxValue: 100f, rangePolicy: DataRangePolicy.RejectRuntime),
            Definition("Attribute.Validate", DataValueType.Float, 0f, minValue: 0f, maxValue: 100f, rangePolicy: DataRangePolicy.Validate));

        AssertTrue("clamp runtime accepts", storage.SetUntyped("Attribute.Clamp", 120f, DataWriteSource.Runtime));
        AssertEqual("clamped value", 100f, storage.Get<float>("Attribute.Clamp"));
        AssertFalse("clamp runtime rejects loader out of range", storage.SetUntyped("Attribute.Clamp", 120f, DataWriteSource.Loader));
        AssertFalse("reject runtime rejects", storage.SetUntyped("Attribute.Reject", 120f, DataWriteSource.Runtime));
        AssertFalse("validate rejects loader", storage.SetUntyped("Attribute.Validate", -1f, DataWriteSource.Loader));
    }

    private void Data_Set_ShouldRespectAllowedValues()
    {
        var data = new Data(BuildCatalogFromDefinitions(null, Definition(
            "Unit.Team",
            DataValueType.String,
            "neutral",
            allowedValues: new[] { new DataAllowedValue { Value = "player", Label = "Player" }, new DataAllowedValue { Value = "enemy", Label = "Enemy" } })));

        AssertTrue("allowed value accepted", data.Set(new DataKey<string>("Unit.Team"), "player"));
        AssertFalse("unknown allowed value rejected", data.Set(new DataKey<string>("Unit.Team"), "boss"));
        AssertEqual("value after rejected allowed value", "player", data.Get(new DataKey<string>("Unit.Team")));
    }

    private void Data_RemoveAndClear_ShouldReturnDescriptorDefault()
    {
        var data = new Data(BuildCatalogFromDefinitions(null,
            Definition("Attribute.BaseHp", DataValueType.Float, 10f),
            Definition("Attribute.Speed", DataValueType.Float, 1f)));

        data.Set(new DataKey<float>("Attribute.BaseHp"), 20f);
        data.Set(new DataKey<float>("Attribute.Speed"), 2f);
        AssertTrue("remove existing", data.Remove("Attribute.BaseHp"));
        AssertEqual("default after remove", 10f, data.Get(new DataKey<float>("Attribute.BaseHp")));
        data.Clear();
        AssertEqual("default after clear", 1f, data.Get(new DataKey<float>("Attribute.Speed")));
    }

    private void Data_Set_ShouldPublishPropertyChanged()
    {
        var storage = CreateRuntimeStorage(Definition("Attribute.BaseHp", DataValueType.Float, 10f));
        var changes = new List<DataChangeRecord>();
        storage.Changed += changes.Add;

        storage.SetUntyped("Attribute.BaseHp", 20f, DataWriteSource.Runtime);

        AssertEqual("change count", 1, changes.Count);
        AssertEqual("change key", "Attribute.BaseHp", changes[0].StableKey);
        AssertEqual("change old value", 10f, changes[0].OldValue);
        AssertEqual("change new value", 20f, changes[0].NewValue);
    }

    private void Data_AddModifier_ShouldRespectModifierPolicy()
    {
        var storage = CreateRuntimeStorage(
            Definition("Attribute.BaseDamage", DataValueType.Float, 10f, modifierPolicy: DataModifierPolicy.Numeric),
            Definition("Unit.Name", DataValueType.String, "Slime"),
            Definition("Attribute.DebugOnly", DataValueType.Float, 10f, modifierPolicy: DataModifierPolicy.DebugOnly));

        AssertTrue("numeric modifier accepted", storage.AddModifier("Attribute.BaseDamage", new DataModifier(ModifierType.Additive, 5f, id: "damage_bonus")));
        AssertFalse("non numeric modifier rejected", storage.AddModifier("Unit.Name", new DataModifier(ModifierType.Additive, 5f, id: "name_bonus")));
        AssertFalse("debug only rejects runtime modifier", storage.AddModifier("Attribute.DebugOnly", new DataModifier(ModifierType.Additive, 5f, id: "runtime_debug_bonus")));
        AssertTrue("debug modifier accepted", storage.AddModifier("Attribute.DebugOnly", new DataModifier(ModifierType.Additive, 5f, id: "debug_bonus"), DataWriteSource.Debug));
    }

    private void Data_AddModifier_ShouldRejectUnknownTarget()
    {
        var storage = CreateRuntimeStorage(Definition("Attribute.BaseDamage", DataValueType.Float, 10f, modifierPolicy: DataModifierPolicy.Numeric));
        AssertThrowsMessage<KeyNotFoundException>(
            "unknown modifier target rejected",
            () => storage.AddModifier("Attribute.Missing", new DataModifier(ModifierType.Additive, 5f)),
            "Attribute.Missing");
    }

    private void Data_AddModifier_ShouldApplyModifierPipeline()
    {
        var data = new Data(BuildCatalogFromDefinitions(null, Definition("Attribute.Power", DataValueType.Float, 100f, maxValue: 200f, modifierPolicy: DataModifierPolicy.Numeric)));
        data.TryAddModifier("Attribute.Power", new DataModifier(ModifierType.Additive, 50f, priority: 0, id: "add"));
        data.TryAddModifier("Attribute.Power", new DataModifier(ModifierType.Multiplicative, 1.2f, priority: 1, id: "mul"));
        data.TryAddModifier("Attribute.Power", new DataModifier(ModifierType.FinalAdditive, 5f, priority: 2, id: "final"));
        AssertEqual("additive multiplicative final additive", 185f, data.Get(new DataKey<float>("Attribute.Power")));

        data.TryAddModifier("Attribute.Power", new DataModifier(ModifierType.Cap, 150f, priority: 3, id: "cap"));
        AssertEqual("cap modifier", 150f, data.Get(new DataKey<float>("Attribute.Power")));

        data.TryAddModifier("Attribute.Power", new DataModifier(ModifierType.Override, 80f, priority: -1, id: "override"));
        AssertEqual("override modifier", 80f, data.Get(new DataKey<float>("Attribute.Power")));
    }

    private void Data_RemoveModifiersBySource_ShouldOnlyRemoveMatchingSource()
    {
        var sourceA = new object();
        var sourceB = new object();
        var storage = CreateRuntimeStorage(Definition("Attribute.Speed", DataValueType.Float, 10f, modifierPolicy: DataModifierPolicy.Numeric));
        storage.AddModifier("Attribute.Speed", new DataModifier(ModifierType.Additive, 5f, id: "a", source: sourceA));
        storage.AddModifier("Attribute.Speed", new DataModifier(ModifierType.Additive, 7f, id: "b", source: sourceB));

        AssertEqual("removed source count", 1, storage.RemoveModifiersBySource(sourceA));
        AssertEqual("only matching source removed", 17f, storage.Get<float>("Attribute.Speed"));
        AssertEqual("remaining modifier count", 1, storage.GetModifiers("Attribute.Speed").Count);
    }

    private void Data_ModifierChange_ShouldPublishChangeAndDirtyDependents()
    {
        var registry = CreateRegistry(new FixedComputeResolver("FinalPowerResolver"));
        var catalog = BuildCatalogFromDefinitions(registry,
            Definition("Attribute.Power", DataValueType.Float, 100f, modifierPolicy: DataModifierPolicy.Numeric),
            new DataDefinition
            {
                StableKey = "Attribute.FinalPower",
                ValueType = DataValueType.Float,
                DefaultValue = 0f,
                StoragePolicy = DataStoragePolicy.Computed,
                WritePolicy = DataWritePolicy.ComputedReadonly,
                ComputeId = "FinalPowerResolver",
                Dependencies = new[] { "Attribute.Power" }
            });
        var storage = new DataRuntimeStorage(catalog);
        var changes = new List<DataChangeRecord>();
        storage.Changed += changes.Add;

        storage.AddModifier("Attribute.Power", new DataModifier(ModifierType.Additive, 25f, id: "bonus"));

        AssertEqual("modifier change count", 1, changes.Count);
        AssertEqual("modifier old value", 100f, changes[0].OldValue);
        AssertEqual("modifier new value", 125f, changes[0].NewValue);
        AssertTrue("dependent computed dirty", storage.IsComputedDirty("Attribute.FinalPower"));
    }

    private void Data_GetComputed_ShouldUseResolverDependenciesAndComputeParams()
    {
        var registry = CreateRegistry(new ParametricAddResolver());
        var data = CreateRuntimeDataFromDescriptors(registry, new[]
        {
            Descriptor("Attribute.BaseHp", "float", "100"),
            Descriptor("Attribute.HpBonus", "float", "25"),
            Descriptor("Attribute.FinalHp", "float", "0") with
            {
                StoragePolicy = "computed",
                WritePolicy = "computed_readonly",
                ComputeId = "ParametricAdd",
                Dependencies = new List<string> { "Attribute.BaseHp", "Attribute.HpBonus" },
                ComputeParams = new Dictionary<string, string> { ["bonus_multiplier"] = "2" }
            }
        });

        AssertEqual("computed uses dependencies and params", 150f, data.Get<float>("Attribute.FinalHp"));
    }

    private void Data_GetComputed_ShouldCacheUntilDependencyChanges()
    {
        var resolver = new CountingAttributeBonusResolver();
        var registry = CreateRegistry(resolver);
        var data = CreateRuntimeDataFromDescriptors(registry, new[]
        {
            Descriptor("Attribute.BaseAttack", "float", "100"),
            Descriptor("Attribute.AttackBonus", "float", "50"),
            Descriptor("Attribute.FinalAttack", "float", "0") with
            {
                StoragePolicy = "computed",
                WritePolicy = "computed_readonly",
                ComputeId = "CountingAttributeBonus",
                Dependencies = new List<string> { "Attribute.BaseAttack", "Attribute.AttackBonus" }
            }
        });

        AssertEqual("first computed value", 150f, data.Get<float>("Attribute.FinalAttack"));
        AssertEqual("cached computed value", 150f, data.Get<float>("Attribute.FinalAttack"));
        AssertEqual("cached resolver count", 1, resolver.ComputeCount);
        data.SetUntyped("Attribute.BaseAttack", 200f, DataWriteSource.Runtime);
        AssertEqual("recomputed after dependency set", 300f, data.Get<float>("Attribute.FinalAttack"));
        AssertEqual("dirty resolver count", 2, resolver.ComputeCount);
    }

    private void Data_ComputedDirty_ShouldPropagateTransitively()
    {
        var registry = CreateRegistry(new ParametricAddResolver(), new CountingAttributeBonusResolver());
        var data = CreateRuntimeDataFromDescriptors(registry, new[]
        {
            Descriptor("A", "float", "10"),
            Descriptor("B", "float", "0") with
            {
                StoragePolicy = "computed",
                WritePolicy = "computed_readonly",
                ComputeId = "ParametricAdd",
                Dependencies = new List<string> { "A" }
            },
            Descriptor("C", "float", "0") with
            {
                StoragePolicy = "computed",
                WritePolicy = "computed_readonly",
                ComputeId = "CountingAttributeBonus",
                Dependencies = new List<string> { "B" }
            }
        });

        AssertEqual("initial transitive computed", 10f, data.Get<float>("C"));
        data.SetUntyped("A", 20f, DataWriteSource.Runtime);
        AssertEqual("transitive recomputed value", 20f, data.Get<float>("C"));
    }
}
