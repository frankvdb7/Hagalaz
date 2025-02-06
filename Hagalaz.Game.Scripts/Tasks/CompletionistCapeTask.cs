using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Tasks
{
    /// <summary>
    ///     Contains task script for the completionist cape emote.
    /// </summary>
    public class CompletionistCapeTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;
        

        /// <summary>
        ///     Constructs new completionist task.
        /// </summary>
        public CompletionistCapeTask(ICharacter actor, short id, Action finishCallback)
        {
            _actor = actor;
            _id = id;
            _finishCallback = finishCallback;
            _interruptEvent = _actor.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                _finishCallback?.Invoke();
                Cancel();
                return false;
            });

            TickActionMethod = PerformTickImpl;
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
        ///     Npc id.
        /// </summary>
        private readonly short _id;

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (TickCount == 1)
            {
                _actor.QueueGraphic(Graphic.Create(307));
                _actor.QueueAnimation(Animation.Create(356));
            }
            else if (TickCount == 5)
            {
                _actor.Appearance.TurnToNpc(_id);
                _actor.QueueAnimation(Animation.Create(1174));
                _actor.QueueGraphic(Graphic.Create(1443));
            }
            else if (TickCount == 7)
            {
                _actor.Configurations.SendSetCameraShake(4, 4, 4, 4, 4);
            }
            else if (TickCount == 15)
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