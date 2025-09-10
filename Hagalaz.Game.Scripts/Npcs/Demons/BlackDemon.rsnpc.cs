using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Demons
{
    /// <summary>
    ///     Contains black demon script.
    /// </summary>
    [NpcScriptMetaData([4702, 4703, 4704, 4705])]
    public class BlackDemon : NpcScriptBase
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Render's attack.
        /// </summary>
        public override void RenderAttack() => Owner.QueueAnimation(Animation.Create(64));

        /// <summary>
        ///     Render's defence.
        /// </summary>
        public override void RenderDefence(int delay) => Owner.QueueAnimation(Animation.Create(65, delay));

        /// <summary>
        ///     Render's this npc death.
        /// </summary>
        /// <returns></returns>
        public override int RenderDeath()
        {
            Owner.QueueAnimation(Animation.Create(67));
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
        public override AttackBonus GetAttackBonusType() => AttackBonus.Slash;
    }
}