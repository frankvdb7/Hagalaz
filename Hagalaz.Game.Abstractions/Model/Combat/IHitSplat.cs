namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHitSplat
    {
        /// <summary>
        /// Contains hit splat sender , 
        /// can be null.
        /// </summary>
        IRuneObject? Sender { get; }
        /// <summary>
        /// Contains first splat type.
        /// </summary>
        HitSplatType FirstSplatType { get; }
        /// <summary>
        /// Contains second splat type.
        /// </summary>
        HitSplatType SecondSplatType { get; }
        /// <summary>
        /// Contains hit delay.
        /// </summary>
        int Delay { get; }
        /// <summary>
        /// Contains boolean if first splat is golden.
        /// </summary>
        bool FirstSplatCritical { get; }
        /// <summary>
        /// Contains first splat damage.
        /// </summary>
        int FirstSplatDamage { get; }
        /// <summary>
        /// Contains boolean if second splat is golden.
        /// </summary>
        bool SecondSplatCritical { get; }
        /// <summary>
        /// Contains second splat damage.
        /// </summary>
        int SecondSplatDamage { get; }
    }
}
