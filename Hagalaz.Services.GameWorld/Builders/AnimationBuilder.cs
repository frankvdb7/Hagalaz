using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Builders
{
    /// <summary>
    ///
    /// </summary>
    public class AnimationBuilder : IAnimationBuilder, IAnimationId, IAnimationOptional, IAnimationBuild
    {
        private readonly IAnimationService _repository;
        private int _id;
        private int _delay;
        private int? _priority;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="repository"></param>
        public AnimationBuilder(IAnimationService repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IAnimationId Create() => new AnimationBuilder(_repository);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IAnimationOptional WithId(int id)
        {
            _id = id;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IAnimation Build()
        {
            var priority = _priority ?? _repository.FindAnimationById(_id).Priority;
            return new Animation(_id, _delay, priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        public IAnimationOptional WithDelay(int delay)
        {
            _delay = delay;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public IAnimationOptional WithPriority(int priority)
        {
            _priority = priority;
            return this;
        }
    }
}