using System;
using Hagalaz.Cache.Abstractions.Providers;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Providers;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Factories;
using Hagalaz.Cache.Types.Hooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hagalaz.Cache.Extensions
{
    /// <summary>
    /// Provides extension methods for setting up Hagalaz cache services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Hagalaz game cache services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddGameCache(this IServiceCollection services) => AddGameCache(services, options => { });

        /// <summary>
        /// Adds the Hagalaz game cache services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="options">An <see cref="Action{CacheOptions}"/> to configure the provided <see cref="CacheOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddGameCache(this IServiceCollection services, Action<CacheOptions> options)
        {
            services.Configure(options);
            services.TryAddSingleton<IFileStoreLoader, FileStoreLoader>();
            services.TryAddSingleton<IFileStore>(provider =>
            {
                var cacheOptions = provider.GetRequiredService<IOptions<CacheOptions>>();
                var factory = provider.GetRequiredService<IFileStoreLoader>();
                return factory.Open(cacheOptions.Value.Path);
            });
            services.TryAddSingleton<IReferenceTableProvider, ReferenceTableProvider>();
            services.TryAddSingleton<ICacheWriter, CacheWriter>();
            services.TryAddSingleton<IContainerDecoder, ContainerDecoder>();
            services.TryAddSingleton<IArchiveDecoder, ArchiveDecoder>();
            services.TryAddSingleton<IChecksumTableCodec, ChecksumTableCodec>();
            services.TryAddSingleton<IIndexCodec, IndexCodec>();
            services.TryAddSingleton<IReferenceTableCodec, ReferenceTableCodec>();
            services.TryAddSingleton<ICacheAPI, CacheApi>();
            services.TryAddSingleton<IHuffmanCodeProvider, HuffmanCodeProvider>();
            services.TryAddTransient<IMapDecoder, MapDecoder>();
            services.TryAddTransient<IHuffmanDecoder, HuffmanCodec>();
            services.TryAddTransient<IHuffmanEncoder, HuffmanCodec>();
            services.TryAddTransient<IItemTypeCodec, ItemTypeCodec>();
            services.TryAddTransient<ITypeProvider<IItemType>, ItemTypeProvider>();
            services.TryAddTransient<ITypeProvider<INpcType>, TypeProvider<INpcType, NpcTypeData>>();
            services.TryAddTransient<ITypeProvider<ISpriteType>, TypeProvider<ISpriteType, SpriteTypeData>>();
            services.TryAddTransient<ITypeProvider<IQuestType>, TypeProvider<IQuestType, QuestTypeData>>();
            services.TryAddTransient<ITypeProvider<IObjectType>, TypeProvider<IObjectType, ObjectTypeData>>();
            services.TryAddTransient<ITypeProvider<IAnimationType>, TypeProvider<IAnimationType, AnimationTypeData>>();
            services.TryAddTransient<ITypeFactory<IItemType>, ItemTypeFactory>();
            services.TryAddTransient<ITypeFactory<INpcType>, NpcTypeFactory>();
            services.TryAddTransient<ITypeFactory<ISpriteType>, SpriteTypeFactory>();
            services.TryAddTransient<ITypeFactory<IQuestType>, QuestTypeFactory>();
            services.TryAddTransient<ITypeFactory<IObjectType>, ObjectTypeFactory>();
            services.TryAddTransient<ITypeFactory<IAnimationType>, AnimationTypeFactory>();
            services.TryAddTransient<ITypeEventHook<IItemType>, ItemTypeEventHook>();
            services.TryAddTransient<ITypeEventHook<IObjectType>, ObjectTypeEventHook>();
            services.TryAddTransient<ITypeEventHook<IAnimationType>, AnimationTypeEventHook>();
            return services;
        }
    }
}
