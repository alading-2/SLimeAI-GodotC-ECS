using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Slime.Test.DataOS;

/// <summary>
/// 验证 descriptor catalog 与 generated DataKey handle。
/// </summary>
public partial class DataCatalogTestScene : DataSceneTestBase
{
    protected override void RunTests()
    {
        BuildCatalog_ShouldRejectDuplicateStableKey();
        BuildCatalog_ShouldRejectEmptyStableKey();
        BuildCatalog_ShouldRejectUnknownValueType();
        BuildCatalog_ShouldConvertDescriptorDefaultValue();
        BuildCatalog_ShouldConvertTypedAndJsonDefaultValue();
        BuildCatalog_ShouldConvertStringArrayDefaultValue();
        BuildCatalog_ShouldConsumeRepositoryRuntimeSnapshotDescriptors();
        RepositorySnapshot_ShouldContainMigratedLegacyDescriptors();
        GeneratedDataKeyHandles_ShouldBeThinStableKeyHandles();
        BuildCatalog_ShouldConsumeMinimalDescriptorFixture();
        BuildCatalog_ShouldRejectInvalidDefaultValue();
        BuildCatalog_ShouldRejectInvalidPolicy();
        BuildCatalog_ShouldBuildDependentComputedIndex();
        BuildCatalog_ShouldRejectUnknownDependency();
        BuildCatalog_ShouldRejectComputeCycle();
        BuildCatalog_ShouldRejectComputedWithoutComputeId();
        BuildCatalog_ShouldRejectMissingComputeResolver();
        BuildCatalog_ShouldBuildIndexesIdempotently();
        BuildCatalog_ShouldFreezeAfterIndexBuild();
        DataComputeRegistry_ShouldRejectInvalidRegistrations();
    }

    private void BuildCatalog_ShouldRejectDuplicateStableKey()
    {
        var loader = CreateLoader();
        AssertThrowsMessage<InvalidOperationException>(
            "reject duplicate stable key",
            () => loader.BuildCatalog(new[] { Descriptor("Attribute.BaseHp", "float", "10"), Descriptor("Attribute.BaseHp", "float", "20") }),
            "重复");
    }

    private void BuildCatalog_ShouldRejectEmptyStableKey()
    {
        var loader = CreateLoader();
        AssertThrowsMessage<InvalidOperationException>(
            "reject empty stable key",
            () => loader.BuildCatalog(new[] { Descriptor("", "float", "10") }),
            "StableKey");
    }

    private void BuildCatalog_ShouldRejectUnknownValueType()
    {
        var loader = CreateLoader();
        AssertThrowsMessage<InvalidOperationException>(
            "reject unknown value type",
            () => loader.BuildCatalog(new[] { Descriptor("Attribute.BaseHp", "unknown", "10") }),
            "value_type");
    }

    private void BuildCatalog_ShouldConvertDescriptorDefaultValue()
    {
        var catalog = CreateLoader().BuildCatalog(new[]
        {
            Descriptor("Attribute.BaseHp", "float", "10.5"),
            Descriptor("Ability.Level", "int", "3"),
            Descriptor("Feature.Enabled", "bool", "true"),
            Descriptor("Unit.Name", "string", "Slime")
        });

        AssertEqual("float default", 10.5f, catalog.GetRequired("Attribute.BaseHp").DefaultValue);
        AssertEqual("int default", 3, catalog.GetRequired("Ability.Level").DefaultValue);
        AssertEqual("bool default", true, catalog.GetRequired("Feature.Enabled").DefaultValue);
        AssertEqual("string default", "Slime", catalog.GetRequired("Unit.Name").DefaultValue);
    }

    private void BuildCatalog_ShouldConvertTypedAndJsonDefaultValue()
    {
        using var json = JsonDocument.Parse("12.5");
        var catalog = CreateLoader().BuildCatalog(new[]
        {
            Descriptor("Attribute.BaseHp", "float", 10.5f),
            Descriptor("Ability.Level", "int", 3),
            Descriptor("Feature.Enabled", "bool", true),
            Descriptor("Attribute.JsonFloat", "float", json.RootElement.Clone())
        });

        AssertEqual("typed float default", 10.5f, catalog.GetRequired("Attribute.BaseHp").DefaultValue);
        AssertEqual("typed int default", 3, catalog.GetRequired("Ability.Level").DefaultValue);
        AssertEqual("typed bool default", true, catalog.GetRequired("Feature.Enabled").DefaultValue);
        AssertEqual("json float default", 12.5f, catalog.GetRequired("Attribute.JsonFloat").DefaultValue);
    }

