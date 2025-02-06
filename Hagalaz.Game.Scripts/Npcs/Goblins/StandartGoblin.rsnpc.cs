using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Goblins
{
    /// <summary>
    ///     Contains standart goblin script.
    /// </summary>
    public class StandartGoblin : NpcScriptBase
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Render's attack.
        /// </summary>
        public override void RenderAttack() => Owner.QueueAnimation(Animation.Create(6184));

        /// <summary>
        ///     Render's defence.
        /// </summary>
        public override void RenderDefence(int delay) => Owner.QueueAnimation(Animation.Create(6183, delay));

        /// <summary>
        ///     Render's this npc death.
        /// </summary>
        /// <returns></returns>
        public override int RenderDeath()
        {
            Owner.QueueAnimation(Animation.Create(6182));
            return 5;
        }

        /// <summary>
        ///     Get's attack distance of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackDistance() => 1;

        /// <summary>
        ///     Get's attack speed of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackSpeed() => 4;

        /// <summary>
        ///     Get's attack style of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.MeleeAggressive;

        /// <summary>
        ///     Get's attack bonus of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Crush;

        /// <summary>
        ///     Get's npcs suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableNpcs() => [2274, 2275, 2276, 2277, 2278, 2279, 2280, 2281];
    }
}