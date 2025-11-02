using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Factories;

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