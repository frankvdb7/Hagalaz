using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos
{
    /// <summary>
    ///     Contains the general graardor script.
    /// </summary>
    public class GeneralGraardor : General
    {
        private readonly IAudioBuilder _soundBuilder;

        /// <summary>
        ///     Initializes the static <see cref="GeneralGraardor" /> class.
        /// </summary>
        static GeneralGraardor()
        {
            SpeakData.Add("Death to our enemies!", 3219);
            SpeakData.Add("Split their skulls!", 3229);
            SpeakData.Add("CHAAARGE!", 3220);
            SpeakData.Add("We feast on the bones of our enemies tonight!", 3206);
            SpeakData.Add("Crush them underfoot!", 3224);
            SpeakData.Add("All glory to Bandos!", 3205);
            SpeakData.Add("Brargh!", 3207);
            SpeakData.Add("GRAAAAAAAAAR!", 3209);
            SpeakData.Add("Break their bones!", 3221);
            SpeakData.Add("FOR THE GLORY OF THE BIG HIGH WAR GOD!", 3228);
        }

        public GeneralGraardor(IAudioBuilder soundBuilder)
        {
            _soundBuilder = soundBuilder;
        }

        /// <summary>
        ///     Contains speak data.
        /// </summary>
        private static readonly Dictionary<string, short> SpeakData = new Dictionary<string, short>();

        /// <summary>
        ///     The attack type
        ///     0 = Melee.
        ///     1 = Ranged.
        /// </summary>
        private int _attackType;

        /// <summary>
        ///     The speak delay.
        /// </summary>
        private int _speakDelay;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            switch (_attackType)
            {
                case 0:
                    {
                        // Melee attack
                        RenderAttack();
                        var preDmg = target.Combat.IncomingAttack(Owner, DamageType.StandardMelee, ((INpcCombat)Owner.Combat).GetMeleeDamage(target), 0);
                        var soaked = -1;
                        var damage = target.Combat.Attack(Owner, DamageType.StandardMelee, preDmg, ref soaked);
                        var splat = new HitSplat(Owner);
                        splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMeleeDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetMeleeMaxHit(target) <= damage);
                        if (soaked != -1)
                        {
                            splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                        }

                        target.QueueHitSplat(splat);
                        break;
                    }
                case 1:
                    {
                        // Ranged attack

                        Owner.QueueAnimation(Animation.Create(7063));

                        var combat = (INpcCombat)Owner.Combat;

                        var visibleCharacters = Owner.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => IsAggressiveTowards(c) && c.Combat.CanBeAttackedBy(Owner));
                        foreach (var c in visibleCharacters)
                        {
                            var deltaX = Owner.Location.X - c.Location.X;
                            var deltaY = Owner.Location.Y - c.Location.Y;
                            if (deltaX < 0)
                            {
                                deltaX = -deltaX;
                            }

                            if (deltaY < 0)
                            {
                                deltaY = -deltaY;
                            }

                            var delay = (byte)(25 + deltaX * 2 + deltaY * 2);

                            var projectile = new Projectile(1200);
                            projectile.SetSenderData(Owner, 0);
                            projectile.SetReceiverData(c, 0);
                            projectile.SetFlyingProperties(40, delay, 0, 180, false);
                            projectile.Display();

                            var dmg = ((INpcCombat)Owner.Combat).GetRangeDamage(c);
                            dmg = c.Combat.IncomingAttack(Owner, DamageType.StandardRange, dmg, delay);

                            var toAttack = c;
                            c.QueueTask(new RsTask(() =>
                            {
                                var soak = -1;
                                var damage = toAttack.Combat.Attack(Owner, DamageType.StandardRange, dmg, ref soak);
                                var splat = new HitSplat(Owner);
                                splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitRangeDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetRangeMaxHit(toAttack) <= damage);
                                if (soak != -1)
                                {
                                    splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                                }

                                toAttack.QueueHitSplat(splat);
                            }, CreatureHelper.CalculateTicksForClientTicks(delay)));
                        }

                        break;
                    }
            }

            GenerateAttackType(target);
        }

        /// <summary>
        ///     Generates the type of the attack.
        /// </summary>
        /// <param name="target">The target.</param>
        private void GenerateAttackType(ICreature target)
        {
            var random = RandomStatic.Generator.NextDouble();
            var rangedChance = 0.25;

            if (!Owner.WithinRange(target, 1))
            {
                rangedChance += 0.075;
            }

            _attackType = random <= rangedChance ? 1 : 0;
        }

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance()
        {
            switch (_attackType)
            {
                case 0:
                    return 1;
                case 1:
                    return 7;
                default:
                    return base.GetAttackDistance();
            }
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => _attackType == 1 ? AttackBonus.Ranged : AttackBonus.Crush;

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => _attackType == 1 ? AttackStyle.RangedAccurate : AttackStyle.MeleeAggressive;

        /// <summary>
        ///     Get's if this npc can retaliate to specific character attack.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can retaliate to] the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanRetaliateTo(ICreature creature) => Owner.Combat.IsInCombat() && Owner.Combat.RecentAttackers.All(attacker => Owner.Combat.Target == null || attacker.Attacker != Owner.Combat.Target);

        /// <summary>
        ///     Tick's npc.
        /// </summary>
        public override void Tick()
        {
            if (!Owner.Combat.IsInCombat() || Owner.SpeakingText != null)
            {
                return;
            }

            _speakDelay++;
            if (_speakDelay <= 10)
            {
                return;
            }

            _speakDelay = 0;
            foreach (var data in SpeakData.Where(data => RandomStatic.Generator.NextDouble() <= 0.008))
            {
                Owner.Speak(data.Key);
                var voice = _soundBuilder.Create().AsVoice().WithId(data.Value).Build();
                voice.PlayWithinDistance(Owner, 8);
                return;
            }
        }

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() =>
        [
            6260
        ];
    }
}