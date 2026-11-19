using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    /// <summary>
    ///     Akrisae the Doomed: melee attacker whose protection prayer follows the style that hurts him most. After
    ///     <see cref="AkrisaeProtection.SwitchDamage" /> from one style he protects against it and skips his next attack.
    /// </summary>
    [NpcScriptMetaData([14297])]
    public class AkrisaeTheDoomed : BarrowBrother
    {
        private readonly AkrisaeProtection _protection = new();

        public AkrisaeTheDoomed(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
        }

        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            var damage = ((INpcCombat)Owner.Combat).GetMeleeDamage(target);
            var maxDamage = ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target);
            var handle = Owner.Combat.PerformAttack(new AttackParams
            {
                Target = target.Handle,
                DamageType = DamageType.StandardMelee,
                Damage = damage,
                MaxDamage = maxDamage
            });

            if (target is ICharacter character)
            {
                handle.RegisterResultHandler(result =>
                {
                    if (!character.Prayers.IsPraying(NormalPrayer.ProtectFromMelee))
                    {
                        Owner.Speak("Ahh, more prayers.");
                        character.Statistics.DrainPrayerPoints(result.DamageLifePoints.Count);
                    }
                });
            }
        }

        public override int OnIncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay) =>
            base.OnIncomingAttack(attacker, damageType, _protection.Reduce(damageType, damage), delay);

        public override int OnAttack(ICreature attacker, DamageType damageType, int damage)
        {
            if (_protection.Record(damageType, damage))
            {
                Owner.Combat.ResetCombatDelay(); // changing prayer costs him his next attack
            }

            return damage;
        }

        public override void OnCancelTarget()
        {
            _protection.Reset();
            base.OnCancelTarget();
        }

        public override void OnDeath() => _protection.Reset();

        public override AttackBonus GetAttackBonusType() => AttackBonus.Crush;

        public override AttackStyle GetAttackStyle() => AttackStyle.MeleeAccurate;
    }
}
