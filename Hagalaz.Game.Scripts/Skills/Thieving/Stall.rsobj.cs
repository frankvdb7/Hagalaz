using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Thieving
{
    /// <summary>
    /// </summary>
    public class Stall : GameObjectScript
    {
        /// <summary>
        ///     Contains the steal definition.
        /// </summary>
        public StealDefinition Definition { get; private set; }

        /// <summary>
        ///     Contains the stall owner.
        /// </summary>
        public INpc? StallOwner => Owner.Region.FindAllNpcs().FirstOrDefault(npc => npc.Appearance.CompositeID == Definition.NpcOwnerID);

        /// <summary>
        ///     Contains the local guards.
        /// </summary>
        public List<INpc> Guards { get; }

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
            if (clickType == GameObjectClickType.Option1Click || clickType == GameObjectClickType.Option2Click)
            {
                clicker.Interrupt(this);
                clicker.QueueTask(() => Thieving.Steal(clicker, Owner, this));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's objectIDS which are suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects()
        {
            var objects = new List<int>();
            objects.AddRange(Thieving.Sd.SelectMany(sd => sd.GameObjectIDs));
            return objects.ToArray();
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
            foreach (var def in Thieving.Sd)
            {
                foreach (var objectID in def.GameObjectIDs)
                {
                    if (objectID == Owner.Id)
                    {
                        Definition = def;
                        return;
                    }
                }
            }
        }
    }
}