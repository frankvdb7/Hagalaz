using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.HintIcon
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating a hint icon that targets an entity,
    /// allowing for entity-specific optional parameters like flash speed.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IHintIconBuilder"/>.
    /// It inherits from <see cref="IHintIconOptional"/>, providing access to common optional parameters as well.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHintIconEntityOptional : IHintIconOptional
    {
        /// <summary>
        /// Sets the flash speed for the hint icon when attached to an entity.
        /// </summary>
        /// <param name="flashSpeed">The speed at which the icon flashes.</param>
        /// <returns>The same builder instance to allow for further optional configuration.</returns>
        IHintIconEntityOptional WithFlashSpeed(int flashSpeed);
    }
}