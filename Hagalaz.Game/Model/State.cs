using System;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.DependencyInjection.Extensions;
using Hagalaz.Game.Abstractions.Features.States;

namespace Hagalaz.Game.Model
{
    /// <summary>
    /// State class.
    /// </summary>
    public class State : IState
    {
        /// <summary>
        /// Contains a callback for when the state is removed.
        /// This provides easier access to the OnStateRemoved method,
        /// because you do not need to implement a script for this state.
        /// </summary>
        private Action? _onRemovedCallback;

        /// <summary>
        /// Contains the delay in ticks at which the state will be removed.
        /// </summary>
        public int RemoveDelay { get; private set; }

        /// <summary>
        /// Contains type of the state.
        /// </summary>
        /// <value>The type of the state.</value>
        public StateType StateType { get; }

        /// <summary>
        /// Contains the the state script.
        /// </summary>
        public IStateScript Script { get; }

        /// <summary>
        /// 
        /// </summary>
        public bool Removed => RemoveDelay <= 0;

        /// <summary>
        /// Constructs new state instance.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="removeDelay">The expiry tick.</param>
        public State(StateType type, int removeDelay)
        {
            RemoveDelay = removeDelay;
            StateType = type;
            var provider = ServiceLocator.Current.GetInstance<IStateScriptProvider>();
            var scope = ServiceLocator.Current.CreateScope();
            Script = (IStateScript)scope.ServiceProvider.GetRequiredService(provider.FindByType(type));
        }

        /// <summary>
        /// Constructs new state instance.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="removeDelay">The expiry tick.</param>
        /// <param name="onRemoveCallback">The configuration remove callback.</param>
        public State(StateType type, int removeDelay, Action onRemoveCallback)
            : this(type, removeDelay) =>
            _onRemovedCallback = onRemoveCallback;

        /// <summary>
        /// 
        /// </summary>
        public void Tick()
        {
            if (Removed)
            {
                return;
            }

            if (--RemoveDelay <= 0)
            {
                Remove();
            }
        }

        private void Remove() => _onRemovedCallback?.Invoke();

        private void ReleaseUnmanagedResources() => _onRemovedCallback = null;

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        ~State() => ReleaseUnmanagedResources();
    }
}