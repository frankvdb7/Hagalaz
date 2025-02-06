using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character.Packet
{
    /// <summary>
    /// Contains event for color selected.
    /// </summary>
    public class ColorSelectedEvent : CharacterEvent
    {
        /// <summary>
        /// Contains selected color id.
        /// </summary>
        public int SelectedColorID { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorSelectedEvent"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="colorID">The color Id.</param>
        public ColorSelectedEvent(ICharacter target, int colorID) : base(target) => SelectedColorID = colorID;
    }
}