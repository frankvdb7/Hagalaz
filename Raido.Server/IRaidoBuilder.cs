using Microsoft.Extensions.DependencyInjection;

namespace Raido.Server
{
    /// <summary>
    /// A builder for configuring Raido services.
    /// </summary>
    public interface IRaidoBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> where Raido services are configured.
        /// </summary>
        IServiceCollection Services { get; }
    }
}