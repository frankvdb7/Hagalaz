using System.Diagnostics.CodeAnalysis;

namespace Raido.Server.Extensions
{
    public static class RaidoCallerContextExtensions
    {
        public static bool TryGetItem<TValue>(this RaidoCallerContext context, object key, [NotNullWhen(true)] out TValue? value)
        {
            if (context.Items.TryGetValue(key, out var obj) && obj is TValue val)
            {
                value = val;
                return true;
            }

            value = default;
            return false;
        }
    }
}