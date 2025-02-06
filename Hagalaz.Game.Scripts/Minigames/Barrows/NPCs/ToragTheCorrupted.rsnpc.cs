using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    [NpcScriptMetaData([2029])]
    public class ToragTheCorrupted : BarrowBrother
    {
        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            base.PerformAttack(target);

            if (target is ICharacter character)
            {
                if (RandomStatic.Generator.NextDouble() >= 0.75)
                {
                    character.Statistics.DrainRunEnergy(10);
                    character.QueueGraphic(Graphic.Create(399));
                    character.SendChatMessage("You feel drained of energy.");
                }
            }
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Crush;

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.MeleeControlled;
    }
}