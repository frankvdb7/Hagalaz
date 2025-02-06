using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;
using Raido.Server.Internal;

namespace Raido.Server.Extensions
{
    public static class RaidoBuilderExtensions
    {
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

        public static IServiceCollection AddRaidoProtocol<TProtocol, TImplementation>(this IServiceCollection services)
            where TProtocol : class, IRaidoProtocol where TImplementation : class, TProtocol =>
            services.AddRaidoProtocol<TProtocol, TImplementation>(configure: null);

        public static IServiceCollection AddRaidoProtocol<TImplementation>(
            this IServiceCollection services, Action<IRaidoProtocolBuilder<TImplementation>> configure)
            where TImplementation : class, IRaidoProtocol => services.AddRaidoProtocol<IRaidoProtocol, TImplementation>(configure);

        public static IServiceCollection AddRaidoProtocol<TImplementation>(this IServiceCollection services)
            where TImplementation : class, IRaidoProtocol => services.AddRaidoProtocol<IRaidoProtocol, TImplementation>(configure: null);

        public static IRaidoBuilder AddHub<THub>(this IRaidoBuilder builder) where THub : RaidoHub => builder.AddHub<THub>(_ => { });

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