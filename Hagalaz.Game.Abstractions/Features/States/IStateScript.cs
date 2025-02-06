using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Features.States
{
    /// <summary>
    /// 
    /// </summary>
    public interface IStateScript
    {
        /// <summary>
        /// Determines whether this instance is serializable.
        /// By default, the state is not serializable.
        /// </summary>
        /// <returns></returns>
        bool IsSerializable();
        /// <summary>
        /// Gets called when the state is removed.
        /// By default, this method invokes the OnRemoveCallback of the state, if any.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        void OnStateRemoved(IState state, ICreature creature);
        /// <summary>
        /// Gets called when the state is added.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="creature">The creature.</param>
        void OnStateAdded(IState state, ICreature creature);
    }
}
