using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using System.Linq;
using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache;
using Hagalaz.Cache.Types.Factories;
using Hagalaz.Cache.Models;

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

        [Fact]
        public void AddGameCache_DisposesFileStoreOnceWhenServiceProviderIsDisposed()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            FileStream OpenStoreFile(string fileName) => new(
                Path.Combine(directory, fileName),
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.ReadWrite);

            var loader = new Mock<IFileStoreLoader>();
            loader.Setup(current => current.Open(It.IsAny<string>())).Returns(() => new FileStore(
                    OpenStoreFile("main_file_cache.dat2"),
                    [OpenStoreFile("main_file_cache.idx0")],
                    OpenStoreFile("main_file_cache.idx255"),
                    new IndexCodec(),
                    new SectorCodec()));
            _services.AddSingleton(loader.Object);
            _services.AddGameCache();

            try
            {
                using var serviceProvider = _services.BuildServiceProvider();
                serviceProvider.GetRequiredService<ICacheAPI>();
                serviceProvider.GetRequiredService<IFileStore>();
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void AddGameCache_UsesConfiguredSnapshotCapacityThroughNormalOptionsBinding()
        {
            var store = new Mock<IFileStore>();
            store.SetupGet(current => current.IndexFileCount).Returns(255);
            byte[] containerBytes;
            using (var data = new MemoryStream([3, 4, 5]))
            using (var container = new Container(data))
            {
                containerBytes = container.Encode(false);
            }
            store.Setup(current => current.Read(0, 0)).Returns(() => new MemoryStream(containerBytes));

            var entry = new Mock<IReferenceTableEntry>();
            entry.SetupGet(current => current.Capacity).Returns(1);
            var table = new Mock<IReferenceTable>();
            table.SetupGet(current => current.Capacity).Returns(1);
            table.Setup(current => current.GetEntry(0)).Returns(entry.Object);
            var referenceTables = new Mock<IReferenceTableProvider>();
            referenceTables.Setup(current => current.ReadReferenceTable(0)).Returns(table.Object);

            _services.AddSingleton<IFileStore>(store.Object);
            _services.AddSingleton<IReferenceTableProvider>(referenceTables.Object);
            _services.AddGameCache(options => options.ArchiveSnapshotCacheSizeBytes = 0);
            using var serviceProvider = _services.BuildServiceProvider();
            var cache = serviceProvider.GetRequiredService<ICacheAPI>();

            using var first = cache.Read(0, 0, 0);
            using var second = cache.Read(0, 0, 0);

            Assert.Equal(new byte[] { 3, 4, 5 }, first.ToArray());
            Assert.Equal(new byte[] { 3, 4, 5 }, second.ToArray());
            store.Verify(current => current.Read(0, 0), Times.Exactly(2));
            Assert.Equal(64L * 1024 * 1024, new CacheOptions().ArchiveSnapshotCacheSizeBytes);
        }

        [Fact]
        public void AddGameCache_RejectsNegativeSnapshotCapacityAtConstruction()
        {
            _services.AddSingleton<IFileStore>(Mock.Of<IFileStore>());
            _services.AddSingleton<IReferenceTableProvider>(Mock.Of<IReferenceTableProvider>());
            _services.AddSingleton<ICacheWriter>(Mock.Of<ICacheWriter>());
            _services.AddGameCache(options => options.ArchiveSnapshotCacheSizeBytes = -1);
            using var serviceProvider = _services.BuildServiceProvider();

            Assert.Throws<ArgumentOutOfRangeException>(() => serviceProvider.GetRequiredService<ICacheAPI>());
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
