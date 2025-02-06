using System;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.DependencyInjection.Extensions
{
    /// <summary>
    /// ServiceLocator to migrate to dependency injection more easier
    /// </summary>
    [Obsolete]
    public class ServiceLocator
    {
        private readonly IServiceProvider _currentServiceProvider;
        private static IServiceProvider _serviceProvider = null!;
        private static ServiceLocator _instance = null!;
        private ServiceLocator(IServiceProvider currentServiceProvider) => _currentServiceProvider = currentServiceProvider;
        public static ServiceLocator Current => _instance ??= new ServiceLocator(_serviceProvider);
        public static void SetLocatorProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
        public TService GetInstance<TService>() where TService : notnull => _currentServiceProvider.GetRequiredService<TService>();
        public IServiceScope CreateScope() => _currentServiceProvider.CreateScope();
    }
}
