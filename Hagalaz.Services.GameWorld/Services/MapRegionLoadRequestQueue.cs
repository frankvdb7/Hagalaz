using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Services;

/// <summary>
/// Bridges synchronous region publication and the asynchronous load scheduler.
/// </summary>
public sealed class MapRegionLoadRequestQueue : IMapRegionLoadRequestSink
{
    private readonly Channel<IMapRegion> _requests = Channel.CreateUnbounded<IMapRegion>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    public void RequestLoad(IMapRegion region) => _requests.Writer.TryWrite(region);

    internal bool TryRequestLoad(IMapRegion region) => _requests.Writer.TryWrite(region);

    internal IAsyncEnumerable<IMapRegion> ReadAllAsync(CancellationToken cancellationToken) =>
        _requests.Reader.ReadAllAsync(cancellationToken);

    internal void Complete() => _requests.Writer.TryComplete();
}
