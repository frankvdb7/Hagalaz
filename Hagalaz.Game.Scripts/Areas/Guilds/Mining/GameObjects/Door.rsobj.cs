using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.Guilds.Mining.GameObjects
{
    /// <summary>
    ///     Contains gate script.
    /// </summary>
    [GameObjectScriptMetaData([2112])]
    public class Door : GameObjectScript
    {
        private readonly IMovementBuilder _movementBuilder;
        private readonly IGameObjectService _gameObjectService;

        public Door(IMovementBuilder movementBuilder, IGameObjectService gameObjectService)
        {
            _movementBuilder = movementBuilder;
            _gameObjectService = gameObjectService;
        }

        /// <summary>
        ///     Happens on character click.
        /// </summary>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                var direction = clicker.Location.GetDirection(Owner.Location);
                if (direction != DirectionFlag.None && direction != DirectionFlag.North && direction != DirectionFlag.South)
                {
                    return;
                }

                clicker.Movement.Lock(true);
                var oldRotation = Owner.Rotation;
                const int newRotation = 2;
                _gameObjectService.UpdateGameObject(new GameObjectUpdate
                {
                    Instance = Owner,
                    Rotation = newRotation
                });
                var destination = Location.Create(clicker.Location.X,
                    direction == DirectionFlag.None ? clicker.Location.Y + 1 : clicker.Location.Y + DirectionHelper.DirectionDeltaY[(int)direction],
                    clicker.Location.Z,
                    clicker.Location.Dimension);
                var mov = _movementBuilder.Create()
                    .WithStart(clicker.Location)
                    .WithEnd(destination)
                    .WithEndSpeed(45)
                    .WithFaceDirection(direction == DirectionFlag.None ? FaceDirection.North : FaceDirection.South)
                    .Build();
                clicker.QueueAnimation(Animation.Create(819));
                clicker.Movement.Teleport(Teleport.Create(destination));
                clicker.QueueForceMovement(mov);
                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Unlock(false);
                        _gameObjectService.UpdateGameObject(new GameObjectUpdate
                        {
                            Instance = Owner,
                            Rotation = oldRotation
                        });
                    },
                    3));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}