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
            
            services.TryAddTransient(typeof(RaidoConnectionHandler));

            services.TryAddSingleton(typeof(RaidoConnectionStore));
            services.TryAddSingleton(typeof(IRaidoProtocolResolver), typeof(DefaultRaidoProtocolResolver));
            services.TryAddSingleton(typeof(IRaidoLifetimeManager), typeof(DefaultRaidoLifetimeManager));
            services.TryAddSingleton(typeof(IRaidoContext), typeof(DefaultRaidoContext));
            services.TryAddSingleton(typeof(IRaidoDispatcher), typeof(DefaultRaidoDispatcher));
            services.TryAddTransient(typeof(IRaidoConnectionContextBuilder), typeof(DefaultRaidoConnectionContextBuilder));
            services.TryAddScoped(typeof(IRaidoCallerContextAccessor), typeof(DefaultRaidoCallerContextAccessor));
            services.TryAddScoped(typeof(IRaidoHubActivator<>), typeof(DefaultRaidoHubActivator<>));

            services.TryAddTransient(typeof(IRaidoCodecFactory<>), typeof(DefaultRaidoCodecFactory<>));
            services.TryAddSingleton(typeof(IRaidoCodec<>), typeof(DefaultRaidoCodec<>));

            services.TryAddSingleton(new RaidoServerActivitySource());

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