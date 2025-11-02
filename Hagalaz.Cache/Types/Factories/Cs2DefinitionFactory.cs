using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;

namespace Hagalaz.Cache.Types.Factories
{
    public class Cs2DefinitionFactory : ITypeFactory<ICs2Definition>
    {
        public ICs2Definition CreateType(int id) => new Cs2Definition(id);
    }
}
