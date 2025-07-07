using System;
using Hagalaz.Game.Abstractions.Builders.HitSplat;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Tasks
{
    /// <summary>
    /// </summary>
    public class OverloadBoostTask : RsTickTask
    {
        /// <summary>
        ///     The drinker.
        /// </summary>
        private readonly ICharacter _drinker;

        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OverloadTask" /> class.
        /// </summary>
        /// <param name="drinker">The drinker.</param>
        public OverloadBoostTask(ICharacter drinker)
        {
            _drinker = drinker;
            _interruptEvent = drinker.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            TickActionMethod = PerformTickImpl;
        }

        /// <summary>
        ///     Boosts the attack.
        /// </summary>
        private void BoostAttack()
        {
            var amount = (int)(5 + Math.Floor(0.22 * _drinker.Statistics.LevelForExperience(StatisticsConstants.Attack)));
            var max = _drinker.Statistics.LevelForExperience(StatisticsConstants.Attack) + amount;
            _drinker.Statistics.HealSkill(StatisticsConstants.Attack, (byte)max, (byte)amount);
        }

        /// <summary>
        ///     Boosts the strength.
        /// </summary>
        private void BoostStrength()
        {
            var amount = (int)(5 + Math.Floor(0.22 * _drinker.Statistics.LevelForExperience(StatisticsConstants.Strength)));
            var max = _drinker.Statistics.LevelForExperience(StatisticsConstants.Strength) + amount;
            _drinker.Statistics.HealSkill(StatisticsConstants.Strength, (byte)max, (byte)amount);
        }

        /// <summary>
        ///     Boosts the defence.
        /// </summary>
        private void BoostDefence()
        {
            var amount = (int)(5 + Math.Floor(0.22 * _drinker.Statistics.LevelForExperience(StatisticsConstants.Defence)));
            var max = _drinker.Statistics.LevelForExperience(StatisticsConstants.Defence) + amount;
            _drinker.Statistics.HealSkill(StatisticsConstants.Defence, (byte)max, (byte)amount);
        }

        /// <summary>
        ///     Boosts the ranged.
        /// </summary>
        private void BoostRanged()
        {
            var amount = (int)(4 + Math.Floor(_drinker.Statistics.LevelForExperience(StatisticsConstants.Ranged) / 5.2));
            var max = _drinker.Statistics.LevelForExperience(StatisticsConstants.Ranged) + amount;
            _drinker.Statistics.HealSkill(StatisticsConstants.Ranged, (byte)max, (byte)amount);
        }

        /// <summary>
        ///     Boosts the magic.
        /// </summary>
        private void BoostMagic()
        {
            const int amount = 7;
            var max = _drinker.Statistics.LevelForExperience(StatisticsConstants.Magic) + amount;
            _drinker.Statistics.HealSkill(StatisticsConstants.Magic, (byte)max, amount);
        }

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (TickCount < 1 || TickCount > 5)
            {
                return;
            }

            var damage = 100;
            damage = _drinker.Statistics.DamageLifePoints(damage);
            var hitSplatBuilder = _drinker.ServiceProvider.GetRequiredService<IHitSplatBuilder>();
            var hitSplat = hitSplatBuilder.Create()
                .AddSprite(s => s
                    .WithDamage(damage)
                    .WithSplatType(HitSplatType.HitSimpleDamage)
                    .AsCriticalDamage())
                .FromSender(_drinker)
                .Build();
            _drinker.QueueHitSplat(hitSplat);
            _drinker.QueueAnimation(Animation.Create(3170));
            switch (TickCount)
            {
                case 1: BoostAttack(); break;
                case 2: BoostStrength(); break;
                case 3: BoostDefence(); break;
                case 4: BoostRanged(); break;
                case 5:
                    BoostMagic();
                    Cancel();
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            _drinker.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent!);
        }
    }
}