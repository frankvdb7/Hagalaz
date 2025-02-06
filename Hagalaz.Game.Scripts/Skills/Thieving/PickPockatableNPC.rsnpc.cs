using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Skills.Thieving
{
    /// <summary>
    /// </summary>
    public class PickPockatableNpc : NpcScriptBase
    {
        /// <summary>
        ///     The pickpocket definition.
        /// </summary>
        private PickPocketDefinition _definition;

        /// <summary>
        ///     Happens when character clicks NPC and then walks to it and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacter is overrided or/and
        ///     handles to click this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option3Click)
            {
                clicker.Interrupt(this);
                clicker.QueueTask(() => Thieving.PickPocket(clicker, Owner, _definition));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs()
        {
            var npcs = new List<int>();
            foreach (var def in Thieving.Ppd)
            foreach (var npcId in def.NpcIDs)
            {
                npcs.Add(npcId);
            }

            return npcs.ToArray();
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
            foreach (var def in Thieving.Ppd)
            {
                foreach (var npcId in def.NpcIDs)
                {
                    if (npcId == Owner.Definition.Id)
                    {
                        _definition = def;
                        return;
                    }
                }
            }
        }
    }
}