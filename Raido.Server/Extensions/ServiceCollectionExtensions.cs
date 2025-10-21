using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;
using Raido.Server.Internal;

namespace Raido.Server.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the core Raido server services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>A <see cref="IRaidoBuilder"/> that can be used to further configure the Raido server.</returns>
        public static IRaidoBuilder AddRaidoServerCore(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IConfigureOptions<RaidoOptions>, RaidoOptionsSetup>();
            
            services.TryAddTransient<RaidoConnectionHandler>();

            services.TryAddSingleton<RaidoConnectionStore>();
            services.TryAddSingleton<IRaidoProtocolResolver, DefaultRaidoProtocolResolver>();
            services.TryAddSingleton<IRaidoLifetimeManager, DefaultRaidoLifetimeManager>();
            services.TryAddSingleton<IRaidoContext, DefaultRaidoContext>();
            services.TryAddSingleton<IRaidoDispatcher, DefaultRaidoDispatcher>();
            services.TryAddTransient<IRaidoConnectionContextBuilder, DefaultRaidoConnectionContextBuilder>();
            services.TryAddScoped<IRaidoCallerContextAccessor, DefaultRaidoCallerContextAccessor>();
            services.TryAddScoped(typeof(IRaidoHubActivator<>), typeof(DefaultRaidoHubActivator<>));

            services.TryAddTransient(typeof(IRaidoCodecFactory<>), typeof(DefaultRaidoCodecFactory<>));
            services.TryAddSingleton(typeof(IRaidoCodec<>), typeof(DefaultRaidoCodec<>));

            services.TryAddSingleton(new RaidoServerActivitySource());
            services.TryAddSingleton<RaidoMetrics>();

            services.AddAuthorization();
            return new DefaultRaidoBuilder(services);
        }
        
        /// <summary>
        /// Adds the Raido server services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="options">A delegate to configure the <see cref="RaidoOptions"/>.</param>
        /// <returns>A <see cref="IRaidoBuilder"/> that can be used to further configure the Raido server.</returns>
        public static IRaidoBuilder AddRaidoServer(this IServiceCollection services, Action<RaidoOptions> options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var builder = services.AddRaidoServerCore();
            builder.Services.Configure(options);

            return builder;
        }
        
        /// <summary>
        /// Adds the Raido server services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>A <see cref="IRaidoBuilder"/> that can be used to further configure the Raido server.</returns>
        public static IRaidoBuilder AddRaidoServer(this IServiceCollection services) => services.AddRaidoServer(_ => { });
    }
}