using System.Runtime.InteropServices;

namespace Hagalaz.Extensions;

public static class DictionaryExtensions
{
    public static TValue? GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue? value)
        where TKey : notnull
    {
        ref var val = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out var exists);
        if (exists)
        {
            return val;
        }

        val = value;
        return value;
    }
}