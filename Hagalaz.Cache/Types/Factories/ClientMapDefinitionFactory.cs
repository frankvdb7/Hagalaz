using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Factories;

namespace Hagalaz.Cache.Types
{
    public class ClientMapDefinitionFactory : ITypeFactory<IClientMapDefinition>
    {
        public IClientMapDefinition CreateType(int id) => new ClientMapDefinition(id);
    }
}
