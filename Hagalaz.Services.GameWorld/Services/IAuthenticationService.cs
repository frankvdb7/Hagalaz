using System.Threading.Tasks;
using Hagalaz.Services.GameWorld.Services.Model;

namespace Hagalaz.Services.GameWorld.Services
{
    public interface IAuthenticationService
    {
        ValueTask<SignInResult> SignInLobbyAsync(SignInRequest request);
        ValueTask<SignInResult> SignInWorldAsync(SignInRequest request);
        Task SignOutAsync();
    }
}
