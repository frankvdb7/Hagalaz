using System.Net;

namespace Hagalaz.Services.GameWorld.Services.Model;

public sealed record WorldReconnectAuthenticationRequest(
    string Login,
    string Password,
    IPAddress? RemoteAddress,
    string ConnectionId);
