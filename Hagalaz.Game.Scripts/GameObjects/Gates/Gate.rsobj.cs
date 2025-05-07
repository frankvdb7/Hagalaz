using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects.Gates
{
    /// <summary>
    ///     Contains gate script.
    /// </summary>
    [GameObjectScriptMetaData([2623])]
    public class Gate : GameObjectScript
    {
        private readonly IGameObjectService _gameObjectService;

        public Gate(IGameObjectService gameObjectService) => _gameObjectService = gameObjectService;

        /// <summary>
        ///     Happens on character click.
        /// </summary>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                var direction = clicker.Location.GetDirection(Owner.Location);
                if (direction != DirectionFlag.None && direction != DirectionFlag.West && direction != DirectionFlag.East)
                {
                    return;
                }

                var oldRotation = Owner.Rotation;
                const int newRotation = 3;
                _gameObjectService.UpdateGameObject(new GameObjectUpdate
                {
                    Instance = Owner, Rotation = newRotation
                });
                var destination =
                    Location.Create(direction == DirectionFlag.None
                            ? clicker.Location.X - 1
                            : clicker.Location.X + DirectionHelper.DirectionDeltaX[(int)direction],
                        clicker.Location.Y,
                        clicker.Location.Z,
                        clicker.Location.Dimension);
                clicker.Movement.AddToQueue(destination);
                clicker.QueueTask(new RsTask(() =>
                    {
                        _gameObjectService.UpdateGameObject(new GameObjectUpdate
                        {
                            Instance = Owner, Rotation = oldRotation
                        });
                    },
                    3));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}