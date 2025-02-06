using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Tasks
{
    /// <summary>
    ///     Contains task script for the give thanks emote.
    /// </summary>
    public class GiveThanksTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;

        /// <summary>
        ///     Construct's new home teleport task.
        /// </summary>
        public GiveThanksTask(ICharacter actor, Action finishCallback)
        {
            _actor = actor;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = _actor.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                _finishCallback?.Invoke();
                Cancel();
                return false;
            });
            _finishCallback = finishCallback;
        }

        /// <summary>
        ///     Contains finish callback.
        /// </summary>
        private readonly Action _finishCallback;

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private readonly ICharacter _actor;

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (TickCount == 1)
            {
                _actor.QueueAnimation(Animation.Create(10994));
            }
            else if (TickCount == 2)
            {
                _actor.QueueGraphic(Graphic.Create(86));
            }
            else if (TickCount == 3)
            {
                _actor.QueueAnimation(Animation.Create(10996));
                _actor.Appearance.TurnToNpc(8499);
            }
            else if (TickCount == 7)
            {
                _finishCallback.Invoke();
                Cancel();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            _actor.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent!);
        }
    }
}