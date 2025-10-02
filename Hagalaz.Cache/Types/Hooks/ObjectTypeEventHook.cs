using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types.Hooks
{
    public class ObjectTypeEventHook : ITypeEventHook<IObjectType>
    {
        public void AfterDecode(ITypeProvider<IObjectType> provider, IObjectType[] types)
        {
            foreach (var type in types)
            {
                type.AfterDecode();
            }
        }
    }
}