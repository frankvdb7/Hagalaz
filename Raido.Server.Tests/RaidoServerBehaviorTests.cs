using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO.Pipelines;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Raido.Common.Messages;
using Raido.Common.Protocol;
using Raido.Server.Internal;
using Raido.Server.Internal.Diagnostics;
using Raido.Server.Internal.Reflection;

namespace Raido.Server.Tests;

[TestClass]
[DoNotParallelize]
public sealed class RaidoServerBehaviorTests
{
    private sealed class MetadataHub : RaidoHub
    {
        public void Handle(TestMessage message, TestDependency dependency, IEnumerable<TestDependency> dependencies) { }
    }

    private sealed class TestDependency { }

    private sealed class PingProtocol : IRaidoProtocol
    {
        public string Name => "ping";
        public int Version => 1;
        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out RaidoMessage message)
        {
            consumed = input.End;
            examined = input.End;
            message = PingMessage.Instance;
            return true;
        }

        public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output)
        {
            output.GetSpan(1)[0] = 1;
            output.Advance(1);
        }
        public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message) => new byte[] { 9, 8, 7 };
        public bool IsVersionSupported(int version) => version == Version;
    }

    private sealed class RecordingEventListener : EventListener
    {
        public ConcurrentBag<int> EventIds { get; } = new();

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventId >= 0)
            {
                EventIds.Add(eventData.EventId);
            }
        }
    }

    [TestMethod]
    public void ActivityCreator_InvalidRemoteParentStillPreservesTagsTraceStateAndBaggage()
    {
        var source = new ActivitySource("Raido.Server.Tests.Coverage");
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => candidate.Name == source.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };

        var headers = new Dictionary<string, object?>
        {
            ["traceparent"] = "not-a-w3c-parent",
            ["tracestate"] = "vendor=value",
            ["baggage"] = "first=one,second=two"
        };

        static void GetHeader(object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues)
        {
            fieldValues = null;
            var values = (Dictionary<string, object?>)carrier!;
            fieldValue = values.TryGetValue(fieldName, out var value) ? value?.ToString() : null;
        }

        using var activity = ActivityCreator.CreateFromRemote(
            source,
            DistributedContextPropagator.Current,
            headers,
            GetHeader,
            "remote-operation",
            ActivityKind.Server,
            new[] { new KeyValuePair<string, object?>("tag", "value") },
            null,
            diagnosticsOrLoggingEnabled: true);

        Assert.IsNotNull(activity);
        Assert.AreEqual("remote-operation", activity!.OperationName);
        Assert.AreEqual("value", activity.GetTagItem("tag"));
        Assert.AreEqual("one", activity.GetBaggageItem("first"));
        Assert.AreEqual("two", activity.GetBaggageItem("second"));
    }

    [TestMethod]
    public void ActivityCreator_UsesValidRemoteContextAndLinks()
    {
        var source = new ActivitySource("Raido.Server.Tests.RemoteContext");
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => candidate.Name == source.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        var headers = new Dictionary<string, object?>
        {
            ["traceparent"] = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
            ["tracestate"] = "vendor=value",
            ["baggage"] = "request=remote"
        };

        static void GetHeader(object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues)
        {
            fieldValues = null;
            var values = (Dictionary<string, object?>)carrier!;
            fieldValue = values.TryGetValue(fieldName, out var value) ? value?.ToString() : null;
        }

        using var parent = new Activity("parent").Start();
        using var activity = ActivityCreator.CreateFromRemote(
            source,
            DistributedContextPropagator.Current,
            headers,
            GetHeader,
            "remote-operation",
            ActivityKind.Server,
            new[] { new KeyValuePair<string, object?>("source", "remote") },
            new[] { new ActivityLink(parent.Context) },
            diagnosticsOrLoggingEnabled: true);

        Assert.IsNotNull(activity);
        Assert.AreEqual("vendor=value", activity!.TraceStateString);
        Assert.AreEqual("remote", activity.GetTagItem("source"));
        Assert.AreEqual("remote", activity.GetBaggageItem("request"));
        Assert.AreEqual(1, activity.Links.Count());
    }

    [TestMethod]
    public void ActivityCreator_AddsBaggageWhenNoTraceParentExists()
    {
        var source = new ActivitySource("Raido.Server.Tests.BaggageOnly");
        var headers = new Dictionary<string, object?>
        {
            ["baggage"] = "request=untraced"
        };

        static void GetHeader(object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues)
        {
            fieldValues = null;
            var values = (Dictionary<string, object?>)carrier!;
            fieldValue = values.TryGetValue(fieldName, out var value) ? value?.ToString() : null;
        }

        using var activity = ActivityCreator.CreateFromRemote(
            source,
            DistributedContextPropagator.Current,
            headers,
            GetHeader,
            "baggage-only",
            ActivityKind.Internal,
            null,
            null,
            diagnosticsOrLoggingEnabled: true);

        Assert.IsNotNull(activity);
        Assert.IsNull(activity!.ParentId);
        Assert.AreEqual("untraced", activity.GetBaggageItem("request"));
    }

    [TestMethod]
    public void EventSource_InitializesCountersAndEmitsConnectionEvents()
    {
        using var listener = new RecordingEventListener();
        listener.EnableEvents(RaidoEventSource.Log, EventLevel.Informational, EventKeywords.All);

        RaidoEventSource.Log.ConnectionStart("coverage");
        RaidoEventSource.Log.ConnectionStop("coverage", Stopwatch.GetTimestamp(), Stopwatch.GetTimestamp());
        RaidoEventSource.Log.ConnectionTimedOut("coverage");

        listener.DisableEvents(RaidoEventSource.Log);

        CollectionAssert.Contains(listener.EventIds.ToArray(), 1);
        CollectionAssert.Contains(listener.EventIds.ToArray(), 2);
        CollectionAssert.Contains(listener.EventIds.ToArray(), 3);
    }

    [TestMethod]
    public void CallerAndHubContexts_ExposeConnectionAndInvocationMetadata()
    {
        var local = new IPEndPoint(IPAddress.Loopback, 4350);
        var remote = new IPEndPoint(IPAddress.Loopback, 4351);
        var user = new ClaimsPrincipal(new ClaimsIdentity("test"));
        var features = new FeatureCollection();
        features.Set<IConnectionUserFeature>(new ConnectionUserFeature { User = user });
        var items = new Dictionary<object, object?>();
        var raw = Substitute.For<ConnectionContext>();
        raw.ConnectionId.Returns("metadata");
        raw.LocalEndPoint.Returns(local);
        raw.RemoteEndPoint.Returns(remote);
        raw.Items.Returns(items);
        raw.Features.Returns(features);
        raw.ConnectionClosed.Returns(CancellationToken.None);
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(Substitute.For<PipeReader>());
        transport.Output.Returns(Substitute.For<PipeWriter>());
        raw.Transport.Returns(transport);

        var connection = new RaidoConnectionContext(raw, new RaidoConnectionContextOptions(), NullLoggerFactory.Instance)
        {
            Protocol = Substitute.For<IRaidoProtocol>()
        };
        var caller = new DefaultRaidoCallerContext(connection);

        Assert.AreSame(local, connection.LocalEndPoint);
        Assert.AreSame(local, connection.LocalEndPoint);
        Assert.AreSame(remote, connection.RemoteEndPoint);
        Assert.AreSame(remote, connection.RemoteEndPoint);
        Assert.AreSame(user, caller.User);
        Assert.AreSame(local, caller.LocalIPEndPoint);
        Assert.AreSame(remote, caller.RemoteIPEndPoint);
        Assert.AreSame(items, caller.Items);

        var method = typeof(MetadataHub).GetMethod(nameof(MetadataHub.Handle))!;
        var executor = ObjectMethodExecutor.Create(method, typeof(MetadataHub).GetTypeInfo());
        var provider = new ServiceCollection().BuildServiceProvider();
        var hub = new MetadataHub();
        var arguments = new object?[] { new TestMessage() };
        var invocation = new RaidoHubInvocationContext(executor, caller, provider, hub, arguments);
        var lifetime = new RaidoHubLifetimeContext(caller, provider, hub);

        Assert.AreSame(caller, invocation.Context);
        Assert.AreSame(hub, invocation.Hub);
        Assert.AreSame(method, invocation.HubMethod);
        Assert.AreSame(arguments, invocation.HubMethodArguments);
        Assert.AreSame(provider, invocation.ServiceProvider);
        Assert.AreSame(caller, lifetime.Context);
        Assert.AreSame(hub, lifetime.Hub);
        Assert.AreSame(provider, lifetime.ServiceProvider);
    }

    [TestMethod]
    public void ConnectionStore_EnumeratorsExposeCurrentThroughBothInterfaces()
    {
        var first = CreateConnection("first");
        var second = CreateConnection("second");
        var store = new RaidoConnectionStore();
        store.Add(first);
        store.Add(second);

        using var typed = store.GetEnumerator();
        Assert.IsTrue(typed.MoveNext());
        Assert.IsNotNull(typed.Current);

        var untyped = ((IEnumerable)store).GetEnumerator();
        try
        {
            Assert.IsTrue(untyped.MoveNext());
            Assert.IsInstanceOfType<RaidoConnectionContext>(untyped.Current);
        }
        finally
        {
            ((IDisposable)untyped).Dispose();
        }
    }

    [TestMethod]
    public void HubMethodDescriptor_SeparatesMessageAndServiceArguments()
    {
        var services = new ServiceCollection().AddSingleton<TestDependency>().BuildServiceProvider();
        var method = typeof(MetadataHub).GetMethod(nameof(MetadataHub.Handle))!;
        var executor = ObjectMethodExecutor.Create(method, typeof(MetadataHub).GetTypeInfo());
        var descriptor = new RaidoHubMethodDescriptor(executor, services.GetRequiredService<IServiceProviderIsService>(), Array.Empty<IAuthorizeData>());

        Assert.AreEqual(1, descriptor.ParameterTypes.Count);
        Assert.AreEqual(typeof(TestMessage), descriptor.ParameterTypes[0]);
        Assert.IsFalse(descriptor.IsServiceArgument(0));
        Assert.IsTrue(descriptor.IsServiceArgument(1));
        Assert.IsTrue(descriptor.IsServiceArgument(2));
        Assert.AreEqual(typeof(void), descriptor.NonAsyncReturnType);
    }

    [TestMethod]
    public async Task ConnectionContext_PingWriterSendsBytesAndStopsAfterAbort()
    {
        var output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(Substitute.For<PipeReader>());
        transport.Output.Returns(output.Writer);
        var raw = Substitute.For<ConnectionContext>();
        raw.ConnectionId.Returns("ping");
        raw.Transport.Returns(transport);
        raw.Features.Returns(new FeatureCollection());
        raw.ConnectionClosed.Returns(CancellationToken.None);
        var connection = new RaidoConnectionContext(raw, new RaidoConnectionContextOptions(), NullLoggerFactory.Instance)
        {
            Protocol = new PingProtocol()
        };

        var ping = typeof(RaidoConnectionContext).GetMethod("TryWritePingSlowAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)ping.Invoke(connection, null)!;
        var read = await output.Reader.ReadAsync();
        CollectionAssert.AreEqual(new byte[] { 9, 8, 7 }, read.Buffer.ToArray());
        output.Reader.AdvanceTo(read.Buffer.End);

        connection.Abort();
        await (Task)ping.Invoke(connection, null)!;
        await connection.AbortAsync();
        Assert.IsTrue(connection.ConnectionAbortedToken.IsCancellationRequested);
    }

    [TestMethod]
    public async Task ConnectionContext_WriteWaitsForAnAlreadyHeldWriteLock()
    {
        var output = new Pipe();
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(Substitute.For<PipeReader>());
        transport.Output.Returns(output.Writer);
        var raw = Substitute.For<ConnectionContext>();
        raw.ConnectionId.Returns("serialized");
        raw.Transport.Returns(transport);
        raw.Features.Returns(new FeatureCollection());
        raw.ConnectionClosed.Returns(CancellationToken.None);
        var connection = new RaidoConnectionContext(raw, new RaidoConnectionContextOptions(), NullLoggerFactory.Instance)
        {
            Protocol = new PingProtocol()
        };

        var writeLock = (SemaphoreSlim)typeof(RaidoConnectionContext)
            .GetField("_writeLock", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(connection)!;
        Assert.IsTrue(writeLock.Wait(0));
        var pending = connection.WriteAsync(new TestMessage());
        Assert.IsFalse(pending.IsCompleted);
        writeLock.Release();

        await pending;
        var read = await output.Reader.ReadAsync();
        CollectionAssert.AreEqual(new byte[] { 1 }, read.Buffer.ToArray());
        output.Reader.AdvanceTo(read.Buffer.End);

        Assert.IsTrue(writeLock.Wait(0));
        var abort = connection.AbortAsync();
        Assert.IsFalse(abort.IsCompleted);
        writeLock.Release();
        await abort;
    }

    private sealed class ConnectionUserFeature : IConnectionUserFeature
    {
        public ClaimsPrincipal? User { get; set; }
    }

    private static RaidoConnectionContext CreateConnection(string id)
    {
        var raw = Substitute.For<ConnectionContext>();
        raw.ConnectionId.Returns(id);
        raw.Features.Returns(new FeatureCollection());
        raw.ConnectionClosed.Returns(CancellationToken.None);
        var transport = Substitute.For<IDuplexPipe>();
        transport.Input.Returns(Substitute.For<PipeReader>());
        transport.Output.Returns(Substitute.For<PipeWriter>());
        raw.Transport.Returns(transport);
        return new RaidoConnectionContext(raw, new RaidoConnectionContextOptions(), NullLoggerFactory.Instance);
    }
}
