using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;
using Raido.Server.Internal;

namespace Raido.Server.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IRaidoBuilder"/>.
    /// </summary>
    public static class RaidoBuilderExtensions
    {
        /// <summary>
        /// Adds a Raido protocol to the service collection.
        /// </summary>
        /// <typeparam name="TProtocol">The type of the protocol.</typeparam>
        /// <typeparam name="TImplementation">The type of the protocol implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">A delegate to configure the protocol.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddRaidoProtocol<TProtocol, TImplementation>(
            this IServiceCollection services, Action<IRaidoProtocolBuilder<TImplementation>>? configure)
            where TProtocol : class, IRaidoProtocol where TImplementation : class, TProtocol
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddTransient<TProtocol, TImplementation>();
            services.AddTransient<TImplementation>();

            var protocolBuilder = new DefaultRaidoProtocolBuilder<TImplementation>(services);
            configure?.Invoke(protocolBuilder);
            return services;
        }

        /// <summary>
        /// Adds a Raido protocol to the service collection.
        /// </summary>
        /// <typeparam name="TProtocol">The type of the protocol.</typeparam>
        /// <typeparam name="TImplementation">The type of the protocol implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddRaidoProtocol<TProtocol, TImplementation>(this IServiceCollection services)
            where TProtocol : class, IRaidoProtocol where TImplementation : class, TProtocol =>
            services.AddRaidoProtocol<TProtocol, TImplementation>(configure: null);

        /// <summary>
        /// Adds a Raido protocol to the service collection.
        /// </summary>
        /// <typeparam name="TImplementation">The type of the protocol implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">A delegate to configure the protocol.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddRaidoProtocol<TImplementation>(
            this IServiceCollection services, Action<IRaidoProtocolBuilder<TImplementation>> configure)
            where TImplementation : class, IRaidoProtocol => services.AddRaidoProtocol<IRaidoProtocol, TImplementation>(configure);

        /// <summary>
        /// Adds a Raido protocol to the service collection.
        /// </summary>
        /// <typeparam name="TImplementation">The type of the protocol implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddRaidoProtocol<TImplementation>(this IServiceCollection services)
            where TImplementation : class, IRaidoProtocol => services.AddRaidoProtocol<IRaidoProtocol, TImplementation>(configure: null);

        /// <summary>
        /// Adds a Raido hub to the builder.
        /// </summary>
        /// <typeparam name="THub">The type of the hub.</typeparam>
        /// <param name="builder">The Raido builder.</param>
        /// <returns>The Raido builder.</returns>
        public static IRaidoBuilder AddHub<THub>(this IRaidoBuilder builder) where THub : RaidoHub => builder.AddHub<THub>(_ => { });

        /// <summary>
        /// Adds a Raido hub to the builder.
        /// </summary>
        /// <typeparam name="THub">The type of the hub.</typeparam>
        /// <param name="builder">The Raido builder.</param>
        /// <param name="options">A delegate to configure the hub options.</param>
        /// <returns>The Raido builder.</returns>
        public static IRaidoBuilder AddHub<THub>(this IRaidoBuilder builder, Action<RaidoHubOptions<THub>> options) where THub : RaidoHub
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IConfigureOptions<RaidoHubOptions<THub>>, RaidoHubOptionsSetup<THub>>();
            builder.Services.AddSingleton(typeof(IRaidoHubDispatcher), typeof(DefaultRaidoHubDispatcher<THub>));
            builder.Services.Configure(options);

            return builder;
        }
    }
}