using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Model.Combat
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

        public HitSplat() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HitSplat" /> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        [Obsolete("Use the IHitSplatBuilder instead")]
        public HitSplat(IRuneObject? sender) => Sender = sender;

        /// <summary>
        /// Set's first splat in this hitsplat.
        /// </summary>
        /// <param name="type">Type of the hitsplat.</param>
        /// <param name="damage">Damage of the hitsplat.</param>  
        /// <param name="golden">Wheter hit is golden.</param>
        /// <returns></returns>
        public HitSplat SetFirstSplat(HitSplatType type, int damage, bool golden)
        {
            FirstSplatType = type;
            FirstSplatDamage = damage;
            FirstSplatCritical = golden;
            return this;
        }

        /// <summary>
        /// Sets the first splat.
        /// If the damage is 90% of the max damage, the splat is golden.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="damage">The damage.</param>
        /// <param name="maxDamage">The maximum damage.</param>
        /// <returns></returns>
        public HitSplat SetFirstSplat(HitSplatType type, int damage, int maxDamage) => SetFirstSplat(type, damage, maxDamage <= damage * 0.9);

        /// <summary>
        /// Set's second splat in this hitsplat.
        /// </summary>
        /// <param name="type">Type of the hitsplat.</param>
        /// <param name="damage">Damage of the hitsplat.</param>
        /// <param name="golden">Wheter hit is golden.</param>
        /// <returns></returns>
        public HitSplat SetSecondSplat(HitSplatType type, int damage, bool golden)
        {
            SecondSplatType = type;
            SecondSplatDamage = damage;
            SecondSplatCritical = golden;
            return this;
        }

        /// <summary>
        /// Sets the second splat.
        /// If the damage is 90% of the max damage, the splat is golden.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="damage">The damage.</param>
        /// <param name="maxDamage">The maximum damage.</param>
        /// <returns></returns>
        public HitSplat SetSecondSplat(HitSplatType type, int damage, int maxDamage) => SetSecondSplat(type, damage, maxDamage <= damage * 0.9);
    }
}