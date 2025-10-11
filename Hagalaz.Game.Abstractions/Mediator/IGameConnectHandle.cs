using System;

namespace Hagalaz.Game.Abstractions.Mediator
{
    /// <summary>
    /// Defines a contract for a handle that represents a connection to the game mediator.
    /// This handle can be used to disconnect a consumer from the mediator.
    /// </summary>
    public interface IGameConnectHandle : IDisposable
    {
        /// <summary>
        /// Disconnects the associated consumer from the game mediator.
        /// </summary>
        public void Disconnect();
    }
}
