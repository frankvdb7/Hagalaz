namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// 
    /// </summary>
    public interface IObjectType : IType
    {
        /// <summary>
        /// Contains the id of this object.
        /// </summary>
        /// <value>The id.</value>
        int Id { get; }
        /// <summary>
        /// Contains name of this object.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; set; }
        /// <summary>
        /// Contains object size X.
        /// </summary>
        /// <value>The size X.</value>
        int SizeX { get; }
        /// <summary>
        /// Contains object size Y.
        /// </summary>
        /// <value>The size Y.</value>
        int SizeY { get; }
        /// <summary>
        /// Contains flag if object is solid.
        /// </summary>
        /// <value><c>true</c> if solid; otherwise, <c>false</c>.</value>
        bool Solid { get; }
        /// <summary>
        /// Gets or sets the varbit file identifier.
        /// </summary>
        /// <value>
        /// The varbit file identifier.
        /// </value>
        int VarpBitFileId { get; }
        /// <summary>
        /// Contains some other clip bool.
        /// </summary>
        /// <value><c>true</c> if walkable; otherwise, <c>false</c>.</value>
        bool Gateway { get; }
        /// <summary>
        /// Contains clip type.
        /// </summary>
        /// <value>The type of the clip.</value>
        int ClipType { get; }
        /// <summary>
        /// Gets or sets the surroundings.
        /// </summary>
        /// <value>
        /// The surroundings.
        /// </value>
        byte Surroundings { get; }
        /// <summary>
        /// Gets the actions.
        /// </summary>
        /// <value>The actions.</value>
        public string?[] Actions { get; }
        /// <summary>
        /// Called after decode
        /// </summary>
        void AfterDecode();
    }
}
