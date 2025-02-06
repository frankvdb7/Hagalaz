using System.Linq;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Armadyl
{
    /// <summary>
    ///     Contains the armadyl faction npc script.
    /// </summary>
    public class ArmadylFaction : NpcScriptBase
    {
        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.RangedAccurate;

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Ranged;

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance() => 8;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();

            var deltaX = Owner.Location.X - target.Location.X;
            var deltaY = Owner.Location.Y - target.Location.Y;
            if (deltaX < 0)
            {
                deltaX = -deltaX;
            }

            if (deltaY < 0)
            {
                deltaY = -deltaY;
            }

            var delay = (byte)(20 + deltaX * 5 + deltaY * 5);

            var projectile = new Projectile(1190);
            projectile.SetSenderData(Owner, 50, false);
            projectile.SetReceiverData(target, 35);
            projectile.SetFlyingProperties(30, delay, 0, 180, false);
            projectile.Display();

            var dmg = ((INpcCombat)Owner.Combat).GetRangeDamage(target);
            dmg = target.Combat.IncomingAttack(Owner, DamageType.StandardRange, dmg, delay);

            Owner.QueueTask(new RsTask(() =>
                {
                    var soak = -1;
                    var damage = target.Combat.Attack(Owner, DamageType.StandardRange, dmg, ref soak);
                    var splat = new HitSplat(Owner);
                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitRangeDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetRangeMaxHit(target) <= damage);
                    if (soak != -1)
                    {
                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                    }

                    target.QueueHitSplat(splat);
                }, CreatureHelper.CalculateTicksForClientTicks(delay)));
        }

        /// <summary>
        ///     Get's if this npc can be attacked by the specified character ('attacker').
        ///     By default , this method does check if this npc is attackable.
        ///     This method also checks if the attacker is a character, wether or not it
        ///     has the required slayer level.
        /// </summary>
        /// <param name="attacker">Creature which is attacking this npc.</param>
        /// <returns>
        ///     If attack can be performed.
        /// </returns>
        public override bool CanBeAttackedBy(ICreature attacker)
        {
            var style = attacker.Combat.GetAttackStyle();
            if (style != AttackStyle.MeleeAccurate && style != AttackStyle.MeleeAggressive && style != AttackStyle.MeleeControlled && style != AttackStyle.MeleeDefensive)
            {
                return base.CanBeAttackedBy(attacker);
            }

            if (attacker is ICharacter character)
            {
                character.SendChatMessage("The aviansie is flying too high for you to attack using melee.");
            }

            if (CanRetaliateTo(attacker))
            {
                Owner.QueueTask(new RsTask(() => Owner.Combat.SetTarget(attacker), 1));
            }

            return false;

        }

        /// <summary>
        ///     Get's if this npc can aggro attack specific character.
        ///     By default this method does check if character is character.
        ///     This method does not get called by ticks if npc reaction is not aggresive.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///     <c>true</c> if this instance can aggro the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsAggressiveTowards(ICreature creature)
        {
            if (!Owner.WithinRange(creature, 1 + GetAttackDistance()))
            {
                return false;
            }

            if (!(creature is INpc npc))
            {
                return creature.Combat.RecentAttackers.Count() <= 2;
            }

            var script = npc.Script;
            if (script is ArmadylFaction || script is FamiliarScriptBase)
            {
                return false;
            }

            return creature.Combat.RecentAttackers.Count() <= 2;
        }

        /// <summary>
        ///     Performs the aggressiveness check.
        /// </summary>
        public override void AggressivenessTick()
        {
            if (Owner.Combat.IsInCombat())
            {
                return;
            }

            var creatures = Owner.Viewport.VisibleCreatures;
            if (!creatures.OfType<ICharacter>().Any())
            {
                return;
            }

            for (var i = 0; i < creatures.Count; i++)
            {
                var creature = creatures[RandomStatic.Generator.Next(creatures.Count)];
                if (!IsAggressiveTowards(creature))
                {
                    continue;
                }

                if (Owner.Combat.SetTarget(creature))
                {
                    return;
                }
            }
        }

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() => GodwarsConstants.ArmadylFactionNpCs;

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}