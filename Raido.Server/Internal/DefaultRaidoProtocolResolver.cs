using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Raido.Common.Protocol;

namespace Raido.Server.Internal;

internal class DefaultRaidoProtocolResolver : IRaidoProtocolResolver
{
    private readonly ILogger<DefaultRaidoProtocolResolver> _logger;
    private readonly List<IRaidoProtocol> _allProtocols;
    private readonly Dictionary<string, IRaidoProtocol> _availableProtocols;

    public IReadOnlyList<IRaidoProtocol> AllProtocols => _allProtocols;

    public DefaultRaidoProtocolResolver(IEnumerable<IRaidoProtocol> protocols, ILogger<DefaultRaidoProtocolResolver> logger)
    {
        _logger = logger ?? NullLogger<DefaultRaidoProtocolResolver>.Instance;
        _availableProtocols = new Dictionary<string, IRaidoProtocol>(StringComparer.OrdinalIgnoreCase);

        foreach (var protocol in protocols)
        {
            Log.RegisteredProtocol(_logger, protocol.Name, protocol.GetType());
            _availableProtocols[protocol.Name] = protocol;
        }

        _allProtocols = _availableProtocols.Values.ToList();
    }

    public IRaidoProtocol? GetProtocol(string protocolName, IReadOnlyList<string>? supportedProtocols)
    {
        protocolName = protocolName ?? throw new ArgumentNullException(nameof(protocolName));

        if (supportedProtocols != null && !supportedProtocols.Contains(protocolName, StringComparer.OrdinalIgnoreCase) ||
            !_availableProtocols.TryGetValue(protocolName, out var protocol))
        {
            // null result indicates protocol is not supported
            // result will be validated by the caller
            return null;
        }

        Log.FoundImplementationForProtocol(_logger, protocolName);
        return protocol;
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, Type, Exception?> _registeredProtocol = LoggerMessage.Define<string, Type>(LogLevel.Debug,
            new EventId(1, "RegisteredRaidoProtocol"),
            "Registered protocol: {ProtocolName}, implemented by {ImplementationType}.");

        private static readonly Action<ILogger, string, Exception?> _foundImplementationForProtocol = LoggerMessage.Define<string>(LogLevel.Debug,
            new EventId(2, "FoundImplementationForRaidoProtocol"),
            "Found protocol implementation for requested protocol: {ProtocolName}.");

        public static void RegisteredProtocol(ILogger logger, string protocolName, Type implementationType) =>
            _registeredProtocol(logger, protocolName, implementationType, null);

        public static void FoundImplementationForProtocol(ILogger logger, string protocolName) => _foundImplementationForProtocol(logger, protocolName, null);
    }
}