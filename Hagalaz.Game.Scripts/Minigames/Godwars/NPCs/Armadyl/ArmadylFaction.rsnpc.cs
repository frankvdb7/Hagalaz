using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Armadyl
{
    /// <summary>
    ///     Contains the armadyl faction npc script.
    /// </summary>
    [NpcScriptMetaData([6229, 6230, /*6231,*/ 6232, 6233, 6234, 6235, 6236, 6237, 6238, 6239, 6240, 6241, 6242, 6243, 6244, 6245, 6246])]
    public class ArmadylFaction : NpcScriptBase
    {
        private readonly IProjectileBuilder _projectileBuilder;

        public ArmadylFaction(IProjectileBuilder projectileBuilder) => _projectileBuilder = projectileBuilder;

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

            var delay = 20 + deltaX * 5 + deltaY * 5;

            _projectileBuilder.Create()
                .WithGraphicId(1190)
                .FromCreature(Owner)
                .ToCreature(target)
                .WithDuration(delay)
                .WithAngle(180)
                .WithDelay(30)
                .WithFromHeight(50)
                .WithToHeight(35)
                .Send();

            Owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = ((INpcCombat)Owner.Combat).GetRangeDamage(target),
                MaxDamage = ((INpcCombat)Owner.Combat).GetRangeMaxHit(target),
                Delay = delay,
                DamageType = DamageType.StandardRange,
                Target = target
            });
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
            if (style != AttackStyle.MeleeAccurate && style != AttackStyle.MeleeAggressive && style != AttackStyle.MeleeControlled &&
                style != AttackStyle.MeleeDefensive)
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

            if (creature is not INpc npc)
            {
                return creature.Combat.RecentAttackers.Count() <= 2;
            }

            var script = npc.Script;
            if (script is ArmadylFaction or FamiliarScriptBase)
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

            foreach (var t in creatures)
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
    }
}