using System;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Common.Tasks
{
    /// <summary>
    /// 
    /// </summary>
    public class MakeTask : RsTickTask
    {
        /// <summary>
        /// The times performed.
        /// </summary>
        public int CurrentMakeCount { get; set; }

        /// <summary>
        /// The times to perform.
        /// </summary>
        public int TotalMakeCount { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MakeTask" /> class.
        /// </summary>
        /// <param name="totalMakeCount">The total make count.</param>
        /// <param name="tickActionImpl">The perform tick implementation.</param>
        public MakeTask(int totalMakeCount, Action tickActionImpl)
            : base(tickActionImpl) =>
            TotalMakeCount = totalMakeCount;
    }
}