using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.GameObjects
{
    /// <summary>
    /// </summary>
    [GameObjectScriptMetaData([
        6718, 6719, 6720, 6721, 6722, 6724, 6725, 6726, 6727, 6728, 6729, 6730, 6737, 6738, 6739, 6740, 6741, 6743, 6744, 6745, 6746, 6747, 6748, 6749
    ])]
    public class BarrowCryptDoor : GameObjectScript
    {
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
                var direction = clicker.Location.GetDirection(Owner.Location);
                ILocation? destination = null;
                switch (Owner.Rotation)
                {
                    // south doors
                    // north
                    case 3 when direction.HasFlag(DirectionFlag.North):
                        destination = Location.Create(Owner.Location.X,
                            Owner.Location.Y == clicker.Location.Y ? (short)(Owner.Location.Y + 1) : (short)(clicker.Location.Y + 1),
                            clicker.Location.Z,
                            clicker.Location.Dimension);
                        break;
                    case 3 when direction.HasFlag(DirectionFlag.East):
                        destination = Location.Create(Owner.Location.X,
                            Owner.Location.Y == clicker.Location.Y ? (short)(Owner.Location.Y - 1) : (short)(clicker.Location.Y + 1),
                            clicker.Location.Z,
                            clicker.Location.Dimension);
                        break;
                    case 3:
                        destination = Location.Create(Owner.Location.X,
                            Owner.Location.Y == clicker.Location.Y ? (short)(clicker.Location.Y - 1) : (short)(Owner.Location.Y - 1),
                            clicker.Location.Z,
                            clicker.Location.Dimension);
                        break;
                    // north doors
                    // south
                    case 1 when direction.HasFlag(DirectionFlag.South):
                        destination = Location.Create(Owner.Location.X,
                            Owner.Location.Y == clicker.Location.Y ? (short)(Owner.Location.Y - 1) : (short)(clicker.Location.Y - 1),
                            clicker.Location.Z,
                            clicker.Location.Dimension);
                        break;
                    // east
                    case 1 when direction.HasFlag(DirectionFlag.East):
                        destination = Location.Create(Owner.Location.X,
                            Owner.Location.Y == clicker.Location.Y ? (short)(Owner.Location.Y + 1) : (short)(clicker.Location.Y - 1),
                            clicker.Location.Z,
                            clicker.Location.Dimension);
                        break;
                    case 1:
                        destination = Location.Create(Owner.Location.X,
                            Owner.Location.Y == clicker.Location.Y ? (short)(clicker.Location.Y + 1) : (short)(Owner.Location.Y + 1),
                            clicker.Location.Z,
                            clicker.Location.Dimension);
                        break;
                    // west doors
                    // east
                    case 0 when direction.HasFlag(DirectionFlag.East):
                        destination = Location.Create(Owner.Location.X == clicker.Location.X ? (short)(Owner.Location.X + 1) : (short)(clicker.Location.X + 1),
                            Owner.Location.Y,
                            clicker.Location.Z,
                            clicker.Location.Dimension);
                        break;
                    case 0:
                        destination = Location.Create(Owner.Location.X == clicker.Location.X ? (short)(clicker.Location.X - 1) : (short)(Owner.Location.X - 1),
                            Owner.Location.Y,
                            clicker.Location.Z,
                            clicker.Location.Dimension);
                        break;
                    // east doors
                    // west
                    case 2 when direction.HasFlag(DirectionFlag.West):
                        destination = Location.Create(Owner.Location.X == clicker.Location.X ? (short)(Owner.Location.X - 1) : (short)(clicker.Location.X - 1),
                            Owner.Location.Y,
                            clicker.Location.Z,
                            clicker.Location.Dimension);
                        break;
                    case 2:
                        destination = Location.Create(Owner.Location.X == clicker.Location.X ? (short)(clicker.Location.X + 1) : (short)(Owner.Location.X + 1),
                            Owner.Location.Y,
                            clicker.Location.Z,
                            clicker.Location.Dimension);
                        break;
                }

                if (destination == null)
                {
                    return;
                }

                clicker.Movement.Lock(true);

                clicker.Movement.Teleport(Teleport.Create(destination));
                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Unlock(false);
                        if (Owner.Location.Equals(clicker.Location))
                        {
                            clicker.AddState(new BarrowsBetweenDoorsState { TicksLeft = int.MaxValue });
                        }
                        else
                        {
                            clicker.RemoveState<BarrowsBetweenDoorsState>();
                        }

                        clicker.Configurations.SendStandardConfiguration(1270,
                            clicker.HasState<BarrowsBetweenDoorsState>() ? 1 : 0); // hide or show the black 'roof'
                        clicker.GetOrAddScript<BarrowsScript>().CryptGameObjectClickPerformed(); // spawn barrow brother if available
                    },
                    1));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}