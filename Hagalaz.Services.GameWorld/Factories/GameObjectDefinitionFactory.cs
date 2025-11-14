using Hagalaz.Cache.Abstractions.Types.Factories;
using Hagalaz.Services.GameWorld.Data.Model;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class GameObjectDefinitionFactory : ITypeFactory<GameObjectDefinition>
    {
        public GameObjectDefinition CreateType(int typeId) => new(typeId);
    }
}
