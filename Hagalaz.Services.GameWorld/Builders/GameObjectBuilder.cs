using System;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class GameObjectBuilder : IGameObjectBuilder, IGameObjectId, IGameObjectLocation, IGameObjectOptional, IGameObjectBuild
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGameObjectScriptProvider _gameObjectScriptProvider;
        private int _id;
        private ILocation _location = default!;
        private int _rotation = 0;
        private ShapeType _shapeType = ShapeType.GroundDefault;
        private IGameObjectScript? _script;
        private Type? _scriptType;
        private bool _isStatic = false;

        public GameObjectBuilder(IServiceProvider serviceProvider, IGameObjectScriptProvider gameObjectScriptProvider)
        {
            _serviceProvider = serviceProvider;
            _gameObjectScriptProvider = gameObjectScriptProvider;
        }

        public IGameObjectId Create() => new GameObjectBuilder(_serviceProvider.CreateScope().ServiceProvider, _gameObjectScriptProvider);

        public IGameObjectLocation WithId(int id)
        {
            _id = id;
            return this;
        }

        public IGameObjectOptional WithLocation(ILocation location)
        {
            _location = location;
            return this;
        }

        public IGameObjectOptional WithRotation(int rotation)
        {
            _rotation = rotation;
            return this;
        }

        public IGameObjectOptional WithShape(ShapeType shapeType)
        {
            _shapeType = shapeType;
            return this;
        }

        public IGameObjectOptional WithScript(IGameObjectScript script)
        {
            _script = script;
            return this;
        }

        public IGameObjectOptional WithScript<TScript>() where TScript : IGameObjectScript
        {
            _scriptType = typeof(TScript);
            return this;
        }

        public IGameObjectOptional AsStatic()
        {
            _isStatic = true;
            return this;
        }

        public IGameObject Build()
        {
            if (_script == null)
            {
                var scriptType = _scriptType ?? _gameObjectScriptProvider.GetGameObjectScriptTypeById(_id);
                _script = (IGameObjectScript)_serviceProvider.GetRequiredService(scriptType);
            }
            var definition = _serviceProvider.GetRequiredService<IGameObjectService>().FindGameObjectDefinitionById(_id);
            return new GameObject(_id, _location, _rotation, _shapeType, _isStatic, definition, _script);
        }
    }
}