using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects.Ladders
{
    /// <summary>
    ///     Contains standart ladder script.
    /// </summary>
    public class LadderObjectScript : GameObjectScript
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize()
        {
        }

        /// <summary>
        ///     Happens on character click.
        /// </summary>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                clicker.Interrupt(this);
                clicker.Movement.Lock(true);
                if (Owner.Definition.HasAction("Climb-down"))
                {
                    clicker.QueueAnimation(Animation.Create(827));
                    clicker.QueueTask(new RsTask(() =>
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create(clicker.Location.X, clicker.Location.Y, (byte)(clicker.Location.Z - 1), clicker.Location.Dimension)));
                            clicker.Movement.Unlock(false);
                        }, 2));
                }
                else
                {
                    clicker.QueueAnimation(Animation.Create(828));
                    clicker.QueueTask(new RsTask(() =>
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create(clicker.Location.X, clicker.Location.Y, (byte)(clicker.Location.Z + 1), clicker.Location.Dimension)));
                            clicker.Movement.Unlock(false);
                        },2));
                }

                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }


        /// <summary>
        ///     Get's suitable object ids.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() =>
        [
            26982, 26983, //edgeville
            69499, 69502, 69504, 69505 // gnome stronghold
        ];
    }
}