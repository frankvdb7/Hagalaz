using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Common.Tasks
{
    /// <summary>
    /// Contains abstract class for generic methods for reach task.
    /// </summary>
    public abstract class ReachTask : RsTickTask
    {
        /// <summary>
        /// Contains conditions on which this task
        /// can't be interrupted.
        /// </summary>
        /// <value>The interrupt conditions.</value>
        private readonly Type[] _interruptConditions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReachTask" /> class.
        /// </summary>
        /// <param name="conditions">The conditions.</param>
        protected ReachTask(Type[] conditions) => _interruptConditions = conditions;

        /// <summary>
        /// Gets if this task can be interrupted by specific source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns><c>true</c> if this instance can interrupt the specified source; otherwise, <c>false</c>.</returns>
        protected bool CanInterrupt(object source)
        {
            if (source == null)
                return true;
            var type = source.GetType();
            return _interruptConditions.All(t => !t.IsAssignableFrom(type));
        }
    }
}