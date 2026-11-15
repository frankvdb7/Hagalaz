using System;
using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Mediator;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class GameMediatorTests
{
    private sealed record TestMessage(int Id);

    [TestMethod]
    public async Task Publish_WhenHandlerThrows_LogsErrorWithMessageTypeAndException()
    {
        var failure = new InvalidOperationException("handler failed");
        var logger = new RecordingLogger<GameMediator>();
        await using var provider = new ServiceCollection().AddMediator(_ => { }).BuildServiceProvider(true);
        var mediator = new GameMediator(provider.GetRequiredService<IMediator>(), logger);
        var handled = new TaskCompletionSource();
        mediator.ConnectHandler<TestMessage>(_ =>
        {
            handled.SetResult();
            throw failure;
        });

        mediator.Publish(new TestMessage(1));

        await handled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var entry = await logger.WaitForEntryAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(LogLevel.Error, entry.Level);
        Assert.AreSame(failure, entry.Exception);
        StringAssert.Contains(entry.Message, nameof(TestMessage));
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly TaskCompletionSource<(LogLevel Level, Exception? Exception, string Message)> _first = new();

        public Task<(LogLevel Level, Exception? Exception, string Message)> WaitForEntryAsync(TimeSpan timeout) =>
            _first.Task.WaitAsync(timeout);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            _first.TrySetResult((logLevel, exception, formatter(state, exception)));
    }
}
