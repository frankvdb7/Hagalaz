using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;

namespace Hagalaz.Cache.Types.Factories
{
    /// <summary>
    /// A factory for creating <see cref="IConfigDefinition"/> objects.
    /// </summary>
    public class ConfigDefinitionFactory : ITypeFactory<IConfigDefinition>
    {
        /// <inheritdoc />
        public IConfigDefinition CreateType(int id)
        {
            return new ConfigDefinition(id);
        }
    }
}
