using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Abstractions.Providers
{
    public interface ICs2IntDefinitionProvider
    {
        ICs2IntDefinition GetCs2IntDefinition(int id);
    }
}
