using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Saradomin
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([6248])]
    public class Starlight : BodyGuard
    {
        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.MeleeAccurate;

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Stab;

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}