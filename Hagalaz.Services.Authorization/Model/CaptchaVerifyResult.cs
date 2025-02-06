namespace Hagalaz.Services.Authorization.Model
{
    public class CaptchaVerifyResult
    {
        public static CaptchaVerifyResult Success { get; } = new()
        {
            Succeeded = true
        };
        public static CaptchaVerifyResult Fail { get; } = new()
        {
            Succeeded = false
        };
        
        public bool Succeeded { get; init; }
    }
}