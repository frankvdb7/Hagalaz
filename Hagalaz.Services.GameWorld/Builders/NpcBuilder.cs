using System;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Creatures.Npcs;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class NpcBuilder : INpcBuilder, INpcBuild, INpcOptional, INpcId, INpcLocation
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScope _serviceScope = default!;
        private readonly INpcScriptProvider _npcScriptProvider;
        private ILocation? _minimumBounds;
        private ILocation? _maximumBounds;
        private INpcScript? _script;
        private Type? _scriptType;
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
            if (_script == null)
            {
                var scriptType = _scriptType ?? _npcScriptProvider.GetNpcScriptTypeById(_id);
                _script = (INpcScript)_serviceProvider.GetRequiredService(scriptType);
            }
            var definition = _serviceProvider.GetRequiredService<INpcService>().FindNpcDefinitionById(_id);
            return new Npc(_serviceScope, _location, _minimumBounds, _maximumBounds, _script, _faceDirection, definition);
        }

        public INpcHandle Spawn()
        {
            var npc = Build();
            var npcService = npc.ServiceProvider.GetRequiredService<INpcService>();
            npcService.RegisterAsync(npc).Wait();
            return new NpcHandle(npc);
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

        public INpcOptional WithScript(INpcScript script)
        {
            _script = script;
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