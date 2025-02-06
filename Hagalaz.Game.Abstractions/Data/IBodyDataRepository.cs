using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Data
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBodyDataRepository
    {
        /// <summary>
        /// Determines whether [is disabled slot] [the specified part].
        /// </summary>
        /// <param name="part">The part.</param>
        /// <returns>
        ///   <c>true</c> if [is disabled slot] [the specified part]; otherwise, <c>false</c>.
        /// </returns>
        bool IsDisabledSlot(BodyPart part);

        /// <summary>
        /// Gets the body slot count.
        /// </summary>
        /// <value>
        /// The body slot count.
        /// </value>
        int BodySlotCount { get; }
    }
}