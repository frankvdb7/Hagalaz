namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Zamorak
{
    /// <summary>
    /// </summary>
    public class TstanonKarlak : BodyGuard
    {
        /// <summary>
        ///     Get's npcIDS which are suitable for this script.
        /// </summary>
        /// <returns>
        ///     System.Int32[][].
        /// </returns>
        public override int[] GetSuitableNpcs() => [6204];

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}