using System;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Model.Creatures.Npcs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class NpcBuilder : INpcBuilder, INpcBuild, INpcOptional, INpcId, INpcLocation
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScope _serviceScope = default!;
        private readonly INpcScriptProvider _npcScriptProvider;
        private INpcScript? _script;
        private ILocation? _minimumBounds;
        private ILocation? _maximumBounds;
        private Type? _scriptType;
        private Func<INpcScriptActivator, INpc, INpcScript>? _scriptFactory;
        private DirectionFlag? _faceDirection;
        private ILocation _location = default!;
        private int _id = default!;

        public NpcBuilder(IServiceProvider serviceProvider, INpcScriptProvider npcScriptProvider)
        {
            _serviceProvider = serviceProvider;
            _npcScriptProvider = npcScriptProvider;
        }

        private NpcBuilder(IServiceScope serviceScope, INpcScriptProvider npcScriptProvider)
        {
            _serviceScope = serviceScope;
            _serviceProvider = serviceScope.ServiceProvider;
            _npcScriptProvider = npcScriptProvider;
        }

        public INpcId Create() => new NpcBuilder(_serviceProvider.CreateScope(), _npcScriptProvider);

        public INpc Build()
        {
            var scriptActivator = _serviceProvider.GetRequiredService<INpcScriptActivator>();
            var scriptFactory = _scriptFactory ?? ((activator, owner) =>
                _script ?? activator.Create(_scriptType ?? _npcScriptProvider.GetNpcScriptTypeById(_id), owner));
            Func<INpc, INpcScript> npcScriptFactory = owner => scriptFactory(scriptActivator, owner);
            var npcService = _serviceProvider.GetRequiredService<INpcService>();
            var definition = npcService.FindNpcDefinitionById(_id);
            return new Npc(
                _serviceScope,
                _location,
                _minimumBounds,
                _maximumBounds,
                npcScriptFactory,
                _faceDirection,
                definition,
                _serviceProvider.GetRequiredService<IEventManager>(),
                _serviceProvider.GetRequiredService<IScopedGameMediator>(),
                _serviceProvider.GetRequiredService<ISmartPathFinder>(),
                _serviceProvider.GetRequiredService<IMapRegionService>(),
                _serviceProvider.GetRequiredService<IProjectilePathFinder>(),
                _serviceProvider.GetRequiredService<IOptions<CombatOptions>>(),
                _serviceProvider.GetRequiredService<IHitSplatBuilder>(),
                npcService,
                _serviceProvider.GetRequiredService<ILootService>(),
                _serviceProvider.GetRequiredService<ILootGenerator>(),
                _serviceProvider.GetRequiredService<IGroundItemBuilder>());
        }

        public INpcHandle Spawn()
        {
            var npc = Build();
            var npcService = _serviceProvider.GetRequiredService<INpcService>();
            npcService.RegisterAsync(npc).GetAwaiter().GetResult();
            return new NpcHandle(npc, npcService);
        }

        public INpcOptional WithMinimumBounds(ILocation location)
        {
            _minimumBounds = location;
            return this;
        }

        public INpcOptional WithMaximumBounds(ILocation location)
        {
            _maximumBounds = location;
            return this;
        }

        public INpcOptional WithScript<TScript>() where TScript : INpcScript
        {
            _scriptType = typeof(TScript);
            return this;
        }

        public INpcOptional WithScript(Type type)
        {
            _scriptType = type;
            return this;
        }

        public INpcOptional WithScript(INpcScript script)
        {
            _script = script;
            return this;
        }

        public INpcOptional WithScript(Func<INpcScriptActivator, INpc, INpcScript> factory)
        {
            _scriptFactory = factory;
            return this;
        }

        public INpcOptional WithFaceDirection(DirectionFlag direction)
        {
            _faceDirection = direction;
            return this;
        }

        public INpcLocation WithId(int id)
        {
            _id = id;
            return this;
        }

        public INpcOptional WithLocation(ILocation location)
        {
            _location = location;
            return this;
        }
    }
}