    private void BuildCatalog_ShouldConvertStringArrayDefaultValue()
    {
        var catalog = CreateLoader().BuildCatalog(new[]
        {
            Descriptor("System.Dependencies", "string_array", "DamageService,TimerManager")
        });
        var defaultValue = (string[])catalog.GetRequired("System.Dependencies").DefaultValue!;

        AssertEqual("string array value type", DataValueType.StringArray, catalog.GetRequired("System.Dependencies").ValueType);
        AssertEqual("string array default count", 2, defaultValue.Length);
        AssertEqual("string array default[0]", "DamageService", defaultValue[0]);
        AssertEqual("string array default[1]", "TimerManager", defaultValue[1]);
    }

    private void BuildCatalog_ShouldConsumeRepositoryRuntimeSnapshotDescriptors()
    {
        var catalog = Bootstrap.Catalog;
        AssertEqual("descriptor count", 212, catalog.Count);
        AssertTrue("FinalHp descriptor exists", catalog.TryGet(GeneratedDataKey.FinalHp.StableKey, out var finalHp));
        AssertEqual("FinalHp is computed", DataStoragePolicy.Computed, finalHp.StoragePolicy);
        AssertEqual("Dependencies descriptor is string array", DataValueType.StringArray, catalog.GetRequired("Dependencies").ValueType);
    }

    private void RepositorySnapshot_ShouldContainMigratedLegacyDescriptors()
    {
        var catalog = Bootstrap.Catalog;
        AssertEqual("FinalHp compute id", "AttributeBonus", catalog.GetRequired("FinalHp").ComputeId);
        AssertEqual("Feature.Modifiers storage policy", DataStoragePolicy.AuthoringBlob, catalog.GetRequired("Feature.Modifiers").StoragePolicy);
        AssertEqual("TargetNode runtime ref type", DataValueType.ObjectRef, catalog.GetRequired("TargetNode").ValueType);
    }

    private void GeneratedDataKeyHandles_ShouldBeThinStableKeyHandles()
    {
        var generated = File.ReadAllText(ResolveRepositoryPath("Data/DataKey/Generated/DataKey_Generated.cs"));
        AssertContains("FinalHp generated handle", generated, "public static readonly DataKey<float> FinalHp = new(\"FinalHp\");");
        AssertContains("Feature.Modifiers generated handle", generated, "public static readonly DataKey<slime.data.Features.FeatureModifierEntryData[]> FeatureModifiers = new(\"Feature.Modifiers\");");
        AssertFalse("generated handle has no defaults", generated.Contains("DefaultValue", StringComparison.Ordinal));
        AssertFalse("generated handle has no range policy", generated.Contains("RangePolicy", StringComparison.Ordinal));
        AssertFalse("generated handle has no modifier policy", generated.Contains("ModifierPolicy", StringComparison.Ordinal));
        AssertEqual("generated key is thin stable handle", "FinalHp", GeneratedDataKey.FinalHp.StableKey);
        AssertFalse("generated handle has no compatibility alias", generated.Contains("public static partial class DataKey", StringComparison.Ordinal));
    }

    private void BuildCatalog_ShouldConsumeMinimalDescriptorFixture()
    {
        var loader = CreateLoader(new FixedComputeResolver("Additive"));
        var snapshot = JsonSerializer.Deserialize<RuntimeDataSnapshot>(
            File.ReadAllText(ResolveDataOsPath("Snapshots/Fixtures/minimal_descriptor_snapshot.json")),
            JsonOptions) ?? throw new InvalidOperationException("minimal descriptor fixture deserialize failed");
        var catalog = loader.BuildCatalog(snapshot.Descriptors);

        AssertEqual("fixture modifier policy", DataModifierPolicy.Numeric, catalog.GetRequired("Attribute.BaseDamage").ModifierPolicy);
        AssertEqual("fixture runtime state", DataStoragePolicy.RuntimeState, catalog.GetRequired("Attribute.SessionDamageBonus").StoragePolicy);
        AssertTrue("fixture computed", catalog.GetRequired("Attribute.FinalDamage").IsComputed);
        AssertEqual("fixture authoring blob", DataStoragePolicy.AuthoringBlob, catalog.GetRequired("Feature.Modifiers").StoragePolicy);
        AssertEqual("fixture allowed values", 3, catalog.GetRequired("Unit.Team").AllowedValues.Count);
    }

    private void BuildCatalog_ShouldRejectInvalidDefaultValue()
    {
        AssertThrowsMessage<InvalidOperationException>(
            "reject invalid default",
            () => CreateLoader().BuildCatalog(new[] { Descriptor("Ability.Level", "int", "not-int") }),
            "default");
    }

    private void BuildCatalog_ShouldRejectInvalidPolicy()
    {
        AssertThrowsMessage<InvalidOperationException>(
            "reject invalid policy",
            () => CreateLoader().BuildCatalog(new[] { Descriptor("Attribute.BaseHp", "float", "10") with { StoragePolicy = "bad_policy" } }),
            "storage_policy");
    }

