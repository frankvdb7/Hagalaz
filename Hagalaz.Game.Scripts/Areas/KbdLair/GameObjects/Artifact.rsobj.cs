﻿using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Areas.KbdLair.GameObjects
{
    /// <summary>
    /// </summary>
    /// <seealso cref="GameObjectScript" />
    public class Artifact : GameObjectScript
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
                //clicker.Configurations.SendBITConfiguration(this.owner.Definition.VarpBitFileID, 1); // TODO animation
                new LeverTeleport(Owner, 3051, 3519, 0, 0).PerformTeleport(clicker);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's objectIDS which are suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [1817];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}