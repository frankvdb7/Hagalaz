﻿using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos
{
    /// <summary>
    ///     Contains sergeant steel will script.
    /// </summary>
    public class SergeantSteelWill : BodyGuard
    {
        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            // Magic attack

            RenderAttack();

            var combat = (INpcCombat)Owner.Combat;

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

            var projectile = new Projectile(1203);
            projectile.SetSenderData(Owner, 35, false);
            projectile.SetReceiverData(target, 35);
            projectile.SetFlyingProperties(50, delay, 0, 180, false);
            projectile.Display();

            var dmg = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 164);
            dmg = target.Combat.IncomingAttack(Owner, DamageType.StandardMagic, dmg, delay);

            Owner.QueueTask(new RsTask(() =>
                {
                    var soak = -1;
                    var damage = target.Combat.Attack(Owner, DamageType.StandardMagic, dmg, ref soak);
                    var splat = new HitSplat(Owner);
                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 164) <= damage);
                    if (soak != -1)
                    {
                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                    }

                    target.QueueHitSplat(splat);
                }, CreatureHelper.CalculateTicksForClientTicks(delay)));
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Magic;

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance() => 7;

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() => [6263];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}