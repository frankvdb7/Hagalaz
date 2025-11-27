using Hagalaz.Game.Abstractions.Features.States;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Extensions
{
    public static class StateServiceCollectionExtensions
    {
        public static IServiceCollection AddStates(this IServiceCollection services)
        {
            services.Scan(scan => scan
                .FromApplicationDependencies()
                .AddClasses(classes => classes.AssignableTo<IState>())
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            return services;
        }
    }
}