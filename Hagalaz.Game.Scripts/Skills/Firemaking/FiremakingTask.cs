using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;

namespace Hagalaz.Game.Scripts.Skills.Firemaking
{
    /// <summary>
    ///     Class for firemaking task.
    /// </summary>
    public class FiremakingTask : RsTickTask
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;

        /// <summary>
        ///     Construct's new firemaking task.
        /// </summary>
        /// <param name="performer">The performer.</param>
        /// <param name="logs">The logs.</param>
        /// <param name="finishCallback">The finish callback.</param>
        public FiremakingTask(ICharacter performer, FiremakingDto logs, Action finishCallback)
        {
            Performer = performer;
            Logs = logs;
            FinishCallback = finishCallback;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
        }

        /// <summary>
        ///     Contains the logs.
        /// </summary>
        private FiremakingDto Logs { get; }

        /// <summary>
        ///     Contains finish callback.
        /// </summary>
        internal Action FinishCallback { get; }

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private ICharacter Performer { get; }

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (TickCount == 1 || TickCount % 12 == 0)
            {
                Performer.QueueAnimation(Animation.Create(733));
            }

            if (TickCount % 4 == 0)
            {
                var b = Math.Log((Performer.Statistics.GetSkillLevel(StatisticsConstants.Firemaking) + 1.0) / Logs.RequiredLevel);
                var chance = 0.40 + Math.Exp(b) / Logs.RequiredLevel;
                if (chance > RandomStatic.Generator.NextDouble())
                {
                    FinishCallback();
                    Cancel();
                }
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            Performer.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent!);
        }
    }
}