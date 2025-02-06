using System;
using System.ComponentModel.DataAnnotations;

namespace Hagalaz.Services.Authorization.Extensions
{
    public static class StringExtensions
    {
        private static readonly Lazy<EmailAddressAttribute> CachedEmailAddressAttribute = new(() => new EmailAddressAttribute());
        
        public static bool IsEmail(this string val) => CachedEmailAddressAttribute.Value.IsValid(val);
        
    }
}