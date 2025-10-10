namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// Defines the contract for a game object type, which represents a static object in the game world,
    /// such as a tree, rock, or door. This interface provides all the static data for a specific kind of object.
    /// </summary>
    public interface IObjectType : IType
    {
        /// <summary>
        /// Gets the unique identifier for this object type.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets or sets the name of the object as it appears in-game.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the width of the object in game tiles (x-axis).
        /// </summary>
        int SizeX { get; }

        /// <summary>
        /// Gets the depth of the object in game tiles (y-axis).
        /// </summary>
        int SizeY { get; }

        /// <summary>
        /// Gets a value indicating whether this object is solid, meaning it cannot be walked through.
        /// </summary>
        bool Solid { get; }

        /// <summary>
        /// Gets the ID of the varbit that controls the state of this object.
        /// A varbit can change the object's appearance or behavior based on player progress or other conditions.
        /// </summary>
        int VarpBitFileId { get; }

        /// <summary>
        /// Gets a value indicating whether this object acts as a gateway, which typically means it is walkable
        /// but may have special clipping flags (e.g., a door or a curtain).
        /// </summary>
        bool Gateway { get; }

        /// <summary>
        /// Gets the clipping type of the object, which defines how it obstructs movement and line of sight.
        /// A value of 2 usually means solid, while other values can indicate different behaviors.
        /// </summary>
        int ClipType { get; }

        /// <summary>
        /// Gets a byte flag indicating the object's relationship with its surroundings,
        /// such as how it interacts with adjacent objects or terrain.
        /// </summary>
        byte Surroundings { get; }

        /// <summary>
        /// Gets the array of actions (e.g., "Chop down," "Open," "Mine") that can be performed on this object.
        /// </summary>
        public string?[] Actions { get; }

        /// <summary>
        /// A method that is called after the object data has been decoded from the cache.
        /// This can be used for post-processing, validation, or linking related data.
        /// </summary>
        void AfterDecode();
    }
}
