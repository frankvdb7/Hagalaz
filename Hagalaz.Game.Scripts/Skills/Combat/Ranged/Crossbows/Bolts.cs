using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows
{
    /// <summary>
    ///     Class for misc methods related to bolts and bolts equipment script.
    /// </summary>
    public class Bolts : EquipmentScript
    {
        /// <summary>
        ///     Bronze bolt ids.
        /// </summary>
        public static readonly int[] Bronze = [877, 878, 6061, 6062, 879, 9236];

        /// <summary>
        ///     Blurite bolt ids.
        /// </summary>
        public static readonly int[] Blurite = [9139, 9286, 9293, 9300, 9335, 9237];

        /// <summary>
        ///     Silver bolt ids.
        /// </summary>
        public static readonly int[] Silver = [9145, 9292, 9299, 9306];

        /// <summary>
        ///     Iron bolt ids.
        /// </summary>
        public static readonly int[] Iron = [9140, 9287, 9294, 9301, 880, 9238];

        /// <summary>
        ///     Steel bolt ids.
        /// </summary>
        public static readonly int[] Steel = [9141, 9288, 9295, 9302, 9336, 9239];

        /// <summary>
        ///     Black bolt ids.
        /// </summary>
        public static readonly int[] Black = [13083, 13084, 13085, 13086];

        /// <summary>
        ///     Mithril bolt ids.
        /// </summary>
        public static readonly int[] Mithril = [9142, 9289, 9296, 9303, 9337, 9240, 9338, 9241];

        /// <summary>
        ///     Adamantine bolt ids.
        /// </summary>
        public static readonly int[] Adamantine = [9143, 9290, 9297, 9304, 9339, 9242, 9340, 9243];

        /// <summary>
        ///     Runite bolt ids.
        /// </summary>
        public static readonly int[] Runite = [9144, 9291, 9298, 9305, 9341, 9244, 9342, 9245];

        /// <summary>
        ///     Bone bolts id.
        /// </summary>
        public static readonly int Bone = 8882;

        /// <summary>
        ///     Barbed bolts id.
        /// </summary>
        public static readonly int Barbed = 881;

        /// <summary>
        ///     Mithril grapple id.
        /// </summary>
        public static readonly int MithrilGrapple = 9419;

        /// <summary>
        ///     Bolt rack id.
        /// </summary>
        public static readonly int BoltRack = 4740;

        /// <summary>
        ///     Broad tipped bolts id.
        /// </summary>
        public static readonly int BroadTipped = 13480;

        /// <summary>
        ///     Standart kebbit bolts id.
        /// </summary>
        public static readonly int Kebbit = 10158;

        /// <summary>
        ///     Long kebbit bolts id.
        /// </summary>
        public static readonly int LongKebbit = 10159;

        /// <summary>
        ///     Abyssalbane bolts ids.
        /// </summary>
        public static readonly int[] Abyssalbane = [21675, 21701, 21702, 21703];

        /// <summary>
        ///     Basiliskbane bolts ids.
        /// </summary>
        public static readonly int[] Basiliskbane = [21670, 21687, 21688, 21689];

        /// <summary>
        ///     Dragonbane bolts ids.
        /// </summary>
        public static readonly int[] Dragonbane = [21660, 21680, 21681, 21682];

        /// <summary>
        ///     Wallasalkibane bolts ids.
        /// </summary>
        public static readonly int[] Wallasalkibane = [21665, 21694, 21695, 21696];

        /// <summary>
        ///     Happens when bolts are equiped for this character.
        /// </summary>
        public override void OnEquiped(IItem item, ICharacter character)
        {
            switch (item.Id)
            {
                // OPAL
                case 9236: character.AddState(new State(StateType.EnchantedOpalBoltsEquipped, int.MaxValue)); break;
                // DIAMOND
                case 9243: character.AddState(new State(StateType.EnchantedDiamondBoltsEquipped, int.MaxValue)); break;
                // DRAGON
                case 9244: character.AddState(new State(StateType.EnchantedDragonstoneBoltsEquiped, int.MaxValue)); break;
                // ONYX
                case 9245: character.AddState(new State(StateType.EnchantedOnyxBoltsEquiped, int.MaxValue)); break;
            }

            character.AddState(new State(StateType.BoltsEquiped, int.MaxValue));
        }

        /// <summary>
        ///     Happens when bolts are unequiped for this character.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="character"></param>
        public override void OnUnequiped(IItem item, ICharacter character)
        {
            switch (item.Id)
            {
                // OPAL
                case 9236: character.RemoveState(StateType.EnchantedOpalBoltsEquipped); break;
                // DIAMOND
                case 9243: character.RemoveState(StateType.EnchantedDiamondBoltsEquipped); break;
                // DRAGON
                case 9244: character.RemoveState(StateType.EnchantedDragonstoneBoltsEquiped); break;
                // ONYX
                case 9245: character.RemoveState(StateType.EnchantedOnyxBoltsEquiped); break;
            }

            character.RemoveState(StateType.BoltsEquiped);
        }
    }
}