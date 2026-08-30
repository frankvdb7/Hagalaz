namespace Raido.Server.Internal;

internal interface IRaidoCallerContextTransport
{
    RaidoConnectionContext Connection { get; }
}
