using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains bgs equipment script.
    /// </summary>
    [EquipmentScriptMetaData([11696, 13451])]
    public class BandosGodsword : EquipmentScript
    {
        /// <summary>
        ///     Perform's special attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim creature.</param>
        public override void PerformSpecialAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            RenderAttack(item, attacker, true);
            var combat = (ICharacterCombat)attacker.Combat;

            var damage = combat.GetMeleeDamage(victim, true);
            var maxDamage = combat.GetMeleeMaxHit(victim, false);

            var handle = attacker.Combat.PerformAttack(new AttackParams()
            {
                Damage = damage,
                MaxDamage = maxDamage,
                DamageType = DamageType.FullMelee,
                Target = victim
            });

            if (victim is ICharacter vic)
            {
                handle.RegisterResultHandler(result =>
                {
                    var toDamage = (int)(result.DamageLifePoints.Count * 0.10);
                    if (toDamage > 0)
                    {
                        toDamage -= vic.Statistics.DamageSkill(StatisticsConstants.Defence, toDamage);
                    }

                    if (toDamage > 0)
                    {
                        toDamage -= vic.Statistics.DamageSkill(StatisticsConstants.Strength, toDamage);
                    }

                    if (toDamage > 0)
                    {
                        toDamage -= vic.Statistics.DamageSkill(StatisticsConstants.Attack, toDamage);
                    }

                    if (toDamage > 0)
                    {
                        toDamage -= vic.Statistics.DamageSkill(StatisticsConstants.Prayer, toDamage);
                    }

                    if (toDamage > 0)
                    {
                        toDamage -= vic.Statistics.DamageSkill(StatisticsConstants.Magic, toDamage);
                    }

                    if (toDamage > 0)
                    {
                        vic.Statistics.DamageSkill(StatisticsConstants.Ranged, toDamage);
                    }
                });
            }
        }

        /// <summary>
        ///     Render's this weapon attack.
        /// </summary>
        public override void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
            {
                animator.QueueAnimation(Animation.Create(11991));
                animator.QueueGraphic(Graphic.Create(2114));
            }
            else
            {
                base.RenderAttack(item, animator, specialAttack);
            }
        }

        /// <summary>
        ///     Get's required special energy amount.
        /// </summary>
        /// <returns></returns>
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 1000;

        /// <summary>
        ///     Happens when this item is equiped.
        /// </summary>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new BandosGodswordEquippedState());

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState<BandosGodswordEquippedState>();
    }
}
