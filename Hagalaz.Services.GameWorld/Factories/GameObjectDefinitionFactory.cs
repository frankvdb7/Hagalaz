using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Diagnostics;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class GameObjectDefinitionFactory : ITypeFactory<GameObjectDefinition>
    {
        public GameObjectDefinition CreateType(int typeId)
        {
            GameObjectDefinitionResolutionDiagnostics.RecordMissingArchiveDefinition();
            return new(typeId);
        }
    }
}
