namespace Hagalaz.Utilities
{
    /// <summary>
    /// Provides utility methods for efficiently setting property values, often used in view models
    /// to avoid raising property changed events if the value has not actually changed.
    /// </summary>
    public static class SetPropertyUtility
    {
        /// <summary>
        /// Compares the current and new values of a reference type property and updates the current value only if it has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property, which must be a class.</typeparam>
        /// <param name="currentValue">A reference to the field storing the property's current value.</param>
        /// <param name="newValue">The new value for the property.</param>
        /// <returns>
        /// <c>true</c> if the <paramref name="currentValue"/> was updated to the <paramref name="newValue"/>;
        /// otherwise, <c>false</c> if the values were already equal.
        /// </returns>
        public static bool SetClass<T>(ref T? currentValue, T? newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }


        /// <summary>
        /// Compares the current and new values of a value type property and updates the current value only if it has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property, which must be a struct.</typeparam>
        /// <param name="currentValue">A reference to the field storing the property's current value.</param>
        /// <param name="newValue">The new value for the property.</param>
        /// <returns>
        /// <c>true</c> if the <paramref name="currentValue"/> was updated to the <paramref name="newValue"/>;
        /// otherwise, <c>false</c> if the values were already equal.
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
