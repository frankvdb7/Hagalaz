using Raido.Common.Protocol;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Defines the contract for a hint icon, which is a pointer that directs a player's attention to a specific entity or location on the screen.
    /// </summary>
    public interface IHintIcon
    {
        /// <summary>
        /// Gets or sets the slot index for this hint icon. A character can have multiple hint icons active at once, each in a different slot.
        /// </summary>
        int Index { get; set; }

        /// <summary>
        /// Converts the hint icon's data into a network message that can be sent to the client.
        /// </summary>
        /// <returns>A <see cref="RaidoMessage"/> representing the hint icon.</returns>
        RaidoMessage ToMessage();
    }
}
