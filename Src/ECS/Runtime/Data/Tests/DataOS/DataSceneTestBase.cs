using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Slime.Test.DataOS;

/// <summary>
/// DataOS 单场景测试基类。
/// </summary>
public abstract partial class DataSceneTestBase : Node
{
    private int _passedCount;
    private int _failedCount;
    private ValidationSession? _validationSession;

    protected DataRuntimeBootstrap Bootstrap => DataRuntimeBootstrap.Default;

    protected JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override void _Ready()
    {
        var typeName = GetType().Name;
        _validationSession = ValidationSession.Start(new ValidationSessionOptions
        {
            Name = typeName,
            Owner = "Data",
            ArtifactPath = Path.Combine(".ai-temp", "scene-tests", "artifacts", $"{typeName}.json"),
            ExpectedInputs = "DataOS runtime scene assertions",
            ExpectedObservations = "DataOS checks are written to validation artifact and structured log",
            PassCriteria = "all DataOS checks pass",
            FailCriteria = "any DataOS assertion fails or throws unexpectedly"
        });

        try
        {
            RunTests();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _validationSession.Complete();
        Log.Flush();
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    protected abstract void RunTests();

    protected void AssertEqual<T>(string label, T expected, T actual)
    {
        if (!Equals(expected, actual))
        {
            Fail($"{label}: expected={expected}, actual={actual}");
            return;
        }

        Pass(label);
    }

    protected void AssertTrue(string label, bool condition)
    {
        if (!condition)
        {
            Fail(label);
            return;
        }

        Pass(label);
    }

    protected void AssertFalse(string label, bool condition)
    {
        AssertTrue(label, !condition);
    }

    protected void AssertContains(string label, string actual, string expectedPart)
    {
        if (!actual.Contains(expectedPart, StringComparison.Ordinal))
        {
            Fail($"{label}: expected contains={expectedPart}, actual={actual}");
            return;
        }

        Pass(label);
    }

    protected void AssertThrows<TException>(string label, Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            Pass(label);
            return;
        }
        catch (Exception ex)
        {
            Fail($"{label}: unexpected={ex.GetType().Name}");
            return;
        }

        Fail($"{label}: no exception");
    }

    protected void AssertThrowsMessage<TException>(string label, Action action, string messagePart)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex) when (ex.Message.Contains(messagePart, StringComparison.OrdinalIgnoreCase))
        {
            Pass(label);
            return;
        }
        catch (Exception ex)
        {
            Fail($"{label}: unexpected={ex.GetType().Name}, message={ex.Message}");
            return;
        }

        Fail($"{label}: no exception");
    }

    protected RuntimeDataSnapshotLoader CreateLoader(params IDataComputeResolver[] resolvers)
    {
        return new RuntimeDataSnapshotLoader(CreateRegistry(resolvers));
    }

    protected RuntimeDataSnapshotLoader CreateLoaderWithDefaultResolvers(params IDataComputeResolver[] extraResolvers)
    {
        var registry = CreateRegistry(
            new AttributeBonusComputeResolver(),
            new PercentComputeResolver(),
            new AttackIntervalComputeResolver(),
            new RegenComputeResolver(),
            new EffectiveHpComputeResolver(),
            new DpsComputeResolver());
        for (var i = 0; i < extraResolvers.Length; i++)
        {
            registry.Register(extraResolvers[i]);
        }

        return new RuntimeDataSnapshotLoader(registry);
    }

    protected DataComputeRegistry CreateRegistry(params IDataComputeResolver[] resolvers)
    {
        var registry = new DataComputeRegistry();
        for (var i = 0; i < resolvers.Length; i++)
        {
            registry.Register(resolvers[i]);
        }

        return registry;
    }

    protected RuntimeDataDescriptorDto Descriptor(string stableKey, string valueType, object? defaultValue)
    {
        return new RuntimeDataDescriptorDto
        {
            StableKey = stableKey,
            ValueType = valueType,
            DefaultValue = defaultValue,
            StoragePolicy = "persisted",
            WritePolicy = "read_write",
            RangePolicy = "none",
            ModifierPolicy = "none",
            MigrationPolicy = "default"
        };
    }

    protected DataDefinition Definition(
        string stableKey,
        DataValueType valueType,
        object? defaultValue,
        float? minValue = null,
        float? maxValue = null,
        DataWritePolicy writePolicy = DataWritePolicy.ReadWrite,
        DataRangePolicy rangePolicy = DataRangePolicy.None,
        DataModifierPolicy modifierPolicy = DataModifierPolicy.None,
        IReadOnlyList<DataAllowedValue>? allowedValues = null)
    {
        return new DataDefinition
        {
            StableKey = stableKey,
            ValueType = valueType,
            DefaultValue = defaultValue,
            MinValue = minValue,
            MaxValue = maxValue,
            WritePolicy = writePolicy,
            RangePolicy = rangePolicy,
            ModifierPolicy = modifierPolicy,
            AllowedValues = allowedValues ?? []
        };
    }

    protected DataRuntimeStorage CreateRuntimeStorage(params DataDefinition[] definitions)
    {
        var catalog = BuildCatalogFromDefinitions(null, definitions);
        return new DataRuntimeStorage(catalog);
    }

    protected DataDefinitionCatalog BuildCatalogFromDefinitions(DataComputeRegistry? registry, params DataDefinition[] definitions)
    {
        var catalog = new DataDefinitionCatalog();
        if (registry != null)
        {
            catalog.BindComputeRegistry(registry);
        }

        for (var i = 0; i < definitions.Length; i++)
        {
            catalog.Register(definitions[i]);
        }

        catalog.ValidateAndBuildIndexes();
        return catalog;
    }

    protected Data CreateRuntimeDataFromDescriptors(DataComputeRegistry registry, IEnumerable<RuntimeDataDescriptorDto> descriptors)
    {
        var loader = new RuntimeDataSnapshotLoader(registry);
        var catalog = loader.BuildCatalog(descriptors);
        return new Data(catalog);
    }

    protected string ResolveRepositoryPath(string relativePath)
    {
        var projectRoot = ProjectSettings.GlobalizePath("res://");
        var candidates = new[]
        {
            Path.Combine(projectRoot, relativePath),
            Path.Combine(System.Environment.CurrentDirectory, relativePath),
            Path.Combine(AppContext.BaseDirectory, relativePath)
        };

        for (var i = 0; i < candidates.Length; i++)
        {
            var fullPath = Path.GetFullPath(candidates[i]);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new FileNotFoundException($"{relativePath} not found");
    }

    protected string ResolveDataOsPath(string relativePath)
    {
        return ResolveRepositoryPath(Path.Combine("Data", "DataOS", relativePath));
    }

    private void Pass(string label)
    {
        _passedCount++;
        _validationSession?.Check(
            $"{GetType().Name}.{label}",
            true,
            expected: "pass",
            actual: "pass",
            reasonCode: "dataos-check-pass");
    }

    protected void Fail(string message)
    {
        _failedCount++;
        _validationSession?.Check(
            $"{GetType().Name}.failure",
            false,
            expected: "pass",
            actual: message,
            reasonCode: "dataos-check-fail",
            message: message);
    }
}
