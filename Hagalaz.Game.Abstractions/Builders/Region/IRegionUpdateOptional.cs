using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Region
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a region update where optional
    /// updates, such as adding a graphic, can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IRegionUpdateBuilder"/>.
    /// It also inherits from <see cref="IRegionUpdateBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRegionUpdateOptional : IRegionUpdateBuild
    {
        /// <summary>
        /// Adds a graphic effect to the region update.
        /// </summary>
        /// <param name="graphic">The <see cref="IGraphic"/> object to be added to the region update.</param>
        /// <returns>The final build step of the fluent builder chain.</returns>
        IRegionUpdateBuild WithGraphic(IGraphic graphic);
    }
}