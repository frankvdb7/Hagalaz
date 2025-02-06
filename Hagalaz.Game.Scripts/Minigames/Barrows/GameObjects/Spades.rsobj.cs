using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.GameObjects
{
    /// <summary>
    /// </summary>
    public class Spades : GameObjectScript
    {
        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                Dig(clicker);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Digs this instance.
        /// </summary>
        private bool Dig(ICharacter character)
        {
            character.QueueAnimation(Animation.Create(830));
            var loc = Owner.Location;
            ILocation teleport = null;
            if (loc.Equals(BarrowsConstants.VeracSpades))
            {
                //teleport = Location.Create(3578, 9706, 3, 0); // original verac
                teleport = Location.Create(4077, 5710, 0, 0); // verac / akrisae
            }
            else if (loc.Equals(BarrowsConstants.DharokSpades))
            {
                teleport = Location.Create(3556, 9718, 3, 0);
            }
            else if (loc.Equals(BarrowsConstants.GuthanSpades))
            {
                teleport = Location.Create(3534, 9704, 3, 0);
            }
            else if (loc.Equals(BarrowsConstants.KarilSpades))
            {
                teleport = Location.Create(3546, 9684, 3, 0);
            }
            else if (loc.Equals(BarrowsConstants.ToragSpades))
            {
                teleport = Location.Create(3568, 9683, 3, 0);
            }
            else if (loc.Equals(BarrowsConstants.AhrimSpades))
            {
                teleport = Location.Create(3557, 9703, 3, 0);
            }

            if (teleport == null)
            {
                return false;
            }

            character.Movement.Lock(true);
            character.QueueTask(new RsTask(() =>
                {
                    character.Movement.Teleport(Teleport.Create(teleport));
                    character.Movement.Unlock(false);
                }, 2));
            return true;

        }

        /// <summary>
        ///     Gets the suitable objects.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [66115, 66116];

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}