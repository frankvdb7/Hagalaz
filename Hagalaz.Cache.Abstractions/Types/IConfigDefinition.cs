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
        /// Gets or sets a character value.
        /// </summary>
        char AChar6673 { get; set; }
    }
}
