using Game.Core.Diagnostics;
using Game.Core.Events;

namespace Game.Core.Tests;

public class EventBusTests
{
    [Fact]
    public void Publish_DeliversEventToSubscriber()
    {
        EventBus bus = new();
        TestEvent? received = null;
        TestEvent published = new(5);

        using IDisposable subscription = bus.Subscribe<TestEvent>(domainEvent => received = domainEvent);
        bus.Publish(published);

        Assert.Same(published, received);
    }

    [Fact]
    public void Publish_DeliversToAllSubscribersInSubscriptionOrder()
    {
        EventBus bus = new();
        List<int> order = new();

        bus.Subscribe<TestEvent>(_ => order.Add(1));
        bus.Subscribe<TestEvent>(_ => order.Add(2));
        bus.Subscribe<TestEvent>(_ => order.Add(3));

        bus.Publish(new TestEvent(0));

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    [Fact]
    public void DisposeSubscription_StopsDelivery()
    {
        EventBus bus = new();
        int calls = 0;

        IDisposable subscription = bus.Subscribe<TestEvent>(_ => calls++);
        bus.Publish(new TestEvent(1));
        subscription.Dispose();
        bus.Publish(new TestEvent(2));

        Assert.Equal(1, calls);
    }

    [Fact]
    public void Publish_WithoutSubscribers_DoesNotThrow()
    {
        EventBus bus = new();

        bus.Publish(new TestEvent(1));
    }

    [Fact]
    public void HasSubscribers_ReflectsSubscriptions()
    {
        EventBus bus = new();

        Assert.False(bus.HasSubscribers<TestEvent>());

        IDisposable subscription = bus.Subscribe<TestEvent>(_ => { });
        Assert.True(bus.HasSubscribers<TestEvent>());

        subscription.Dispose();
        Assert.False(bus.HasSubscribers<TestEvent>());
    }

    [Fact]
    public void Clear_RemovesAllSubscriptions()
    {
        EventBus bus = new();
        int calls = 0;

        bus.Subscribe<TestEvent>(_ => calls++);
        bus.Clear();
        bus.Publish(new TestEvent(1));

        Assert.Equal(0, calls);
        Assert.False(bus.HasSubscribers<TestEvent>());
    }

    [Fact]
    public void Subscribe_RejectsNullHandler()
    {
        EventBus bus = new();

        Assert.Throws<ArgumentNullException>(() => bus.Subscribe<TestEvent>(null!));
    }

    [Fact]
    public void Publish_PropagatesHandlerException_AndLogsIt()
    {
        TestLogger logger = new();
        EventBus bus = new(logger);
        bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("boom"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => bus.Publish(new TestEvent(1)));

        Assert.Equal("boom", exception.Message);
        LogEvent logEvent = Assert.Single(logger.Events.Where(entry => entry.Level == LogLevel.Error));
        Assert.Equal(LogLevel.Error, logEvent.Level);
        Assert.Equal("Events", logEvent.Category);
        Assert.Equal("InvalidOperationException", logEvent.Fields["exceptionType"]);
    }

    private sealed record TestEvent(int Value) : IDomainEvent
    {
        public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UnixEpoch;
    }

    private sealed class TestLogger : IGameLogger
    {
        public List<LogEvent> Events { get; } = new();

        public bool IsEnabled(LogLevel level)
        {
            return true;
        }

        public void Log(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}
