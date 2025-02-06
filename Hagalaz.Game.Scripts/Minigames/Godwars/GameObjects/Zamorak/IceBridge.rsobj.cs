using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Zamorak
{
    /// <summary>
    /// </summary>
    public class IceBridge : GameObjectScript
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
                if (clicker.Statistics.GetSkillLevel(StatisticsConstants.Constitution) < 70)
                {
                    clicker.SendChatMessage("You need a constitution level of 70 to cross this bridge.");
                    return;
                }

                clicker.Movement.Lock(true);
                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.SendChatMessage("Dripping, you climb out of the water.");
                        if (clicker.Location.Y == 5332)
                        {
                            clicker.Statistics.DrainPrayerPoints(clicker.Statistics.PrayerPoints);
                            clicker.SendChatMessage("The extreme evil of this area leaves your Prayer drained.");
                        }

                        clicker.Movement.Teleport(Teleport.Create(Location.Create(clicker.Location.X, (short)(clicker.Location.Y == 5332 ? 5344 : 5333), clicker.Location.Z, 0)));
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
        public override int[] GetSuitableObjects() => [26439];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}