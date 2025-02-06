using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Abstractions.Scripts
{
    public interface IPluginStartup
    {
        void Configure(IServiceCollection services);
    }
}