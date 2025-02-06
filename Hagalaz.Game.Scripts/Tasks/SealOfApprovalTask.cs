using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Tasks
{
    /// <summary>
    ///     Contains task script for the seal of approval emote.
    /// </summary>
    public class SealOfApprovalTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;

        /// <summary>
        ///     Construct's new home teleport task.
        /// </summary>
        public SealOfApprovalTask(ICharacter actor, Action finishCallback)
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
                _actor.QueueGraphic(Graphic.Create(1287));
                _actor.QueueAnimation(Animation.Create(15104));
            }
            else if (TickCount == 3)
            {
                _actor.QueueAnimation(Animation.Create(15106));
                var random = RandomStatic.Generator.Next(0, 1);
                _actor.Appearance.TurnToNpc(random == 0 ? 13255 : random == 1 ? 13256 : 13257);
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