namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISkills
    {
        /// <summary>
        /// Gets the magic.
        /// </summary>
        /// <value>
        /// The magic.
        /// </value>
        IMagic Magic { get; }
        /// <summary>
        /// Contains the farming skill.
        /// </summary>
        IFarming Farming { get; }
        /// <summary>
        /// Contains the slayer skill.
        /// </summary>
        ISlayer Slayer { get; }
    }
}
