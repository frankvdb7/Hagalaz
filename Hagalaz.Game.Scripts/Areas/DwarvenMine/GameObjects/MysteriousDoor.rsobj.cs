using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.DwarvenMine.GameObjects
{
    /// <summary>
    /// </summary>
    public class MysteriousDoor : GameObjectScript
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
                ILocation? loc = null;
                if (Owner.Id == GetSuitableObjects()[0])
                {
                    loc = Location.Create(1041, 4575, 0, 0);
                }
                else
                {
                    loc = Location.Create(3034, 9772, 0, 0);
                }

                clicker.Interrupt(this);
                clicker.Movement.Lock(true);
                clicker.QueueAnimation(Animation.Create(827));
                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Teleport(Teleport.Create(loc));
                        clicker.Movement.Unlock(false);
                    }, 2));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's objectIDS which are suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [52855, 52864];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}