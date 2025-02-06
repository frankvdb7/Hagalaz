namespace Hagalaz.Services.GameWorld.Model.Creatures.Combat.Experimental.Combat
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICombatRotation
    {
        /// <summary>
        /// Gets the probability.
        /// </summary>
        /// <value>
        /// The probability.
        /// </value>
        double Probability { get; }

        /// <summary>
        /// Activates this instance.
        /// </summary>
        void Activate();

        /// <summary>
        /// Deactivates this instance.
        /// </summary>
        void Deactivate();
    }
}