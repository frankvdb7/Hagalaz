using System.Linq;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Extensions.Tests
{
[TestClass]
    public class ServiceCollectionExtensionsTests
    {
        private readonly IServiceCollection _services;

        public ServiceCollectionExtensionsTests()
        {
            _services = new ServiceCollection();
        }

        [TestMethod]
        public void AddGameCache_RegistersSingletonServices()
        {
            // Arrange
            _services.AddGameCache();
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<IFileStoreLoader>());
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<IFileStore>());
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<IReferenceTableProvider>());
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<ICacheWriter>());
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<IContainerDecoder>());
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<IArchiveDecoder>());
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<IChecksumTableCodec>());
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<IIndexCodec>());
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<IReferenceTableCodec>());
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<ISectorCodec>());
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<ICacheAPI>());
            Assert.AreEqual(ServiceLifetime.Singleton, _services.GetLifetime<IHuffmanCodeProvider>());
        }

        [TestMethod]
        public void AddGameCache_RegistersTransientServices()
        {
            // Arrange
            _services.AddGameCache();
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<IMapProvider>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<IHuffmanDecoder>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<IHuffmanEncoder>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<IItemTypeCodec>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<INpcTypeCodec>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<IObjectTypeCodec>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ISpriteTypeCodec>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<IItemType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<INpcType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<ISpriteType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<IQuestTypeCodec>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<IAnimationTypeCodec>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<IQuestType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<IObjectType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeProvider<IAnimationType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<IItemType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<INpcType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<ISpriteType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<IQuestType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<IObjectType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeFactory<IAnimationType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeEventHook<IItemType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeEventHook<IObjectType>>());
            Assert.AreEqual(ServiceLifetime.Transient, _services.GetLifetime<ITypeEventHook<IAnimationType>>());
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