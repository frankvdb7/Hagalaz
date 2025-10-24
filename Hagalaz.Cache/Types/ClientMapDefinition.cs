using Hagalaz.Cache.Abstractions.Types;
using System.Collections.Generic;

namespace Hagalaz.Cache.Types
{
    public class ClientMapDefinition : IClientMapDefinition
    {
        public int Id { get; }
        public string DefaultStringValue { get; set; } = "null";
        public int DefaultIntValue { get; set; }
        public char KeyType { get; set; }
        public char ValType { get; set; }
        public int Count { get; set; }
        public Dictionary<int, object>? ValueMap { get; set; }
        public object[]? Values { get; set; }

        public ClientMapDefinition(int id)
        {
            Id = id;
        }

        public int GetIntValue(int key)
        {
            var value = GetValue(key);
            if (value is int intValue)
            {
                return intValue;
            }
            return DefaultIntValue;
        }

        public string GetStringValue(int key)
        {
            var value = GetValue(key);
            if (value is string stringValue)
            {
                return stringValue;
            }
            return DefaultStringValue;
        }

        public object? GetValue(int key)
        {
            if (Values == null)
            {
                if (ValueMap != null && ValueMap.TryGetValue(key, out var value))
                {
                    return value;
                }
                return null;
            }

            if (key < 0 || key >= Values.Length)
                return null;

            return Values[key];
        }
    }
}
