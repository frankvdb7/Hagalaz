using System.Collections.Generic;

namespace Raido.Server.Internal
{
    internal class DefaultRaidoCallerClients : IRaidoCallerClients
    {
        private readonly IRaidoClients _clients;
        private readonly string _connectionId;
        private readonly string[] _currentConnectionId;

        public DefaultRaidoCallerClients(IRaidoClients clients, string connectionId)
        {
            _clients = clients;
            _connectionId = connectionId;
            _currentConnectionId = new[]
            {
                _connectionId
            };
        }

        public IRaidoClientProxy All => _clients.All;
        public IRaidoClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _clients.AllExcept(excludedConnectionIds);
        public IRaidoClientProxy Client(string connectionId) => _clients.Client(connectionId);
        public IRaidoClientProxy Clients(IReadOnlyList<string> connectionIds) => _clients.Clients(connectionIds);
        public IRaidoClientProxy Caller => _clients.Client(_connectionId);
        public IRaidoClientProxy Others => _clients.AllExcept(_currentConnectionId);
    }
}