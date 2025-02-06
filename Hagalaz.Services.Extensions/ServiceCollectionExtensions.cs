using Hagalaz.Services.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.Extensions
{
    /// <summary>
    /// Provides extension methods to register async initializers.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the startup task loader.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IServiceCollection AddStartupTaskLoader(this IServiceCollection services) => services.AddHostedService<StartupServiceExecutor>();
    }
}
