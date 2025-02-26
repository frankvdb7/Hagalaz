using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;
using Raido.Server.Internal;

namespace Raido.Server.Extensions
{
    public static class ServiceCollectionExtensions
    {
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
        
        public static IRaidoBuilder AddRaidoServer(this IServiceCollection services) => services.AddRaidoServer(_ => { });
    }
}