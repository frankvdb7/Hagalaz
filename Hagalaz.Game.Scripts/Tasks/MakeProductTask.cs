using System;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Scripts.Tasks
{
    /// <summary>
    ///     A simple tickable task that will
    /// </summary>
    public class MakeProductTask : RsTickTask
    {
        /// <summary>
        ///     The perform call back.
        /// </summary>
        private readonly Func<bool> _makeCallBack;

        /// <summary>
        ///     Gets the make count.
        /// </summary>
        /// <value>
        ///     The make count.
        /// </value>
        private int MakeCount { get; set; }

        /// <summary>
        ///     Gets the total make count.
        /// </summary>
        /// <value>
        ///     The total make count.
        /// </value>
        private int TotalMakeCount { get; }

        /// <summary>
        ///     Gets the tick delay.
        /// </summary>
        /// <value>
        ///     The tick delay.
        /// </value>
        private int TickDelay { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MakeProductTask" /> class.
        /// </summary>
        /// <param name="totalMakeCount">The total make count.</param>
        /// <param name="tickDelay">The tick delay.</param>
        /// <param name="makeCallBack">The perform call back.</param>
        public MakeProductTask(int totalMakeCount, int tickDelay, Func<bool> makeCallBack)
        {
            _makeCallBack = makeCallBack;
            TotalMakeCount = totalMakeCount;
            TickDelay = tickDelay;

            TickActionMethod = PerformTickImpl;
        }

        /// <summary>
        ///     Performs the tick implementation.
        /// </summary>
        /// <returns></returns>
        protected virtual void PerformTickImpl()
        {
            if (MakeCount == TotalMakeCount)
            {
                Cancel();
                return;
            }

            if (TickCount % TickDelay == 0)
            {
                MakeCount++;
                if (_makeCallBack())
                {
                    Cancel();
                }
            }
        }
    }
}