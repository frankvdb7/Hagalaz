using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Tasks
{
    /// <summary>
    /// </summary>
    public class OverloadTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OverloadTask" /> class.
        /// </summary>
        /// <param name="drinker">The drinker.</param>
        /// <param name="ticks">The ticks.</param>
        public OverloadTask(ICharacter drinker, int ticks)
        {
            _drinker = drinker;
            _ticks = ticks;
            _interruptEvent = drinker.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            TickActionMethod = PerformTickImpl;
        }

        /// <summary>
        ///     The drinker.
        /// </summary>
        private readonly ICharacter _drinker;

        /// <summary>
        ///     The ticks.
        /// </summary>
        private readonly int _ticks;

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
            if (TickCount % 15 == 0 && TickCount != _ticks)
            {
                BoostAttack();
                BoostDefence();
                BoostMagic();
                BoostRanged();
                BoostStrength();
            }

            if (TickCount == _ticks - 30)
            {
                _drinker.SendChatMessage("The overload effect expires in 30 seconds.");
            }
            else if (TickCount == _ticks - 17)
            {
                _drinker.SendChatMessage("The overload effect expires in 10 seconds.");
            }
            else if (TickCount == _ticks - 8)
            {
                _drinker.SendChatMessage("The overload effect expires in 5 seconds.");
            }
            else if (TickCount == _ticks)
            {
                _drinker.RemoveState<OverloadEffectState>();
                Cancel();
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