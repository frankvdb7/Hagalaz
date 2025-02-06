namespace Hagalaz.Game.Abstractions.Logic.Random
{
    /// <summary>
    /// This interface contains the properties an object must have to be a valid result object.
    /// </summary>
    public interface IRandomObject
    {
        /// <summary>
        /// Gets or sets whether this object will always be part of the result set
        /// (Probability is ignored when this flag is set to true)
        /// </summary>
        bool Always { get; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IRandomObject"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        bool Enabled { get; }
        /// <summary>
        /// Contains the probability.
        /// </summary>
        double Probability { get; }
    }
}
