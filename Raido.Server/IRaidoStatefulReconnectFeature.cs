using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Raido.Server
{
    /// <summary>
    /// Provides application control over whether an opted-in logical connection may be retained after transport loss.
    /// </summary>
    public interface IRaidoStatefulReconnectFeature
    {
        /// <summary>
        /// Enables reconnect retention for the logical connection.
        /// </summary>
        void EnableReconnect();

        /// <summary>
        /// Permanently disables stateful reconnect for the logical connection.
        /// </summary>
        void DisableReconnect();

        /// <summary>
        /// Registers a callback invoked after a replacement transport is attached.
        /// </summary>
        void OnReconnected(Func<PipeWriter, Task> callback);
    }
}
