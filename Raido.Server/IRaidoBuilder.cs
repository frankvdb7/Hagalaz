using Microsoft.Extensions.DependencyInjection;

namespace Raido.Server
{
    public interface IRaidoBuilder
    {
        IServiceCollection Services { get; }
    }
}