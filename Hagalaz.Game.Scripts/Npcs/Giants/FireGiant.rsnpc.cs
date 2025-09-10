using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Giants
{
    /// <summary>
    ///     Contains fire giant script.
    /// </summary>
    [NpcScriptMetaData([1582, 1583, 1584, 1585, 1586])]
    public class FireGiant : NpcScriptBase
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Render's attack.
        /// </summary>
        public override void RenderAttack() => Owner.QueueAnimation(Animation.Create(4652));

        /// <summary>
        ///     Render's defence.
        /// </summary>
        public override void RenderDefence(int delay) => Owner.QueueAnimation(Animation.Create(4651, delay));

        /// <summary>
        ///     Render's this npc death.
        /// </summary>
        /// <returns></returns>
        public override int RenderDeath()
        {
            Owner.QueueAnimation(Animation.Create(4653));
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
        public override int GetAttackSpeed() => 5;

        /// <summary>
        ///     Called when [incomming attack].
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">The damage.</param>
        /// <param name="delay">The delay.</param>
        /// <returns></returns>
        public override int OnIncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay)
        {
            if (attacker is ICharacter)
            {
                // TODO - Read spell types and apply weakness.
            }

            return base.OnIncomingAttack(attacker, damageType, damage, delay);
        }

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
    }
}