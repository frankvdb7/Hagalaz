using System.Collections.Generic;
using Hagalaz.Services.GameWorld.Network.Protocol;

namespace Hagalaz.Services.GameWorld.Providers
{
    public interface IClientProtocolResolver
    {
        public IReadOnlyList<IClientProtocol> AllProtocols { get; }
        public IClientProtocol? GetProtocol(int revision);
    }
}
