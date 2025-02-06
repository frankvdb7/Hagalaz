namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreatureAppearance
    {
        /// <summary>
        /// Contains boolean if creature is visible.
        /// </summary>
        bool Visible { get; set; }

        /// <summary>
        /// Contains size of creature.
        /// </summary>
        int Size { get; }
    }
}