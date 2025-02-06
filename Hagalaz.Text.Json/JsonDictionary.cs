using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hagalaz.Text.Json;

public class JsonDictionary : IJsonDictionary
{
    private readonly JsonSerializerOptions _serializerOptions;
    private Dictionary<string, JsonNode> _data = new(StringComparer.OrdinalIgnoreCase);

    public JsonDictionary() : this(new(JsonSerializerOptions.Default)) { }

    public JsonDictionary(JsonSerializerOptions serializerOptions) => _serializerOptions = serializerOptions;

    public void SetValue<T>(string key, T value) where T : struct => _data[key] = JsonValue.Create(value)!;

    public void SetValue(string key, string value) => _data[key] = JsonValue.Create(value)!;

    public void SetObject<T>(string key, T value) where T : class
    {
        var jsonNode = JsonNode.Parse(JsonSerializer.Serialize(value, _serializerOptions))!;
        JsonFlattener.FlattenNode(jsonNode, key, _data);
    }

    public void SetArray<T>(string key, T[] value) => _data[key] = new JsonArray(value.Select(v => JsonValue.Create(v)).ToArray());

    public bool TryGetValue<T>(string key, out T value, T defaultValue = default) where T : struct
    {
        if (!_data.TryGetValue(key, out var node) || node is not JsonValue jsonValue)
        {
            value = defaultValue;
            return false;
        }

        var valueType = typeof(T);
        if (!valueType.IsEnum)
        {
            if (jsonValue.TryGetValue<T>(out var val))
            {
                value = val;
                return true;
            }

            value = defaultValue;
            return false;
        }

        // Handle Enums
        if (Enum.TryParse(valueType, jsonValue.ToString(), true, out var enumVal) && Enum.IsDefined(valueType, enumVal))
        {
            value = (T)enumVal;
            return true;
        }

        value = GetSafeEnumDefault(valueType, defaultValue);
        return false;
    }

    public bool TryGetValue(string key, out string value, string defaultValue = "")
    {
        if (_data.TryGetValue(key, out var node) && node is JsonValue val && val.TryGetValue(out value!))
        {
            return true;
        }

        value = defaultValue;
        return false;
    }

    public bool TryGetObject<T>(string key, [NotNullWhen(true)] out T? value, T? defaultValue = default) where T : class
    {
        if (_data.TryGetValue(key, out var node) && node is JsonObject @object)
        {
            value = @object.Deserialize<T>(_serializerOptions);
            if (value != null)
            {
                return true;
            }
        }

        if (defaultValue != null)
        {
            value = defaultValue;
            return true;
        }

        value = defaultValue;
        return false;
    }

    public T GetValue<T>(string key, T defaultValue = default) where T : struct => TryGetValue<T>(key, out var value) ? value : defaultValue;

    public string GetValue(string key, string defaultValue = "")
    {
        if (_data.TryGetValue(key, out var node) && node is JsonValue value && value.TryGetValue<string>(out var val))
        {
            return val;
        }

        return defaultValue;
    }

    [return: NotNullIfNotNull(nameof(defaultValue))]
    public T? GetObject<T>(string key, T? defaultValue = default) where T : class
    {
        if (_data.TryGetValue(key, out var node) && node is JsonObject @object)
        {
            return @object.Deserialize<T>(_serializerOptions);
        }

        return defaultValue;
    }

    public IEnumerable<T> GetArray<T>(string key, IEnumerable<T>? defaultValue = default)
    {
        if (_data.TryGetValue(key, out var node) && node is JsonArray array)
        {
            foreach (var value in array)
            {
                yield return value!.GetValue<T>();
            }
        }
        else if (defaultValue != null)
        {
            foreach (var item in defaultValue)
            {
                yield return item;
            }
        }
    }

    public void FromJson([StringSyntax(StringSyntaxAttribute.Json)] string jsonData) =>
        _data = JsonFlattener.FlattenJson(jsonData,
            new JsonNodeOptions
            {
                PropertyNameCaseInsensitive = _serializerOptions.PropertyNameCaseInsensitive
            });

    public string ToJson() =>
        JsonFlattener.UnflattenJson(_data,
            new JsonNodeOptions
            {
                PropertyNameCaseInsensitive = _serializerOptions.PropertyNameCaseInsensitive,
            });

    private static T GetSafeEnumDefault<T>(Type type, T defaultValue) where T : struct
    {
        if (!type.IsEnum || !EqualityComparer<T>.Default.Equals(defaultValue, default))
        {
            return defaultValue;
        }

        var values = Enum.GetValues(type);
        return (T)values.GetValue(0)!; // Ensure a valid enum value
    }
}