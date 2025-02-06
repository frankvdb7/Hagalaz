using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    /// <summary>
    ///     TODO: Make him use protection prayers like tormented demons.
    /// </summary>
    [NpcScriptMetaData([14297])]
    public class AkrisaeTheDoomed : BarrowBrother
    {
        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            var damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 170);
            var maxDamage = ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 170);
            var handle = Owner.Combat.PerformAttack(new AttackParams
            {
                Target = target,
                DamageType = DamageType.StandardMagic,
                Damage = damage,
                MaxDamage = maxDamage
            });

            if (target is ICharacter character)
            {
                handle.RegisterResultHandler(result =>
                {
                    if (!character.Prayers.IsPraying(NormalPrayer.ProtectFromMagic))
                    {
                        Owner.Speak("Ahh, more prayers.");
                        character.Statistics.DrainPrayerPoints(result.DamageLifePoints.Count);
                    }
                });
            }
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
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.MagicNormal;
    }
}