namespace Hagalaz.Game.Abstractions.Logic.Hydrations
{
    /// <summary>
    /// Defines a contract for an object that can be "hydrated" from a simpler, serializable representation.
    /// This is typically used to restore a live, in-memory game object from a Data Transfer Object (DTO) that was retrieved from storage or a network transfer.
    /// </summary>
    /// <typeparam name="THydration">The type of the source object used for hydration (e.g., a DTO).</typeparam>
    public interface IHydratable<in THydration>
    {
        /// <summary>
        /// Populates the current object with data from its hydrated form.
        /// </summary>
        /// <param name="hydration">The source object containing the data to apply.</param>
        void Hydrate(THydration hydration);
    }
}
