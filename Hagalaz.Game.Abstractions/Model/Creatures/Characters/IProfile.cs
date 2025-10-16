using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a character's profile, which stores persistent key-value data.
    /// </summary>
    public interface IProfile
    {
        /// <summary>
        /// Sets a value type (struct) for a given key.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The unique key for the data.</param>
        /// <param name="value">The value to store.</param>
        public void SetValue<T>(string key, T value) where T : struct;
        /// <summary>
        /// Sets a string value for a given key.
        /// </summary>
        /// <param name="key">The unique key for the data.</param>
        /// <param name="value">The value to store.</param>
        public void SetValue(string key, string value);
        /// <summary>
        /// Sets a reference type (class) object for a given key.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="key">The unique key for the data.</param>
        /// <param name="object">The object to store.</param>
        public void SetObject<T>(string key, T @object) where T : class;
        /// <summary>
        /// Sets an array for a given key.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="key">The unique key for the data.</param>
        /// <param name="array">The array to store.</param>
        public void SetArray<T>(string key, T[] array);
        /// <summary>
        /// Tries to retrieve a value type (struct) for a given key.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the data to retrieve.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(string key, out T value, T defaultValue = default) where T : struct;
        /// <summary>
        /// Tries to retrieve a string value for a given key.
        /// </summary>
        /// <param name="key">The key of the data to retrieve.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(string key, out string value, string defaultValue = "");
        /// <summary>
        /// Tries to retrieve a reference type (class) object for a given key.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="key">The key of the data to retrieve.</param>
        /// <param name="value">When this method returns, contains the object associated with the specified key, if the key is found; otherwise, null.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetObject<T>(string key, [NotNullWhen(true)] out T? value, T? defaultValue = default) where T : class;
        /// <summary>
        /// Gets a value type (struct) for a given key, or a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the data to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The value associated with the key, or the default value.</returns>
        public T GetValue<T>(string key, T defaultValue = default) where T : struct;
        /// <summary>
        /// Gets a string value for a given key, or a default value if not found.
        /// </summary>
        /// <param name="key">The key of the data to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The value associated with the key, or the default value.</returns>
        public string GetValue(string key, string defaultValue = "");
        /// <summary>
        /// Gets a reference type (class) object for a given key, or a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="key">The key of the data to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The object associated with the key, or the default value.</returns>
        [return: NotNullIfNotNull(nameof(defaultValue))]
        public T? GetObject<T>(string key, T? defaultValue = default) where T : class;
        /// <summary>
        /// Gets an array for a given key, or a default value if not found.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="key">The key of the data to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The array associated with the key, or the default value.</returns>
        public IEnumerable<T> GetArray<T>(string key, IEnumerable<T>? defaultValue = default);
    }
}
