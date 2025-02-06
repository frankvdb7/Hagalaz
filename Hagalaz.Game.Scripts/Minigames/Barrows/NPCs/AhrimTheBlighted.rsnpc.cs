using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    [NpcScriptMetaData([2025])]
    public class AhrimTheBlighted : BarrowBrother
    {
        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            Owner.QueueAnimation(Animation.Create(14223));
            Owner.QueueGraphic(Graphic.Create(457));

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

            var projectile = new Projectile(156);
            projectile.SetSenderData(Owner, 0, false);
            projectile.SetReceiverData(target, 0);
            projectile.SetFlyingProperties(51, (short)(deltaX * 5 + deltaY * 5), 16, 0);
            projectile.Display();

            var damage = combat.GetMagicDamage(target, 160);

            damage = target.Combat.IncomingAttack(Owner, DamageType.StandardMagic, damage, (byte)(51 + deltaX * 5 + deltaY * 5));

            var delay = (byte)(51 + deltaX * 5 + deltaY * 5);

            if (damage == -1)
            {
                target.QueueGraphic(Graphic.Create(85, delay, 150));
            }
            else
            {
                target.QueueGraphic(Graphic.Create(1019, delay));
            }

            Owner.QueueTask(new RsTask(() =>
                {
                    var soak = -1;
                    var dmg = target.Combat.Attack(Owner, DamageType.StandardMagic, damage, ref soak);
                    if (dmg != -1)
                    {
                        var splat = new HitSplat(Owner);
                        splat.SetFirstSplat(HitSplatType.HitMagicDamage, dmg, dmg >= 160);
                        if (soak != -1)
                        {
                            splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                        }

                        target.QueueHitSplat(splat);
                    }
                    else
                    {
                        target.QueueGraphic(Graphic.Create(85, 0, 150));
                    }
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
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.MagicNormal;
    }
}