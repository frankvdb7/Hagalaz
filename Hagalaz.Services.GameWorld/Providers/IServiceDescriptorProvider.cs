using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Providers
{
    public interface IServiceDescriptorProvider
    {
        public IEnumerable<ServiceDescriptor> GetServiceDescriptors();
    }
}