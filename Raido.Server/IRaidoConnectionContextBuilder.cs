using System;
using Microsoft.AspNetCore.Connections;
using Raido.Common.Protocol;

namespace Raido.Server
{
    public interface IRaidoConnectionContextBuilder : IRaidoConnectionContextBuilder<IRaidoConnectionContextBuilderConnection>
    {
    }

    public interface IRaidoConnectionContextBuilder<out TBuilder>
    {
        TBuilder Create();
    }

    public interface IRaidoConnectionContextBuilderConnection
    {
        IRaidoConnectionContextBuilderProtocol WithConnection(ConnectionContext connection);
    }

    public interface IRaidoConnectionContextBuilderProtocol
    {
        IRaidoConnectionContextBuilderOptional WithProtocol(IRaidoProtocol protocol);
        IRaidoConnectionContextBuilderOptional WithProtocol<TProtocol>() where TProtocol : IRaidoProtocol;
    }

    public interface IRaidoConnectionContextBuilderOptional : IRaidoConnectionContextBuilderBuild
    {
        IRaidoConnectionContextBuilderOptional WithKeepAliveInterval(TimeSpan interval);
        IRaidoConnectionContextBuilderOptional WithClientTimeoutInterval(TimeSpan interval);
    }

    public interface IRaidoConnectionContextBuilderBuild
    {
        RaidoConnectionContext Build();
    }
}