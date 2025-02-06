namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGameObject : IEntity
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        int Id { get; }
        /// <summary>
        /// Contains boolean if this object is static.
        /// </summary>
        bool IsStatic { get; }
        /// <summary>
        /// Contains object shape type.
        /// </summary>
        ShapeType ShapeType { get; }
        /// <summary>
        /// Contains object rotation.
        /// </summary>
        int Rotation { get; }
        /// <summary>
        /// Gets the size x.
        /// </summary>
        /// <value>
        /// The size x.
        /// </value>
        int SizeX { get; }
        /// <summary>
        /// Gets the size y.
        /// </summary>
        /// <value>
        /// The size y.
        /// </value>
        int SizeY { get; }
        /// <summary>
        /// Contains boolean if this object is disabled.
        /// </summary>
        bool IsDisabled { get; }
        /// <summary>
        /// Gets the definition.
        /// </summary>
        /// <value>
        /// The definition.
        /// </value>
        IGameObjectDefinition Definition { get; }
        /// <summary>
        /// Gets the script.
        /// </summary>
        /// <value>
        /// The script.
        /// </value>
        IGameObjectScript Script { get; }
        /// <summary>
        /// Get's called when this object is disabled.
        /// </summary>
        void Enable();
        /// <summary>
        /// Get's called when this object is disabled.
        /// </summary>
        void Disable();
    }
}
