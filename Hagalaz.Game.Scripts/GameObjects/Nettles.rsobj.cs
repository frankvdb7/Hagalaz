using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects
{
    [GameObjectScriptMetaData([1181])]
    public class Nettles : GameObjectScript
    {
        private readonly IGameObjectService _gameObjectService;

        public Nettles(IGameObjectService gameObjectService) => _gameObjectService = gameObjectService;

        /// <summary>
        ///     Called when [spawn].
        /// </summary>
        public override void OnSpawn() =>
            _gameObjectService.UpdateGameObject(new GameObjectUpdate
            {
                Instance = Owner,
                Id = 2646
            });

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}