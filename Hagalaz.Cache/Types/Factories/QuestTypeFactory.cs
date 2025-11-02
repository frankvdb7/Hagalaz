using Hagalaz.Cache.Abstractions.Types.Factories;

namespace Hagalaz.Cache.Types.Factories
{
    /// <summary>
    /// 
    /// </summary>
    public class QuestTypeFactory : ITypeFactory<QuestType>
    {
        /// <summary>
        /// Creates the type.
        /// </summary>
        /// <param name="typeId">The type identifier.</param>
        /// <returns></returns>
        public QuestType CreateType(int typeId) => new(typeId);
    }
}
