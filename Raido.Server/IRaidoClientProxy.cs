using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// A proxy for invoking methods on a Raido client.
    /// </summary>
    public interface IRaidoClientProxy
    {
        /// <summary>
        /// Sends a message to the client.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the send operation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send operation.</returns>
        Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : RaidoMessage;
    }
}