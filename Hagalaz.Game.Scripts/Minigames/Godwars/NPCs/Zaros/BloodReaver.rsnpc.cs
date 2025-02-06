using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Zaros
{
    /// <summary>
    ///     Contains blood reaver npc script.
    /// </summary>
    public class BloodReaver : NpcScriptBase
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Render's attack.
        /// </summary>
        public override void RenderAttack() => Owner.QueueAnimation(Animation.Create(7004));

        /// <summary>
        ///     Render's defence.
        /// </summary>
        public override void RenderDefence(int delay) => Owner.QueueAnimation(Animation.Create(7028, delay));

        /// <summary>
        ///     Perform's attack to specific target.
        /// </summary>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            var projectile = new Projectile(374);
            projectile.SetSenderData(Owner, 11);
            projectile.SetReceiverData(target, 15);
            projectile.SetFlyingProperties(50, 30, 15, 192);
            projectile.Display();

            var dmg = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 146);
            dmg = target.Combat.IncomingAttack(Owner, DamageType.FullMagic, dmg, 40);
            target.QueueGraphic(Graphic.Create(375, 40));
            Owner.QueueTask(new RsTask(() =>
                {
                    var soak = -1;
                    var damage = target.Combat.Attack(Owner, DamageType.FullMagic, dmg, ref soak);
                    var splat = new HitSplat(Owner);
                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 146) <= damage);
                    if (soak != -1)
                    {
                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                    }

                    target.QueueHitSplat(splat);
                }, 2));
        }


        /// <summary>
        ///     Render's this npc death.
        /// </summary>
        /// <returns></returns>
        public override int RenderDeath()
        {
            Owner.QueueAnimation(Animation.Create(7000));
            return 7;
        }

        /// <summary>
        ///     Get's attack distance of this reaver.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackDistance() => 10;

        /// <summary>
        ///     Get's attack speed of this reaver.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackSpeed() => 4;

        /// <summary>
        ///     Get's attack style of this reaver.
        /// </summary>
        /// <returns></returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.MeleeAggressive;

        /// <summary>
        ///     Get's attack bonus of this reaver.
        /// </summary>
        /// <returns></returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Magic;

        /// <summary>
        ///     Get's npcs suitable for this script.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableNpcs() => [13458];
    }
}