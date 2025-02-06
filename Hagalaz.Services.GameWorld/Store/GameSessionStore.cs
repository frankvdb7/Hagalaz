using Hagalaz.Collections;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Store
{
    public class GameSessionStore : ConcurrentStore<string, IGameSession>
    {
    }
}
