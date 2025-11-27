using System;
using System.Collections.Concurrent;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Data.Model;

namespace Hagalaz.Services.GameWorld.Services
{
    public class AnimationService : IAnimationService
    {
        private readonly ConcurrentDictionary<int, IAnimationDefinition> _animations = new();
        private readonly IAnimationDefinition _resetAnimation = new AnimationDefinition(-1);
        private readonly ITypeProvider<IAnimationDefinition> _typeProvider;
        private int _cachedTotalAnimations = -1;

        public AnimationService(ITypeProvider<IAnimationDefinition> typeProvider) => _typeProvider = typeProvider;

        public IAnimationDefinition FindAnimationById(int animationId)
        {
            if (animationId == -1)
            {
                return _resetAnimation;
            }
            if (_cachedTotalAnimations == -1)
            {
                _cachedTotalAnimations = _typeProvider.ArchiveSize;
            }
            if (animationId < 0 || animationId > _cachedTotalAnimations)
            {
                throw new ArgumentOutOfRangeException(nameof(animationId));
            }
            if (_animations.TryGetValue(animationId, out var value))
            {
                return value;
            }
            return _animations.GetOrAdd(animationId, _typeProvider.Get);
        }
    }
}