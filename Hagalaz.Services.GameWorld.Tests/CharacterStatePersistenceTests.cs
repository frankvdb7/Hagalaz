using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Features;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Extensions;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.Services.GameWorld.Logic.Hydrators;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterStatePersistenceTests
{
    [TestMethod]
    public void CharacterStateRoundTrip_PreservesDurableStatesAndExcludesRuntimeOnlyStates()
    {
        var stateService = new TestStateService(
            ("default-skulled-state", () => new DefaultSkulledState()),
            ("has-god-wars-hole-rope-state", () => new HasGodWarsHoleRopeState()));
        var source = CreateCharacter(stateService, out _);
        source.AddState(new DefaultSkulledState { TicksLeft = 42 });
        source.AddState(new HasGodWarsHoleRopeState());
        source.AddState(new BowEquippedState());

        var dehydrated = ((IDehydratable<HydratedStateDto>)source).Dehydrate();

        Assert.HasCount(2, dehydrated.StatesEx);
        Assert.IsTrue(dehydrated.StatesEx.Any(state => state.Id == "default-skulled-state" && state.TicksLeft == 42));
        Assert.IsTrue(dehydrated.StatesEx.Any(state => state.Id == "has-god-wars-hole-rope-state" && state.TicksLeft == 0));

        var restored = CreateCharacter(stateService, out _);
        restored.Hydrate(dehydrated);

        Assert.IsTrue(restored.HasState<DefaultSkulledState>());
        Assert.AreEqual(42, restored.GetStates().OfType<DefaultSkulledState>().Single().TicksLeft);
        Assert.IsTrue(restored.HasState<HasGodWarsHoleRopeState>());
        Assert.IsFalse(restored.HasState<BowEquippedState>());
    }

    [TestMethod]
    public void EquipmentHydration_RebuildsRuntimeEquipmentStateAfterStateHydration()
    {
        var stateService = new TestStateService();
        var restored = CreateCharacter(stateService, out var equipmentScript);
        equipmentScript.When(script => script.OnEquipped(Arg.Any<IItem>(), restored))
            .Do(_ => restored.AddState(new BowEquippedState()));

        restored.Hydrate(new HydratedStateDto { StatesEx = [] });
        Assert.IsFalse(restored.HasState<BowEquippedState>());

        ((IHydratable<IReadOnlyList<HydratedItemDto>>)restored.Equipment).Hydrate(
            [new HydratedItemDto(1, 1, (int)EquipmentSlot.Weapon, string.Empty)]);

        Assert.IsTrue(restored.HasState<BowEquippedState>());
        equipmentScript.Received(1).OnEquipped(Arg.Any<IItem>(), restored);
    }

    [TestMethod]
    public void DetachFamiliar_ClearsStateAndAllowsResummon()
    {
        var character = CreateCharacter(new TestStateService(), out _);
        var familiar = Substitute.For<INpc>();
        var familiarScript = Substitute.For<IFamiliarScript>();
        familiarScript.Familiar.Returns(familiar);
        character.AttachFamiliar(familiarScript);

        Assert.IsTrue(character.HasFamiliar());

        character.DetachFamiliar(familiar);

        Assert.IsFalse(character.HasFamiliar());
        character.AttachFamiliar(familiarScript);

        Assert.IsTrue(character.HasFamiliar());
    }

    [TestMethod]
    public void DetachFamiliar_StaleNpcDoesNotClearNewerFamiliar()
    {
        var character = CreateCharacter(new TestStateService(), out _);
        var familiarA = Substitute.For<INpc>();
        var familiarB = Substitute.For<INpc>();
        var scriptA = Substitute.For<IFamiliarScript>();
        var scriptB = Substitute.For<IFamiliarScript>();
        scriptA.Familiar.Returns(familiarA);
        scriptB.Familiar.Returns(familiarB);

        character.AttachFamiliar(scriptA);
        character.AttachFamiliar(scriptB);

        character.DetachFamiliar(familiarA);

        Assert.AreSame(scriptB, character.FamiliarScript);
        Assert.IsTrue(character.HasFamiliar());
    }

    [TestMethod]
    public void FamiliarHydration_StoresPendingData()
    {
        var character = CreateCharacter(new TestStateService(), out _);
        var hydrator = new FamiliarHydrator();

        hydrator.Hydrate(character, new CharacterModel
        {
            Familiar = new HydratedFamiliarDto { FamiliarId = 6815, TicksRemaining = 50 }
        });

        Assert.IsNull(character.FamiliarScript);
        Assert.AreEqual(6815, character.PendingFamiliarId);
    }

    private static Character CreateCharacter(
        TestStateService stateService,
        out IEquipmentScript equipmentScript)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var serviceScope = Substitute.For<IServiceScope>();
        serviceScope.ServiceProvider.Returns(serviceProvider);

        var itemBuilder = Substitute.For<IItemBuilder>();
        var itemId = Substitute.For<IItemId>();
        var itemOptional = Substitute.For<IItemOptional>();
        var item = Substitute.For<IItem>();
        equipmentScript = Substitute.For<IEquipmentScript>();
        item.EquipmentScript.Returns(equipmentScript);
        itemBuilder.Create().Returns(itemId);
        itemId.WithId(Arg.Any<int>()).Returns(itemOptional);
        itemOptional.WithCount(Arg.Any<int>()).Returns(itemOptional);
        itemOptional.WithExtraData(Arg.Any<string>()).Returns(itemOptional);
        itemOptional.Build().Returns(item);

        var bodyDataRepository = Substitute.For<IBodyDataRepository>();
        bodyDataRepository.BodySlotCount.Returns(14);

        var scripts = Substitute.For<IDefaultCharacterScriptProvider>();
        scripts.GetAllScripts().Returns(Array.Empty<IDefaultCharacterScript>());

        Register(serviceProvider, Substitute.For<ICreatureTaskService>());
        Register(serviceProvider, Substitute.For<IScopedGameMediator>());
        Register(serviceProvider, Substitute.For<ICharacterContextProvider>());
        Register(serviceProvider, Substitute.For<IEventManager>());
        Register(serviceProvider, Substitute.For<ISmartPathFinder>());
        Register(serviceProvider, Substitute.For<IProjectilePathFinder>());
        Register(serviceProvider, Substitute.For<IMapRegionService>());
        Register(serviceProvider, Substitute.For<ICharacterLocationService>());
        Register(serviceProvider, Substitute.For<INpcService>());
        Register(serviceProvider, bodyDataRepository);
        Register(serviceProvider, Substitute.For<ICharacterNpcScriptProvider>());
        Register(serviceProvider, Substitute.For<IAnimationBuilder>());
        Register(serviceProvider, Substitute.For<IGraphicBuilder>());
        Register(serviceProvider, Substitute.For<IProjectileBuilder>());
        Register(serviceProvider, Substitute.For<IClientMapDefinitionProvider>());
        Register(serviceProvider, Options.Create(new CombatOptions()));
        Register(serviceProvider, Options.Create(new SkillOptions()));
        Register(serviceProvider, scripts);
        Register(serviceProvider, itemBuilder);
        Register<IStateService>(serviceProvider, stateService);

        var character = new Character(
            serviceScope,
            Substitute.For<IGameSession>(),
            Substitute.For<IGameClient>(),
            Substitute.For<ICharacterContextProvider>(),
            Substitute.For<IEventManager>(),
            Substitute.For<IScopedGameMediator>(),
            Substitute.For<ISmartPathFinder>(),
            Substitute.For<IProjectilePathFinder>(),
            Options.Create(new CombatOptions()),
            Options.Create(new SkillOptions()),
            scripts,
            Substitute.For<ICharacterScriptActivator>(),
            stateService,
            Substitute.For<IMapRegionService>(),
            Substitute.For<IMapUpdateService>(),
            Substitute.For<IMusicService>(),
            Substitute.For<IGameCommandPrompt>(),
            Substitute.For<Microsoft.Extensions.Logging.ILogger<ICharacter>>(),
            Substitute.For<IAudioBuilder>(),
            Substitute.For<IGameMessageService>(),
            itemBuilder,
            Substitute.For<IGroundItemBuilder>(),
            Substitute.For<IAnimationBuilder>(),
            Substitute.For<IGraphicBuilder>(),
            Substitute.For<IProjectileBuilder>(),
            Substitute.For<IHitSplatBuilder>(),
            Substitute.For<INpcService>(),
            bodyDataRepository,
            Substitute.For<ICharacterNpcScriptProvider>(),
            Substitute.For<ICharacterNpcScriptActivator>(),
            Substitute.For<IItemPartFactory>(),
            Substitute.For<ICharacterLocationService>(),
            Substitute.For<IItemService>(),
            Substitute.For<IClientMapDefinitionProvider>(),
            Substitute.For<ISlayerService>(),
            Substitute.For<IRatesService>(),
            Substitute.For<ISlayerTaskGenerator>(),
            Substitute.For<ISlayerTaskCompletedDialogue>(),
            Substitute.For<IFarmingService>(),
            Substitute.For<IGameObjectService>(),
            Substitute.For<IWidgetScriptProvider>());
        ((IHydratable<HydratedDetailsDto>)character).Hydrate(new HydratedDetailsDto
        {
            CoordX = 3200,
            CoordY = 3200
        });
        return character;
    }

    private static void Register<T>(IServiceProvider serviceProvider, T service) where T : class =>
        serviceProvider.GetService(typeof(T)).Returns(service);

    private sealed class TestStateService : IStateService
    {
        private readonly Dictionary<string, Func<IState>> _factories;

        public TestStateService(params (string Id, Func<IState> Factory)[] states)
        {
            _factories = new Dictionary<string, Func<IState>>(StringComparer.Ordinal);
            foreach (var (id, factory) in states)
            {
                _factories.Add(id, factory);
            }
        }

        public bool TryCreateState(string stateId, [NotNullWhen(true)] out IState? state)
        {
            if (_factories.TryGetValue(stateId, out var factory))
            {
                state = factory();
                return true;
            }

            state = null;
            return false;
        }

        public bool TryGetStateId(IState state, [NotNullWhen(true)] out string? stateId)
        {
            foreach (var (id, factory) in _factories)
            {
                if (factory().GetType() == state.GetType())
                {
                    stateId = id;
                    return true;
                }
            }

            stateId = null;
            return false;
        }
    }
}
