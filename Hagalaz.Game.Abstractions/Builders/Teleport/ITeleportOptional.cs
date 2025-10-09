using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Teleport
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a teleport effect where optional
    /// parameters like the Z-coordinate and dimension can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="ITeleportBuilder"/>.
    /// It also inherits from <see cref="ITeleportBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITeleportOptional : ITeleportBuild
    {
        /// <summary>
        /// Sets the Z-coordinate (or plane) for the teleport destination.
        /// </summary>
        /// <param name="z">The Z-coordinate, representing the height level.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        ITeleportOptional WithZ(int z);

        /// <summary>
        /// Sets the dimension for the teleport destination, allowing for teleports to instanced or alternate versions of the game world.
        /// </summary>
        /// <param name="dimension">The dimension identifier.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        ITeleportOptional WithDimension(int dimension);
    }
}