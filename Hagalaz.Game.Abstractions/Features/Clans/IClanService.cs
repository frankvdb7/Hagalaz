namespace Hagalaz.Game.Abstractions.Features.Clans
{
    /// <summary>
    /// Defines a contract for a service that manages clan data and operations.
    /// </summary>
    public interface IClanService
    {
        /// <summary>
        /// Retrieves a clan by its unique name.
        /// </summary>
        /// <param name="clanName">The name of the clan to retrieve.</param>
        /// <returns>The <see cref="IClan"/> object if found; otherwise, <c>null</c>.</returns>
        IClan GetClanByName(string clanName);

        /// <summary>
        /// Creates or updates a clan in the data store.
        /// </summary>
        /// <param name="clan">The clan object to be saved.</param>
        void PutClan(IClan clan);

        /// <summary>
        /// Updates the settings for a specific clan.
        /// </summary>
        /// <param name="clan">The clan whose settings are to be updated.</param>
        /// <param name="settings">The new settings for the clan.</param>
        void PutClanSettings(IClan clan, IClanSettings settings);
    }
}