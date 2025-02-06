using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Services.GameWorld.Features
{
    public class AuthenticationProperties
    {
        public const string AuthenticationPropertiesKey = "authentication_properties";
        public const string IdTokenKey = "id_token";
        public const string AccessTokenKey = "access_token";
        public const string RefreshTokenKey = "refresh_token";
        public const string ScopeKey = "scope";
        public const string ExpiresInKey = "expires_in";
        public const string TokenTypeKey = "token_type";
        public const string ClaimsKey = "claims";
        public const string ClientIdKey = "client_id";

        private readonly IDictionary<string, object?> _items;

        public AuthenticationProperties() : this(null) { }

        public AuthenticationProperties(IDictionary<string, object?>? items) => _items = items ?? new Dictionary<string, object?>(StringComparer.Ordinal);

        public string? IdToken
        {
            get => GetItem<string>(IdTokenKey);
            set => SetItem(IdTokenKey, value);
        }

        public string? AccessToken
        {
            get => GetItem<string>(AccessTokenKey);
            set => SetItem(AccessTokenKey, value);
        }

        public string? RefreshToken
        {
            get => GetItem<string>(RefreshTokenKey);
            set => SetItem(RefreshTokenKey, value);
        }

        public string? Scope
        {
            get => GetItem<string>(ScopeKey);
            set => SetItem(ScopeKey, value);
        }

        public DateTimeOffset? ExpireDate
        {
            get => GetItem<DateTimeOffset>(ExpiresInKey);
            set => SetItem(ExpiresInKey, value);
        }

        public string? TokenType
        {
            get => GetItem<string>(TokenTypeKey);
            set => SetItem(TokenTypeKey, value);
        }

        public string? ClientId
        {
            get => GetItem<string>(ClientIdKey);
            set => SetItem(ClientIdKey, value);
        }

        public IDictionary<string, object>? Claims
        {
            get => GetItem<IDictionary<string, object>>(ClaimsKey);
            set => SetItem(ClaimsKey, value);
        }

        public TValue? GetClaim<TValue>(string claim) => TryGetClaim<TValue>(claim, out var value) ? value : default;

        public bool TryGetClaim<TValue>(string claim, [NotNullWhen(true)] out TValue? value)
        {
            if (Claims == null)
            {
                value = default;
                return false;
            }

            if (Claims.TryGetValue(claim, out var obj) && obj is TValue val)
            {
                value = val;
                return true;
            }

            value = default;
            return false;
        }

        public List<TValue> GetClaimList<TValue>(string claim) => TryGetClaimList<TValue>(claim, out var value) ? value : [];

        public bool TryGetClaimList<TValue>(string claim, out List<TValue> value)
        {
            if (Claims == null)
            {
                value = [];
                return false;
            }

            if (Claims.TryGetValue(claim, out var obj))
            {
                // Check if the object is a List<object> and convert to List<T>
                if (obj is List<object> objectList)
                {
                    List<TValue> convertedList = [];
                    foreach (var item in objectList)
                    {
                        try
                        {
                            if (item is TValue convertedItem)
                            {
                                convertedList.Add(convertedItem);
                            }
                        }
                        catch
                        {
                            value = [];
                            return false;
                        }
                    }

                    value = convertedList;
                    return true;
                }
            }

            value = [];
            return false;
        }

        public TValue? GetItem<TValue>(string key) => TryGetItem<TValue>(key, out var value) ? value : default;

        public bool TryGetItem<TValue>(string key, [NotNullWhen(true)] out TValue? value)
        {
            if (_items.TryGetValue(key, out var obj) && obj is TValue val)
            {
                value = val;
                return true;
            }

            value = default;
            return false;
        }

        public void SetItem<TValue>(string key, TValue? value)
        {
            if (value != null)
            {
                _items[key] = value;
            }
            else
            {
                _items.Remove(key);
            }
        }
    }
}