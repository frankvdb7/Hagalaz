using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// Defines the contract for a game object's data definition, containing its base properties.
    /// </summary>
    /// <seealso cref="IObjectType" />
    public interface IGameObjectDefinition : IObjectType
    {
        /// <summary>
        /// Gets or sets the "Examine" text for this object.
        /// </summary>
        string Examine { get; set; }

        /// <summary>
        /// Gets or sets the ID of the loot table associated with this object.
        /// </summary>
        int LootTableId { get; set; }
    }
}