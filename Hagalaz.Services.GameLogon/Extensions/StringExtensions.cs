using System;
using System.ComponentModel.DataAnnotations;

namespace Hagalaz.Services.GameLogon.Extensions
{
    public static class StringExtensions
    {
        private static readonly Lazy<EmailAddressAttribute> CachedEmailAddressAttribute = new(() => new EmailAddressAttribute());
        
        public static bool IsEmail(this string val) => CachedEmailAddressAttribute.Value.IsValid(val);

        public static string ToJsonString(this object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return System.Text.Json.JsonSerializer.Serialize<object>(obj);
        }
    }
}