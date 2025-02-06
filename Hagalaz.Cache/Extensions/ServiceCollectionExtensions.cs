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

namespace Hagalaz.Cache.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the cache.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns></returns>
        public static IServiceCollection AddGameCache(this IServiceCollection services) => AddGameCache(services, options => { });

        /// <summary>
        /// Adds the cache.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public static IServiceCollection AddGameCache(this IServiceCollection services, Action<CacheOptions> options)
        {
            services.Configure(options);
            services.TryAddSingleton<ICacheAPI, CacheApi>();
            services.TryAddSingleton<IHuffmanCodeProvider, HuffmanCodeProvider>();
            services.TryAddTransient<IMapDecoder, MapDecoder>();
            services.TryAddTransient<IHuffmanDecoder, HuffmanCodec>();
            services.TryAddTransient<IHuffmanEncoder, HuffmanCodec>();
            services.TryAddTransient<ITypeDecoder<IItemType>, TypeDecoder<IItemType, ItemTypeData>>();
            services.TryAddTransient<ITypeDecoder<INpcType>, TypeDecoder<INpcType, NpcTypeData>>();
            services.TryAddTransient<ITypeDecoder<ISpriteType>, TypeDecoder<ISpriteType, SpriteTypeData>>();
            services.TryAddTransient<ITypeDecoder<IQuestType>, TypeDecoder<IQuestType, QuestTypeData>>();
            services.TryAddTransient<ITypeDecoder<IObjectType>, TypeDecoder<IObjectType, ObjectTypeData>>();
            services.TryAddTransient<ITypeDecoder<IAnimationType>, TypeDecoder<IAnimationType, AnimationTypeData>>();
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
