using System;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// 
    /// </summary>
    public interface IState : ITickItem, IDisposable
    {
        /// <summary>
        /// Contains the delay in ticks at which the state will be removed.
        /// </summary>
        int RemoveDelay { get; }
        /// <summary>
        /// Contains type of the state.
        /// </summary>
        /// <value>The type of the state.</value>
        StateType StateType { get; }
        /// <summary>
        /// Contains the the state script.
        /// </summary>
        IStateScript Script { get; }
        /// <summary>
        /// 
        /// </summary>
        bool Removed { get; }
    }
}
