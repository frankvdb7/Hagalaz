using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects.Ladders
{
    /// <summary>
    /// </summary>
    public class LadderFromSummoningObelisk : GameObjectScript
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
                clicker.QueueAnimation(Animation.Create(828));
                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Teleport(Teleport.Create(Location.Create(3078, 3493, 0, 0)));
                        clicker.Movement.Unlock(false);
                    }, 2));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }


        /// <summary>
        ///     Get's suitable object ids.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [28714];
    }
}