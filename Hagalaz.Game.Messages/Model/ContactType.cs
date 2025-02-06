namespace Hagalaz.Game.Messages.Model
{
    /// <summary>
    /// Type of contact (either friend or ignore).
    /// </summary>
    public enum ContactType : byte
    {
        /// <summary>
        /// A contact that is considered a friend to the contact holder.
        /// </summary>
        Friend = 0,
        /// <summary>
        /// A contact that is considered ignored to the contact holder.
        /// </summary>
        Ignore = 1
    }
}
