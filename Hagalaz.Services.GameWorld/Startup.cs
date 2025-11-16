using System.Numerics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Raido.Server.Extensions;
using MassTransit;
using System;
using System.Threading.RateLimiting;
using AutoMapper;
using Hagalaz.Cache;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Cache.Types.Hooks;
using Hagalaz.Cache.Types.Providers;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.Glow;
using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Builders.Region;
using Hagalaz.Game.Abstractions.Builders.Request;
using Hagalaz.Game.Abstractions.Builders.Teleport;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Features.Clans;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.Abstractions;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Hubs;
using Hagalaz.Services.GameWorld.Hubs.Filters;
using Hagalaz.Services.GameWorld.Logic.Characters.Consumers;
using Hagalaz.Services.GameWorld.Logic.Characters.StateMachines;
using Hagalaz.Services.GameWorld.Logic.Characters.States;
using Hagalaz.Services.GameWorld.Logic.Dehydrators;
using Hagalaz.Services.GameWorld.Logic.Hydrators;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using Hagalaz.Services.GameWorld.Mediator;
using Hagalaz.Services.GameWorld.Mediator.Consumers;
using Hagalaz.Services.GameWorld.Network.Consumers;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Hagalaz.Services.GameWorld.Network.Handshake.Decoders;
using Hagalaz.Services.GameWorld.Network.Handshake.Encoders;
using Hagalaz.Services.GameWorld.Network.Protocol;
using Hagalaz.Services.GameWorld.Network.Protocol._742;
using Hagalaz.Services.GameWorld.Network.Protocol._742.Decoders;
using Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Hagalaz.Workers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Hagalaz.Services.Extensions;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Extensions;
using Hagalaz.Services.GameWorld.Logic.Loot;
using Hagalaz.Services.GameWorld.Logic.Skills;
using Hagalaz.Services.GameWorld.Services.Cache;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Polly;
using Polly.CircuitBreaker;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Hagalaz.Services.GameWorld
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();

            // fusion cache
            services.AddFusionCache()
                .WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromMinutes(5))
                .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                .WithDistributedCache(new RedisCache(new RedisCacheOptions
                {
                    Configuration = Configuration.GetConnectionString("cache")
                }))
                .AsHybridCache();

            // polly
            services.AddResiliencePipelineRegistry<string>();

            // services
            services.AddSingleton<Hagalaz.Game.Abstractions.Logic.Random.IRandomProvider, Hagalaz.Services.GameWorld.Logic.Random.DefaultRandomProvider>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IClientPermissionProvider, ClientPermissionProvider>();
            services.AddScoped<IClientProtocolResolver, ClientProtocolResolver>();
            services.AddSingleton<IBackgroundTaskQueue>(_ => new DefaultBackgroundTaskQueue(100));
            services.AddHostedService<WorldStatusService>();
            services.AddHostedService<QueuedHostedService>();
            services.AddHostedService<GameWorkerService>();
            services.AddHostedService<CharacterDehydrationWorkerService>();
            services.AddSingleton<InMemoryEventBus>();
            services.AddSingleton<IEventBus>(provider => provider.GetRequiredService<InMemoryEventBus>());
            services.AddSingleton<IEventManager>(provider => provider.GetRequiredService<InMemoryEventBus>());
            services.AddSingleton<ISystemUpdateService, SystemUpdateService>();
            services.AddSingleton<IRsTaskService, RsTaskService>();
            services.AddTransient<ICreatureTaskService, RsTaskService>();
            services.AddSingleton<IGameMessageService, GameMessageService>();
            services.AddSingleton<IHitSplatRenderTypeProvider, HitSplatRenderTypeProvider>();
            services.AddScoped<IRatesService, RatesService>();
            services.AddScoped<IStateService, StateService>();

            services.AddScoped<IGameSessionService, GameSessionService>();
            services.AddScoped<IGameConnectionService, GameConnectionService>();
            services.AddSingleton<GameSessionStore>();

            // misc
            services.AddSingleton<StateScriptProvider>();
            services.AddSingleton<IStateScriptProvider>(provider => provider.GetRequiredService<StateScriptProvider>());
            services.AddScoped<IStateScriptFactory, StateScriptMetaDataFactory>();
            services.AddStates();

            // character
            services.AddScoped<ICharacterFactory, CharacterFactory>();
            services.AddScoped<ICharacterService, CharacterService>();
            services.AddScoped<ICharacterCreateInfoRepository, CharacterCreateInfoRepository>();
            services.AddSingleton<ICharacterStore, CharacterStore>();
            services.AddScoped<ICharacterRenderMasksWriter, CharacterRenderMasksWriter>();
            services.AddScoped<IDefaultCharacterScriptProvider, DefaultCharacterScriptProvider>();
            services.AddSingleton<CharacterNpcScriptProvider>();
            services.AddSingleton<ICharacterNpcScriptProvider>(provider => provider.GetRequiredService<CharacterNpcScriptProvider>());
            services.AddScoped<ICharacterNpcScriptFactory, CharacterNpcScriptMetaDataFactory>();
            services.AddSingleton<ICharacterLocationService, CharacterLocationService>();
            services.AddScoped<ISlayerTaskDefinitionRepository, SlayerTaskDefinitionRepository>();
            services.AddScoped<ISlayerMasterDefinitionRepository, SlayerMasterDefinitionRepository>();
            services.AddScoped<SlayerService>();
            services.AddScoped<ISlayerService>(provider =>
            {
                var service = provider.GetRequiredService<SlayerService>();
                var cache = provider.GetRequiredService<HybridCache>();
                return new CachedSlayerService(service, cache);
            });
            services.AddScoped<ICharacterContextProvider, CharacterContextAccessor>();
            services.AddScoped<ICharacterContextAccessor>(provider => provider.GetRequiredService<ICharacterContextProvider>());
            services.AddScoped<IItemPartFactory, ItemPartFactory>();

            services.AddScoped<ICharacterHydrationService, CharacterHydrationService>();
            services.AddScoped<ICharacterHydrator, ClaimsHydrator>();
            services.AddScoped<ICharacterHydrator, AppearanceHydrator>();
            services.AddScoped<ICharacterHydrator, DetailsHydrator>();
            services.AddScoped<ICharacterHydrator, StatisticsHydrator>();
            services.AddScoped<ICharacterHydrator, ItemCollectionHydrator>();
            services.AddScoped<ICharacterHydrator, ItemAppearanceCollectionHydrator>();
            services.AddScoped<ICharacterHydrator, FamiliarHydrator>();
            services.AddScoped<ICharacterHydrator, MusicHydrator>();
            services.AddScoped<ICharacterHydrator, FarmingHydrator>();
            services.AddScoped<ICharacterHydrator, SlayerHydrator>();
            services.AddScoped<ICharacterHydrator, NotesHydrator>();
            services.AddScoped<ICharacterHydrator, ProfileHydrator>();
            services.AddScoped<ICharacterHydrator, StateHydrator>();

            services.AddScoped<ICharacterDehydrationService, CharacterDehydrationService>();
            services.AddScoped<ICharacterDehydrator, AppearanceDehydrator>();
            services.AddScoped<ICharacterDehydrator, DetailsDehydrator>();
            services.AddScoped<ICharacterDehydrator, StatisticsDehydrator>();
            services.AddScoped<ICharacterDehydrator, ItemCollectionDehydrator>();
            services.AddScoped<ICharacterDehydrator, ItemAppearanceCollectionDehydrator>();
            services.AddScoped<ICharacterDehydrator, FamiliarDehydrator>();
            services.AddScoped<ICharacterDehydrator, MusicDehydrator>();
            services.AddScoped<ICharacterDehydrator, FarmingDehydrator>();
            services.AddScoped<ICharacterDehydrator, SlayerDehydrator>();
            services.AddScoped<ICharacterDehydrator, NotesDehydrator>();
            services.AddScoped<ICharacterDehydrator, ProfileDehydrator>();
            services.AddScoped<ICharacterDehydrator, StateDehydrator>();

            // npc
            services.AddScoped<INpcSpawnRepository, NpcSpawnRepository>();
            services.AddScoped<INpcService, NpcService>();
            services.AddSingleton<INpcStore, NpcStore>();
            services.AddSingleton<NpcDefinitionStore>();
            services.AddScoped<INpcDefinitionRepository, NpcDefinitionRepository>();
            services.AddScoped<INpcStatisticsRepository, NpcStatisticsRepository>();
            services.AddScoped<INpcBonusesRepository, NpcBonusesRepository>();
            services.AddSingleton<INpcRenderMasksWriter, NpcRenderMasksWriter>();
            services.AddSingleton<NpcScriptProvider>();
            services.AddSingleton<INpcScriptProvider>(provider => provider.GetRequiredService<NpcScriptProvider>());
            services.AddSingleton<FamiliarScriptProvider>();
            services.AddSingleton<IFamiliarScriptProvider>(provider => provider.GetRequiredService<FamiliarScriptProvider>());
            services.AddScoped<IFamiliarScriptFactory, FamiliarScriptFactory>();
            services.AddScoped<INpcScriptFactory, NpcScriptMetaDataFactory>();
            services.AddSingleton<INpcBuilder, NpcBuilder>();

            // map
            services.AddSingleton<IMapRegionService, MapRegionService>();
            services.AddScoped<IMapRegionLoader, MapRegionLoader>();
            services.AddHostedService<MapRegionBackgroundService>();
            services.AddSingleton<ILocationBuilder, LocationBuilder>();
            services.AddSingleton<IRegionUpdateBuilder, RegionUpdateBuilder>();

            // items
            services.AddScoped<IEquipmentDefinitionRepository, EquipmentDefinitionRepository>();
            services.AddScoped<IEquipmentService, EquipmentService>();
            services.AddSingleton<EquipmentStore>();
            services.AddScoped<IItemDefinitionRepository, ItemDefinitionRepository>();
            services.AddSingleton<ItemStore>();
            services.AddScoped<IItemService, ItemService>();
            services.AddScoped<IGroundItemSpawnRepository, GroundItemSpawnRepository>();
            services.AddScoped<IGroundItemService, GroundItemService>();
            services.AddSingleton<IItemScriptProvider>(provider => provider.GetRequiredService<ItemScriptProvider>());
            services.AddSingleton<ItemScriptProvider>();
            services.AddSingleton<EquipmentScriptProvider>();
            services.AddSingleton<IEquipmentScriptProvider>(provider => provider.GetRequiredService<EquipmentScriptProvider>());
            services.AddSingleton<IBodyDataRepository, BodyDataRepository>();
            services.AddScoped<ILootService, LootService>();
            services.AddSingleton<LootStore>();
            services.AddScoped<ILootModifierProvider, LootModifierProvider>();
            services.AddScoped<IGameObjectLootItemRepository, GameObjectLootItemRepository>();
            services.AddScoped<IGameObjectLootRepository, GameObjectLootRepository>();
            services.AddSingleton<ILootGenerator, LootGenerator>();
            services.AddScoped<INpcLootItemRepository, NpcLootItemRepository>();
            services.AddScoped<INpcLootRepository, NpcLootRepository>();
            services.AddScoped<IItemLootItemRepository, ItemLootItemRepository>();
            services.AddScoped<IItemLootRepository, ItemLootRepository>();
            services.AddScoped<IItemContainerFactory, ItemContainerFactory>();
            services.AddScoped<IItemScriptFactory, ItemScriptMetaDataFactory>();
            services.AddScoped<IEquipmentScriptFactory, EquipmentScriptMetaDataFactory>();
            services.AddSingleton<IItemBuilder, ItemBuilder>();

            // gameobject
            services.AddScoped<GameObjectService>();
            services.AddScoped<IGameObjectService>(provider =>
            {
                var inner = provider.GetRequiredService<GameObjectService>();
                var cache = provider.GetRequiredService<HybridCache>();
                return new CachedGameObjectService(inner, cache);
            });
            services.AddScoped<IGameObjectDefinitionRepository, GameObjectDefinitionRepository>();
            services.AddScoped<IGameObjectSpawnRepository, GameObjectSpawnRepository>();
            services.AddSingleton<GameObjectScriptProvider>();
            services.AddSingleton<IGameObjectScriptProvider>(provider => provider.GetRequiredService<GameObjectScriptProvider>());
            services.AddScoped<IGameObjectScriptFactory, GameObjectScriptMetaDataFactory>();

            // animations
            services.AddSingleton<IAnimationService, AnimationService>();
            services.AddSingleton<IAnimationBuilder, AnimationBuilder>();

            // widgets
            services.AddSingleton<WidgetScriptProvider>();
            services.AddSingleton<IWidgetScriptProvider>(provider => provider.GetRequiredService<WidgetScriptProvider>());
            services.AddScoped<IWidgetScriptFactory, WidgetScriptMetaDataFactory>();
            services.AddSingleton<IWidgetBuilder, WidgetBuilder>();
            services.AddSingleton<IWidgetOptionBuilder, WidgetOptionBuilder>();

            // graphics
            services.AddSingleton<IGraphicBuilder, GraphicBuilder>();

            // shops
            services.AddScoped<IShopService, ShopService>();
            services.AddScoped<IShopRepository, ShopRepository>();

            // world
            services.AddScoped<IWorldInfoService, WorldInfoService>();
            services.AddScoped<IWorldRepository, WorldRepository>();
            services.AddSingleton<WorldInfoStore>();
            services.AddSingleton<ShopStore>();
            services.AddSingleton<AreaScriptProvider>();
            services.AddSingleton<IAreaScriptProvider>(provider => provider.GetRequiredService<AreaScriptProvider>());
            services.AddSingleton<AreaStore>();
            services.AddScoped<IAreaService, AreaService>();
            services.AddScoped<IAreaRepository, AreaRepository>();
            services.AddScoped<IAreaScriptFactory, AreaScriptMetaDataFactory>();
            services.AddScoped<IMusicService, MusicService>();
            services.AddScoped<IMusicLocationRepository, MusicLocationRepository>();
            services.AddScoped<IMusicDefinitionRepository, MusicDefinitionRepository>();
            services.AddScoped<ITzhaarWaveDefinitionRepository, TzhaarWaveDefinitionRepository>();
            services.AddScoped<ITzHaarCaveService, TzHaarCaveService>();
            services.AddScoped<ISmartPathFinder, SmartPathFinder>();
            services.AddScoped<ISimplePathFinder, DumbPathFinder>();
            services.AddScoped<IProjectilePathFinder, ProjectilePathFinder>();
            services.AddScoped<IPathFinderProvider, PathFinderProvider>();

            // clans
            services.AddSingleton<IClanService, ClanService>();

            // factories
            services.AddScoped<IGameSessionFactory, GameSessionFactory>();
            services.AddScoped<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();

            // builders
            services.AddSingleton<IGlowBuilder, GlowBuilder>();
            services.AddSingleton<IHintIconBuilder, HintIconBuilder>();
            services.AddSingleton<ITeleportBuilder, TeleportBuilder>();
            services.AddSingleton<IAudioBuilder, AudioBuilder>();
            services.AddSingleton<IProjectileBuilder, ProjectileBuilder>();
            services.AddSingleton<IGroundItemBuilder, GroundItemBuilder>();
            services.AddSingleton<IGameObjectBuilder, GameObjectBuilder>();
            services.AddSingleton<IChatMessageRequestBuilder, ChatMessageRequestBuilder>();
            services.AddSingleton<IMovementBuilder, MovementBuilder>();
            services.AddSingleton<IHitSplatBuilder, HitSplatBuilder>();

            // skills
            services.AddScoped<ICookingFoodRepository, CookingFoodRepository>();
            services.AddScoped<ICookingRawFoodRepository, CookingRawFoodRepository>();
            services.AddScoped<ICookingService, CookingService>();
            services.AddSingleton<ISlayerTaskGenerator, SlayerTaskGenerator>();

            services.AddScoped<ICraftingGemDefinitionRepository, CraftingGemDefinitionRepository>();
            services.AddScoped<ICraftingJewelryDefinitionRepository, CraftingJewelryDefinitionRepository>();
            services.AddScoped<ICraftingLeatherDefinitionRepository, CraftingLeatherDefinitionRepository>();
            services.AddScoped<ICraftingPotteryDefinitionRepository, CraftingPotteryDefinitionRepository>();
            services.AddScoped<ICraftingSilverDefinitionRepository, CraftingSilverDefinitionRepository>();
            services.AddScoped<ICraftingTanDefinitionRepository, CraftingTanDefinitionRepository>();
            services.AddScoped<ICraftingSpinDefinitionRepository, CraftingSpinDefinitionRepository>();
            services.AddScoped<ICraftingService, CraftingService>();
            services.AddScoped<IFiremakingRepository, FiremakingRepository>();
            services.AddScoped<IFiremakingService, FiremakingService>();
            services.AddScoped<IHerbloreService, HerbloreService>();
            services.AddScoped<IHerbloreRepository, HerbloreRepository>();
            services.AddScoped<IPrayerService, PrayerService>();
            services.AddScoped<IPrayerRepository, PrayerRepository>();
            services.AddScoped<IRunecraftingService, RunecraftingService>();
            services.AddScoped<IRunecraftingRepository, RunecraftingRepository>();
            services.AddScoped<ISummoningDefinitionRepository, SummoningDefinitionRepository>();
            services.AddScoped<ISummoningService, SummoningService>();
            services.AddScoped<IFishingSpotDefinitionRepository, FishingSpotDefinitionRepository>();
            services.AddScoped<FishingService>();
            services.AddScoped<IFishingService>(provider =>
            {
                var inner = provider.GetRequiredService<FishingService>();
                var cache = provider.GetRequiredService<HybridCache>();
                return new CachedFishingService(inner, cache);
            });
            services.AddScoped<IWoodcuttingHatchetDefinitionRepository, WoodcuttingHatchetDefinitionRepository>();
            services.AddScoped<IWoodcuttingLogDefinitionRepository, WoodcuttingLogDefinitionRepository>();
            services.AddScoped<IWoodcuttingTreeDefinitionRepository, WoodcuttingTreeDefinitionRepository>();
            services.AddScoped<IWoodcuttingService, WoodcuttingService>();
            services.AddScoped<IMiningOreRepository, MiningOreRepository>();
            services.AddScoped<IMiningPickaxeRepository, MiningPickaxeRepository>();
            services.AddScoped<IMiningRockRepository, MiningRockRepository>();
            services.AddScoped<IMiningService, MiningService>();
            services.AddScoped<IMagicEnchantingSpellsProductRepository, MagicEnchantingSpellsProductRepository>();
            services.AddScoped<IMagicCombatSpellsRepository, MagicCombatSpellsRepository>();
            services.AddScoped<IMagicEnchantingSpellsRepository, MagicEnchantingSpellsRepository>();
            services.AddScoped<IMagicService, MagicService>();
            services.AddScoped<ILodestoneRepository, LodestoneRepository>();
            services.AddScoped<ILodestoneService, LodestoneService>();
            services.AddScoped<IFarmingPatchDefinitionRepository, FarmingPatchDefinitionRepository>();
            services.AddScoped<IFarmingSeedDefinitionRepository, FarmingSeedDefinitionRepository>();
            services.AddScoped<IFarmingService, FarmingService>();

            // cache types
            services.AddTransient<ITypeFactory<IItemDefinition>, ItemDefinitionFactory>();
            services.AddTransient<ITypeFactory<INpcDefinition>, NpcDefinitionFactory>();
            services.AddTransient<ITypeFactory<GameObjectDefinition>, GameObjectDefinitionFactory>();
            services.AddTransient<ITypeFactory<IAnimationDefinition>, AnimationDefinitionFactory>();

            services.AddTransient<ITypeEventHook<INpcDefinition>, NpcDefinitionEventHook>();
            services.AddTransient<ITypeEventHook<IItemDefinition>, ItemTypeEventHook>();
            services.AddTransient<ITypeEventHook<GameObjectDefinition>, ObjectTypeEventHook>();

            services.AddTransient<ITypeProvider<IItemType>, ItemTypeProvider>();
            services.AddTransient<ITypeProvider<IItemDefinition>, ItemDefinitionProvider>();
            services.AddTransient<ITypeProvider<INpcDefinition>, NpcDefinitionProvider>();
            services.AddTransient<ITypeProvider<GameObjectDefinition>, TypeProvider<GameObjectDefinition, ObjectTypeData>>();
            services.AddTransient<ITypeProvider<IAnimationDefinition>, TypeProvider<IAnimationDefinition, AnimationTypeData>>();

            // world options
            services.Configure<WorldOptions>(Configuration.GetSection(WorldOptions.Key), options => options.BindNonPublicProperties = true);
            services.Configure<WorldOptions>(options => options.Id = Configuration.GetValue(ServiceDefaults.EnvironmentVariables.HagalazWorldId, 1));
            services.Configure<CombatOptions>(Configuration.GetSection(CombatOptions.Key));
            services.Configure<ItemOptions>(Configuration.GetSection(ItemOptions.Key));
            services.Configure<GroundItemOptions>(Configuration.GetSection(GroundItemOptions.Key));
            services.Configure<SkillOptions>(Configuration.GetSection(SkillOptions.Key));

            // server options
            services.Configure<KestrelServerOptions>(Configuration.GetSection("Kestrel"));
            services.Configure<CacheOptions>(Configuration.GetSection(CacheOptions.Key));
            services.Configure<ServerConfig>(Configuration.GetSection(ServerConfig.Key));
            services.Configure<RsaCacheConfig>(options =>
            {
                var section = Configuration.GetRequiredSection(RsaCacheConfig.Key);
                options.PublicKey = BigInteger.Parse(section.GetValue<string>("PublicKey")!);
                options.ModulusKey = BigInteger.Parse(section.GetValue<string>("ModulusKey")!);
                options.PrivateKey = BigInteger.Parse(section.GetValue<string>("PrivateKey")!);
            });
            services.Configure<RsaClientConfig>(options =>
            {
                var section = Configuration.GetRequiredSection(RsaClientConfig.Key);
                options.PublicKey = BigInteger.Parse(section.GetValue<string>("PublicKey")!);
                options.ModulusKey = BigInteger.Parse(section.GetValue<string>("ModulusKey")!);
                options.PrivateKey = BigInteger.Parse(section.GetValue<string>("PrivateKey")!);
            });

            // Configure Identity to use the same JWT claims as OpenIddict instead
            // of the legacy WS-Federation claims it uses by default (ClaimTypes),
            // which saves you from doing the mapping in your authorization controller.
            services.Configure<IdentityOptions>(options =>
            {
                options.ClaimsIdentity.UserNameClaimType = Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
                options.ClaimsIdentity.RoleClaimType = Claims.Role;
                options.ClaimsIdentity.EmailClaimType = Claims.Email;
            });

            services.AddGameCache();

            services.Configure<GameServerOptions>(options =>
            {
                var gameOptions = Configuration.GetSection("GameServer");
                options.AuthenticationToken = gameOptions.GetValue<string>("AuthenticationToken")!;
            });

            services.AddRaidoServer(options =>
                {
                    // keep client timeout double keep alive
                    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                    options.AddGlobalFilter<CharacterFilter>();
                })
                .AddHub<HandshakeHub>()
                .AddHub<ConnectionHub>()
                .AddHub<CharacterHub>()
                .AddHub<NpcHub>()
                .AddHub<AdminHub>()
                .AddHub<ContactHub>()
                .AddHub<LobbyHub>()
                .AddHub<ComponentHub>()
                .AddHub<ItemHub>()
                .AddHub<GameObjectHub>()
                .AddHub<ProfileHub>();

            services.AddRaidoProtocol<HandshakeProtocol>(options =>
            {
                options.AddDecoder<ClientHandshakeRequestDecoder>(14);
                options.AddDecoder<ClientUpdateRequestDecoder>(15);
                options.AddDecoder<WorldHandshakeRequestDecoder>(16);
                options.AddDecoder<WorldHandshakeRequestDecoder>(18); // reconnect
                options.AddDecoder<LobbyHandshakeRequestDecoder>(19);

                options.AddEncoder<ClientHandshakeResponseEncoder>();
                options.AddEncoder<ClientSignInResponseEncoder>();
                options.AddEncoder<LobbySignInResponseEncoder>();
                options.AddEncoder<WorldSignInResponseEncoder>();
            });

            services.AddRaidoProtocol<ClientProtocol742>(options =>
            {
                options.AddDecoder<PingMessageDecoder>(30);
                options.AddDecoder<GetWorldInfoRequestDecoder>(78);
                options.AddDecoder<InterfaceComponentLeftClickDecoder>(4);
                options.AddDecoder<InterfaceComponentOption2ClickDecoder>(22);
                options.AddDecoder<InterfaceComponentOption3ClickDecoder>(13);
                options.AddDecoder<InterfaceComponentOption4ClickDecoder>(76);
                options.AddDecoder<InterfaceComponentOption5ClickDecoder>(59);
                options.AddDecoder<InterfaceComponentOption6ClickDecoder>(103);
                options.AddDecoder<InterfaceComponentOption7ClickDecoder>(37);
                options.AddDecoder<InterfaceComponentOption8ClickDecoder>(69);
                options.AddDecoder<InterfaceComponentOption9ClickDecoder>(80);
                options.AddDecoder<InterfaceComponentOption10ClickDecoder>(28);
                options.AddDecoder<InterfaceComponentSpecialClickMessageDecoder>(83);
                options.AddDecoder<InterfaceComponentUrlClickMessageDecoder>(11);
                options.AddDecoder<NpcClickOption1MessageDecoder>(72);
                options.AddDecoder<NpcClickOption2MessageDecoder>(54);
                options.AddDecoder<NpcClickOption3MessageDecoder>(43);
                options.AddDecoder<NpcClickOption4MessageDecoder>(88);
                options.AddDecoder<NpcClickOption5MessageDecoder>(32);
                options.AddDecoder<NpcClickOption6MessageDecoder>(0);
                options.AddDecoder<GameObjectClickOption1MessageDecoder>(40);
                options.AddDecoder<GameObjectClickOption2MessageDecoder>(3);
                options.AddDecoder<GameObjectClickOption3MessageDecoder>(16);
                options.AddDecoder<GameObjectClickOption4MessageDecoder>(85);
                options.AddDecoder<GameObjectClickOption5MessageDecoder>(82);
                options.AddDecoder<GameObjectClickOption6MessageDecoder>(49);
                options.AddDecoder<GroundItemClickOption1MessageDecoder>(51);
                options.AddDecoder<GroundItemClickOption2MessageDecoder>(34);
                options.AddDecoder<GroundItemClickOption3MessageDecoder>(62);
                options.AddDecoder<GroundItemClickOption4MessageDecoder>(27);
                options.AddDecoder<GroundItemClickOption5MessageDecoder>(23);
                options.AddDecoder<GroundItemClickOption6MessageDecoder>(42);
                options.AddDecoder<MovementMessageDecoder>(74); // screen
                options.AddDecoder<MovementMessageDecoder>(41); // minimap
                options.AddDecoder<MouseMovementMessageDecoder>(67); // screen
                options.AddDecoder<MouseMovementMessageDecoder>(98); // window
                options.AddDecoder<FocusEventMessageDecoder>(94); // window
                options.AddDecoder<MouseEventMessageDecoder>(61);
                options.AddDecoder<MouseClickEventMessageDecoder>(90);
                options.AddDecoder<ConsoleCommandMessageDecoder>(20);
                options.AddDecoder<InterfaceComponentRemovedMessageDecoder>(57);
                options.AddDecoder<KeyboardEventMessageDecoder>(24);
                options.AddDecoder<InterfaceComponentTextInputMessageDecoder>(21);
                options.AddDecoder<InterfaceComponentTextInputMessageDecoder>(68);
                options.AddDecoder<InterfaceComponentNumberInputMessageDecoder>(65);
                options.AddDecoder<InterfaceComponentColorInputMessageDecoder>(5);
                options.AddDecoder<CameraMoveEventMessageDecoder>(33);
                options.AddDecoder<InterfaceComponentDragMessageDecoder>(7);
                options.AddDecoder<MusicPlayedMessageDecoder>(19);
                options.AddDecoder<InterfaceComponentUseOnComponentMessageDecoder>(26);
                options.AddDecoder<SetClientChatTypeMessageDecoder>(70);
                options.AddDecoder<SetChatFilterMessageDecoder>(73);
                options.AddDecoder<PublicChatMessageDecoder>(86);
                options.AddDecoder<SetClientWindowMessageDecoder>(31);
                options.AddDecoder<CharacterClickOption1Decoder>(44);
                options.AddDecoder<CharacterClickOption2Decoder>(79);
                options.AddDecoder<CharacterClickOption3Decoder>(50);
                options.AddDecoder<CharacterClickOption4Decoder>(104);
                options.AddDecoder<CharacterClickOption5Decoder>(58);
                options.AddDecoder<CharacterClickOption6Decoder>(36);
                options.AddDecoder<CharacterClickOption7Decoder>(35);
                options.AddDecoder<CharacterClickOption8Decoder>(55);
                options.AddDecoder<CharacterClickOption9Decoder>(53);
                options.AddDecoder<InterfaceComponentUseOnCharacterMessageDecoder>(106);
                options.AddDecoder<InterfaceComponentUseOnNpcMessageDecoder>(6);
                options.AddDecoder<InterfaceComponentUserOnGroundItemMessageDecoder>(96);
                options.AddDecoder<InterfaceComponentUserOnGameObjectMessageDecoder>(107);
                options.AddDecoder<AddIgnoreMessageDecoder>(38);
                options.AddDecoder<AddFriendMessageDecoder>(71);
                options.AddDecoder<RemoveIgnoreMessageDecoder>(17);
                options.AddDecoder<RemoveFriendMessageDecoder>(81);
                options.AddDecoder<AddContactMessageDecoder>(95);
                options.AddDecoder<ClientInfoMessageDecoder>(93);

                options.AddEncoder<DrawCharactersMessageEncoder>();
                options.AddEncoder<DrawNpcsMessageEncoder>();
                options.AddEncoder<DrawFrameComponentMessageEncoder>();
                options.AddEncoder<DrawInterfaceComponentMessageEncoder>();
                options.AddEncoder<DrawItemComponentMessageEncoder>();
                options.AddEncoder<DrawStandardMapMessageEncoder>();
                options.AddEncoder<DrawItemContainerMessageEncoder>();
                options.AddEncoder<FriendsListMessageEncoder>();
                options.AddEncoder<IgnoreListMessageEncoder>();
                options.AddEncoder<PingMessageEncoder>();
                options.AddEncoder<SetConfigMessageEncoder>();
                options.AddEncoder<SetVarpBitMessageEncoder>();
                options.AddEncoder<SetWorldInfoMessageEncoder>();
                options.AddEncoder<SetComponentOptionsMessageEncoder>();
                options.AddEncoder<SetComponentVisibilityMessageEncoder>();
                options.AddEncoder<SetSkillMessageEncoder>();
                options.AddEncoder<SetRunEnergyMessageEncoder>();
                options.AddEncoder<SetChatFilterMessageEncoder>();
                options.AddEncoder<SetCS2IntMessageEncoder>();
                options.AddEncoder<SetItemContainerMessageEncoder>();
                options.AddEncoder<RunCS2ScriptMessageEncoder>();
                options.AddEncoder<RemoveInterfaceComponentMessageEncoder>();
                options.AddEncoder<ChatMessageEncoder>();
                options.AddEncoder<AddGameObjectMessageEncoder>();
                options.AddEncoder<AddGroundItemMessageEncoder>();
                options.AddEncoder<DrawGraphicMessageEncoder>();
                options.AddEncoder<DrawProjectileMessageEncoder>();
                options.AddEncoder<RemoveGameObjectMessageEncoder>();
                options.AddEncoder<RemoveGroundItemMessageEncoder>();
                options.AddEncoder<SetGameObjectAnimationMessageEncoder>();
                options.AddEncoder<SetGroundItemCountMessageEncoder>();
                options.AddEncoder<MapRegionPartUpdateMessageEncoder>();
                options.AddEncoder<LogoutToLoginMessageEncoder>();
                options.AddEncoder<LogoutToLobbyMessageEncoder>();
                options.AddEncoder<DrawStringComponentMessageEncoder>();
                options.AddEncoder<SetComponentAnimationMessageEncoder>();
                options.AddEncoder<DrawNpcComponentMessageEncoder>();
                options.AddEncoder<DrawCharacterComponentMessageEncoder>();
                options.AddEncoder<SetCS2StringMessageEncoder>();
                options.AddEncoder<SetWeightMessageEncoder>();
                options.AddEncoder<SetCharacterOptionMessageEncoder>();
                options.AddEncoder<DrawLocationHintIconMessageEncoder>();
                options.AddEncoder<DrawCreatureHintIconMessageEncoder>();
                options.AddEncoder<RemoveHintIconMessageEncoder>();
                options.AddEncoder<PublicChatMessageEncoder>();
                options.AddEncoder<PlayMusicMessageEncoder>();
                options.AddEncoder<SetMiniMapTypeMessageEncoder>();
                options.AddEncoder<SetSystemUpdateTickMessageEncoder>();
                options.AddEncoder<DrawModelComponentMessageEncoder>();
                options.AddEncoder<DrawSpriteComponentMessageEncoder>();
                options.AddEncoder<SetCameraShakeMessageEncoder>();
                options.AddEncoder<DrawTileStringMessageEncoder>();
                options.AddEncoder<PlayMusicEffectMessageEncoder>();
                options.AddEncoder<PlayVoiceMessageEncoder>();
                options.AddEncoder<SetCameraLocationMessageEncoder>();
                options.AddEncoder<SetCameraLookAtLocationMessageEncoder>();
                options.AddEncoder<AddContactReceiverMessageEncoder>();
                options.AddEncoder<AddContactSenderMessageEncoder>();
            });
            services.AddScoped<IClientProtocol>(provider => provider.GetRequiredService<ClientProtocol742>());

            // rabbit mq
            services.AddAuthorization();
            services.AddMassTransit(x =>
            {
                x.AddDelayedMessageScheduler();
                x.UsingRabbitMq((context, cfg) =>
                {
                    var host = Configuration.GetConnectionString("messaging");
                    if (host == null)
                    {
                        throw new ArgumentNullException(nameof(host));
                    }

                    cfg.UseDelayedMessageScheduler();

                    cfg.Host(host);

                    cfg.ConfigureEndpoints(context);
                });

                x.AddSagaStateMachine<CharacterHydrationStateMachine, CharacterHydrationState>()
                    .InMemoryRepository();
                x.AddSagaStateMachine<CharacterDehydrationStateMachine, CharacterDehydrationState>()
                    .InMemoryRepository();

                x.AddConsumer<WorldUserSignInConsumer>();
                x.AddConsumer<WorldUserSignOutConsumer>();
                x.AddConsumer<ContactSignInConsumer>();
                x.AddConsumer<ContactSignOutConsumer>();
                x.AddConsumer<ContactSettingsChangedConsumer>();
                x.AddConsumer<ContactAddedConsumer>();
                x.AddConsumer<ContactRemovedConsumer>();
                x.AddConsumer<GetContactsResponseConsumer>();
                x.AddConsumer<WorldStatusRequestConsumer>();
                x.AddConsumer<WorldOnlineConsumer>();
                x.AddConsumer<WorldOfflineConsumer>();
                x.AddConsumer<ContactMessageNotificationConsumer>();
            });
            services.AddMediator(options =>
            {
                options.AddScoped<IScopedGameMediator, ScopedGameMediator>();
                options.AddSingleton<IGameMediator, GameMediator>();

                options.AddConsumer<LobbySignInCommandConsumer>();
                options.AddConsumer<LobbySignOutCommandConsumer>();
                options.AddConsumer<WorldSignInCommandConsumer>();
                options.AddConsumer<WorldSignOutCommandConsumer>();
                options.AddConsumer<WorldStatusRequestConsumer>();
                options.AddConsumer<SendWorldInfoCommandConsumer>();

                // character
                options.AddConsumer<ProfileSetObjectActionConsumer>();
                options.AddConsumer<ProfileSetEnumActionConsumer>();
                options.AddConsumer<ProfileSetBoolActionConsumer>();
                options.AddConsumer<ProfileToggleBoolActionConsumer>();
                options.AddConsumer<ProfileSetIntActionConsumer>();
                options.AddConsumer<ProfileSetStringActionConsumer>();
                options.AddConsumer<ProfileIncrementIntActionConsumer>();
                options.AddConsumer<ProfileDecrementIntActionConsumer>();
                options.AddConsumer<ChatSettingsFilterChangedConsumer>();
            });

            // startup tasks
            services.AddStartupTaskLoader();

            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<AreaStore>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<EquipmentStore>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<ItemStore>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<LootStore>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<NpcDefinitionStore>());

            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<ItemScriptProvider>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<GameObjectScriptProvider>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<NpcScriptProvider>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<EquipmentScriptProvider>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<AreaScriptProvider>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<CharacterNpcScriptProvider>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<FamiliarScriptProvider>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<StateScriptProvider>());
            services.AddTransient<IStartupService>(provider => provider.GetRequiredService<WidgetScriptProvider>());

            // policies
            services.AddResiliencePipeline(Constants.Pipeline.AuthSignInPipeline,
                builder =>
                {
                    builder.InstanceName = "SignIn";
                    builder
                        .AddRateLimiter(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1), // Specifies the minimum period between replenishments (e.g., 1 minute).
                            SegmentsPerWindow = 10, // Specifies the maximum number of segments a window is divided into (e.g., 10 segments).
                            AutoReplenishment = true, // Specifies whether the limiter automatically replenishes request counters (true by default).
                            PermitLimit = 5, // Maximum number of requests that can be served in a window (e.g., 5 requests).
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst, // Determines the behavior when not enough resources are available.
                            QueueLimit = 10, // Maximum cumulative permit count of queued acquisition requests (e.g., 10 permits).
                        }))
                        .AddTimeout(TimeSpan.FromSeconds(10))
                        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                        {
                            FailureRatio = 0.1, SamplingDuration = TimeSpan.FromSeconds(30), BreakDuration = TimeSpan.FromSeconds(30), MinimumThroughput = 100
                        });
                });
            services.AddResiliencePipeline(Constants.Pipeline.AuthSignOutPipeline,
                builder =>
                {
                    builder.InstanceName = "SignOut";
                    builder
                        .AddTimeout(TimeSpan.FromSeconds(30))
                        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                        {
                            FailureRatio = 0.1, SamplingDuration = TimeSpan.FromSeconds(30), BreakDuration = TimeSpan.FromSeconds(30), MinimumThroughput = 100
                        });
                });

            services.AddAutoMapper(typeof(Startup));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IMapper mapper)
        {
            if (env.IsDevelopment())
            {
                mapper.ConfigurationProvider.AssertConfigurationIsValid();
            }
        }
    }
}