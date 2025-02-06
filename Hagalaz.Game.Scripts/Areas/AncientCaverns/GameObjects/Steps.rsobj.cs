using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.AncientCaverns.GameObjects
{
    /// <summary>
    /// </summary>
    public class Steps : GameObjectScript
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
                clicker.Interrupt(this);
                clicker.Movement.Lock(true);
                if (Owner.Definition.HasAction("Climb-down"))
                {
                    clicker.QueueAnimation(Animation.Create(827));
                    clicker.QueueTask(new RsTask(() =>
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create((short)(clicker.Location.X + 4), clicker.Location.Y, (byte)(clicker.Location.Z - 1), clicker.Location.Dimension)));
                            clicker.Movement.Unlock(false);
                        }, 2));
                }
                else
                {
                    clicker.QueueAnimation(Animation.Create(828));
                    clicker.QueueTask(new RsTask(() =>
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create((short)(clicker.Location.X - 4), clicker.Location.Y, (byte)(clicker.Location.Z + 1), clicker.Location.Dimension)));
                            clicker.Movement.Unlock(false);
                        }, 2));
                }
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }

        /// <summary>
        ///     Get's objectIDS which are suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [25336, 25338];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}