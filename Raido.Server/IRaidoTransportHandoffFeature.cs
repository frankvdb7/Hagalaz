using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Raido.Server;

/// <summary>
/// Requests that the current transport be handed to an existing logical connection
/// after the current protocol reader has stopped owning the transport.
/// </summary>
public interface IRaidoTransportHandoffFeature
{
    /// <summary>
    /// Registers the one callback that completes the logical transport handoff.
    /// </summary>
    void OnTransportReady(Func<PipeWriter, Task> callback);
}
