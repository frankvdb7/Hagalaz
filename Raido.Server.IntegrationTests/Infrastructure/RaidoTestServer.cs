using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raido.Common.Protocol;
using Raido.Server;
using Raido.Server.Extensions;

namespace Raido.Server.IntegrationTests.Infrastructure;

internal sealed class RaidoTestServer(TimeSpan? reconnectTimeout = null) : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly RaidoTestApplication _application = new();
    private readonly RaidoTestProtocol _protocol = new();
    private readonly TimeSpan _reconnectTimeout = reconnectTimeout ?? TimeSpan.FromSeconds(15);
    private readonly TaskCompletionSource<RaidoHubConnectionContext> _logicalConnection =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<AcceptedPhysicalConnection> _initialConnection =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Channel<AcceptedPhysicalConnection> _replacements =
        Channel.CreateUnbounded<AcceptedPhysicalConnection>();

    private WebApplication? _app;
    private RaidoHubConnectionContext? _logical;
    private Task? _logicalTask;

    public RaidoTestApplication Application => _application;

    public RaidoHubConnectionContext LogicalConnection => _logical!;

    public int Port { get; private set; }

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(RaidoTestServer).Assembly.GetName().Name,
            EnvironmentName = Environments.Development
        });

        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Services.AddRaidoServer(options =>
        {
            options.KeepAliveInterval = TimeSpan.FromHours(1);
            options.ClientTimeoutInterval = TimeSpan.FromHours(1);
            options.StatefulReconnectTimeout = _reconnectTimeout;
        });
        builder.Services.AddSingleton<IRaidoDispatcher>(_application);
        builder.Services.AddSingleton<IRaidoLifetimeManager>(_application);
        builder.Services.AddSingleton(new RaidoTestConnectionHandler(this));
        builder.WebHost.ConfigureKestrel(options => options.Listen(
            IPAddress.Loopback,
            0,
            listenOptions => listenOptions.UseConnectionHandler<RaidoTestConnectionHandler>()));

        _app = builder.Build();
        await _app.StartAsync().ConfigureAwait(false);

        var server = _app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addresses = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!.Addresses;
        Port = new Uri(addresses.Single()).Port;
    }

    public async Task<RaidoTestClient> ConnectClientAsync()
    {
        var client = new TcpClient(AddressFamily.InterNetwork)
        {
            NoDelay = true
        };
        await client.ConnectAsync(IPAddress.Loopback, Port).ConfigureAwait(false);
        return new RaidoTestClient(client);
    }

    public Task<AcceptedPhysicalConnection> AcceptReplacementAsync() =>
        _replacements.Reader.ReadAsync().AsTask();

    public async Task WaitForInitialPhysicalCloseAsync()
    {
        var initial = await _initialConnection.Task.ConfigureAwait(false);
        await initial.Closed.Task.ConfigureAwait(false);
    }

    public Task WaitForPartialFrameAsync() => _protocol.PartialFrameObserved.Task;

    public bool ActivateReplacement(AcceptedPhysicalConnection replacement)
        => LogicalConnection.TcpConnection.TryActivatePersistentConnection(replacement.Context);

    public Task WaitForLogicalConnectionAsync() => _logicalConnection.Task;

    public async ValueTask DisposeAsync()
    {
        try
        {
            _application.ReleaseDispatch.TrySetResult();
            _logical?.Abort();
            if (_logicalTask is not null)
            {
                await _logicalTask.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
        }
        finally
        {
            if (_app is not null)
            {
                try
                {
                    await _app.StopAsync().ConfigureAwait(false);
                }
                finally
                {
                    await _app.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    internal async Task HandleConnectionAsync(ConnectionContext physical)
    {
        var accepted = new AcceptedPhysicalConnection(physical);
        using var closedRegistration = physical.ConnectionClosed.Register(
            static state => ((AcceptedPhysicalConnection)state!).Closed.TrySetResult(),
            accepted);

        RaidoHubConnectionContext? logicalToStart = null;
        lock (_gate)
        {
            if (_logical is null)
            {
                var builder = _app!.Services.GetRequiredService<IRaidoHubConnectionContextBuilder>();
                logicalToStart = builder.Create()
                    .WithConnection(physical)
                    .WithProtocol(_protocol)
                    .WithKeepAliveInterval(TimeSpan.FromHours(1))
                    .WithClientTimeoutInterval(TimeSpan.FromHours(1))
                    .WithStatefulReconnect()
                    .Build();
                _logical = logicalToStart;
                _initialConnection.TrySetResult(accepted);
                _logicalConnection.TrySetResult(logicalToStart);
            }
            else
            {
                _replacements.Writer.TryWrite(accepted);
            }
        }

        if (logicalToStart is not null)
        {
            _logicalTask = _app!.Services.GetRequiredService<RaidoConnectionHandler>().ConnectAsync(logicalToStart);
        }

        await accepted.Closed.Task.ConfigureAwait(false);
    }

    internal sealed class RaidoTestConnectionHandler(RaidoTestServer server) : ConnectionHandler
    {
        public override Task OnConnectedAsync(ConnectionContext connection) => server.HandleConnectionAsync(connection);
    }
}

internal sealed class AcceptedPhysicalConnection(ConnectionContext context)
{
    public ConnectionContext Context { get; } = context;

    public TaskCompletionSource Closed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed class RaidoTestClient(TcpClient client) : IAsyncDisposable
{
    private int _disposed;

    public NetworkStream Stream { get; } = client.GetStream();

    public async Task SendAsync(ReadOnlyMemory<byte> bytes)
    {
        await Stream.WriteAsync(bytes).ConfigureAwait(false);
    }

    public async Task<byte[]> ReadFrameAsync(CancellationToken cancellationToken)
    {
        var frame = new byte[RaidoTestProtocol.FrameSize];
        var offset = 0;
        while (offset < frame.Length)
        {
            var read = await Stream.ReadAsync(frame.AsMemory(offset), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException("The test client closed before a complete frame was received.");
            }

            offset += read;
        }

        return frame;
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            try
            {
                client.Client.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            client.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}

internal sealed class RaidoTestApplication : IRaidoDispatcher, IRaidoLifetimeManager
{
    private readonly ConcurrentDictionary<int, TaskCompletionSource> _messageSignals = new();
    private readonly ConcurrentQueue<int> _receivedMessageIds = new();
    private int _dispatcherConnectedCount;
    private int _dispatcherDisconnectedCount;
    private int _lifetimeConnectedCount;
    private int _lifetimeDisconnectedCount;

    public TaskCompletionSource DispatcherConnected { get; } = NewSignal();
    public TaskCompletionSource DispatcherDisconnected { get; } = NewSignal();
    public TaskCompletionSource LifetimeDisconnected { get; } = NewSignal();
    public TaskCompletionSource DispatchEntered { get; } = NewSignal();
    public TaskCompletionSource ReleaseDispatch { get; } = NewSignal();
    public bool HoldDispatch { get; set; }
    public IReadOnlyCollection<int> ReceivedMessageIds => _receivedMessageIds.ToArray();
    public int DispatcherConnectedCount => Volatile.Read(ref _dispatcherConnectedCount);
    public int DispatcherDisconnectedCount => Volatile.Read(ref _dispatcherDisconnectedCount);
    public int LifetimeConnectedCount => Volatile.Read(ref _lifetimeConnectedCount);
    public int LifetimeDisconnectedCount => Volatile.Read(ref _lifetimeDisconnectedCount);

    public Task WaitForMessageAsync(int messageId) =>
        _messageSignals.GetOrAdd(messageId, static _ => NewSignal()).Task;

    Task IRaidoDispatcher.OnConnectedAsync(RaidoHubConnectionContext connection)
    {
        Interlocked.Increment(ref _dispatcherConnectedCount);
        DispatcherConnected.TrySetResult();
        return Task.CompletedTask;
    }

    async Task IRaidoDispatcher.DispatchMessageAsync(RaidoHubConnectionContext connection, RaidoMessage message)
    {
        var testMessage = (RaidoTestMessage)message;
        _receivedMessageIds.Enqueue(testMessage.Id);
        _messageSignals.GetOrAdd(testMessage.Id, static _ => NewSignal()).TrySetResult();
        if (HoldDispatch)
        {
            DispatchEntered.TrySetResult();
            await ReleaseDispatch.Task.ConfigureAwait(false);
        }
    }

    Task IRaidoDispatcher.OnDisconnectedAsync(RaidoHubConnectionContext connection, Exception? exception)
    {
        Interlocked.Increment(ref _dispatcherDisconnectedCount);
        DispatcherDisconnected.TrySetResult();
        return Task.CompletedTask;
    }

    Task IRaidoLifetimeManager.OnConnectedAsync(RaidoHubConnectionContext connection)
    {
        Interlocked.Increment(ref _lifetimeConnectedCount);
        return Task.CompletedTask;
    }

    Task IRaidoLifetimeManager.OnDisconnectedAsync(RaidoHubConnectionContext connection)
    {
        Interlocked.Increment(ref _lifetimeDisconnectedCount);
        LifetimeDisconnected.TrySetResult();
        return Task.CompletedTask;
    }

    Task IRaidoLifetimeManager.SendAllAsync(RaidoMessage message, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IRaidoLifetimeManager.SendAllExceptAsync(
        RaidoMessage message,
        IReadOnlyList<string> excludedConnectionIds,
        CancellationToken cancellationToken) => Task.CompletedTask;

    Task IRaidoLifetimeManager.SendConnectionAsync(
        RaidoMessage message,
        string connectionId,
        CancellationToken cancellationToken) => Task.CompletedTask;

    Task IRaidoLifetimeManager.SendConnectionsAsync(
        RaidoMessage message,
        IReadOnlyList<string> connectionIds,
        CancellationToken cancellationToken) => Task.CompletedTask;

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed class RaidoTestProtocol : IRaidoProtocol
{
    public const int FrameSize = 2;
    private const byte PayloadMarker = 0xA5;

    public string Name => "integration";
    public int Version => 1;
    public TaskCompletionSource PartialFrameObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static byte[] Encode(byte id) => [id, PayloadMarker];

    public bool TryParseMessage(
        in ReadOnlySequence<byte> input,
        ref SequencePosition consumed,
        ref SequencePosition examined,
        [MaybeNullWhen(false)] out RaidoMessage message)
    {
        if (input.Length < FrameSize)
        {
            if (input.Length == 1 && input.FirstSpan[0] == 0x11)
            {
                PartialFrameObserved.TrySetResult();
            }

            consumed = input.Start;
            examined = input.End;
            message = null;
            return false;
        }

        var frame = input.Slice(0, FrameSize).ToArray();
        consumed = input.GetPosition(FrameSize);
        examined = consumed;
        message = new RaidoTestMessage(frame[0]);
        return true;
    }

    public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output)
    {
        var id = message is RaidoTestMessage testMessage ? testMessage.Id : (byte)0xFF;
        var destination = output.GetSpan(FrameSize);
        destination[0] = id;
        destination[1] = PayloadMarker;
        output.Advance(FrameSize);
    }

    public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message)
    {
        var id = message is RaidoTestMessage testMessage ? testMessage.Id : (byte)0xFF;
        return new byte[] { id, PayloadMarker };
    }

    public bool IsVersionSupported(int version) => version == Version;
}

internal sealed class RaidoTestMessage(byte id) : RaidoMessage
{
    public byte Id { get; } = id;
}
