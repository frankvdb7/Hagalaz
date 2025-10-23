using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types.Factories
{
    public class GraphicTypeFactory : ITypeFactory<IGraphicType>
    {
        public IGraphicType CreateType(int id)
        {
            return new GraphicType(id);
        }
    }
}