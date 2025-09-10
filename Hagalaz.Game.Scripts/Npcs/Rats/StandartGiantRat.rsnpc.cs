using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Rats
{
    /// <summary>
    ///     Contains standart giant rat script.
    /// </summary>
    [NpcScriptMetaData([86, 87, 446, 4395, 8828, 8829])]
    public class StandartGiantRat : NpcScriptBase
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Render's attack.
        /// </summary>
        public override void RenderAttack() => Owner.QueueAnimation(Animation.Create(14859));

        /// <summary>
        ///     Render's defence.
        /// </summary>
        public override void RenderDefence(int delay) => Owner.QueueAnimation(Animation.Create(14861, delay));

        /// <summary>
        ///     Render's this npc death.
        /// </summary>
        /// <returns></returns>
        public override int RenderDeath()
        {
            Owner.QueueAnimation(Animation.Create(14860));
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