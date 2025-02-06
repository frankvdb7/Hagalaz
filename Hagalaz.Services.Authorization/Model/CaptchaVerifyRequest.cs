namespace Hagalaz.Services.Authorization.Model
{
    public class CaptchaVerifyRequest
    {
        public string Token { get; init; } = null!;
    }
}