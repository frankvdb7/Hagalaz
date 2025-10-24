using System.Collections.Generic;

namespace Hagalaz.Cache.Abstractions.Types
{
    public interface IClientMapDefinition : IType
    {
        string DefaultStringValue { get; set; }
        int DefaultIntValue { get; set; }
        char KeyType { get; set; }
        char ValType { get; set; }
        int Count { get; set; }
        Dictionary<int, object>? ValueMap { get; set; }
        object[]? Values { get; set; }

        int GetIntValue(int key);
        string GetStringValue(int key);
        object? GetValue(int key);
    }
}
