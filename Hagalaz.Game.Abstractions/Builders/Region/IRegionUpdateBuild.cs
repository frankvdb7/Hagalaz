using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Game.Abstractions.Builders.Region
{
    /// <summary>
    /// Represents the final step in the fluent builder pattern for creating a region update.
    /// This interface provides methods to either construct the <see cref="IRegionPartUpdate"/> object
    /// or to construct and immediately queue it for processing.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IRegionUpdateBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRegionUpdateBuild
    {
        /// <summary>
        /// Builds the configured <see cref="IRegionPartUpdate"/> instance without queueing it for processing.
        /// This is useful if the update object needs to be manipulated further before being sent.
        /// </summary>
        /// <returns>A new <see cref="IRegionPartUpdate"/> object configured with the specified properties.</returns>
        IRegionPartUpdate Build();

        /// <summary>
        /// Builds the configured <see cref="IRegionPartUpdate"/> instance and immediately queues it to be sent to the relevant clients.
        /// </summary>
        /// <returns>The <see cref="IRegionPartUpdate"/> object that was queued for processing.</returns>
        IRegionPartUpdate Queue();
    }
}