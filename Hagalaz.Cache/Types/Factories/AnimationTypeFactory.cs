using Hagalaz.Cache.Abstractions.Types.Factories;

namespace Hagalaz.Cache.Types.Factories
{
    public class AnimationTypeFactory : ITypeFactory<AnimationType>
    {
        public AnimationType CreateType(int typeId) => new(typeId);
    }
}
