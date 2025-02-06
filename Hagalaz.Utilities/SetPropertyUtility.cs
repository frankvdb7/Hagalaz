namespace Hagalaz.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public static class SetPropertyUtility
    {
        /// <summary>
        /// Sets the property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentValue">The current value.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns>
        /// True if the new value has been set. False if the current value equals the new value.
        /// </returns>
        public static bool SetClass<T>(ref T? currentValue, T? newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }


        /// <summary>
        /// Sets the property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentValue">The current value.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns>
        /// True if the new value has been set. False if the current value equals the new value.
        /// </returns>
        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }
    }
}
