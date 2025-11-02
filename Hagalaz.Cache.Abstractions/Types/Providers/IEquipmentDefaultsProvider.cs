namespace Hagalaz.Cache.Abstractions.Types.Providers
{
    /// <summary>
    /// Defines the contract for a provider of equipment defaults.
    /// </summary>
    public interface IEquipmentDefaultsProvider
    {
        /// <summary>
        /// Gets the equipment defaults.
        /// </summary>
        /// <returns>The equipment defaults.</returns>
        IEquipmentDefaults Get();
    }
}
