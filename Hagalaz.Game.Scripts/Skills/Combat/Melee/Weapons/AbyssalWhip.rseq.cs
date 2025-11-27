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
    ///     Contains abyssal whip equipment script.
    /// </summary>
    [EquipmentScriptMetaData([4151, 15441, 15442, 15443, 15444])]
    public class AbyssalWhip : EquipmentScript
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

            var handle = attacker.Combat.PerformAttack(new AttackParams
            {
                Target = victim,
                DamageType = DamageType.FullMelee,
                Damage = damage,
                MaxDamage = maxDamage
            });
            victim.QueueGraphic(Graphic.Create(2108));

            if (victim is ICharacter character)
            {
                handle.RegisterResultHandler(result =>
                {
                    character.Statistics.DrainRunEnergy(result.DamageLifePoints.Count / 4);
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
                animator.QueueAnimation(Animation.Create(11971));
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
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 500;

        /// <summary>
        ///     Happens when this item is equipped.
        /// </summary>
        public override void OnEquipped(IItem item, ICharacter character) => character.AddState(new AbyssalWhipEquippedState());

        /// <summary>
        ///     Happens when this item is unequipped.
        /// </summary>
        public override void OnUnequipped(IItem item, ICharacter character) => character.RemoveState<AbyssalWhipEquippedState>();
    }
}
