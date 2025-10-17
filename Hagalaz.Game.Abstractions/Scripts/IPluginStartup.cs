using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Abstractions.Scripts
{
    /// <summary>
    /// Defines a contract for a plugin's startup class, allowing it to register its services
    /// with the application's dependency injection container.
    /// </summary>
    public interface IPluginStartup
    {
        /// <summary>
        /// Configures the services for the plugin.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        void Configure(IServiceCollection services);
    }
}