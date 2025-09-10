using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.Taverly.GameObjects
{
    /// <summary>
    ///     Contains ladder script.
    /// </summary>
    [GameObjectScriptMetaData([74864])]
    public class TrainingLadder : GameObjectScript
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
                if (clicker.Location.X >= 3065 && clicker.Location.X <= 3070 && clicker.Location.Y >= 10254 && clicker.Location.Y <= 10260)
                {
                    // KBD
                    clicker.QueueTask(new RsTask(() =>
                        {
                            clicker.Movement.Teleport(Teleport.Create(Location.Create(3017, 3848, 0, 0)));
                            clicker.Movement.Unlock(false);
                        }, 2));
                }
                else
                {
                    clicker.QueueTask(new RsTask(() =>
                        {
                            var beginLadder = Owner.Location.X == 2884 || Owner.Location.Y == 9797;
                            clicker.Movement.Teleport(Teleport.Create(Location.Create(beginLadder ? 3096 : 3115, beginLadder ? 3468 : 3452, 0, 0)));
                            clicker.Movement.Unlock(false);
                        }, 2));
                }

                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}