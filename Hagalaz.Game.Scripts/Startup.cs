using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Features;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Scripts;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Commands;
using Hagalaz.Game.Scripts.Minigames.DuelArena;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Scripts.Model.Maps;
using Hagalaz.Game.Scripts.Model.States;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Providers;
using Hagalaz.Game.Scripts.Skills.Cooking;
using Hagalaz.Game.Scripts.Skills.Crafting;
using Hagalaz.Game.Scripts.Skills.Farming;
using Hagalaz.Game.Scripts.Skills.Fishing;
using Hagalaz.Game.Scripts.Skills.Fletching;
using Hagalaz.Game.Scripts.Skills.Herblore.Herbs;
using Hagalaz.Game.Scripts.Skills.Herblore.Potions;
using Hagalaz.Game.Scripts.Skills.Magic.MiscSpells;
using Hagalaz.Game.Scripts.Skills.Smithing;
using Hagalaz.Game.Scripts.Skills.Summoning;
using Hagalaz.Game.Scripts.Skills.Woodcutting;

namespace Hagalaz.Game.Scripts
{
    public class Startup : IPluginStartup
    {
        public void Configure(IServiceCollection services)
        {
            // services
            services.AddSingleton<IDefaultFamiliarScriptProvider, DefaultFamiliarScriptProvider>();
            services.AddScoped<DefaultFamiliarScript>();
            services.AddSingleton<IDefaultNpcScriptProvider, DefaultNpcScriptProvider>();
            services.AddScoped<DefaultNpcScript>();
            services.AddSingleton<IDefaultCharacterNpcScriptProvider, DefaultCharacterNpcScriptProvider>();
            services.AddScoped<DefaultCharacterNpcScript>();
            services.AddSingleton<IDefaultGameObjectScriptProvider, DefaultGameObjectScriptProvider>();
            services.AddScoped<DefaultGameObjectScript>();
            services.AddSingleton<IDefaultAreaScriptProvider, DefaultAreaScriptProvider>();
            services.AddScoped<DefaultAreaScript>();
            services.AddSingleton<IDefaultEquipmentScriptProvider, DefaultEquipmentScriptProvider>();
            services.AddScoped<DefaultEquipmentScript>();
            services.AddSingleton<IDefaultItemScriptProvider, DefaultItemScriptProvider>();
            services.AddScoped<DefaultItemScript>();
            services.AddSingleton<IDefaultStateScriptProvider, DefaultStateScriptProvider>();
            services.AddScoped<DefaultStateScript>();
            services.AddSingleton<IDefaultWidgetScriptProvider, DefaultWidgetScriptProvider>();
            services.AddScoped<DefaultWidgetScript>();

            services.AddSingleton<IHerbloreSkillService, HerbloreSkillService>();
            services.AddSingleton<IPotionSkillService, PotionSkillService>();
            services.AddSingleton<IFishingSkillService, FishingSkillService>();
            services.AddSingleton<ISummoningSkillService, SummoningSkillService>();
            services.AddSingleton<IWoodcuttingSkillService, WoodcuttingSkillService>();
            services.AddSingleton<ICraftingSkillService, CraftingSkillService>();
            services.AddSingleton<IFarmingSkillService, FarmingSkillService>();
            services.AddSingleton<IFletchingSkillService, FletchingSkillService>();

            // commands
            services.AddSingleton<IGameCommandPrompt, GameCommandPrompt>();
            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IGameCommand>())
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            // items
            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IItemScript>().Where(type => !type.IsAssignableFrom(typeof(DefaultItemScript))))
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IItemScriptFactory>())
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            // equipment
            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IEquipmentScript>().Where(type => !type.IsAssignableFrom(typeof(DefaultEquipmentScript))))
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IEquipmentScriptFactory>())
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            // npcs
            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes =>
                    classes.AssignableTo<INpcScript>()
                        .Where(type => !type.IsAssignableFrom(typeof(DefaultNpcScript)) && !type.IsAssignableFrom(typeof(DefaultFamiliarScript))))
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<INpcScriptFactory>())
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IFamiliarScriptFactory>())
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            // game objects
            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IGameObjectScript>().Where(type => !type.IsAssignableFrom(typeof(DefaultGameObjectScript))))
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IGameObjectScriptFactory>())
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            // character npc scripts
            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<ICharacterNpcScript>().Where(type => !type.IsAssignableFrom(typeof(DefaultCharacterNpcScript))))
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            services.Scan(scan => scan.FromAssemblyOf<ICharacterNpcScriptFactory>()
                .AddClasses(classes => classes.AssignableTo<ICharacterNpcScriptFactory>())
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            // character scripts
            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<ICharacterScript>()
                    .Where(type => !type.IsAssignableFrom(typeof(IDefaultCharacterScript)) && !type.IsAssignableFrom(typeof(DuelArenaCombatScript))))
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            // area
            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IAreaScript>().Where(type => !type.IsAssignableFrom(typeof(DefaultAreaScript))))
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IAreaScriptFactory>())
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            // state
            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IStateScript>().Where(type => !type.IsAssignableFrom(typeof(DefaultStateScript))))
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IStateScriptFactory>())
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            // widgets
            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IWidgetScript>())
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            services.Scan(scan => scan.FromAssemblyOf<Startup>()
                .AddClasses(classes => classes.AssignableTo<IWidgetScriptFactory>())
                .AsSelfWithInterfaces()
                .WithScopedLifetime());

            services.AddTransient<IBonesToPeaches, BonesToPeaches>();
            services.AddTransient<IBonesToBananas, BonesToBananas>();
            services.AddTransient<IHighLevelAlchemy, HighLevelAlchemy>();
            services.AddTransient<ILowLevelAlchemy, LowLevelAlchemy>();

            services.AddTransient<JewelryTask>();
            services.AddTransient<LeatherTask>();
            services.AddTransient<ForgeTask>();
            services.AddTransient<CookingTask>();
            services.AddTransient<SmeltTask>();
            services.AddTransient<CutGemTask>();
            services.AddTransient<SilverTask>();
            services.AddTransient<SpinTask>();
            services.AddTransient<CleanHerbTask>();
        }
    }
}