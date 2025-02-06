using System.Collections.Generic;
using System.Linq;
using Hagalaz.Services.GameWorld.Network.Protocol;

namespace Hagalaz.Services.GameWorld.Providers
{
    public class ClientProtocolResolver : IClientProtocolResolver
    {
        private readonly List<IClientProtocol> _allProtocols;
        private readonly Dictionary<int, IClientProtocol> _availableProtocols;

        public IReadOnlyList<IClientProtocol> AllProtocols => _allProtocols;

        public ClientProtocolResolver(IEnumerable<IClientProtocol> protocols)
        {
            _availableProtocols = new Dictionary<int, IClientProtocol>();

            foreach (var protocol in protocols)
            {
                _availableProtocols[protocol.Version] = protocol;
            }

            _allProtocols = _availableProtocols.Values.ToList();
        }

        public IClientProtocol? GetProtocol(int revision) => !_availableProtocols.TryGetValue(revision, out var protocol) ? null : protocol;
    }
}
