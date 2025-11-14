using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains the dragon scimitar script.
    /// </summary>
    [EquipmentScriptMetaData([4587])]
    public class DragonScimitar : EquipmentScript
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
            var damageMax = combat.GetMeleeMaxHit(victim, false);
            attacker.Combat.PerformAttack(new AttackParams()
            {
                Damage = damage,
                MaxDamage = damageMax,
                DamageType = DamageType.FullMelee,
                Target = victim
            });

            if (victim is not ICharacter vic)
            {
                return;
            }

            vic.Prayers.DeactivatePrayer(NormalPrayer.ProtectFromMagic);
            vic.Prayers.DeactivatePrayer(NormalPrayer.ProtectFromMelee);
            vic.Prayers.DeactivatePrayer(NormalPrayer.ProtectFromRanged);
            vic.Prayers.DeactivatePrayer(NormalPrayer.ProtectFromSummoning);

            EventHappened pAllowHandler = null!;
            pAllowHandler = vic.RegisterEventHandler(new EventHappened<PrayerAllowEvent>(e =>
            {
                if (e.Prayer != NormalPrayer.ProtectFromMagic && e.Prayer != NormalPrayer.ProtectFromMelee && e.Prayer != NormalPrayer.ProtectFromRanged && e.Prayer != NormalPrayer.ProtectFromSummoning)
                {
                    return false;
                }

                vic.SendChatMessage("Your protection prayers have been disabled by your opponents special attack!");
                return true;

            }));

            victim.QueueTask(new RsTask(() => vic.UnregisterEventHandler<PrayerAllowEvent>(pAllowHandler), 8));
        }

        /// <summary>
        ///     Render's this weapon attack.
        /// </summary>
        public override void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
            {
                animator.QueueAnimation(Animation.Create(12031));
                animator.QueueGraphic(Graphic.Create(2118));
            }
            else
            {
                base.RenderAttack(item, animator, false);
            }
        }

        /// <summary>
        ///     Get's required special energy amount.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <returns>
        ///     System.Int16.
        /// </returns>
        public override int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 550;

        /// <summary>
        ///     Happens when this item is equiped.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new DragonScimitarEquippedState());

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState<DragonScimitarEquippedState>();
    }
}
