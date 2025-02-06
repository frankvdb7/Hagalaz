using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Saradomin
{
    /// <summary>
    /// </summary>
    public class SaradominAltar : GameObjectScript
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
            if (clickType == GameObjectClickType.Option1Click) // pray
            {
                if (!clicker.HasState(StateType.SaradominAltarPrayed))
                {
                    if (clicker.Combat.IsInCombat())
                    {
                        return;
                    }

                    if (clicker.Statistics.HealPrayerPoints(990) != 0)
                    {
                        clicker.QueueAnimation(Animation.Create(645));
                        clicker.SendChatMessage("You prayed to the gods and they restored your prayer points.");
                        clicker.AddState(new State(StateType.SaradominAltarPrayed, 1000));
                    }
                    else
                    {
                        clicker.SendChatMessage("You cannot restore any more prayer points.");
                    }
                }
                else
                {
                    clicker.SendChatMessage("You cannot pray at this altar yet.");
                }

                return;
            }

            if (clickType == GameObjectClickType.Option2Click) // teleport
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2829, 5287, 2, 0).PerformTeleport(clicker);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's objectIDS which are suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [26287];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}