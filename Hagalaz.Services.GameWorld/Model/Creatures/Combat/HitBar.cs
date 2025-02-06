using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Combat
{
    /// <summary>
    /// 
    /// </summary>
    public readonly struct HitBar : IHitBar
    {
        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public HitBarType Type { get; }

        /// <summary>
        /// Gets the delay.
        /// </summary>
        /// <value>
        /// The delay.
        /// </value>
        public int Delay { get; }

        /// <summary>
        /// The speed of updating changes (in 20ms cycles).
        /// </summary>
        /// <value>
        /// The speed.
        /// </value>
        public int Speed { get; }

        /// <summary>
        /// Gets the current life points.
        /// </summary>
        /// <value>
        /// The current life points.
        /// </value>
        public int CurrentLifePoints { get; }

        /// <summary>
        /// Gets the new life points.
        /// </summary>
        /// <value>
        /// The new life points.
        /// </value>
        public int NewLifePoints { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitBar" /> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="currentLifePoints">The current life points.</param>
        /// <param name="newLifePoints">The new life points.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="speed">The speed.</param>
        public HitBar(HitBarType type, int currentLifePoints, int newLifePoints, int delay, int speed)
        {
            Type = type;
            Delay = delay;
            Speed = speed;
            CurrentLifePoints = currentLifePoints;
            NewLifePoints = newLifePoints;
        }
    }
}