    private void BuildCatalog_ShouldBuildDependentComputedIndex()
    {
        var catalog = CreateLoader(new FixedComputeResolver("FinalHpResolver")).BuildCatalog(new[]
        {
            Descriptor("Attribute.BaseHp", "float", "100"),
            Descriptor("Attribute.HpBonus", "float", "20"),
            Descriptor("Attribute.FinalHp", "float", "0") with
            {
                StoragePolicy = "computed",
                WritePolicy = "computed_readonly",
                ComputeId = "FinalHpResolver",
                Dependencies = new List<string> { "Attribute.BaseHp", "Attribute.HpBonus" }
            }
        });

        var dependents = catalog.GetDependentComputedKeys("Attribute.BaseHp");
        AssertEqual("dependent count", 1, dependents.Count);
        AssertEqual("dependent key", "Attribute.FinalHp", dependents[0]);
    }

    private void BuildCatalog_ShouldRejectUnknownDependency()
    {
        AssertThrowsMessage<InvalidOperationException>(
            "reject unknown dependency",
            () => CreateLoader(new FixedComputeResolver("FinalHpResolver")).BuildCatalog(new[]
            {
                Descriptor("Attribute.FinalHp", "float", "0") with
                {
                    StoragePolicy = "computed",
                    WritePolicy = "computed_readonly",
                    ComputeId = "FinalHpResolver",
                    Dependencies = new List<string> { "Attribute.Missing" }
                }
            }),
            "dependency");
    }

    private void BuildCatalog_ShouldRejectComputeCycle()
    {
        AssertThrowsMessage<InvalidOperationException>(
            "reject compute cycle",
            () => CreateLoader(new FixedComputeResolver("Cycle")).BuildCatalog(new[]
            {
                Descriptor("A", "float", "0") with { StoragePolicy = "computed", WritePolicy = "computed_readonly", ComputeId = "Cycle", Dependencies = new List<string> { "B" } },
                Descriptor("B", "float", "0") with { StoragePolicy = "computed", WritePolicy = "computed_readonly", ComputeId = "Cycle", Dependencies = new List<string> { "A" } }
            }),
            "cycle");
    }

    private void BuildCatalog_ShouldRejectComputedWithoutComputeId()
    {
        AssertThrowsMessage<InvalidOperationException>(
            "reject computed without id",
            () => CreateLoader().BuildCatalog(new[] { Descriptor("Attribute.FinalHp", "float", "0") with { StoragePolicy = "computed", WritePolicy = "computed_readonly" } }),
            "compute_id");
    }

    private void BuildCatalog_ShouldRejectMissingComputeResolver()
    {
        AssertThrowsMessage<InvalidOperationException>(
            "reject missing resolver",
            () => CreateLoader().BuildCatalog(new[]
            {
                Descriptor("Attribute.BaseHp", "float", "100"),
                Descriptor("Attribute.FinalHp", "float", "0") with
                {
                    StoragePolicy = "computed",
                    WritePolicy = "computed_readonly",
                    ComputeId = "MissingResolver",
                    Dependencies = new List<string> { "Attribute.BaseHp" }
                }
            }),
            "resolver");
    }

    private void BuildCatalog_ShouldBuildIndexesIdempotently()
    {
        var catalog = CreateLoader(new FixedComputeResolver("FinalHpResolver")).BuildCatalog(new[]
        {
            Descriptor("Attribute.BaseHp", "float", "100"),
            Descriptor("Attribute.FinalHp", "float", "0") with
            {
                StoragePolicy = "computed",
                WritePolicy = "computed_readonly",
                ComputeId = "FinalHpResolver",
                Dependencies = new List<string> { "Attribute.BaseHp" }
            }
        });

        catalog.ValidateAndBuildIndexes();
        catalog.ValidateAndBuildIndexes();
        AssertEqual("idempotent dependent count", 1, catalog.GetDependentComputedKeys("Attribute.BaseHp").Count);
    }

    private void BuildCatalog_ShouldFreezeAfterIndexBuild()
    {
        var catalog = CreateLoader().BuildCatalog(new[]
        {
            Descriptor("Attribute.BaseHp", "float", "100")
        });

        AssertThrowsMessage<InvalidOperationException>(
            "catalog freezes after build",
            () => catalog.Register(Definition("Attribute.Late", DataValueType.Float, 0f)),
            "frozen");
    }

    private void DataComputeRegistry_ShouldRejectInvalidRegistrations()
    {
        var registry = new DataComputeRegistry();
        registry.Register(new FixedComputeResolver("FinalPower"));

        AssertThrowsMessage<InvalidOperationException>("reject empty resolver id", () => registry.Register(new FixedComputeResolver("")), "ComputeId");
        AssertThrowsMessage<InvalidOperationException>("reject duplicate resolver id", () => registry.Register(new FixedComputeResolver("FinalPower")), "重复");
        AssertThrowsMessage<KeyNotFoundException>("reject missing resolver lookup", () => registry.GetRequired("Missing"), "Missing");
    }

}
