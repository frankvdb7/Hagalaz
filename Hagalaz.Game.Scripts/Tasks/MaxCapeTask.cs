using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Tasks
{
    /// <summary>
    ///     Contains task script for the max cape emote.
    /// </summary>
    public class MaxCapeTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;

        /// <summary>
        ///     Construct's new home teleport task.
        /// </summary>
        public MaxCapeTask(ICharacter actor, INpc performer)
        {
            _npcRegistration = actor.ServiceProvider.GetRequiredService<INpcService>();
            _actor = actor;
            _performer = performer;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = _actor.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                _finishCallback?.Invoke();
                Cancel();
                return false;
            });
            _finishCallback = () =>
            {
                _actor.ResetFacing();
                _npcRegistration.UnregisterAsync(_performer);
            };
            _npcRegistration.RegisterAsync(_performer);
            _performer.FaceCreature(_actor);
            _actor.FaceCreature(_performer);
        }

        /// <summary>
        ///     Contains finish callback.
        /// </summary>
        private readonly Action _finishCallback;

        /// <summary>
        ///     Contains actor.
        /// </summary>
        private readonly ICharacter _actor;

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private readonly INpc _performer;

        /// <summary>
        /// 
        /// </summary>
        private readonly INpcService _npcRegistration;

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (TickCount == 1)
            {
                _performer.QueueAnimation(Animation.Create(1434));
                _performer.QueueGraphic(Graphic.Create(1482));
                _actor.QueueAnimation(Animation.Create(1179));
            }
            else if (TickCount == 2)
            {
                _performer.QueueAnimation(Animation.Create(1436));
                _performer.QueueGraphic(Graphic.Create(1486));
                _actor.QueueAnimation(Animation.Create(1180));
            }
            else if (TickCount == 3)
            {
                _performer.QueueGraphic(Graphic.Create(1498));
                _actor.QueueAnimation(Animation.Create(1181));
            }
            else if (TickCount == 4)
            {
                _actor.QueueAnimation(Animation.Create(1182));
            }
            else if (TickCount == 5)
            {
                _performer.QueueAnimation(Animation.Create(1448));
                _actor.QueueAnimation(Animation.Create(1250));
            }
            else if (TickCount == 6)
            {
                _actor.QueueAnimation(Animation.Create(1251));
                _actor.QueueGraphic(Graphic.Create(1499));
                _performer.QueueAnimation(Animation.Create(1454));
                _performer.QueueGraphic(Graphic.Create(1504));
            }
            else if (TickCount == 11)
            {
                _actor.QueueAnimation(Animation.Create(1291));
                _actor.QueueGraphic(Graphic.Create(1686));
                _actor.QueueGraphic(Graphic.Create(1598));
                _performer.QueueAnimation(Animation.Create(1440));
            }
            else if (TickCount == 19)
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