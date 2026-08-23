using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Combat
{
    /// <summary>
    /// Represents single hit splat.
    /// </summary>
    public class HitSplat : IHitSplat
    {
        /// <summary>
        /// Contains hit splat sender , 
        /// can be null.
        /// </summary>
        public IRuneObject? Sender { get; init; }

        /// <summary>
        /// Contains first splat type.
        /// </summary>
        public HitSplatType FirstSplatType { get; set; } = HitSplatType.None;

        /// <summary>
        /// Contains second splat type.
        /// </summary>
        public HitSplatType SecondSplatType { get; set; } = HitSplatType.None;

        /// <summary>
        /// Contains boolean if first splat is golden.
        /// </summary>
        public bool FirstSplatCritical { get; set; }

        /// <summary>
        /// Contains boolean if second splat is golden.
        /// </summary>
        public bool SecondSplatCritical { get; set; }

        /// <summary>
        /// Contains first splat damage.
        /// </summary>
        public int FirstSplatDamage { get; set; }

        /// <summary>
        /// Contains second splat damage.
        /// </summary>
        public int SecondSplatDamage { get; set; }

        /// <summary>
        /// Contains hit delay.
        /// </summary>
        public int Delay { get; init; }

        internal HitSplat() { }
    }
}
