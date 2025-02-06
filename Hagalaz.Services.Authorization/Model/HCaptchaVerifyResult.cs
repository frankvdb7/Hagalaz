using System;
using System.Text.Json.Serialization;

namespace Hagalaz.Services.Authorization.Model
{
    public class HCaptchaVerifyResult
    {
        public bool Success { get; init;  }
        [JsonPropertyName("challenge_ts")]
        public DateTimeOffset Timestamp { get; init; }
        public string HostName { get; init; }
        public bool Credit { get; init; }
        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; init; }
    }
}