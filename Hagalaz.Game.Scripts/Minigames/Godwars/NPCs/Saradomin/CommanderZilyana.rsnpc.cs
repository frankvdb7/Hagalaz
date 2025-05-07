using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model.Combat;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Saradomin
{
    /// <summary>
    /// </summary>
    public class CommanderZilyana : General
    {
        private readonly IAudioBuilder _soundBuilder;

        /// <summary>
        ///     Initializes the static <see cref="CommanderZilyana" /> class.
        /// </summary>
        static CommanderZilyana()
        {
            _speakData.Add("Death to the enemies of the light!", 3247);
            _speakData.Add("Slay the evil ones!", 3242);
            _speakData.Add("Saradomin lend me strength!", 3263);
            _speakData.Add("May Saradomin be my sword.", 3251);
            _speakData.Add("Good will always triumph!", 3260);
            _speakData.Add("Forward! Our allies are with us!", 3245);
            _speakData.Add("Saradomin is with us!", 3266);
            _speakData.Add("In the name of Saradomin!", 3250);
            _speakData.Add("Attack! Find the Godsword!", 3258);
            _speakData.Add("All praise Saradomin!", 3262);
        }

        public CommanderZilyana(IAudioBuilder soundBuilder) => _soundBuilder = soundBuilder;

        /// <summary>
        ///     Contains speak data.
        /// </summary>
        private static readonly Dictionary<string, short> _speakData = new Dictionary<string, short>();

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
            _attackType = RandomStatic.Generator.Next(0, 2);
            if (_attackType == 0) // melee
            {
                base.PerformAttack(target);
            }
            else if (_attackType == 1) // magic
            {
                Owner.QueueAnimation(Animation.Create(6967));
                target.QueueTask(new RsTask(() =>
                    {
                        target.QueueGraphic(Graphic.Create(1207));
                        var combat = (INpcCombat)Owner.Combat;
                        var hit1 = combat.GetMagicDamage(target, 310);
                        var hit2 = combat.GetMagicDamage(target, 310);
                        var standartMax = combat.GetMagicMaxHit(target, 310);
                        hit1 = target.Combat.IncomingAttack(Owner, DamageType.StandardMagic, hit1, 0);
                        hit2 = target.Combat.IncomingAttack(Owner, DamageType.StandardMagic, hit2, 0);
                        var soak1 = -1;
                        var damage1 = target.Combat.Attack(Owner, DamageType.StandardMagic, hit1, ref soak1);
                        var soak2 = -1;
                        var damage2 = target.Combat.Attack(Owner, DamageType.StandardMagic, hit2, ref soak2);
                        var splat1 = new HitSplat(Owner);
                        splat1.SetFirstSplat(damage1 <= 0 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage1 <= 0 ? 0 : damage1, standartMax <= damage1);
                        if (soak1 != -1)
                        {
                            splat1.SetSecondSplat(HitSplatType.HitDefendedDamage, soak1, false);
                        }

                        target.QueueHitSplat(splat1);

                        var splat2 = new HitSplat(Owner);
                        splat2.SetFirstSplat(damage2 <= 0 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage2 <= 0 ? 0 : damage2, standartMax <= damage2);
                        if (soak2 != -1)
                        {
                            splat2.SetSecondSplat(HitSplatType.HitDefendedDamage, soak2, false);
                        }

                        target.QueueHitSplat(splat2);
                    }, 3));
            }
        }

        /// <summary>
        ///     Get's attack speed of this npc.
        ///     By default, this method does return Definition.AttackSpeed.
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackSpeed() => _attackType == 1 ? 10 : base.GetAttackSpeed();

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => _attackType == 0 ? AttackBonus.Slash : AttackBonus.Magic;

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance() => _attackType == 0 ? 1 : 8;

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => _attackType == 0 ? AttackStyle.MeleeAccurate : AttackStyle.MagicNormal;

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
            foreach (var data in _speakData.Where(data => RandomStatic.Generator.NextDouble() <= 0.008))
            {
                Owner.Speak(data.Key);
                _soundBuilder.Create().AsVoice().WithId(data.Value).Build().PlayWithinDistance(Owner, 8);
                return;
            }
        }

        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() => [6247];
    }
}