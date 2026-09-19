using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Scripts.Skills.Fishing
{
    /// <summary>
    /// </summary>
    public class FishingTask : RsTickTask
    {
        /// <summary>
        ///     Construct's new firemaking task.
        /// </summary>
        public FishingTask(
            ICharacter performer,
            Func<INpc, bool> finishCallback,
            double chance,
            EntityHandle<ICreature> fishingSpotHandle,
            int animId)
        {
            _performer = performer;
            _finishCallback = finishCallback;
            _entityService = performer.ServiceProvider.GetRequiredService<IEntityService>();
            TickActionMethod = PerformTickImpl;
            _interruptEvent = performer.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                Cancel();
                return false;
            });
            _chance = chance;
            _fishingSpotHandle = fishingSpotHandle;
            _animId = animId;
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened? _interruptEvent;
        
        /// <summary>
        ///     Contains finish callback.
        /// </summary>
        private readonly Func<INpc, bool> _finishCallback;

        /// <summary>
        ///     Contains performer.
        /// </summary>
        private readonly ICharacter _performer;

        /// <summary>
        /// Resolves live world entities.
        /// </summary>
        private readonly IEntityService _entityService;

        /// <summary>
        ///     The chance of getting log from the tree.
        /// </summary>
        private readonly double _chance;

        /// <summary>
        ///     The fishing spot.
        /// </summary>
        private readonly EntityHandle<ICreature> _fishingSpotHandle;

        /// <summary>
        ///     The fishing animation id.
        /// </summary>
        private readonly int _animId;

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (!_entityService.TryResolve(_fishingSpotHandle, out var creature)
                || creature is not INpc fishingSpot)
            {
                Cancel();
                return;
            }

            // check if fishing spot has exhausted.
            if (!_performer.Viewport.VisibleCreatures.Contains(fishingSpot))
            {
                Cancel();
                return;
            }

            // check if fishing spot has moved.
            if (fishingSpot.Movement.Moved || fishingSpot.Movement.Moving)
            {
                Cancel();
                return;
            }

            var randomValue = RandomStatic.Generator.NextDouble();
            if (randomValue <= _chance)
            {
                // Invoke callback. If the callback returns true, that means the task is finished.
                if (_finishCallback(fishingSpot))
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
