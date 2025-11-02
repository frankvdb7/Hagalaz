namespace Hagalaz.Cache.Abstractions.Model
{
    public interface IReferenceTableChildEntry
    {
        /// <summary>
        /// Contains the identifier.
        /// </summary>
        int Id { get; set; }

        /// <summary>
        /// Contains the index.
        /// </summary>
        int Index { get; }
    }
}