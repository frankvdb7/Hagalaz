namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICharacterScript : ICreatureScript
    {
        /// <summary>
        /// Contains character.
        /// </summary>
        ICharacter Character { get; }
        /// <summary>
        /// Gets called when character is interrupted.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="source">Object which performed the interruption,
        /// this parameter can be null , but it is not encouraged to do so.
        /// Best use would be to set the invoker class instance as source.</param>
        void OnInterrupt(object source);
        /// <summary>
        /// Happens when character enter's world.
        /// By default, this method does nothing.
        /// </summary>
        void OnRegistered();
        /// <summary>
        /// Get's called when character is destroyed permanently.
        /// By default, this method does nothing.
        /// </summary>
        void OnDestroy();
        /// <summary>
        /// Determines whether this instance [can render skull].
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <returns></returns>
        bool CanRenderSkull(SkullIcon icon);
        /// <summary>
        /// Get's if character is busy.
        /// By default this method returns false.
        /// </summary>
        /// <returns></returns>
        bool IsBusy();
        /// <summary>
        /// Called when this script is removed from the character.
        /// By default this method does nothing.
        /// </summary>
        void OnRemove();
    }
}
