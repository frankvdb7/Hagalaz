using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Text.Json
{
    public interface IJsonDictionary
    {
        void SetValue<T>(string key, T value) where T : struct;
        void SetValue(string key, string value);
        void SetObject<T>(string key, T value) where T : class;
        void SetArray<T>(string key, T[] value);
        bool TryGetValue<T>(string key, out T value, T defaultValue = default) where T : struct;
        bool TryGetValue(string key, out string value, string defaultValue = "");
        bool TryGetObject<T>(string key, [NotNullWhen(true)] out T? value, T? defaultValue = default) where T : class;
        T GetValue<T>(string key, T defaultValue = default) where T : struct;
        string GetValue(string key, string defaultValue = "");
        [return: NotNullIfNotNull(nameof(defaultValue))]
        T? GetObject<T>(string key, T? defaultValue = default) where T : class;
        IEnumerable<T> GetArray<T>(string key, IEnumerable<T>? defaultValue = default);
        void FromJson([StringSyntax(StringSyntaxAttribute.Json)] string jsonData);
        string ToJson();
    }
}