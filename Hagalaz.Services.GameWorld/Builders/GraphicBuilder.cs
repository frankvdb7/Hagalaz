using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Builders
{
    /// <summary>
    /// 
    /// </summary>
    public class GraphicBuilder : IGraphicBuilder, IGraphicId, IGraphicOptional, IGraphicBuild
    {
        private int _id;
        private int _delay;
        private int _height;
        private int _rotation;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IGraphicId Create() => new GraphicBuilder();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IGraphicOptional WithId(int id)
        {
            _id = id;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IGraphic Build() => new Graphic(_id, _delay, _height, _rotation);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        public IGraphicOptional WithDelay(int delay)
        {
            _delay = delay;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        public IGraphicOptional WithHeight(int height)
        {
            _height = height;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public IGraphicOptional WithRotation(int rotation)
        {
            _rotation = rotation;
            return this;
        }
    }
}