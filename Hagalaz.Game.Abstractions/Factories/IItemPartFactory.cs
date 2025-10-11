using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Defines a contract for a factory that creates instances of <see cref="IItemPart"/>.
    /// An item part is a component used in item combination or creation processes.
    /// </summary>
    public interface IItemPartFactory
    {
        /// <summary>
        /// Creates a new item part based on a given item ID.
        /// </summary>
        /// <param name="itemId">The unique identifier of the item to create a part from.</param>
        /// <returns>A new <see cref="IItemPart"/> instance.</returns>
        IItemPart Create(int itemId);
    }
}