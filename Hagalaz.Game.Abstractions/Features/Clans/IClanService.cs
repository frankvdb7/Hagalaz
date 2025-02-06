namespace Hagalaz.Game.Abstractions.Features.Clans
{
    /// <summary>
    /// 
    /// </summary>
    public interface IClanService
    {
        /// <summary>
        /// Gets the clan by identifier.
        /// </summary>
        /// <param name="clanName">The name.</param>
        /// <returns></returns>
        IClan GetClanByName(string clanName);

        /// <summary>
        /// Puts the clan.
        /// </summary>
        /// <param name="clan">The clan.</param>
        void PutClan(IClan clan);

        /// <summary>
        /// Puts the clan settings.
        /// </summary>
        /// <param name="clan">The clan.</param>
        /// <param name="settings">The settings.</param>
        void PutClanSettings(IClan clan, IClanSettings settings);
    }
}