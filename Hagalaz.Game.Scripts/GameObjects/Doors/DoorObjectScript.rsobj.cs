using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects.Doors
{
    /// <summary>
    ///     Contains a standard door script.
    /// </summary>
    [GameObjectScriptMetaData([
        1533, //burthorphe
        30864, //monastery
        1804, 15536, 29732, 37123, // edgeville
        10527, 10529 //slayer tower
    ])]
    public class DoorObjectScript : GameObjectScript
    {
        /// <summary>
        ///     The default rotation
        /// </summary>
        private int _defaultRotation;

        private readonly IGameObjectService _gameObjectService;

        public DoorObjectScript(IGameObjectService gameObjectService) => _gameObjectService = gameObjectService;

        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                if (_defaultRotation == 1 || _defaultRotation == 3)
                {
                    var newRotation = _defaultRotation - 1;
                    _gameObjectService.UpdateGameObject(new GameObjectUpdate
                    {
                        Instance = Owner,
                        Rotation = Owner.Rotation == newRotation ? _defaultRotation : newRotation
                    });
                }
                else if (_defaultRotation == 0 || _defaultRotation == 2)
                {
                    var newRotation = _defaultRotation + 1;
                    _gameObjectService.UpdateGameObject(new GameObjectUpdate
                    {
                        Instance = Owner,
                        Rotation = Owner.Rotation == newRotation ? _defaultRotation : newRotation
                    });
                }

                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize() => _defaultRotation = Owner.Rotation;
    }
}