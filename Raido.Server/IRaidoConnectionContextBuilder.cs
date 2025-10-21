using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Connections;
using Raido.Common.Protocol;

namespace Raido.Server
{
    /// <summary>
    /// A builder for creating <see cref="RaidoConnectionContext"/> instances.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoConnectionContextBuilder : IRaidoConnectionContextBuilder<IRaidoConnectionContextBuilderConnection>
    {
    }

    /// <summary>
    /// A builder for creating <see cref="RaidoConnectionContext"/> instances.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the builder.</typeparam>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoConnectionContextBuilder<out TBuilder>
    {
        /// <summary>
        /// Creates a new builder instance.
        /// </summary>
        /// <returns>A new builder instance.</returns>
        TBuilder Create();
    }

    /// <summary>
    /// A builder for creating <see cref="RaidoConnectionContext"/> instances.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoConnectionContextBuilderConnection
    {
        /// <summary>
        /// Sets the connection for the context.
        /// </summary>
        /// <param name="connection">The connection to use.</param>
        /// <returns>The next builder in the chain.</returns>
        IRaidoConnectionContextBuilderProtocol WithConnection(ConnectionContext connection);
    }

    /// <summary>
    /// A builder for creating <see cref="RaidoConnectionContext"/> instances.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoConnectionContextBuilderProtocol
    {
        /// <summary>
        /// Sets the protocol for the context.
        /// </summary>
        /// <param name="protocol">The protocol to use.</param>
        /// <returns>The next builder in the chain.</returns>
        IRaidoConnectionContextBuilderOptional WithProtocol(IRaidoProtocol protocol);

        /// <summary>
        /// Sets the protocol for the context.
        /// </summary>
        /// <typeparam name="TProtocol">The type of the protocol.</typeparam>
        /// <returns>The next builder in the chain.</returns>
        IRaidoConnectionContextBuilderOptional WithProtocol<TProtocol>() where TProtocol : IRaidoProtocol;
    }

    /// <summary>
    /// A builder for creating <see cref="RaidoConnectionContext"/> instances.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoConnectionContextBuilderOptional : IRaidoConnectionContextBuilderBuild
    {
        /// <summary>
        /// Sets the keep-alive interval for the context.
        /// </summary>
        /// <param name="interval">The keep-alive interval.</param>
        /// <returns>The builder instance.</returns>
        IRaidoConnectionContextBuilderOptional WithKeepAliveInterval(TimeSpan interval);

        /// <summary>
        /// Sets the client timeout interval for the context.
        /// </summary>
        /// <param name="interval">The client timeout interval.</param>
        /// <returns>The builder instance.</returns>
        IRaidoConnectionContextBuilderOptional WithClientTimeoutInterval(TimeSpan interval);
    }

    /// <summary>
    /// A builder for creating <see cref="RaidoConnectionContext"/> instances.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRaidoConnectionContextBuilderBuild
    {
        /// <summary>
        /// Builds the <see cref="RaidoConnectionContext"/>.
        /// </summary>
        /// <returns>The created <see cref="RaidoConnectionContext"/>.</returns>
        RaidoConnectionContext Build();
    }
}