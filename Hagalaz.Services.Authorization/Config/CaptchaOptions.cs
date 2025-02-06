namespace Hagalaz.Services.Authorization.Config
{
    public class CaptchaOptions
    {
        public const string Captcha = "Captcha";
        
        public string ApiBaseUrl { get; init; }
        public string SiteKey { get; init; }
        public string Secret { get; init;  }
    }
}