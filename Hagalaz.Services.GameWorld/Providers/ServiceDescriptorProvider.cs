using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Providers
{
    public class ServiceDescriptorProvider : IServiceDescriptorProvider
    {
        private readonly IServiceCollection _serviceCollection;

        public ServiceDescriptorProvider(IServiceCollection serviceCollection) => _serviceCollection = serviceCollection;

        public IEnumerable<ServiceDescriptor> GetServiceDescriptors() => _serviceCollection;
    }
}