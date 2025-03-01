﻿using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects.Altars
{
    /// <summary>
    /// </summary>
    public class MonasteryAltar : GameObjectScript
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
                if (clicker.Statistics.HealPrayerPoints(1010, clicker.Statistics.PrayerPoints + 20) != 0)
                {
                    clicker.QueueAnimation(Animation.Create(645));
                    clicker.SendChatMessage("You prayed to the gods and they restored your prayer points.");
                }
                else
                {
                    clicker.SendChatMessage("You cannot restore any more prayer points.");
                }

                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's objectIDS which are suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() =>
        [
            2640 //monastery
        ];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}