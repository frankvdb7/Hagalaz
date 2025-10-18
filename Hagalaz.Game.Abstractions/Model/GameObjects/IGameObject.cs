namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// Defines the contract for an interactive object in the game world.
    /// </summary>
    public interface IGameObject : IEntity
    {
        /// <summary>
        /// Gets the unique ID of the game object.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets a value indicating whether this object is static (part of the map) or dynamic (spawned).
        /// </summary>
        bool IsStatic { get; }

        /// <summary>
        /// Gets the shape type of the object, which defines its basic form and interaction model.
        /// </summary>
        ShapeType ShapeType { get; }

        /// <summary>
        /// Gets the rotation of the object.
        /// </summary>
        int Rotation { get; }

        /// <summary>
        /// Gets the horizontal size of the object in tiles.
        /// </summary>
        int SizeX { get; }

        /// <summary>
        /// Gets the vertical size of the object in tiles.
        /// </summary>
        int SizeY { get; }

        /// <summary>
        /// Gets a value indicating whether this object is currently disabled and non-interactive.
        /// </summary>
        bool IsDisabled { get; }

        /// <summary>
        /// Gets the data definition for this game object.
        /// </summary>
        IGameObjectDefinition Definition { get; }

        /// <summary>
        /// Gets the script that controls the object's behavior and logic.
        /// </summary>
        IGameObjectScript Script { get; }

        /// <summary>
        /// Enables the game object, making it interactive.
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables the game object, making it non-interactive.
        /// </summary>
        void Disable();
    }
}