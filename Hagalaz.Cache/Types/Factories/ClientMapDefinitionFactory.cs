using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;

namespace Hagalaz.Cache.Types.Factories
{
    public class ClientMapDefinitionFactory : ITypeFactory<IClientMapDefinition>
    {
        public IClientMapDefinition CreateType(int id) => new ClientMapDefinition(id);
    }
}
