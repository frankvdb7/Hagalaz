using Hagalaz.Cache.Types;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Data.Model
{
    public class AnimationDefinition : AnimationType, IAnimationDefinition
    {
        public AnimationDefinition(int id)
            : base(id)
        {
        }
    }
}