using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.HintIcon
{
    /// <summary>
    /// Represents a step in the fluent builder pattern for creating a hint icon where common optional
    /// parameters like model ID and arrow ID can be specified.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IHintIconBuilder"/>.
    /// It also inherits from <see cref="IHintIconBuild"/>, allowing the build process to be finalized at this stage.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHintIconOptional : IHintIconBuild
    {
        /// <summary>
        /// Sets the model ID to be displayed within the hint icon.
        /// </summary>
        /// <param name="modelId">The unique identifier for the model.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHintIconOptional WithModelId(int modelId);

        /// <summary>
        /// Sets the ID of the arrow style to be used for the hint icon.
        /// </summary>
        /// <param name="arrowId">The unique identifier for the arrow style.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHintIconOptional WithArrowId(int arrowId);
    }
}