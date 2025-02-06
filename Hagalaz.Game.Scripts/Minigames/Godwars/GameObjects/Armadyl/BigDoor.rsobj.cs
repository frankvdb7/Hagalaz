using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Armadyl
{
    /// <summary>
    /// </summary>
    public class BigDoor : GameObjectScript
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
                if (clicker.Location.Y == 5295)
                {
                    var script = clicker.GetScript<GodwarsScript>();
                    if (script == null)
                    {
                        return;
                    }

                    if (script.ArmadylKills < 20)
                    {
                        clicker.SendChatMessage("You need 20 Armadyl kills to pass through this door.");
                        return;
                    }

                    script.ResetArmadylKills();
                }

                clicker.Movement.Lock(true);
                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Teleport(Teleport.Create(Location.Create(clicker.Location.X, (short)(clicker.Location.Y == 5296 ? 5295 : 5296), clicker.Location.Z, 0)));
                        clicker.Movement.Unlock(false);
                    }, 1));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's objectIDS which are suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [26426];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}