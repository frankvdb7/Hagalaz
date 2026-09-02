using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Connections;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// A builder for creating <see cref="RaidoHubConnectionContext"/> instances.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoHubConnectionContextBuilder : IRaidoHubConnectionContextBuilder<IRaidoHubConnectionContextBuilderConnection>
    {
    }

    /// <summary>
    /// A builder for creating <see cref="RaidoHubConnectionContext"/> instances.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the builder.</typeparam>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoHubConnectionContextBuilder<out TBuilder>
    {
        /// <summary>
        /// Creates a new builder instance.
        /// </summary>
        /// <returns>A new builder instance.</returns>
        TBuilder Create();
    }

    /// <summary>
    /// A builder for creating <see cref="RaidoHubConnectionContext"/> instances.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoHubConnectionContextBuilderConnection
    {
        /// <summary>
        /// Sets the connection for the context.
        /// </summary>
        /// <param name="connection">The connection to use.</param>
        /// <returns>The next builder in the chain.</returns>
        IRaidoHubConnectionContextBuilderProtocol WithConnection(ConnectionContext connection);
    }

    /// <summary>
    /// A builder for creating <see cref="RaidoHubConnectionContext"/> instances.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoHubConnectionContextBuilderProtocol
    {
        /// <summary>
        /// Sets the protocol for the context.
        /// </summary>
        /// <param name="protocol">The protocol to use.</param>
        /// <returns>The next builder in the chain.</returns>
        IRaidoHubConnectionContextBuilderOptional WithProtocol(IRaidoProtocol protocol);

        /// <summary>
        /// Sets the protocol for the context.
        /// </summary>
        /// <typeparam name="TProtocol">The type of the protocol.</typeparam>
        /// <returns>The next builder in the chain.</returns>
        IRaidoHubConnectionContextBuilderOptional WithProtocol<TProtocol>() where TProtocol : IRaidoProtocol;
    }

    /// <summary>
    /// A builder for creating <see cref="RaidoHubConnectionContext"/> instances.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoHubConnectionContextBuilderOptional : IRaidoHubConnectionContextBuilderBuild
    {
        /// <summary>
        /// Sets the keep-alive interval for the context.
        /// </summary>
        /// <param name="interval">The keep-alive interval.</param>
        /// <returns>The builder instance.</returns>
        IRaidoHubConnectionContextBuilderOptional WithKeepAliveInterval(TimeSpan interval);

        /// <summary>
        /// Sets the client timeout interval for the context.
        /// </summary>
        /// <param name="interval">The client timeout interval.</param>
        /// <returns>The builder instance.</returns>
        IRaidoHubConnectionContextBuilderOptional WithClientTimeoutInterval(TimeSpan interval);

        /// <summary>
        /// Enables stateful reconnect for the context.
        /// </summary>
        /// <returns>The builder instance.</returns>
        IRaidoHubConnectionContextBuilderOptional WithStatefulReconnect();
    }

    /// <summary>
    /// A builder for creating <see cref="RaidoHubConnectionContext"/> instances.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoHubConnectionContextBuilderBuild
    {
        /// <summary>
        /// Builds the <see cref="RaidoHubConnectionContext"/>.
        /// </summary>
        /// <returns>The created <see cref="RaidoHubConnectionContext"/>.</returns>
        RaidoHubConnectionContext Build();
    }
}