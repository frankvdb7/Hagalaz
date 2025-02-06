using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Combat.Melee.Weapons
{
    /// <summary>
    ///     Contains the dragon scimitar script.
    /// </summary>
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
            var hit = combat.GetMeleeDamage(victim, true);
            var standardMax = combat.GetMeleeMaxHit(victim, false);
            combat.PerformSoulSplit(victim, hit);
            hit = victim.Combat.IncomingAttack(attacker, DamageType.FullMelee, hit, 0);
            combat.AddMeleeExperience(hit);
            var soak = -1;
            hit = victim.Combat.Attack(attacker, DamageType.FullMelee, hit, ref soak);

            var splat = new HitSplat(attacker);
            splat.SetFirstSplat(hit <= 0 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage, hit <= 0 ? 0 : hit, standardMax <= hit);
            if (soak != -1)
            {
                splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
            }

            victim.QueueHitSplat(splat);

            if (!(victim is ICharacter vic))
            {
                return;
            }

            vic.Prayers.DeactivatePrayer(NormalPrayer.ProtectFromMagic);
            vic.Prayers.DeactivatePrayer(NormalPrayer.ProtectFromMelee);
            vic.Prayers.DeactivatePrayer(NormalPrayer.ProtectFromRanged);
            vic.Prayers.DeactivatePrayer(NormalPrayer.ProtectFromSummoning);

            EventHappened pAllowHandler = null;
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
        public override void OnEquiped(IItem item, ICharacter character) => character.AddState(new State(StateType.DragonScimitarEquiped, int.MaxValue));

        /// <summary>
        ///     Happens when this item is unequiped.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public override void OnUnequiped(IItem item, ICharacter character) => character.RemoveState(StateType.DragonScimitarEquiped);

        /// <summary>
        ///     Get's items for which this script is made.
        /// </summary>
        /// <returns>
        ///     Return's array of item ids for which this script is suitable.
        /// </returns>
        public override IEnumerable<int> GetSuitableItems() => [4587];
    }
}