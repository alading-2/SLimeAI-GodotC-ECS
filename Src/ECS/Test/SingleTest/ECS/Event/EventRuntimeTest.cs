using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public partial class EventRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(EventRuntimeTest));
    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        try
        {
            TestSubscribePublishAndDispose();
            TestRegistrationOrder();
            TestHandlerExceptionIsolation();
            TestSameTypeReentryBlocked();
            TestDifferentTypeCascadeAllowed();
            TestDifferentEntitySameTypeAllowed();
            TestEntityAndWorldScopeRejectInvalidPayload();
            TestBroadcastRoutesToEntityAndWorld();
            TestExportObservation();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"Event runtime test completed: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void TestSubscribePublishAndDispose()
    {
        var bus = new EntityEventBus("entity:test-dispose");
        var handled = 0;

        var token = bus.Subscribe<TestEntityEvent>(_ => handled++);
        bus.Publish(new TestEntityEvent(1));
        token.Dispose();
        bus.Publish(new TestEntityEvent(2));

        AssertEqual("Dispose 应停止后续 handler", 1, handled);
    }

    private void TestRegistrationOrder()
    {
        var bus = new EntityEventBus("entity:test-order");
        var order = new List<int>();

        bus.Subscribe<TestEntityEvent>(_ => order.Add(1));
        bus.Subscribe<TestEntityEvent>(_ => order.Add(2));
        bus.Publish(new TestEntityEvent(1));

        AssertEqual("handler 应按注册顺序执行", "1,2", string.Join(",", order));
    }

    private void TestHandlerExceptionIsolation()
    {
        var bus = new EntityEventBus("entity:test-exception");
        var handled = 0;

        bus.Subscribe<TestEntityEvent>(_ => throw new InvalidOperationException("expected-test-exception"));
        bus.Subscribe<TestEntityEvent>(_ => handled++);
        bus.Publish(new TestEntityEvent(1));

        AssertEqual("异常 handler 不应阻断后续 handler", 1, handled);
    }

    private void TestSameTypeReentryBlocked()
    {
        var bus = new EntityEventBus("entity:test-reentry");
        var handled = new List<int>();

        bus.Subscribe<TestEntityEvent>(data =>
        {
            handled.Add(data.Value);
            bus.Publish(new TestEntityEvent(data.Value + 1));
        });
        bus.Subscribe<TestEntityEvent>(data => handled.Add(data.Value * 10));
        bus.Publish(new TestEntityEvent(1));

        AssertEqual("同 bus 同类型重入应被阻断但外层继续", "1,10", string.Join(",", handled));
    }

    private void TestDifferentTypeCascadeAllowed()
    {
        var bus = new EntityEventBus("entity:test-cascade");
        var order = new List<string>();

        bus.Subscribe<TestEntityEvent>(_ =>
        {
            order.Add("entity");
            bus.Publish(new TestCascadeEvent(2));
        });
        bus.Subscribe<TestCascadeEvent>(data => order.Add($"cascade:{data.Value}"));
        bus.Publish(new TestEntityEvent(1));

        AssertEqual("不同类型事件允许级联", "entity,cascade:2", string.Join(",", order));
    }

    private void TestDifferentEntitySameTypeAllowed()
    {
        var first = new EntityEventBus("entity:first");
        var second = new EntityEventBus("entity:second");
        var handled = new List<string>();

        first.Subscribe<TestEntityEvent>(data =>
        {
            handled.Add($"first:{data.Value}");
            second.Publish(new TestEntityEvent(data.Value + 1));
        });
        second.Subscribe<TestEntityEvent>(data => handled.Add($"second:{data.Value}"));
        first.Publish(new TestEntityEvent(1));

        AssertEqual("不同 bus 同类型事件允许嵌套", "first:1,second:2", string.Join(",", handled));
    }

    private void TestEntityAndWorldScopeRejectInvalidPayload()
    {
        var entity = new EntityEventBus("entity:test-scope");
        var world = new WorldEventBus();
        var entityHandled = 0;
        var globalHandled = 0;

        entity.Subscribe<TestGlobalEvent>(_ => globalHandled++);
        world.Subscribe<TestEntityEvent>(_ => entityHandled++);
        entity.Publish(new TestGlobalEvent(1));
        world.Publish(new TestEntityEvent(1));

        AssertEqual("EntityEventBus 应拒绝 global-only payload", 0, globalHandled);
        AssertEqual("WorldEventBus 应拒绝 entity-only payload", 0, entityHandled);
    }

    private void TestBroadcastRoutesToEntityAndWorld()
    {
        var world = new WorldEventBus();
        var entity = new EntityEventBus("entity:test-broadcast", world);
        var entityHandled = 0;
        var worldHandled = 0;

        entity.Subscribe<TestBroadcastEvent>(_ => entityHandled++);
        world.Subscribe<TestBroadcastEvent>(_ => worldHandled++);
        entity.Publish(new TestBroadcastEvent(1));

        AssertEqual("broadcast 应触发 entity handler", 1, entityHandled);
        AssertEqual("broadcast 应触发 world handler", 1, worldHandled);
    }

    private void TestExportObservation()
    {
        var bus = new EntityEventBus("entity:test-observation");
        var token = bus.Subscribe<TestEntityEvent>(_ => { });
        bus.Publish(new TestEntityEvent(5));

        const string path = "user://event-runtime-test-dump.json";
        bus.ExportObservation(path);

        var absolutePath = ProjectSettings.GlobalizePath(path);
        using var json = JsonDocument.Parse(File.ReadAllText(absolutePath));
        var root = json.RootElement;

        AssertEqual("Observation 应包含 BusName", "entity:test-observation", root.GetProperty("BusName").GetString());
        AssertEqual("Observation 应包含 EmittedCounts", true, root.GetProperty("EmittedCounts").EnumerateObject().Any());
        AssertEqual("Observation 应包含 Subscriptions", true, root.GetProperty("Subscriptions").EnumerateObject().Any());

        token.Dispose();
    }

    private void AssertEqual<T>(string name, T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Pass(name);
        }
        else
        {
            Fail($"{name}: expected={expected}, actual={actual}");
        }
    }

    private void Pass(string name)
    {
        _passedCount++;
        _log.Info($"PASS: {name}");
    }

    private void Fail(string name)
    {
        _failedCount++;
        _log.Error($"FAIL: {name}");
    }

    private readonly record struct TestEntityEvent(int Value) : IEntityEvent;
    private readonly record struct TestCascadeEvent(int Value) : IEntityEvent;
    private readonly record struct TestGlobalEvent(int Value) : IGlobalEvent;
    private readonly record struct TestBroadcastEvent(int Value) : IBroadcastEvent;
}
