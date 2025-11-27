using Hagalaz.Game.Abstractions.Features.States;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Scripts.Extensions
{
    public static class StateServiceCollectionExtensions
    {
        public static IServiceCollection AddScriptStates(this IServiceCollection services)
        {
            services.Scan(scan => scan
                .FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IState>())
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            return services;
        }
    }
}