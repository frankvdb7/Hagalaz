using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Represents a configuration definition.
    /// </summary>
    public class ConfigDefinition : IConfigDefinition
    {
        /// <inheritdoc />
        public int Id { get; }

        /// <inheritdoc />
        public int DefaultValue { get; set; }

        /// <inheritdoc />
        public char ValueType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigDefinition"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public ConfigDefinition(int id)
        {
            Id = id;
        }
    }
}
