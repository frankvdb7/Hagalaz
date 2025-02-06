using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="IObjectType" />
    public interface IGameObjectDefinition : IObjectType
    {
        /// <summary>
        /// Contains examine of this object.
        /// </summary>
        /// <value>The examine.</value>
        string Examine { get; set; }
        /// <summary>
        /// Contains the game object loot Id.
        /// </summary>
        /// <value>The game object loot Id.</value>
        int LootTableId { get; set; }
    }
}
