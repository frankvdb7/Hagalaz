namespace Hagalaz.Game.Scripts.Minigames.DuelArena
{
    /// <summary>
    ///     The single terminal result of one duel, shared by both participants' <see cref="DuelArenaCombatScript" /> instances.
    /// </summary>
    /// <remarks>
    ///     A terminal entry point claims the result before any side effect runs, so re-entrant or repeated callbacks from either
    ///     participant are rejected. A claim whose payout is rejected is released and may be retried; once completed the result
    ///     never changes again.
    /// </remarks>
    public sealed class DuelResult
    {
        private bool _claimed;

        /// <summary>
        ///     Gets a value indicating whether the duel result was successfully completed.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        ///     Claims the result for one terminal transition.
        /// </summary>
        /// <returns>True when the caller owns the result; false when it is already claimed or completed.</returns>
        public bool TryClaim()
        {
            if (_claimed)
            {
                return false;
            }

            _claimed = true;
            return true;
        }

        /// <summary>
        ///     Marks the claimed result as successfully completed. It can no longer be released or claimed again.
        /// </summary>
        public void Complete() => IsCompleted = true;

        /// <summary>
        ///     Releases an uncompleted claim so a later terminal entry can retry; does nothing once completed.
        /// </summary>
        public void Release()
        {
            if (!IsCompleted)
            {
                _claimed = false;
            }
        }
    }
}
