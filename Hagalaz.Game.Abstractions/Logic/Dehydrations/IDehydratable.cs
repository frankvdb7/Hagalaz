namespace Hagalaz.Game.Abstractions.Logic.Dehydrations
{
    /// <summary>
    /// Defines a contract for an object that can be "dehydrated" into a simpler, serializable representation.
    /// This is typically used to convert a live, in-memory game object into a Data Transfer Object (DTO) for storage or network transfer.
    /// </summary>
    /// <typeparam name="TDehydration">The type of the resulting dehydrated object (e.g., a DTO).</typeparam>
    public interface IDehydratable<out TDehydration>
    {
        /// <summary>
        /// Converts the current object into its dehydrated, serializable form.
        /// </summary>
        /// <returns>The dehydrated representation of the object.</returns>
        TDehydration Dehydrate();
    }
}
