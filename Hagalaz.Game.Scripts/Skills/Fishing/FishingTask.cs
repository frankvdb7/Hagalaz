using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Skills.Fishing
{
    /// <summary>
    /// </summary>
    public class FishingTask : RsTickTask
    {
        /// <summary>
        ///     Construct's new firemaking task.
        /// </summary>
        public FishingTask(ICharacter performer, Func<ValueTask<bool>> finishCallback, double chance, INpc fishingSpot, int animId)
        {
            _performer = performer;
            _finishCallback = finishCallback;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            _chance = chance;
            _fishingSpot = fishingSpot;
            _animId = animId;
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;
        
        /// <summary>
        ///     Contains finish callback.
        /// </summary>
        private readonly Func<ValueTask<bool>> _finishCallback;

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private readonly ICharacter _performer;

        /// <summary>
        ///     The chance of getting log from the tree.
        /// </summary>
        private readonly double _chance;

        /// <summary>
        ///     The fishing spot.
        /// </summary>
        private readonly INpc _fishingSpot;

        /// <summary>
        ///     The fishing animation id.
        /// </summary>
        private readonly int _animId;

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private async void PerformTickImpl()
        {
            // check if fishing spot has exhausted.
            if (_fishingSpot.IsDestroyed || !_performer.Viewport.VisibleCreatures.Contains(_fishingSpot))
            {
                Cancel();
                return;
            }

            // check if fishing spot has moved.
            if (_fishingSpot.Movement.Moved || _fishingSpot.Movement.Moving)
            {
                Cancel();
                return;
            }

            var randomValue = RandomStatic.Generator.NextDouble();
            if (randomValue <= _chance)
            {
                // Invoke callback. If the callback returns true, that means the task is finished.
                if (await _finishCallback())
                {
                    Cancel();
                    return;
                }
            }

            if (TickCount % 4 == 0)
            {
                _performer.QueueAnimation(Animation.Create(_animId));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            _performer.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent!);
        }
    }
}