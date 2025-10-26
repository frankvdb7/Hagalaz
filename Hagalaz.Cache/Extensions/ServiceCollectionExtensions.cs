using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Providers;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Defaults;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Providers;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Defaults;
using Hagalaz.Cache.Types.Defaults.Codecs;
using Hagalaz.Cache.Types.Factories;
using Hagalaz.Cache.Types.Hooks;
using Microsoft.Extensions.DependencyInjection;
using System;
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
            services.TryAddSingleton<ISectorCodec, SectorCodec>();
            services.TryAddSingleton<ICacheAPI, CacheApi>();
            services.TryAddSingleton<IHuffmanCodeProvider, HuffmanCodeProvider>();
            services.TryAddTransient<IHuffmanDecoder, HuffmanCodec>();
            services.TryAddTransient<IHuffmanEncoder, HuffmanCodec>();
            services.TryAddTransient<IItemTypeCodec, ItemTypeCodec>();
            services.TryAddTransient<INpcTypeCodec, NpcTypeCodec>();
            services.TryAddTransient<IObjectTypeCodec, ObjectTypeCodec>();
            services.TryAddTransient<ISpriteTypeCodec, SpriteTypeCodec>();
            services.TryAddTransient<ITypeProvider<IItemType>, ItemTypeProvider>();
            services.TryAddTransient<ITypeProvider<INpcType>, NpcTypeProvider>();
            services.TryAddTransient<ITypeProvider<ISpriteType>, SpriteTypeProvider>();
            services.TryAddTransient<IQuestTypeCodec, QuestTypeCodec>();
            services.TryAddTransient<IAnimationTypeCodec, AnimationTypeCodec>();
            services.TryAddTransient<ITypeProvider<IQuestType>>(provider =>
                new TypeProvider<IQuestType, QuestTypeData>(
                    provider.GetRequiredService<ICacheAPI>(),
                    provider.GetRequiredService<ITypeFactory<IQuestType>>(),
                    provider.GetRequiredService<IQuestTypeCodec>(),
                    provider.GetService<ITypeEventHook<IQuestType>>()));
            services.TryAddTransient<ITypeProvider<IObjectType>, ObjectTypeProvider>();
            services.TryAddTransient<ITypeProvider<IAnimationType>>(provider =>
                new TypeProvider<IAnimationType, AnimationTypeData>(
                    provider.GetRequiredService<ICacheAPI>(),
                    provider.GetRequiredService<ITypeFactory<IAnimationType>>(),
                    provider.GetRequiredService<IAnimationTypeCodec>(),
                    provider.GetService<ITypeEventHook<IAnimationType>>()));
            services.TryAddTransient<ITypeFactory<IItemType>, ItemTypeFactory>();
            services.TryAddTransient<ITypeFactory<INpcType>, NpcTypeFactory>();
            services.TryAddTransient<ITypeFactory<ISpriteType>, SpriteTypeFactory>();
            services.TryAddTransient<ITypeFactory<IQuestType>, QuestTypeFactory>();
            services.TryAddTransient<ITypeFactory<IObjectType>, ObjectTypeFactory>();
            services.TryAddTransient<ITypeFactory<IAnimationType>, AnimationTypeFactory>();
            services.TryAddTransient<ITypeEventHook<IItemType>, ItemTypeEventHook>();
            services.TryAddTransient<ITypeEventHook<IObjectType>, ObjectTypeEventHook>();
            services.TryAddTransient<ITypeEventHook<IAnimationType>, AnimationTypeEventHook>();
            services.TryAddTransient<IGraphicTypeCodec, GraphicTypeCodec>();
            services.TryAddTransient<ITypeProvider<IGraphicType>>(provider =>
                new TypeProvider<IGraphicType, GraphicTypeData>(
                    provider.GetRequiredService<ICacheAPI>(),
                    provider.GetRequiredService<ITypeFactory<IGraphicType>>(),
                    provider.GetRequiredService<IGraphicTypeCodec>(),
                    provider.GetService<ITypeEventHook<IGraphicType>>()));
            services.TryAddTransient<ITypeFactory<IGraphicType>, GraphicTypeFactory>();
            services.TryAddTransient<ICs2DefinitionCodec, Cs2DefinitionCodec>();
            services.TryAddTransient<ITypeProvider<ICs2Definition>, Cs2DefinitionProvider>();
            services.TryAddTransient<ITypeFactory<ICs2Definition>, Cs2DefinitionFactory>();
            services.TryAddTransient<ICs2IntDefinitionCodec, Cs2IntDefinitionCodec>();
            services.TryAddTransient<ITypeFactory<ICs2IntDefinition>, Cs2IntDefinitionFactory>();
            services.TryAddTransient<ICs2IntDefinitionProvider, Cs2IntDefinitionProvider>();

            services.TryAddTransient<ITypeCodec<IClientMapDefinition>, ClientMapDefinitionCodec>();
            services.TryAddTransient<ITypeFactory<IClientMapDefinition>, ClientMapDefinitionFactory>();
            services.TryAddTransient<IClientMapDefinitionProvider, ClientMapDefinitionProvider>();

            services.TryAddTransient<ITypeCodec<IConfigDefinition>, ConfigDefinitionCodec>();
            services.TryAddTransient<ITypeFactory<IConfigDefinition>, ConfigDefinitionFactory>();
            services.TryAddTransient<IConfigDefinitionProvider, ConfigDefinitionProvider>();

            services.TryAddTransient<ITypeCodec<IVarpBitDefinition>, VarpBitDefinitionCodec>();
            services.TryAddTransient<ITypeFactory<IVarpBitDefinition>, VarpBitDefinitionFactory>();
            services.TryAddTransient<IVarpBitDefinitionProvider, VarpBitDefinitionProvider>();

            services.TryAddTransient<IMapCodec, MapCodec>();
            services.TryAddTransient<ITypeFactory<IMapType>, MapTypeFactory>();
            services.TryAddTransient<IMapProvider, MapProvider>();

            services.TryAddTransient<ITypeCodec<IEquipmentDefaults>, EquipmentDefaultsCodec>();
            services.TryAddTransient<IEquipmentDefaultsProvider, EquipmentDefaultsProvider>();

            return services;
        }
    }
}
