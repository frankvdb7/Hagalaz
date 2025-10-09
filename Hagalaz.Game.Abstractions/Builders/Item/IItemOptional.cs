using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Item
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating an item where optional
    /// parameters like count and extra data can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IItemBuilder"/>.
    /// It also inherits from <see cref="IItemBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IItemOptional : IItemBuild
    {
        /// <summary>
        /// Sets the stack size or count for the item.
        /// </summary>
        /// <param name="count">The number of items in the stack.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IItemOptional WithCount(int count);

        /// <summary>
        /// Attaches extra, custom data to the item as a string.
        /// </summary>
        /// <param name="data">The string data to attach to the item.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IItemOptional WithExtraData(string data);
    }
}