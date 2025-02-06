using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.AncientCaverns.GameObjects
{
    [GameObjectScriptMetaData([47236])]
    public class EnergyBarrier : GameObjectScript
    {
        private readonly IMovementBuilder _movementBuilder;

        public EnergyBarrier(IMovementBuilder movementBuilder)
        {
            _movementBuilder = movementBuilder;
        }

        /// <summary>
        ///     Happens when character click's this object and then walks to it
        ///     and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacterClick is overrided
        ///     than this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                clicker.Interrupt(this);
                clicker.Movement.Lock(true);

                var direction = clicker.Location.GetDirection(Owner.Rotation == 3 || Owner.Rotation == 2 ? Owner.Location.X : clicker.Location.X, Owner.Rotation == 3 ? Owner.Location.Y : clicker.Location.Y);
                ILocation? destination = null;
                switch (direction)
                {
                    case DirectionFlag.North:
                        destination = Location.Create(clicker.Location.X, (short)(clicker.Location.Y + 1), clicker.Location.Z, clicker.Location.Dimension);
                        break;
                    case DirectionFlag.East:
                        destination = Location.Create((short)(clicker.Location.X + 1), clicker.Location.Y, clicker.Location.Z, clicker.Location.Dimension);
                        break;
                    case DirectionFlag.South:
                        destination = Location.Create(clicker.Location.X, (short)(clicker.Location.Y - 1), clicker.Location.Z, clicker.Location.Dimension);
                        break;
                    case DirectionFlag.West:
                        destination = Location.Create((short)(clicker.Location.X - 1), clicker.Location.Y, clicker.Location.Z, clicker.Location.Dimension);
                        break;
                    default:
                    {
                        if (Owner.Rotation == 3)
                        {
                            destination = Location.Create(clicker.Location.X, (short)(clicker.Location.Y - 1), clicker.Location.Z, clicker.Location.Dimension);
                        }
                        else if (Owner.Rotation == 2)
                        {
                            destination = Location.Create((short)(clicker.Location.X + 1), clicker.Location.Y, clicker.Location.Z, clicker.Location.Dimension);
                        }

                        break;
                    }
                }

                if (destination != null)
                {
                    var movement = _movementBuilder
                        .Create()
                        .WithStart(clicker.Location)
                        .WithEnd(destination)
                        .WithEndSpeed(70)
                        .WithDirection(direction)
                        .Build();
                    clicker.Movement.Teleport(Teleport.Create(destination));
                    clicker.QueueForceMovement(movement);
                }

                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Unlock(false);
                    }, 1));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}