using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.Wilderness.GameObjects
{
    /// <summary>
    ///     Contains wilderness ditch script.
    /// </summary>
    [GameObjectScriptMetaData([65076, 65077, 65078, 65079, 65080, 65081, 65082, 65083, 65084, 65085, 65086, 65087])]
    public class WildernessDitch : GameObjectScript
    {
        private readonly IMovementBuilder _movementBuilder;

        public WildernessDitch(IMovementBuilder movementBuilder)
        {
            _movementBuilder = movementBuilder;
        }

        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Happens on character click.
        /// </summary>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                clicker.Interrupt(this);
                clicker.Movement.Lock(true);

                var direction = DirectionHelper.GetDirection(clicker.Location.X,
                    clicker.Location.Y,
                    Owner.Rotation == 1 || Owner.Rotation == 3 ? Owner.Location.X : clicker.Location.X,
                    Owner.Rotation == 0 || Owner.Rotation == 2 ? Owner.Location.Y : clicker.Location.Y);
                ILocation destination;
                if (direction.HasFlag(DirectionFlag.North)) // north
                {
                    destination = Location.Create(clicker.Location.X, clicker.Location.Y + 3, clicker.Location.Z, clicker.Location.Dimension);
                }
                else if (direction.HasFlag(DirectionFlag.East)) // east
                {
                    destination = Location.Create(clicker.Location.X + 3, clicker.Location.Y, clicker.Location.Z, clicker.Location.Dimension);
                }
                else if (direction.HasFlag(DirectionFlag.South)) // south
                {
                    destination = Location.Create(clicker.Location.X, clicker.Location.Y - 3, clicker.Location.Z, clicker.Location.Dimension);
                }
                else // west
                {
                    destination = Location.Create(clicker.Location.X - 3, clicker.Location.Y, clicker.Location.Z, clicker.Location.Dimension);
                }

                var mov = _movementBuilder
                    .Create()
                    .WithStart(clicker.Location)
                    .WithEnd(destination)
                    .WithEndSpeed(70)
                    .Build();
                clicker.QueueAnimation(Animation.Create(6132));
                clicker.QueueForceMovement(mov);
                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Teleport(Teleport.Create(destination));
                        clicker.Movement.Unlock(false);
                    },
                    3));
            }
        }
    }
}