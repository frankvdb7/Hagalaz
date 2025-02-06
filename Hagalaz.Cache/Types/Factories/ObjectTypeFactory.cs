namespace Hagalaz.Cache.Types.Factories
{
    public class ObjectTypeFactory : ITypeFactory<ObjectType>
    {
        /// <summary>
        /// Creates the type.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        public ObjectType CreateType(int typeId) => new(typeId);
    }
}
