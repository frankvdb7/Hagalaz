using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Providers;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using System.Linq;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Extensions.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        private readonly IServiceCollection _services;

        public ServiceCollectionExtensionsTests()
        {
            _services = new ServiceCollection();
        }

        [Fact]
        public void AddGameCache_RegistersSingletonServices()
        {
            // Arrange
            _services.AddGameCache();
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<IFileStoreLoader>());
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<IFileStore>());
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<IReferenceTableProvider>());
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<ICacheWriter>());
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<IContainerDecoder>());
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<IArchiveDecoder>());
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<IChecksumTableCodec>());
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<IIndexCodec>());
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<IReferenceTableCodec>());
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<ISectorCodec>());
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<ICacheAPI>());
            Assert.Equal(ServiceLifetime.Singleton, _services.GetLifetime<IHuffmanCodeProvider>());
        }

        [Fact]
        public void AddGameCache_RegistersTransientServices()
        {
            // Arrange
            _services.AddGameCache();
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<IMapProvider>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<IHuffmanDecoder>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<IHuffmanEncoder>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<IItemTypeCodec>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<INpcTypeCodec>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<IObjectTypeCodec>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ISpriteTypeCodec>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<IItemType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<INpcType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<ISpriteType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<IQuestTypeCodec>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<IAnimationTypeCodec>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<IQuestType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<IObjectType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<IAnimationType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<IItemType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<INpcType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<ISpriteType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<IQuestType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<IObjectType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<IAnimationType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeEventHook<IItemType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeEventHook<IObjectType>>());
            Assert.Equal(ServiceLifetime.Transient, _services.GetLifetime<ITypeEventHook<IAnimationType>>());
        }
    }

    public static class TestServiceCollectionExtensions
    {
        public static ServiceLifetime GetLifetime<T>(this IServiceCollection services)
        {
            var serviceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
            return serviceDescriptor?.Lifetime ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }
    }
}