using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    public interface IProfile
    {
        public void SetValue<T>(string key, T value) where T : struct;
        public void SetValue(string key, string value);
        public void SetObject<T>(string key, T @object) where T : class;
        public void SetArray<T>(string key, T[] array);
        public bool TryGetValue<T>(string key, out T value, T defaultValue = default) where T : struct;
        public bool TryGetValue(string key, out string value, string defaultValue = "");
        public bool TryGetObject<T>(string key, [NotNullWhen(true)] out T? value, T? defaultValue = default) where T : class;
        public T GetValue<T>(string key, T defaultValue = default) where T : struct;
        public string GetValue(string key, string defaultValue = "");
        [return: NotNullIfNotNull(nameof(defaultValue))]
        public T? GetObject<T>(string key, T? defaultValue = default) where T : class;
        public IEnumerable<T> GetArray<T>(string key, IEnumerable<T>? defaultValue = default);
    }
}
