using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Common.Tasks
{
    /// <summary>
    /// Contains creature reach task.
    /// This task performs movement to the target creature
    /// using the available world routefinder.
    /// The task ends when either:
    /// 1:Target character disappears or becomes not visible
    /// 2:Path to target creature can't be found
    /// 3:Target creature was sucessfully reached.
    /// 4:Movement is locked.
    /// 5:The Interrupt() is called (In this case, the callback (OnActionFinished()) is not called ) 
    /// </summary>
    public class CreatureReachTask : ReachTask
    {
        /// <summary>
        /// Contains creature which needs to reach target.
        /// </summary>
        private readonly ICreature _reacher;

        /// <summary>
        /// Contains target creature.
        /// </summary>
        private readonly ICreature _target;

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
        /// Constructs new creature reach task.
        /// </summary>
        /// <param name="reacher">The reacher.</param>
        /// <param name="target">The target.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="conditions">The conditions.</param>
        public CreatureReachTask(ICreature reacher, ICreature target, Action<bool> callback, params Type[] conditions)
            : base(conditions)
        {
            _reacher = reacher;
            _target = target;
            _finishCallback = callback;
            TickActionMethod = PerformTickImpl;
            _interruptEvent = _reacher.RegisterEventHandler<CreatureInterruptedEvent>(e =>
            {
                if (!CanInterrupt(e.Source))
                {
                    return false;
                }

                if (TickCount >= 1 && _reacher.FacedCreature == _target)
                {
                    _reacher.ResetFacing();
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
            if (_reacher.Movement.Locked || _target.IsDestroyed || !_reacher.Viewport.VisibleCreatures.Contains(_target))
            {
                if (TickCount >= 1 && _reacher.FacedCreature == _target)
                    _reacher.ResetFacing();
                _finishCallback.Invoke(false);
                Cancel();
                return;
            }

            if (TickCount == 1)
            {
                _reacher.FaceCreature(_target);
            }

            var path = _pathFinder.Find(_reacher, _target, true);
            if (!path.Successful && !path.MovedNear || path.MovedNearDestination)
            {
                _reacher.FaceLocation(_target.Location, _target.Size, _target.Size);
                _reacher.ResetFacing();
                _finishCallback(false);
                Cancel();
                return;
            }

            if (path.ReachedDestination)
            {
                _reacher.FaceLocation(_target.Location, _target.Size, _target.Size);
                _reacher.ResetFacing();
                _finishCallback(true);
                Cancel();
                return;
            }

            _reacher.Movement.AddToQueue(path);
        }

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