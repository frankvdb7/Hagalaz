using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Common.Tasks
{
    /// <summary>
    /// Contains ground item reach task.
    /// This task performs movement to the target ground item
    /// using the available world routefinder.
    /// The task ends when either:
    /// 1:Target ground item location becomes not visible in game map.
    /// 2:Path to target ground item can't be found
    /// 3:Target ground item was sucessfully reached.
    /// 4:Movement is locked.
    /// 5:The Interrupt() is called (In this case, the callback (OnActionFinished()) is not called ) 
    /// </summary>
    public class GroundItemReachTask : ReachTask
    {
        /// <summary>
        /// Contains creature which needs to reach target.
        /// </summary>
        private readonly ICreature _reacher;

        /// <summary>
        /// Contains target ground item handle.
        /// </summary>
        private readonly EntityHandle<IGroundItem> _targetHandle;
        private readonly IEntityService _entityService;

        /// <summary>
        /// Contains finish callback.
        /// </summary>
        private readonly Action<bool> _finishCallback;

        /// <summary>
        /// The path finder
        /// </summary>
        private readonly IPathFinder _pathFinder;

        /// <summary>
        /// 
        /// </summary>
        private readonly EventHappened _interruptEvent;

        /// <summary>
        /// Constructs new ground item reach task.
        /// </summary>
        /// <param name="reacher">The reacher.</param>
        /// <param name="target">The target handle.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="conditions">The conditions.</param>
        public GroundItemReachTask(ICreature reacher, EntityHandle<IGroundItem> target, Action<bool> callback, params Type[] conditions)
            : base(conditions)
        {
            _reacher = reacher;
            _targetHandle = target;
            _entityService = _reacher.ServiceProvider.GetRequiredService<IEntityService>();
            _finishCallback = callback;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = _reacher.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                if (!CanInterrupt(e.Source))
                {
                    return false;
                }

                Cancel();
                return false;
            });
            _pathFinder = reacher.ServiceProvider.GetRequiredService<IPathFinderProvider>().Smart;
        }

        /// <summary>
        /// Implementation of superclass method callback.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            var target = ResolveTarget();
            if (target is null || _reacher.Movement.Locked || target.Owner != null && target.Owner != _reacher || !_reacher.Viewport.InBounds(target.Location))
            {
                _finishCallback.Invoke(false);
                Cancel();
                return;
            }

            var path = _pathFinder.Find(_reacher, target, true);
            if (!path.Successful && !path.MovedNear || path.MovedNearDestination)
            {
                _reacher.FaceLocation(target.Location);
                _finishCallback(false);
                Cancel();
                return;
            }

            if (path.ReachedDestination)
            {
                _finishCallback(true);
                Cancel();
                return;
            }

            _reacher.Movement.AddToQueue(path);
        }

        private IGroundItem? ResolveTarget() =>
            _entityService.TryResolve<IGroundItem>(_targetHandle, out var target) ? target : null;

        /// <summary>
        /// 
        /// </summary>
        public override void Cancel()
        {
            base.Cancel();
            _reacher.UnregisterEventHandler<CreatureInterruptedEvent>(_interruptEvent);
        }
    }
}
