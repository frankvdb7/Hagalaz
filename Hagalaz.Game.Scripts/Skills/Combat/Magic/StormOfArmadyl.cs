using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     This class contains a script for the storm of armadyl combat spell.
    /// </summary>
    public class StormOfArmadyl : StandardCombatSpell
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="StormOfArmadyl" /> class.
        /// </summary>
        /// <param name="dto">The definition.</param>
        public StormOfArmadyl(CombatSpellDto dto)
            : base(dto)
        {
        }

        /// <summary>
        ///     Get's speed of this spell.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public override int GetCastingSpeed(ICharacter caster)
        {
            var weapon = caster.Equipment[EquipmentSlot.Weapon];
            if (weapon != null && weapon.Id == (short)StaffType.ArmadylBattleStaff)
            {
                return 4;
            }

            return 5;
        }

        /// <summary>
        ///     Perform's attack to specific target.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="victim"></param>
        public override void PerformAttack(ICharacter caster, ICreature victim)
        {
            caster.QueueAnimation(Animation.Create(Dto.CastAnimationId));
            caster.QueueGraphic(Graphic.Create(Dto.CastGraphicId));

            var combat = (ICharacterCombat)caster.Combat;

            caster.Statistics.AddExperience(StatisticsConstants.Magic, Dto.BaseExperience);

            var deltaX = caster.Location.X - victim.Location.X;
            var deltaY = caster.Location.Y - victim.Location.Y;
            if (deltaX < 0)
            {
                deltaX = -deltaX;
            }

            if (deltaY < 0)
            {
                deltaY = -deltaY;
            }

            var boost = (caster.Statistics.LevelForExperience(StatisticsConstants.Magic) - Dto.RequiredLevel) * 5;
            var max = combat.GetMagicMaxHit(victim, Dto.BaseDamage + boost);
            var damage = combat.GetMagicDamage(victim, max);
            if (damage > 0 && damage < boost)
            {
                damage += boost;
            }

            combat.PerformSoulSplit(victim, damage);

            damage = victim.Combat.IncomingAttack(caster, DamageType.FullMagic, damage, (byte)(51 + deltaX * 5 + deltaY * 5));
            combat.AddMagicExperience(damage);

            var delay = (byte)(51 + deltaX * 5 + deltaY * 5);
            RenderProjectile(caster, victim, delay);

            if (damage == -1)
            {
                victim.QueueGraphic(Graphic.Create(85, delay, 150));
            }
            else if (Dto.EndGraphicId != -1)
            {
                victim.QueueGraphic(Graphic.Create(Dto.EndGraphicId, delay));
            }

            caster.QueueTask(new RsTask(() =>
                {
                    var soak = -1;
                    var dmg = victim.Combat.Attack(caster, DamageType.FullMagic, damage, ref soak);
                    if (dmg != -1)
                    {
                        var splat = new HitSplat(caster);
                        splat.SetFirstSplat(HitSplatType.HitMagicDamage, dmg, dmg >= 160 + boost);
                        if (soak != -1)
                        {
                            splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                        }

                        victim.QueueHitSplat(splat);
                    }
                    else
                    {
                        victim.QueueGraphic(Graphic.Create(85, 0, 150));
                    }
                }, CreatureHelper.CalculateTicksForClientTicks(delay)));
        }

        /// <summary>
        ///     Renders the projectile.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        /// <param name="delay">The delay.</param>
        public override void RenderProjectile(ICharacter caster, ICreature victim, int delay)
        {
            var projectileBuilder = caster.ServiceProvider.GetRequiredService<IProjectileBuilder>();
            projectileBuilder.Create()
                .WithGraphicId(Dto.ProjectileId)
                .FromCreature(caster)
                .ToCreature(victim)
                .WithDuration(delay - 51)
                .WithFromHeight(43)
                .WithToHeight(25)
                .WithDelay(51)
                .WithSlope(16)
                .WithAngle(20)
                .Send();
        }
    }
}