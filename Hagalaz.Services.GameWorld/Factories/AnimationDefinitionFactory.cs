using Hagalaz.Cache.Types.Factories;
using Hagalaz.Services.GameWorld.Data.Model;

namespace Hagalaz.Services.GameWorld.Factories
{
    public class AnimationDefinitionFactory : ITypeFactory<AnimationDefinition>
    {
        public AnimationDefinition CreateType(int typeId) => new(typeId);
    }
}
