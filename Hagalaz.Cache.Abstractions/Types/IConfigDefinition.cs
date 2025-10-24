namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Represents a configuration definition.
    /// </summary>
    public interface IConfigDefinition : IType
    {
        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        int DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the character representing the type of the configuration value.
        /// </summary>
        char ValueType { get; set; }
    }
}
