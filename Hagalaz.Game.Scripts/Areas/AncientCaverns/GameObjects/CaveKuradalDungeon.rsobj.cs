using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.AncientCaverns.GameObjects
{
    /// <summary>
    /// </summary>
    public class CaveKuradalDungeon : GameObjectScript
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
                clicker.QueueAnimation(Animation.Create(827));
                clicker.QueueTask(new RsTask(() =>
                    {
                        if (Owner.Id == 47231)
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create(1735, 5313, 1, clicker.Location.Dimension)));
                        }
                        else if (Owner.Id == 47232)
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create(1661, 5257, 0, clicker.Location.Dimension)));
                        }

                        clicker.Movement.Unlock(false);
                    }, 2));
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
        public override int[] GetSuitableObjects() => [47231, 47232];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}