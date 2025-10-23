using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types.Factories
{
    public class Cs2IntDefinitionFactory : ITypeFactory<ICs2IntDefinition>
    {
        public ICs2IntDefinition CreateType(int id) => new Cs2IntDefinition(id);
    }
}
