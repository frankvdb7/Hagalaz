using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common;

using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    [NpcScriptMetaData([2025])]
    public class AhrimTheBlighted : BarrowBrother
    {
        private readonly IProjectileBuilder _projectileBuilder;

        public AhrimTheBlighted(INpc owner, IProjectileBuilder projectileBuilder, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator) => _projectileBuilder = projectileBuilder;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            Owner.QueueAnimation(Animation.Create(14223));
            Owner.QueueGraphic(Graphic.Create(457));

            var combat = (INpcCombat)Owner.Combat;

            var delay = NpcAttackDelay.FromDistance(Owner, target, 51);

            _projectileBuilder.Create()
                .WithGraphicId(156)
                .FromCreature(Owner)
                .ToCreature(target)
                .WithDuration(delay)
                .WithDelay(51)
                .WithSlope(16)
                .Send();


            var damage = combat.GetMagicDamage(target, 160);

            var handler = combat.PerformAttack(new AttackParams()
            {
                Target = target.Handle, DamageType = DamageType.StandardMagic, Damage = damage, MaxDamage = damage, Delay = delay
            });


            handler.RegisterResultHandler(result =>
            {
                target.QueueGraphic(result.DamageLifePoints.Succeeded ? Graphic.Create(1019) : Graphic.Create(85, 0, 150));
                if (result.DamageLifePoints.Succeeded && target is ICharacter character)
                {
                    ApplyBlightedAura(character, RandomStatic.Generator.NextDouble());
                }
            });
        }

        /// <summary>
        ///     Chance that a successful hit lowers the target's Strength (about 1 in 5 casts per the Darkan archive).
        /// </summary>
        internal const double BlightedAuraChance = 0.20;

        /// <summary>
        ///     Strength levels removed by Blighted Aura.
        /// </summary>
        internal const int BlightedAuraLevels = 5;

        /// <summary>
        ///     Blighted Aura: a successful magic hit may lower the target's Strength by five levels.
        /// </summary>
        internal static void ApplyBlightedAura(ICharacter target, double roll)
        {
            if (roll < BlightedAuraChance)
            {
                target.Statistics.DamageSkill(StatisticsConstants.Strength, BlightedAuraLevels);
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
