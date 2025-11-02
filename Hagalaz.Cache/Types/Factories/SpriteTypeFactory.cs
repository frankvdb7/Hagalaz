using Hagalaz.Cache.Abstractions.Types.Factories;

namespace Hagalaz.Cache.Types.Factories
{
    /// <summary>
    /// 
    /// </summary>
    public class SpriteTypeFactory : ITypeFactory<SpriteType>
    {
        /// <summary>Creates the type.</summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        public SpriteType CreateType(int typeId) => new(typeId);
    }
}
