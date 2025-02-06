using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Knights
{
    /// <summary>
    ///     Contains black knight script.
    /// </summary>
    public class StandartBlackKnight : NpcScriptBase
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Render's attack.
        /// </summary>
        public override void RenderAttack() => Owner.QueueAnimation(Animation.Create(385));

        /// <summary>
        ///     Render's defence.
        /// </summary>
        public override void RenderDefence(int delay) => Owner.QueueAnimation(Animation.Create(387, delay));

        /// <summary>
        ///     Get's attack distance of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackDistance() => 1;

        /// <summary>
        ///     Get's attack speed of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackSpeed() => 5;

        /// <summary>
        ///     Get's attack style of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.MeleeAggressive;

        /// <summary>
        ///     Get's attack bonus of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Slash;

        /// <summary>
        ///     Get's npcs suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableNpcs() => [178, 179];
    }
}