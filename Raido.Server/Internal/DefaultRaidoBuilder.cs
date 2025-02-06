using System;
using Microsoft.Extensions.DependencyInjection;

namespace Raido.Server.Internal
{
    internal class DefaultRaidoBuilder : IRaidoBuilder
    {
        public IServiceCollection Services { get; }

        public DefaultRaidoBuilder(IServiceCollection services) => Services = services ?? throw new ArgumentNullException(nameof(services));
    }
}