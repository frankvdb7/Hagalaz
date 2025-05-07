namespace Hagalaz.Services.Authorization.Config
{
    public class CaptchaOptions
    {
        public const string Captcha = "Captcha";
        
        public required string ApiBaseUrl { get; init; }
        public required string SiteKey { get; init; }
        public required string Secret { get; init;  }
    }
}