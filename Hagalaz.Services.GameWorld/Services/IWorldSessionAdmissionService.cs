using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Services.Model;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Services;

public interface IWorldSessionAdmissionService
{
    ValueTask<SignInResult> AdmitAsync(
        SignInRequest signInRequest,
        RaidoCallerContext context,
        uint masterId,
        AuthenticationProperties authenticationProperties,
        CancellationToken cancellationToken = default);
}
