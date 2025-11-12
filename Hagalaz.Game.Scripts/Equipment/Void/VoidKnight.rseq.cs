using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Utilities;

namespace Hagalaz.Game.Scripts.Equipment.Void
{
    /// <summary>
    ///     Contains void knight script.
    /// </summary>
    [EquipmentScriptMetaData([8839, 19785, 8840, 19786, 8842, 11663, 11664, 11665])]
    public class VoidKnight : EquipmentScript
    {
        /// <summary>
        ///     The VOIDGLOVES
        /// </summary>
        public static readonly int Voidgloves = 8842;

        /// <summary>
        ///     The VOIDMAGEHELM
        /// </summary>
        public static readonly int Voidmagehelm = 11663;

        /// <summary>
        ///     The VOIDRANGERHELM
        /// </summary>
        public static readonly int Voidrangerhelm = 11664;

        /// <summary>
        ///     The VOIDMELEEHELM
        /// </summary>
        public static readonly int Voidmeleehelm = 11665;

        /// <summary>
        ///     The VOIDTOP
        /// </summary>
        public static readonly int[] Voidtop = [8839, 19785];

        /// <summary>
        ///     The VOIDROBE
        /// </summary>
        public static readonly int[] Voidrobe = [8840, 19786];

        /// <summary>
        ///     Called when [equiped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnEquiped(IItem item, ICharacter character)
        {
            var legs = character.Equipment[EquipmentSlot.Legs];
            var chest = character.Equipment[EquipmentSlot.Chest];
            var hands = character.Equipment[EquipmentSlot.Hands];
            var hat = character.Equipment[EquipmentSlot.Hat];
            if (legs == null || chest == null || hands == null || hat == null)
            {
                return;
            }

            if (!Voidrobe.Contains(legs.Id) || !Voidtop.Contains(chest.Id) || hands.Id != Voidgloves)
            {
                return;
            }

            if (hat.Id == Voidmagehelm)
            {
                character.AddState(new VoidMagicEquipedState());
            }
            else if (hat.Id == Voidrangerhelm)
            {
                character.AddState(new VoidRangedEquipedState());
            }
            else if (hat.Id == Voidmeleehelm)
            {
                character.AddState(new VoidMeleeEquipedState());
            }
        }

        /// <summary>
        ///     Called when [unequiped].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void OnUnequiped(IItem item, ICharacter character)
        {
            if (item.Id == Voidmagehelm)
            {
                character.RemoveState<VoidMagicEquipedState>();
            }
            else if (item.Id == Voidrangerhelm)
            {
                character.RemoveState<VoidRangedEquipedState>();
            }
            else if (item.Id == Voidmeleehelm)
            {
                character.RemoveState<VoidMeleeEquipedState>();
            }
        }
    }
}