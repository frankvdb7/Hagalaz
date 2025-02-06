namespace Hagalaz.Cache.Types.Factories
{
    /// <summary>
    /// 
    /// </summary>
    public class ItemTypeFactory : ITypeFactory<ItemType>
    {
        /// <summary>
        /// Creates the type.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        public ItemType CreateType(int typeId) => new(typeId);
    }
}
